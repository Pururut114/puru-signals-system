using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    public enum DataSlotOp { Set, Add, Subtract, Multiply, Divide }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/SetDataSlot")]
    [AddComponentMenu("PSS/Actions/PSS_SetDataSlot [Action]")]
    public class PSS_SetDataSlot : PSS_ActionBase
    {
        [PSS_Field("Target Slot")]
        public PSS_DataSlot target;

        [PSS_Field("Operation")]
        public DataSlotOp operation = DataSlotOp.Set;

        [PSS_Header("Value")]
        [PSS_Field("Bool")]
        public bool   valueBool;
        [PSS_Field("Int")]
        public int    valueInt;
        [PSS_Field("Float")]
        public float  valueFloat;
        [PSS_Field("Vector3")]
        public Vector3 valueVec3;
        [PSS_Field("String")]
        public string  valueString = "";

        protected override void OnExecute()
        {
            if (target == null) return;

            switch (target.valueType)
            {
                case DataSlotType.Bool:
                    target.SetBool(valueBool);
                    break;

                case DataSlotType.Int:
                    switch (operation)
                    {
                        case DataSlotOp.Set:      target.SetInt(valueInt);         break;
                        case DataSlotOp.Add:      target.AddInt(valueInt);         break;
                        case DataSlotOp.Subtract: target.SubInt(valueInt);         break;
                        case DataSlotOp.Multiply: target.MulInt(valueInt);         break;
                        case DataSlotOp.Divide:   target.DivInt(valueInt);         break;
                    }
                    break;

                case DataSlotType.Float:
                    switch (operation)
                    {
                        case DataSlotOp.Set:      target.SetFloat(valueFloat);     break;
                        case DataSlotOp.Add:      target.AddFloat(valueFloat);     break;
                        case DataSlotOp.Subtract: target.SubFloat(valueFloat);     break;
                        case DataSlotOp.Multiply: target.MulFloat(valueFloat);     break;
                        case DataSlotOp.Divide:   target.DivFloat(valueFloat);     break;
                    }
                    break;

                case DataSlotType.Vector3:
                    target.SetVec3(valueVec3);
                    break;

                case DataSlotType.String:
                    target.SetString(valueString);
                    break;
            }
        }
    }
}
