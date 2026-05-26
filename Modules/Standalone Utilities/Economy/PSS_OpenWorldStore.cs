using UdonSharp;
using UnityEngine;
using VRC.Economy;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/Economy/PSS_OpenWorldStore [Utility]")]
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
