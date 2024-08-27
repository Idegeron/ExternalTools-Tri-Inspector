using System.Collections.Generic;
using TriInspector;
using TriInspector.Drawers;
using TriInspector.Elements;
using UnityEngine;

[assembly: RegisterTriValueDrawer(typeof(KeyValuePairDrawer), TriDrawerOrder.Fallback)]

namespace TriInspector.Drawers
{
    public class KeyValuePairDrawer : TriValueDrawer<KeyValuePair<object, object>>
    {
        public override TriElement CreateElement(TriValue<KeyValuePair<object, object>> propertyValue, TriElement next)
        {
            var keyValuePairElement = new KeyValuePairElement();

            foreach (var childTriProperty in propertyValue.Property.ChildrenProperties)
            {
                keyValuePairElement.AddChild(new TriPropertyElement(childTriProperty));
            }
            
            return keyValuePairElement; 
        }
        
        private class KeyValuePairElement : TriHorizontalGroupElement
        {
            public override void OnGUI(Rect position)
            {
                using (TriPropertyOverrideContext.BeginOverride(new KeyValuePairOverrideContext()))
                {
                    base.OnGUI(position);
                }
            }
        }
        
        private class KeyValuePairOverrideContext : TriPropertyOverrideContext
        {
            public override bool TryGetDisplayName(TriProperty property, out GUIContent displayName)
            {
                if (property.Parent.IsArrayElement || property.Parent.TryGetAttribute<HideLabelAttribute>(out _))
                {
                    displayName = GUIContent.none;
                    
                    return true;
                }

                displayName = default;
                
                return false;
            }
        }
    }
}