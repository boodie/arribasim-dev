using libsecondlife;

namespace OpenSim.Region.Environment.Scenes
{
    /// <summary>
    /// A class for triggering remote scene events.
    /// </summary>
    public class EventManager
    {
        public delegate void OnFrameDelegate();
        public event OnFrameDelegate OnFrame;

        public delegate void OnBackupDelegate(Interfaces.IRegionDataStore datastore);
        public event OnBackupDelegate OnBackup;

        public delegate void OnNewPresenceDelegate(ScenePresence presence);
        public event OnNewPresenceDelegate OnNewPresence;

        public delegate void OnRemovePresenceDelegate(LLUUID uuid);
        public event OnRemovePresenceDelegate OnRemovePresence;

        public delegate void OnParcelPrimCountTaintedDelegate();
        public event OnParcelPrimCountTaintedDelegate OnParcelPrimCountTainted;

        public delegate void OnParcelPrimCountUpdateDelegate();
        public event OnParcelPrimCountUpdateDelegate OnParcelPrimCountUpdate;

        public delegate void OnParcelPrimCountAddDelegate(SceneObject obj);
        public event OnParcelPrimCountAddDelegate OnParcelPrimCountAdd;

        public void TriggerOnFrame()
        {
            if (OnFrame != null)
            {
                OnFrame();
            }
        }

        public void TriggerOnNewPresence(ScenePresence presence)
        {
            if (OnNewPresence != null)
                OnNewPresence(presence);
        }

        public void TriggerOnRemovePresence(LLUUID uuid)
        {
            if (OnRemovePresence != null)
            {
                OnRemovePresence(uuid);
            }
        }

        public void TriggerOnBackup(Interfaces.IRegionDataStore dstore)
        {
            if (OnBackup != null)
            {
                OnBackup(dstore);
            }
        }

        public void TriggerParcelPrimCountTainted()
        {
            if (OnParcelPrimCountTainted != null)
            {
                OnParcelPrimCountTainted();
            }
        }
        public void TriggerParcelPrimCountUpdate()
        {
            if (OnParcelPrimCountUpdate != null)
            {
                OnParcelPrimCountUpdate();
            }
        }
        public void TriggerParcelPrimCountAdd(SceneObject obj)
        {
            if (OnParcelPrimCountAdd != null)
            {
                OnParcelPrimCountAdd(obj);
            }
        }
    }
}
