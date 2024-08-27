using System;
using System.Collections;
using System.Collections.Generic;
using TriInspector;
using TriInspector.Drawers;
using TriInspector.Elements;
using TriInspector.Utilities;
using TriInspectorUnityInternalBridge;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

[assembly: RegisterTriValueDrawer(typeof(DictionaryDrawer), TriDrawerOrder.Fallback)]

namespace TriInspector.Drawers
{
    public class DictionaryDrawer : TriValueDrawer<Dictionary<object, object>>
    {
        public override TriElement CreateElement(TriValue<Dictionary<object, object>> propertyValue, TriElement next)
        {
            return new DictionaryElement(propertyValue.Property.ChildrenProperties[0]);
        }

        private class DictionaryElement : TriElement
        {
            private const float DraggableAreaExtraWidth = 14f;
            private const float FooterExtraSpace = 4;

            private readonly TriProperty _triProperty;
            private readonly TriProperty _keyTriProperty;
            private readonly TriProperty _valueTriProperty;
            private readonly TriElement _keyTriElement;
            private readonly TriElement _valueTriElement;
            private readonly ReorderableList _reorderableList;
            private readonly DictionaryTreeView _dictionaryTreeView;

            private object _keyInstance;
            private object _valueInstance;
            private bool _reloadRequired;
            private bool _heightDirty;
            private bool _isExpanded;
            private bool _displayAddBlock;
            private int _arraySize;
            private float _lastContentWidth;

            public DictionaryElement(TriProperty triProperty)
            {
                _triProperty = triProperty;
                _keyTriProperty = new TriProperty(triProperty.PropertyTree, null, new TriPropertyDefinition(null, null, 0, "Key", _triProperty.ArrayElementType.GenericTypeArguments[0],
                    (self, index) => _keyInstance,
                    (self, index, value) =>
                    {
                        _keyInstance = value;
                        return _keyInstance;
                    },
                    null, false), null);
                _valueTriProperty = new TriProperty(triProperty.PropertyTree, null, new TriPropertyDefinition(null, null, 1, "Value", _triProperty.ArrayElementType.GenericTypeArguments[1],
                    (self, index) => _valueInstance,
                    (self, index, value) =>
                    {
                        _valueInstance = value;
                        return _valueInstance;
                    },
                    null, false), null);
                _keyTriElement = new TriPropertyElement(_keyTriProperty);
                _valueTriElement = new TriPropertyElement(_valueTriProperty);
                _reorderableList = new ReorderableList(null, _triProperty.ArrayElementType)
                {
                    draggable = false,
                    displayAdd = true,
                    displayRemove = true,
                    drawHeaderCallback = DrawHeaderCallback,
                    elementHeightCallback = ElementHeightCallback,
                    drawElementCallback = DrawElementCallback,
                    onAddCallback = AddElementCallback,
                    onRemoveCallback = RemoveElementCallback,
                    onReorderCallbackWithDetails = ReorderCallback,
                };
                _dictionaryTreeView = new DictionaryTreeView(triProperty, this, _reorderableList)
                {
                    SelectionChangedCallback = SelectionChangedCallback,
                };
                _reloadRequired = true;

                _keyTriElement.AttachInternal();
                _valueTriElement.AttachInternal();
            }

            public override bool Update()
            {
                var dirty = false;

                if (_triProperty.TryGetSerializedProperty(out var serializedProperty) && serializedProperty.isArray)
                {
                    _reorderableList.serializedProperty = serializedProperty;
                }
                else if (_triProperty.Value != null)
                {
                    _reorderableList.list = (IList) _triProperty.Value;
                }
                else if (_reorderableList.list == null)
                {
                    _reorderableList.list = (IList) (_triProperty.FieldType.IsArray
                        ? Array.CreateInstance(_triProperty.ArrayElementType, 0)
                        : Activator.CreateInstance(_triProperty.FieldType));
                }

                if (_triProperty.IsExpanded)
                {
                    dirty |= GenerateChildren();
                }
                else
                {
                    dirty |= ClearChildren();
                }

                dirty |= base.Update();

                if (dirty)
                {
                    ReorderableListProxy.ClearCacheRecursive(_reorderableList);
                }

                dirty |= ReloadIfRequired();

                if (dirty)
                {
                    _heightDirty = true;
                    _dictionaryTreeView.multiColumnHeader.ResizeToFit();
                }

                _keyTriElement.Update();
                _valueTriElement.Update();

                return dirty;
            }

            public override float GetHeight(float width)
            {
                _dictionaryTreeView.Width = width;

                if (_heightDirty)
                {
                    _heightDirty = false;
                    _dictionaryTreeView.RefreshHeight();
                }

                var height = 0f;
                height += _reorderableList.headerHeight;

                if (_triProperty.IsExpanded)
                {
                    height += _dictionaryTreeView.totalHeight;
                    height += _reorderableList.footerHeight;
                    height += FooterExtraSpace;
                }
                
                if (_displayAddBlock)
                {
                    height += _keyTriElement.GetHeight(width) + _valueTriElement.GetHeight(width) + EditorGUIUtility.standardVerticalSpacing * 6;
                }

                return height;
            }

            public override void OnGUI(Rect position)
            {
                var headerRect = new Rect(position)
                {
                    height = _reorderableList.headerHeight,
                };
                var elementsRect = new Rect(position)
                {
                    yMin = headerRect.yMax,
                    height = _dictionaryTreeView.totalHeight + FooterExtraSpace,
                };
                var elementsContentRect = new Rect(elementsRect)
                {
                    xMin = elementsRect.xMin + 1,
                    xMax = elementsRect.xMax - 1,
                    yMax = elementsRect.yMax - FooterExtraSpace,
                };
                var footerRect = new Rect(position)
                {
                    yMin = elementsRect.yMax,
                };

                if (!_triProperty.IsExpanded)
                {
                    ReorderableListProxy.DoListHeader(_reorderableList, headerRect);
                    return;
                }

                if (Event.current.isMouse && Event.current.type == EventType.MouseDrag)
                {
                    _heightDirty = true;
                    _dictionaryTreeView.multiColumnHeader.ResizeToFit();
                }

                if (Event.current.type == EventType.Repaint)
                {
                    ReorderableListProxy.defaultBehaviours.boxBackground.Draw(elementsRect,
                        false, false, false, false);
                }

                using (TriPropertyOverrideContext.BeginOverride(new DictionaryElementPropertyOverrideContext()))
                {
                    ReorderableListProxy.DoListHeader(_reorderableList, headerRect);
                }

                EditorGUI.BeginChangeCheck();

                _dictionaryTreeView.OnGUI(elementsContentRect);

                if (EditorGUI.EndChangeCheck())
                {
                    _heightDirty = true;
                    _triProperty.PropertyTree.RequestRepaint();
                }

                ReorderableListProxy.defaultBehaviours.DrawFooter(footerRect, _reorderableList);

                if (_displayAddBlock)
                {
                    DisplayAddBlock(new Rect(footerRect)
                    {
                        yMin = footerRect.yMin - EditorGUIUtility.standardVerticalSpacing,
                        height = _keyTriElement.GetHeight(footerRect.width) 
                                 + _valueTriElement.GetHeight(footerRect.width) 
                                 + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 6
                    });
                }
            }

            private void DrawHeaderCallback(Rect rect)
            {
                var arraySizeRect = new Rect(rect)
                {
                    xMin = rect.xMax - 100,
                };

                var content = _triProperty.Parent.DisplayNameContent;
                    
                if (_triProperty.TryGetSerializedProperty(out var serializedProperty))
                {
                    EditorGUI.BeginProperty(rect, content, serializedProperty);
                    _triProperty.IsExpanded = EditorGUI.Foldout(rect, _triProperty.IsExpanded, content, true);
                    EditorGUI.EndProperty();
                }
                else
                {
                    _triProperty.IsExpanded = EditorGUI.Foldout(rect, _triProperty.IsExpanded, content, true);
                }

                var label = _reorderableList.count == 0 ? "Empty" : $"{_reorderableList.count} items";
                
                GUI.Label(arraySizeRect, label, new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal =
                    {
                        textColor = EditorGUIUtility.isProSkin
                            ? new Color(0.6f, 0.6f, 0.6f)
                            : new Color(0.3f, 0.3f, 0.3f),
                    },
                });
            }
            
            private float ElementHeightCallback(int index)
            {
                if (index >= ChildrenCount)
                {
                    return EditorGUIUtility.singleLineHeight;
                }

                return GetChild(index).GetHeight(_lastContentWidth);
            }

            private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                if (index >= ChildrenCount)
                {
                    return;
                }

                if (!_reorderableList.draggable)
                {
                    rect.xMin += DraggableAreaExtraWidth;
                }

                using (TriPropertyOverrideContext.BeginOverride(new DictionaryElementPropertyOverrideContext()))
                {
                    GetChild(index).OnGUI(rect);
                }
            }
            
            private void SelectionChangedCallback(int index)
            {
                _reorderableList.index = index;
            }
           
            private void AddElementCallback(ReorderableList reorderableList)
            {
                _displayAddBlock = !_displayAddBlock;
                
                _triProperty.PropertyTree.RequestRepaint();
            }
            
            private void RemoveElementCallback(ReorderableList reorderableList)
            {
                if (_triProperty.TryGetSerializedProperty(out _))
                {
                    ReorderableListProxy.defaultBehaviours.DoRemoveButton(reorderableList);
                    _triProperty.NotifyValueChanged();
                    return;
                }

                var template = CloneValue();
                var ind = reorderableList.index;

                _triProperty.SetValues(targetIndex =>
                {
                    var value = (IList) _triProperty.GetValue(targetIndex);

                    if (_triProperty.FieldType.IsArray)
                    {
                        var array = Array.CreateInstance(_triProperty.ArrayElementType, template.Length - 1);
                        Array.Copy(template, 0, array, 0, ind);
                        Array.Copy(template, ind + 1, array, ind, array.Length - ind);
                        value = array;
                    }
                    else
                    {
                        value?.RemoveAt(ind);
                    }

                    return value;
                });
            }
            
            private void ReorderCallback(ReorderableList reorderableList, int oldIndex, int newIndex)
            {
                if (_triProperty.TryGetSerializedProperty(out _))
                {
                    _triProperty.NotifyValueChanged();
                    return;
                }

                var mainValue = _triProperty.Value;

                _triProperty.SetValues(targetIndex =>
                {
                    var value = (IList) _triProperty.GetValue(targetIndex);

                    if (value == mainValue)
                    {
                        return value;
                    }

                    var element = value[oldIndex];
                    for (var index = 0; index < value.Count - 1; ++index)
                    {
                        if (index >= oldIndex)
                        {
                            value[index] = value[index + 1];
                        }
                    }

                    for (var index = value.Count - 1; index > 0; --index)
                    {
                        if (index > newIndex)
                        {
                            value[index] = value[index - 1];
                        }
                    }

                    value[newIndex] = element;

                    return value;
                });
            }
            
            private TriElement CreateItemElement(TriProperty triProperty)
            {
                return new DictionaryRowElement(triProperty);
            }
            
            private bool GenerateChildren()
            {
                var count = _reorderableList.count;

                if (ChildrenCount == count)
                {
                    return false;
                }

                while (ChildrenCount < count)
                {
                    var property = _triProperty.ArrayElementProperties[ChildrenCount];
                    AddChild(CreateItemElement(property));
                }

                while (ChildrenCount > count)
                {
                    RemoveChildAt(ChildrenCount - 1);
                }

                return true;
            }

            private bool ClearChildren()
            {
                if (ChildrenCount == 0)
                {
                    return false;
                }

                RemoveAllChildren();

                return true;
            }
            
            private bool ReloadIfRequired()
            {
                if (!_reloadRequired &&
                    _triProperty.IsExpanded == _isExpanded &&
                    _triProperty.ArrayElementProperties.Count == _arraySize)
                {
                    return false;
                }

                _reloadRequired = false;
                _isExpanded = _triProperty.IsExpanded;
                _arraySize = _triProperty.ArrayElementProperties.Count;

                _dictionaryTreeView.Reload();

                return true;
            }
            
            private object CreateDefaultElementValue()
            {
                var canActivate = _triProperty.ArrayElementType.IsValueType ||
                                  _triProperty.ArrayElementType.GetConstructor(Type.EmptyTypes) != null;

                return canActivate ? Activator.CreateInstance(_triProperty.ArrayElementType) : null;
            }

            private Array CloneValue()
            {
                var list = (IList) _triProperty.Value;
                var template = Array.CreateInstance(_triProperty.ArrayElementType, list?.Count ?? 0);
                list?.CopyTo(template, 0);
                return template;
            }

            private void DisplayAddBlock(Rect position)
            {
                var keyRect = new Rect(position.xMin + EditorGUIUtility.standardVerticalSpacing, position.yMin + EditorGUIUtility.standardVerticalSpacing * 2, position.width - EditorGUIUtility.standardVerticalSpacing * 2, _keyTriElement.GetHeight(position.width));
                var valueRect = new Rect(keyRect.xMin, keyRect.yMax + EditorGUIUtility.standardVerticalSpacing, keyRect.width,_valueTriElement.GetHeight(position.width));
                var buttonDoneRect = new Rect(valueRect.xMin, valueRect.yMax + EditorGUIUtility.standardVerticalSpacing, valueRect.width/2, EditorGUIUtility.singleLineHeight);
                var buttonCancelRect = new Rect(buttonDoneRect.xMin + buttonDoneRect.width, valueRect.yMax + EditorGUIUtility.standardVerticalSpacing, valueRect.width/2, EditorGUIUtility.singleLineHeight);
                
                TriEditorGUI.DrawBox(position, TriEditorStyles.Box);

                _keyTriElement.OnGUI(keyRect);
                _valueTriElement.OnGUI(valueRect);

                GUI.enabled = _keyInstance != null && !((IDictionary)_triProperty.Parent.Value).Contains(_keyInstance);
                
                if (GUI.Button(buttonDoneRect, "Done"))
                {
                    ((IDictionary)_triProperty.Parent.Value).Add(_keyInstance, _valueInstance);

                    _keyInstance = default;
                    _valueInstance = default;
                }

                GUI.enabled = true;
                
                if (GUI.Button(buttonCancelRect, "Cancel"))
                {
                    _displayAddBlock = false;
                }
            }
        }

        private class DictionaryRowElement : TriPropertyCollectionBaseElement
        {
            public List<KeyValuePair<TriElement, GUIContent>> TriElements { get; }

            public DictionaryRowElement(TriProperty triProperty)
            {
                DeclareGroups(triProperty.ValueType);

                TriElements = new List<KeyValuePair<TriElement, GUIContent>>();

                if (triProperty.PropertyType == TriPropertyType.Generic)
                {
                    foreach (var childProperty in triProperty.ChildrenProperties)
                    {
                        var oldChildrenCount = ChildrenCount;
                        var props = new TriPropertyElement.Props
                        {
                            forceInline = true,
                        };
                        
                        AddProperty(childProperty, props, out var group);

                        if (oldChildrenCount != ChildrenCount)
                        {
                            var element = GetChild(ChildrenCount - 1);
                            var headerContent = new GUIContent(group ?? childProperty.DisplayName);

                            TriElements.Add(new KeyValuePair<TriElement, GUIContent>(element, headerContent));
                        }
                    }
                }
                else
                {
                    var element = new TriPropertyElement(triProperty, new TriPropertyElement.Props
                    {
                        forceInline = true,
                    });
                    var headerContent = new GUIContent("Element");

                    AddChild(element);
                    
                    TriElements.Add(new KeyValuePair<TriElement, GUIContent>(element, headerContent));
                }
            }
        }
        
        [Serializable]
        private class DictionaryTreeView : TreeView
        {
            private readonly TriProperty _triProperty;
            private readonly TriElement _triElement;
            private readonly ReorderableList _reorderableList;
            private readonly DictionaryTreeItemPropertyOverrideContext _dictionaryTreeItemPropertyOverrideContext;
            private readonly DictionaryTreeItemPropertyOverrideAvailability _dictionaryTreeItemPropertyOverrideAvailability;

            private bool _wasRendered;

            public Action<int> SelectionChangedCallback;

            public DictionaryTreeView(TriProperty triProperty, TriElement triElement, ReorderableList reorderableList)
                : base(new TreeViewState(), new DictionaryColumnHeader())
            {
                _triProperty = triProperty;
                _triElement = triElement;
                _reorderableList = reorderableList;
                _dictionaryTreeItemPropertyOverrideContext = new DictionaryTreeItemPropertyOverrideContext();
                _dictionaryTreeItemPropertyOverrideAvailability = new DictionaryTreeItemPropertyOverrideAvailability();

                showAlternatingRowBackgrounds = true;
                showBorder = false;
                useScrollView = false;

                multiColumnHeader.ResizeToFit();
                multiColumnHeader.visibleColumnsChanged += header => header.ResizeToFit();
            }

            public float Width { get; set; }

            public void RefreshHeight()
            {
                RefreshCustomRowHeights();
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                base.SelectionChanged(selectedIds);

                if (SelectionChangedCallback != null && selectedIds.Count == 1)
                {
                    SelectionChangedCallback.Invoke(selectedIds[0]);
                }
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem(0, -1, string.Empty);
                var columns = new List<MultiColumnHeaderState.Column>
                {
                    new MultiColumnHeaderState.Column
                    {
                        width = 16, autoResize = false, canSort = false, allowToggleVisibility = false,
                    },
                };

                if (_triProperty.IsExpanded)
                {
                    for (var index = 0; index < _triProperty.ArrayElementProperties.Count; index++)
                    {
                        var rowChildProperty = _triProperty.ArrayElementProperties[index];
                        
                        root.AddChild(new DictionaryTreeItem(index, rowChildProperty));

                        if (index == 0)
                        {
                            foreach (var kvp in ((DictionaryRowElement) (_triElement.GetChild(0))).TriElements)
                            {
                                columns.Add(new MultiColumnHeaderState.Column
                                {
                                    headerContent = kvp.Value,
                                    headerTextAlignment = TextAlignment.Center,
                                    autoResize = true,
                                    canSort = false,
                                });
                            }
                        }
                    }
                }

                if (root.children == null)
                {
                    root.AddChild(new DictionaryTreeEmptyItem());
                }

                if (multiColumnHeader.state == null ||
                    multiColumnHeader.state.columns.Length == 1)
                {
                    multiColumnHeader.state = new MultiColumnHeaderState(columns.ToArray());
                }

                return root;
            }

            protected override float GetCustomRowHeight(int row, TreeViewItem item)
            {
                if (item is DictionaryTreeEmptyItem)
                {
                    return EditorGUIUtility.singleLineHeight;
                }

                var height = 0f;
                var rowElement = (DictionaryRowElement) _triElement.GetChild(row);

                foreach (var visibleColumnIndex in multiColumnHeader.state.visibleColumns)
                {
                    var cellWidth = _wasRendered
                        ? multiColumnHeader.GetColumnRect(visibleColumnIndex).width
                        : Width / Mathf.Max(1, multiColumnHeader.state.visibleColumns.Length);

                    var cellHeight = visibleColumnIndex == 0
                        ? EditorGUIUtility.singleLineHeight
                        : rowElement.TriElements[visibleColumnIndex - 1].Key.GetHeight(cellWidth);

                    height = Math.Max(height, cellHeight);
                }

                return height + EditorGUIUtility.standardVerticalSpacing * 2;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (args.item is DictionaryTreeEmptyItem)
                {
                    base.RowGUI(args);
                    return;
                }

                var rowElement = (DictionaryRowElement) _triElement.GetChild(args.row);

                for (var i = 0; i < multiColumnHeader.state.visibleColumns.Length; i++)
                {
                    var visibleColumnIndex = multiColumnHeader.state.visibleColumns[i];
                    var rowIndex = args.row;

                    var cellRect = args.GetCellRect(i);
                    cellRect.yMin += EditorGUIUtility.standardVerticalSpacing;

                    if (visibleColumnIndex == 0)
                    {
                        ReorderableListProxy.defaultBehaviours.DrawElementDraggingHandle(cellRect, rowIndex,
                            _reorderableList.index == rowIndex, _reorderableList.index == rowIndex, _reorderableList.draggable);
                        continue;
                    }

                    var cellElement = rowElement.TriElements[visibleColumnIndex - 1].Key;
                    cellRect.height = cellElement.GetHeight(cellRect.width);

                    using (TriGuiHelper.PushLabelWidth(EditorGUIUtility.labelWidth / rowElement.ChildrenCount))
                    using (TriPropertyOverrideContext.BeginOverride(_dictionaryTreeItemPropertyOverrideContext))
                    using (TriPropertyOverrideAvailability.BeginOverride(_dictionaryTreeItemPropertyOverrideAvailability))
                    {
                        cellElement.OnGUI(cellRect);
                    }
                }

                _wasRendered = true;
            }
        }
        
        [Serializable]
        private class DictionaryColumnHeader : MultiColumnHeader
        {
            public DictionaryColumnHeader() : base(null)
            {
                canSort = false;
                height = DefaultGUI.minimumHeight;
            }
        }

        [Serializable]
        private class DictionaryTreeEmptyItem : TreeViewItem
        {
            public DictionaryTreeEmptyItem() : base(0, 0, "Table is Empty")
            {
            }
        }

        [Serializable]
        private class DictionaryTreeItem : TreeViewItem
        {
            public TriProperty TriProperty { get; }
            
            public DictionaryTreeItem(int id, TriProperty triProperty) : base(id, 0)
            {
                TriProperty = triProperty;
            }
        }

        private class DictionaryElementPropertyOverrideContext : TriPropertyOverrideContext
        {
            public override bool TryGetDisplayName(TriProperty property, out GUIContent displayName)
            {
                var showLabels = property.TryGetAttribute(out ListDrawerSettingsAttribute settings) &&
                                 settings.ShowElementLabels;

                if (!showLabels)
                {
                    displayName = GUIContent.none;
                    return true;
                }

                displayName = default;
                return false;
            }
        }
        
        private class DictionaryTreeItemPropertyOverrideContext : TriPropertyOverrideContext
        {
            public override bool TryGetDisplayName(TriProperty property, out GUIContent displayName)
            {
                displayName = GUIContent.none;
                return true;
            }
        }
        
        private class DictionaryTreeItemPropertyOverrideAvailability : TriPropertyOverrideAvailability
        {
            public override bool TryIsEnable(TriProperty property, out bool isEnable)
            {
                isEnable = !property.RawName.Equals("Key");

                return !isEnable;
            }
        }
    }
}
