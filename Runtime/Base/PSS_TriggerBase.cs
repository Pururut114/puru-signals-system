using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    public abstract class PSS_TriggerBase : PSS_ModuleBase
    {
        // Канал, который активирует этот триггер.
        // Принимает PSS_ChannelLocal и PSS_ChannelGlobal (наследует Local).
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
