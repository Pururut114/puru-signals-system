using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    public enum AnimParamType { Trigger, Bool, Int, Float }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/AnimationParam")]
    [AddComponentMenu("PSS/Actions/PSS_AnimationParam [Action]")]
    public class PSS_AnimationParam : PSS_ActionBase
    {
        [PSS_Field("Animators", isList: true)]
        public Animator[] targets;

        [PSS_Field("Parameter Name")]
        public string paramName = "";

        [PSS_Field("Parameter Type")]
        public AnimParamType paramType = AnimParamType.Trigger;

        // Все три поля всегда сериализуются — активно только то, что соответствует paramType
        [PSS_Field("Bool Value")]
        public bool valueBool = true;

        [PSS_Field("Int Value")]
        public int valueInt = 0;

        [PSS_Field("Float Value")]
        public float valueFloat = 0f;

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(paramName)) return;

            foreach (var a in targets)
            {
                if (a == null) continue;
                switch (paramType)
                {
                    case AnimParamType.Trigger: a.SetTrigger(paramName);           break;
                    case AnimParamType.Bool:    a.SetBool(paramName, valueBool);   break;
                    case AnimParamType.Int:     a.SetInteger(paramName, valueInt); break;
                    case AnimParamType.Float:   a.SetFloat(paramName, valueFloat); break;
                }
            }
        }
    }
}
