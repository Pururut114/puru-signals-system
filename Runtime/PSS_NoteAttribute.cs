using System;

namespace PuruSignals
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PSS_NoteAttribute : Attribute
    {
        public readonly string Text;
        public PSS_NoteAttribute(string text) { Text = text; }
    }
}
