using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/MoveToPoint")]
    [AddComponentMenu("PSS/Actions/PSS_MoveToPoint [Action]")]
    public class PSS_MoveToPoint : PSS_ActionBase
    {
        [PSS_Field("Object to move")]
        public Transform objectToMove;

        [PSS_Field("Destination")]
        public Transform destination;

        [PSS_Field("Copy rotation")]
        public bool copyRotation = true;

        protected override void OnExecute()
        {
            if (objectToMove == null || destination == null) return;
            if (copyRotation)
                objectToMove.SetPositionAndRotation(destination.position, destination.rotation);
            else
                objectToMove.position = destination.position;
        }
    }
}
