using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    public enum SetActiveOp { True, False, Toggle }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/SetActive")]
    [AddComponentMenu("PSS/Actions/PSS_SetActive [Action]")]
    public class PSS_SetActive : PSS_ActionBase
    {
        [PSS_Field("Targets", isList: true)]
        public GameObject[] targets;

        [PSS_Field("Operation", tooltip: "True=включить, False=выключить, Toggle=инвертировать")]
        public SetActiveOp operation = SetActiveOp.True;

        protected override void OnExecute()
        {
            foreach (var t in targets)
            {
                if (t == null) continue;
                switch (operation)
                {
                    case SetActiveOp.True:   t.SetActive(true);  break;
                    case SetActiveOp.False:  t.SetActive(false); break;
                    case SetActiveOp.Toggle: t.SetActive(!t.activeSelf); break;
                }
            }
        }
    }
}
