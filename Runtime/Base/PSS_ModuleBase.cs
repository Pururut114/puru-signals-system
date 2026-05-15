using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    public abstract class PSS_ModuleBase : UdonSharpBehaviour
    {
        [HideInInspector] public string moduleName;
    }
}
