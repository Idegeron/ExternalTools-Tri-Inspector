using TriInspector;
using TriInspector.Drawers;
using UnityEditor;
using UnityEngine;

[assembly: RegisterTriAttributeDrawer(typeof(MinMaxSliderDrawer), TriDrawerOrder.Decorator)]

namespace TriInspector.Drawers
{
    public class MinMaxSliderDrawer : TriAttributeDrawer<MinMaxSliderAttribute>
    {
        public override TriElement CreateElement(TriProperty property, TriElement next)
        {
            return new MinMaxSliderElement(property, Attribute);
        }

        private class MinMaxSliderElement : TriElement
        {
            private readonly TriProperty _property;
            private readonly MinMaxSliderAttribute _minMaxSliderAttribute;
            
            public MinMaxSliderElement(TriProperty property, MinMaxSliderAttribute minMaxSliderAttribute)
            {
                _property = property;
                _minMaxSliderAttribute = minMaxSliderAttribute;
            }

            public override float GetHeight(float width)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position)
            {
                var propertyType = _property.ValueType;

                var label = _property.DisplayNameContent;
                
                label.tooltip = _minMaxSliderAttribute.Min.ToString("F2") + " to " + _minMaxSliderAttribute.Max.ToString("F2");

                var controlRect = EditorGUI.PrefixLabel(position, label);

                var splittedRect = SplitRect(controlRect, 3);

                if (propertyType == typeof(Vector2))
                {
                    EditorGUI.BeginChangeCheck();

                    var vector = (Vector2)_property.Value;
                    var minVal = vector.x;
                    var maxVal = vector.y;

                    minVal = EditorGUI.FloatField(splittedRect[0], float.Parse(minVal.ToString("F2")));
                    maxVal = EditorGUI.FloatField(splittedRect[2], float.Parse(maxVal.ToString("F2")));

                    EditorGUI.MinMaxSlider(splittedRect[1], ref minVal, ref maxVal,
                        _minMaxSliderAttribute.Min, _minMaxSliderAttribute.Max);

                    if (minVal < _minMaxSliderAttribute.Min)
                    {
                        minVal = _minMaxSliderAttribute.Min;
                    }

                    if (maxVal > _minMaxSliderAttribute.Max)
                    {
                        maxVal = _minMaxSliderAttribute.Max;
                    }

                    vector = new Vector2(minVal > maxVal ? maxVal : minVal, maxVal);

                    if (EditorGUI.EndChangeCheck())
                    {
                        _property.SetValue(vector);
                    }

                }
                else if (propertyType == typeof(Vector2Int))
                {
                    EditorGUI.BeginChangeCheck();

                    var vector = (Vector2Int)_property.Value;
                    var minVal = (float)vector.x;
                    var maxVal = (float)vector.y;

                    minVal = EditorGUI.FloatField(splittedRect[0], minVal);
                    maxVal = EditorGUI.FloatField(splittedRect[2], maxVal);

                    EditorGUI.MinMaxSlider(splittedRect[1], ref minVal, ref maxVal,
                        _minMaxSliderAttribute.Min, _minMaxSliderAttribute.Max);

                    if (minVal < _minMaxSliderAttribute.Min)
                    {
                        maxVal = _minMaxSliderAttribute.Min;
                    }

                    if (minVal > _minMaxSliderAttribute.Max)
                    {
                        maxVal = _minMaxSliderAttribute.Max;
                    }

                    vector = new Vector2Int(Mathf.FloorToInt(minVal > maxVal ? maxVal : minVal),
                        Mathf.FloorToInt(maxVal));

                    if (EditorGUI.EndChangeCheck())
                    {
                        _property.SetValue(vector);
                    }
                }
            }
            
            private Rect[] SplitRect(Rect rectToSplit, int n)
            {
                var rects = new Rect[n];

                for (var i = 0; i < n; i++)
                {
                    rects[i] = new Rect(rectToSplit.position.x + (i * rectToSplit.width / n), rectToSplit.position.y,
                        rectToSplit.width / n, rectToSplit.height);
                }

                var padding = (int) rects[0].width - 40;
                var space = 5;

                rects[0].width -= padding + space;
                rects[2].width -= padding + space;

                rects[1].x -= padding;
                rects[1].width += padding * 2;

                rects[2].x += padding + space;

                return rects;
            }
        }
    }
}