using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/SetPickupable")]
    [AddComponentMenu("PSS/Actions/PSS_SetPickupable [Action]")]
    public class PSS_SetPickupable : PSS_ActionBase
    {
        [PSS_Field("Targets", isList: true)]
        public GameObject[] targets;

        [PSS_Field("Pickupable")]
        public bool pickupable = true;

        [PSS_Field("Drop if disabling", tooltip: "Выбросить из рук если pickupable = false")]
        public bool dropIfDisabling = true;

        protected override void OnExecute()
        {
            foreach (var go in targets)
            {
                if (go == null) continue;
                var pickup = (VRC_Pickup)go.GetComponent(typeof(VRC_Pickup));
                if (pickup == null) continue;
                if (!pickupable && dropIfDisabling)
                    pickup.Drop();
                pickup.pickupable = pickupable;
            }
        }
    }
}
