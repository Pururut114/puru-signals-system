using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("PSS/Standalone Utilities/Select/PSS_MultiSelectButton [Utility]")]
    public class PSS_MultiSelectButton : UdonSharpBehaviour
    {
        [Header("Controller")]
        public PSS_MultiSelectController controller;

        [Header("Selection")]
        [Tooltip("Index to select on interact. -1 = SelectNone.")]
        public int indexToSelect = -1;

        [Tooltip("If true: interacting when this index is already active deselects it (-1).")]
        public bool toggleMode = false;

        public override void Interact()
        {
            if (controller == null) return;

            if (indexToSelect < 0)
                controller.SelectNone();
            else if (toggleMode)
                controller.SelectToggle(indexToSelect);
            else
                controller.SelectIndex(indexToSelect);
        }

        // Callable from other Udon objects
        public void Trigger()
        {
            Interact();
        }
    }
}
