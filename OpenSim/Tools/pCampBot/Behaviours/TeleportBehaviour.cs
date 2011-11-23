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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using OpenMetaverse;
using pCampBot.Interfaces;

namespace pCampBot
{
    /// <summary>
    /// Teleport to a random region on the grid.
    /// </summary>
    public class TeleportBehaviour : IBehaviour
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string Name { get { return "Teleport"; } }

        public void Action(Bot bot)
        {
            Random rng = bot.Manager.Rng;
            GridRegion[] knownRegions;

            lock (bot.Manager.RegionsKnown)
            {
                if (bot.Manager.RegionsKnown.Count == 0)
                {
                    m_log.DebugFormat(
                        "[TELEPORT BEHAVIOUR]: Ignoring teleport action for {0} since no regions are known yet", bot.Name);
                    return;
                }

                knownRegions = bot.Manager.RegionsKnown.Values.ToArray();
            }

            Simulator sourceRegion = bot.Client.Network.CurrentSim;
            GridRegion destRegion = knownRegions[rng.Next(knownRegions.Length)];
            Vector3 destPosition = new Vector3(rng.Next(255), rng.Next(255), 50);

            m_log.DebugFormat(
                "[TELEPORT BEHAVIOUR]: Teleporting {0} from {1} {2} to {3} {4}",
                bot.Name, sourceRegion.Name, bot.Client.Self.SimPosition, destRegion.Name, destPosition);

            bot.Client.Self.Teleport(destRegion.RegionHandle, destPosition);
        }
    }
}