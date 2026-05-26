#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace PuruSignals.Editor
{
    [InitializeOnLoad]
    public static class PSS_StandaloneUtilityEditor
    {
        static PSS_StandaloneUtilityEditor()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
        }

        static void OnHeaderGUI(UnityEditor.Editor editor)
        {
            if (editor.target == null) return;
            var note = (PSS_NoteAttribute)Attribute.GetCustomAttribute(
                editor.target.GetType(), typeof(PSS_NoteAttribute));
            if (note == null) return;
            EditorGUILayout.HelpBox(note.Text, MessageType.None);
        }
    }
}
#endif
