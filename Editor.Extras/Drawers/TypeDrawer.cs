using System;
using System.Collections.Generic;
using System.Linq;
using TriInspector;
using TriInspector.Drawers;
using TriInspector.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[assembly: RegisterTriValueDrawer(typeof(TypeDrawer), TriDrawerOrder.Fallback)]

namespace TriInspector.Drawers
{
    public class TypeDrawer : TriValueDrawer<Type>
    {
        public override float GetHeight(float width, TriValue<Type> propertyValue, TriElement next)
        {
            return EditorGUIUtility.singleLineHeight * 2;
        }

        public override void OnGUI(Rect position, TriValue<Type> propertyValue, TriElement next)
        {
            var type = propertyValue.SmartValue;
            var typeName = type != null ? TriTypeUtilities.GetTypeNiceName(type) : "[None]";
            var typeNameContent = new GUIContent(typeName);

            if (!propertyValue.Property.IsArrayElement && 
                !propertyValue.Property.TryGetAttribute<HideLabelAttribute>(out var hideLabelAttribute))
            {
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), propertyValue.Property.DisplayNameContent);
            }

            if (EditorGUI.DropdownButton(position, typeNameContent, FocusType.Passive))
            {
                var dropdown = new TypeDropDown(propertyValue, new AdvancedDropdownState());
                
                dropdown.Show(position);

                Event.current.Use();
            }
        }
        
        private class TypeDropDown : AdvancedDropdown
        {
            private readonly TriValue<Type> _propertyValue;

            public bool CanHideHeader { get; private set; }

            public TypeDropDown(TriValue<Type> propertyValue, AdvancedDropdownState state) : base(state)
            {
                _propertyValue = propertyValue;
                
                minimumSize = new Vector2(0, 120);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                _propertyValue.Property.TryGetAttribute<TypeConstraintAttribute>(out var typeConstraintAttribute);
                
                var types = TriReflectionUtilities
                    .AllTypes
                    .Where(type => typeConstraintAttribute == null || typeConstraintAttribute.AssemblyType.IsAssignableFrom(type))
                    .Where(type => (typeConstraintAttribute == null && !type.IsAbstract) || typeConstraintAttribute != null &&
                        typeConstraintAttribute.AllowAbstract && type.IsAbstract)
                    .ToList();

                var groupByNamespace = types.Count > 20;

                CanHideHeader = !groupByNamespace;

                var root = new TypeGroupItem("Type");
                
                root.AddChild(new TypeItem(null));
                
                root.AddSeparator();

                foreach (var type in types)
                {
                    IEnumerable<string> namespaceEnumerator = groupByNamespace && type.Namespace != null
                        ? type.Namespace.Split('.')
                        : Array.Empty<string>();

                    root.AddTypeChild(type, namespaceEnumerator.GetEnumerator());
                }

                root.Build();

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (!(item is TypeItem referenceTypeItem))
                {
                    return;
                }

                if (referenceTypeItem.Type == null)
                {
                    _propertyValue.SetValue(null);
                }
                else
                {
                    _propertyValue.SetValue(referenceTypeItem.Type);
                }
            }

            private class TypeGroupItem : AdvancedDropdownItem
            {
                private static readonly Texture2D ScriptIcon = EditorGUIUtility.FindTexture("cs Script Icon");

                private readonly List<TypeItem> _childItems = new ();

                private readonly Dictionary<string, TypeGroupItem> _childGroups = new ();

                public TypeGroupItem(string name) : base(name)
                {
                }

                public void AddTypeChild(Type type, IEnumerator<string> namespaceRemaining)
                {
                    if (!namespaceRemaining.MoveNext())
                    {
                        _childItems.Add(new TypeItem(type, ScriptIcon));
                        
                        return;
                    }

                    var namespaceName = namespaceRemaining.Current ?? "";

                    if (!_childGroups.TryGetValue(namespaceName, out var child))
                    {
                        _childGroups[namespaceName] = child = new TypeGroupItem(namespaceName);
                    }

                    child.AddTypeChild(type, namespaceRemaining);
                }

                public void Build()
                {
                    foreach (var child in _childGroups.Values.OrderBy(it => it.name))
                    {
                        AddChild(child);

                        child.Build();
                    }

                    AddSeparator();

                    foreach (var child in _childItems)
                    {
                        AddChild(child);
                    }
                }
            }

            private class TypeItem : AdvancedDropdownItem
            {
                public TypeItem(Type type, Texture2D preview = null)
                    : base(type != null ? TriTypeUtilities.GetTypeNiceName(type) : "[None]")
                {
                    Type = type;
                    icon = preview;
                }

                public Type Type { get; }
            }
        } 
    }
}