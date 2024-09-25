using System;
using System.Diagnostics;

namespace TriInspector
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    [Conditional("UNITY_EDITOR")]
    public class TypeNameAttribute : Attribute
    {
        public string Name { get; }

        public TypeNameAttribute(string name)
        {
            Name = name;
        }
    }
}