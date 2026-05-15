using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    public enum StateSyncBoolOp { SetTrue, SetFalse, Toggle }
    public enum StateSyncNumOp  { Set, Add, Subtract }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/SetStateSync")]
    [AddComponentMenu("PSS/Actions/PSS_SetStateSync [Action]")]
    public class PSS_SetStateSync : PSS_ActionBase
    {
        [PSS_Field("Target")]
        public PSS_StateSync target;

        [PSS_Field("Bool Operation")]
        public StateSyncBoolOp boolOp = StateSyncBoolOp.Toggle;

        [PSS_Field("Int / Float Operation")]
        public StateSyncNumOp numOp = StateSyncNumOp.Set;

        [PSS_Field("Bool Value")]
        public bool valueBool = true;

        [PSS_Field("Int Value")]
        public int valueInt = 0;

        [PSS_Field("Float Value")]
        public float valueFloat = 0f;

        protected override void OnExecute()
        {
            if (target == null) return;

            switch (target.valueType)
            {
                case StateSyncType.Bool:
                    switch (boolOp)
                    {
                        case StateSyncBoolOp.SetTrue:  target.SetBool(true);  break;
                        case StateSyncBoolOp.SetFalse: target.SetBool(false); break;
                        case StateSyncBoolOp.Toggle:   target.Toggle();       break;
                    }
                    break;

                case StateSyncType.Int:
                    switch (numOp)
                    {
                        case StateSyncNumOp.Set:      target.SetInt(valueInt);                    break;
                        case StateSyncNumOp.Add:      target.SetInt(target.GetInt() + valueInt);  break;
                        case StateSyncNumOp.Subtract: target.SetInt(target.GetInt() - valueInt);  break;
                    }
                    break;

                case StateSyncType.Float:
                    switch (numOp)
                    {
                        case StateSyncNumOp.Set:      target.SetFloat(valueFloat);                      break;
                        case StateSyncNumOp.Add:      target.SetFloat(target.GetFloat() + valueFloat);  break;
                        case StateSyncNumOp.Subtract: target.SetFloat(target.GetFloat() - valueFloat);  break;
                    }
                    break;
            }
        }
    }
}
