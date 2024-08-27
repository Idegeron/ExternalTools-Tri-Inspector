using System;
using System.Diagnostics;

namespace TriInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class DisableByNameAttribute : Attribute
    {
        public string Name { get; }

        public DisableByNameAttribute(string name)
        {
            Name = name;
        }
    }
}