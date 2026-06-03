using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    public abstract class PSS_TriggerBase : PSS_ModuleBase
    {
        public PSS_ChannelLocal channel;

        protected void Fire()
        {
            if (channel != null)
                channel.Trigger();
        }

        protected void FireWithPlayer(VRCPlayerApi player)
        {
            if (channel != null)
                channel.TriggerWithPlayer(player);
        }
    }
}
