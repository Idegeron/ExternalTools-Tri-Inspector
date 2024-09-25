using System;
using System.Diagnostics;
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
        public Color OddElementColor { get; set; } = new Color(0.25f, 0.25f, 0.25f, 1f);
        public Color EvenElementColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 1f);
    }
}