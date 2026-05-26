#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UdonSharpEditor;

namespace PuruSignals.Editor
{
    public class PSS_StandaloneUtilityEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var note = (PSS_NoteAttribute)Attribute.GetCustomAttribute(
                target.GetType(), typeof(PSS_NoteAttribute));
            if (note != null)
                EditorGUILayout.HelpBox(note.Text, MessageType.None);

            DrawDefaultInspector();
        }
    }
}
#endif
