/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using log4net;
using OpenMetaverse;
using OpenSim.Framework.Monitoring;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Timer = OpenSim.Region.ScriptEngine.Shared.Api.Plugins.Timer;

namespace OpenSim.Region.ScriptEngine.Shared.Api
{
    /// <summary>
    /// Handles LSL commands that takes long time and returns an event, for example timers, HTTP requests, etc.
    /// </summary>
    public class AsyncCommandManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Thread cmdHandlerThread;
        private static int cmdHandlerThreadCycleSleepms;

        /// <summary>
        /// Lock for reading/writing static components of AsyncCommandManager.
        /// </summary>
        /// <remarks>
        /// This lock exists so that multiple threads from different engines and/or different copies of the same engine
        /// are prevented from running non-thread safe code (e.g. read/write of lists) concurrently.
        /// </remarks>
        private static ReaderWriterLock staticLock = new ReaderWriterLock();

        private static List<IScriptEngine> m_ScriptEngines =
                new List<IScriptEngine>();

        public IScriptEngine m_ScriptEngine;

        private static Dictionary<IScriptEngine, Dataserver> m_Dataserver =
                new Dictionary<IScriptEngine, Dataserver>();
        private static Dictionary<IScriptEngine, Timer> m_Timer =
                new Dictionary<IScriptEngine, Timer>();
        private static Dictionary<IScriptEngine, Listener> m_Listener =
                new Dictionary<IScriptEngine, Listener>();
        private static Dictionary<IScriptEngine, HttpRequest> m_HttpRequest =
                new Dictionary<IScriptEngine, HttpRequest>();
        private static Dictionary<IScriptEngine, SensorRepeat> m_SensorRepeat =
                new Dictionary<IScriptEngine, SensorRepeat>();
        private static Dictionary<IScriptEngine, XmlRequest> m_XmlRequest =
                new Dictionary<IScriptEngine, XmlRequest>();

        public Dataserver DataserverPlugin
        {
            get 
            {
                staticLock.AcquireReaderLock(-1);
                try
                {
                    return m_Dataserver[m_ScriptEngine];
                }
                finally
                {
                    staticLock.ReleaseReaderLock();
                }
            }
        }

        public Timer TimerPlugin
        {
            get 
            { 
                staticLock.AcquireReaderLock(-1);
                try
                {
                    return m_Timer[m_ScriptEngine];
                }
                finally
                {
                    staticLock.ReleaseReaderLock();
                }
            }
        }

        public HttpRequest HttpRequestPlugin
        {
            get 
            { 
                staticLock.AcquireReaderLock(-1);
                try
                {
                    return m_HttpRequest[m_ScriptEngine];
                }
                finally
                {
                    staticLock.ReleaseReaderLock();
                }
            }
        }

        public Listener ListenerPlugin
        {
            get 
            {
                staticLock.AcquireReaderLock(-1);
                try
                {
                    return m_Listener[m_ScriptEngine];
                }
                finally
                {
                    staticLock.ReleaseReaderLock();
                }
            }
        }

        public SensorRepeat SensorRepeatPlugin
        {
            get 
            { 
                staticLock.AcquireReaderLock(-1);
                try
                {
                    return m_SensorRepeat[m_ScriptEngine];
                }
                finally
                {
                    staticLock.ReleaseReaderLock();
                }
            }
        }

        public XmlRequest XmlRequestPlugin
        {
            get 
            {
                staticLock.AcquireReaderLock(-1);
                try
                {
                    return m_XmlRequest[m_ScriptEngine];
                }
                finally
                {
                    staticLock.ReleaseReaderLock();
                }
            }
        }

        public IScriptEngine[] ScriptEngines
        {
            get 
            {
                staticLock.AcquireReaderLock(-1);
                try
                {
                    return m_ScriptEngines.ToArray();
                }
                finally
                {
                    staticLock.ReleaseReaderLock();
                }
            }
        }

        public AsyncCommandManager(IScriptEngine _ScriptEngine)
        {
            m_ScriptEngine = _ScriptEngine;

            // If there is more than one scene in the simulator or multiple script engines are used on the same region
            // then more than one thread could arrive at this block of code simultaneously.  However, it cannot be
            // executed concurrently both because concurrent list operations are not thread-safe and because of other
            // race conditions such as the later check of cmdHandlerThread == null.
            staticLock.AcquireReaderLock(-1);
            try
            {
                if (m_ScriptEngines.Contains(m_ScriptEngine))
                {
                    return;
                }

                LockCookie lc = staticLock.UpgradeToWriterLock(-1);
                try
                {
                    if (m_ScriptEngines.Contains(m_ScriptEngine))
                    {
                        return;
                    }

                    if (m_ScriptEngines.Count == 0)
                        ReadConfig();

                    if (!m_ScriptEngines.Contains(m_ScriptEngine))
                        m_ScriptEngines.Add(m_ScriptEngine);

                    // Create instances of all plugins
                    if (!m_Dataserver.ContainsKey(m_ScriptEngine))
                        m_Dataserver[m_ScriptEngine] = new Dataserver(this);
                    if (!m_Timer.ContainsKey(m_ScriptEngine))
                        m_Timer[m_ScriptEngine] = new Timer(this);
                    if (!m_HttpRequest.ContainsKey(m_ScriptEngine))
                        m_HttpRequest[m_ScriptEngine] = new HttpRequest(this);
                    if (!m_Listener.ContainsKey(m_ScriptEngine))
                        m_Listener[m_ScriptEngine] = new Listener(this);
                    if (!m_SensorRepeat.ContainsKey(m_ScriptEngine))
                        m_SensorRepeat[m_ScriptEngine] = new SensorRepeat(this);
                    if (!m_XmlRequest.ContainsKey(m_ScriptEngine))
                        m_XmlRequest[m_ScriptEngine] = new XmlRequest(this);

                    StartThread();
                }
                finally
                {
                    staticLock.DowngradeFromWriterLock(ref lc);
                }
            }
            finally
            {
                staticLock.ReleaseReaderLock();
            }
        }

        private static void StartThread()
        {
            if (cmdHandlerThread == null)
            {
                // Start the thread that will be doing the work
                cmdHandlerThread
                    = Watchdog.StartThread(
                        CmdHandlerThreadLoop, "AsyncLSLCmdHandlerThread", ThreadPriority.Normal, true, true);
            }
        }

        private void ReadConfig()
        {
//            cmdHandlerThreadCycleSleepms = m_ScriptEngine.Config.GetInt("AsyncLLCommandLoopms", 100);
            // TODO: Make this sane again
            cmdHandlerThreadCycleSleepms = 100;
        }

        ~AsyncCommandManager()
        {
            // Shut down thread
//            try
//            {
//                if (cmdHandlerThread != null)
//                {
//                    if (cmdHandlerThread.IsAlive == true)
//                    {
//                        cmdHandlerThread.Abort();
//                        //cmdHandlerThread.Join();
//                    }
//                }
//            }
//            catch
//            {
//            }
        }

        /// <summary>
        /// Main loop for the manager thread
        /// </summary>
        private static void CmdHandlerThreadLoop()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(cmdHandlerThreadCycleSleepms);

                    DoOneCmdHandlerPass();

                    Watchdog.UpdateThread();
                }
                catch (Exception e)
                {
                    m_log.Error("[ASYNC COMMAND MANAGER]: Exception in command handler pass: ", e);
                }
            }
        }

        private static void DoOneCmdHandlerPass()
        {
            staticLock.AcquireReaderLock(-1);
            try
            {
                // Check HttpRequests
                m_HttpRequest[m_ScriptEngines[0]].CheckHttpRequests();

                // Check XMLRPCRequests
                m_XmlRequest[m_ScriptEngines[0]].CheckXMLRPCRequests();

                foreach (IScriptEngine s in m_ScriptEngines)
                {
                    // Check Listeners
                    m_Listener[s].CheckListeners();

                    // Check timers
                    m_Timer[s].CheckTimerEvents();

                    // Check Sensors
                    m_SensorRepeat[s].CheckSenseRepeaterEvents();

                    // Check dataserver
                    m_Dataserver[s].ExpireRequests();
                }
            }
            finally
            {
                staticLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Unregister a specific script from facilities (and all its pending commands)
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        public static void UnregisterScriptFacilities(IScriptEngine engine, uint localID, UUID itemID, bool resetScript)
        {
//            m_log.DebugFormat("[ASYNC COMMAND MANAGER]: Removing facilities for script {0}", itemID);

            staticLock.AcquireReaderLock(-1);
            try
            {
                // Remove dataserver events
                m_Dataserver[engine].RemoveEvents(localID, itemID);

                if (resetScript)
                {
                    // Remove from: Timers
                    m_Timer[engine].UnSetTimerEvents(localID, itemID);

                    // Remove from: HttpRequest
                    IHttpRequestModule iHttpReq = engine.World.RequestModuleInterface<IHttpRequestModule>();
                    if (iHttpReq != null)
                        iHttpReq.StopHttpRequestsForScript(itemID);
                }

                IWorldComm comms = engine.World.RequestModuleInterface<IWorldComm>();
                if (comms != null)
                    comms.DeleteListener(itemID);

                IXMLRPC xmlrpc = engine.World.RequestModuleInterface<IXMLRPC>();
                if (xmlrpc != null)
                {
                    xmlrpc.DeleteChannels(itemID);
                    xmlrpc.CancelSRDRequests(itemID);
                }

                // Remove Sensors
                m_SensorRepeat[engine].UnSetSenseRepeaterEvents(localID, itemID);
            }
            finally
            {
                staticLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Get the sensor repeat plugin for this script engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static SensorRepeat GetSensorRepeatPlugin(IScriptEngine engine)
        {
            staticLock.AcquireReaderLock(-1);
            try
            {
                if (m_SensorRepeat.ContainsKey(engine))
                    return m_SensorRepeat[engine];
                else
                    return null;
            }
            finally
            {
                staticLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Get the dataserver plugin for this script engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static Dataserver GetDataserverPlugin(IScriptEngine engine)
        {
            staticLock.AcquireReaderLock(-1);
            try
            {
                if (m_Dataserver.ContainsKey(engine))
                    return m_Dataserver[engine];
                else
                    return null;
            }
            finally
            {
                staticLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Get the timer plugin for this script engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static Timer GetTimerPlugin(IScriptEngine engine)
        {
            staticLock.AcquireReaderLock(-1);
            try
            {
                if (m_Timer.ContainsKey(engine))
                    return m_Timer[engine];
                else
                    return null;
            }
            finally
            {
                staticLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Get the listener plugin for this script engine.
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static Listener GetListenerPlugin(IScriptEngine engine)
        {
            staticLock.AcquireReaderLock(-1);
            try
            {
                if (m_Listener.ContainsKey(engine))
                    return m_Listener[engine];
                else
                    return null;
            }
            finally
            {
                staticLock.ReleaseReaderLock();
            }
        }

        public static Object[] GetSerializationData(IScriptEngine engine, UUID itemID)
        {
            List<Object> data = new List<Object>();

            staticLock.AcquireReaderLock(-1);
            try
            {
                Object[] listeners = m_Listener[engine].GetSerializationData(itemID);
                if (listeners.Length > 0)
                {
                    data.Add("listener");
                    data.Add(listeners.Length);
                    data.AddRange(listeners);
                }

                Object[] timers=m_Timer[engine].GetSerializationData(itemID);
                if (timers.Length > 0)
                {
                    data.Add("timer");
                    data.Add(timers.Length);
                    data.AddRange(timers);
                }

                Object[] sensors = m_SensorRepeat[engine].GetSerializationData(itemID);
                if (sensors.Length > 0)
                {
                    data.Add("sensor");
                    data.Add(sensors.Length);
                    data.AddRange(sensors);
                }
            }
            finally
            {
                staticLock.ReleaseReaderLock();
            }

            return data.ToArray();
        }

        public static void CreateFromData(IScriptEngine engine, uint localID,
                UUID itemID, UUID hostID, Object[] data)
        {
            int idx = 0;
            int len;

            while (idx < data.Length)
            {
                string type = data[idx].ToString();
                len = (int)data[idx+1];
                idx+=2;

                if (len > 0)
                {
                    Object[] item = new Object[len];
                    Array.Copy(data, idx, item, 0, len);

                    idx+=len;

                    staticLock.AcquireReaderLock(-1);
                    try
                    {
                        switch (type)
                        {
                            case "listener":
                                m_Listener[engine].CreateFromData(localID, itemID,
                                                            hostID, item);
                                break;
                            case "timer":
                                m_Timer[engine].CreateFromData(localID, itemID,
                                                            hostID, item);
                                break;
                            case "sensor":
                                m_SensorRepeat[engine].CreateFromData(localID,
                                                            itemID, hostID, item);
                                break;
                        }
                    }
                    finally
                    {
                        staticLock.ReleaseReaderLock();
                    }
                }
            }
        }
    }
}
