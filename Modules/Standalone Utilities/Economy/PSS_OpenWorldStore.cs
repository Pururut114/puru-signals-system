using UdonSharp;
using UnityEngine;
using VRC.Economy;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/Economy/PSS_OpenWorldStore [Utility]")]
    [PSS_Note("Opens the world store page on Interact or via Open(). Requires Creator Economy to be enabled for the world.")]
    public class PSS_OpenWorldStore : UdonSharpBehaviour
    {
        public override void Interact()
        {
            Store.OpenWorldStorePage();
        }

        public void Open()
        {
            Store.OpenWorldStorePage();
        }
    }
}
