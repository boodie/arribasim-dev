using System;
using System.Collections.Generic;
using System.Text;
using libsecondlife;
using libsecondlife.Packets;
using OpenSim.Physics.Manager;
using OpenSim.Framework.Interfaces;

namespace OpenSim.world
{
    partial class Avatar
    {
        /// <summary>
        /// 
        /// </summary>
        public override void update()
        {
            

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteAvatar"></param>
        public void SendUpdateToOtherClient(Avatar remoteAvatar)
        {
          
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ObjectUpdatePacket CreateUpdatePacket()
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendInitialPosition()
        {
            Console.WriteLine("sending initial Avatar data");
            this.ControllingClient.SendAvatarData(this.regionData, this.firstname, this.lastname, this.uuid, this.localid, new LLVector3(128, 128, 60));
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendOurAppearance()
        {
           
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="OurClient"></param>
        public void SendOurAppearance(IClientAPI OurClient)
        {
            this.ControllingClient.SendWearables(this.Wearables);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="avatarInfo"></param>
        public void SendAppearanceToOtherAgent(Avatar avatarInfo)
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="visualParam"></param>
        public void SetAppearance(byte[] texture, AgentSetAppearancePacket.VisualParamBlock[] visualParam)
        {
           
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopMovement()
        {
           
        }

        /// <summary>
        ///  Very likely to be deleted soon!
        /// </summary>
        /// <returns></returns>
        public ImprovedTerseObjectUpdatePacket.ObjectDataBlock CreateTerseBlock()
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="animID"></param>
        /// <param name="seq"></param>
        public void SendAnimPack(LLUUID animID, int seq)
        {
            
          
        }

        /// <summary>
        /// 
        /// </summary>
        public void SendAnimPack()
        {
           
        }

    }
}
