using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace TriInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class ListDrawerSettingsAttribute : Attribute
    {
        public bool Draggable { get; set; } = true;
        public bool HideAddButton { get; set; }
        public bool HideRemoveButton { get; set; }
        public bool AlwaysExpanded { get; set; }
        public bool AlwaysElementsExpanded { get; set; }
        public bool ShowElementLabels { get; set; }
    }
}