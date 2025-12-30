using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using HG.CompBindChef.Bindings;
using HG.CompBindChef.Editor.Utils;

namespace HG.CompBindChef.Editor.Bindings
{
    [CustomPropertyDrawer(typeof(BindRecipeSource.TargetedBindRecipe))]
    public class TargetedBindRecipeDrawer : PropertyDrawer
    {
        public const float BLOCK_MARGIN_Y = 4;
        // Component target;
        // BindRecipe recipe;
        //     // string componentType;
        //     // string propertyType;
        //     // string propGetter;
        //     // string propSetter;
        //     // string propChangedEvtAdd;
        //     // string propChangedEvtRemove;
        
        public static readonly float Height = EditorGUIUtility.singleLineHeight * 5 + BLOCK_MARGIN_Y * 6;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Height;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent removeButtonContent)
        {
            EditorGUI.BeginChangeCheck();
            
            var props = TargetedBindRecipeEditorProxy.Create(property);
            
            var memberLabelWidth = GUI.skin.button.CalcSize(TargetedBindRecipeEditorProxy.PropertyValueChangedEventLabel).x;
            var categoryLabelWidth = GUI.skin.button.CalcSize(new GUIContent("UnityEvent")).x;
            
            var objectRect = new Rect(rect.x + 8, rect.y + BLOCK_MARGIN_Y, 
                rect.width - 16, EditorGUIUtility.singleLineHeight);
            var memberLabelRect = new Rect(objectRect) {width = memberLabelWidth};
            var categoryLabelRect = new Rect(objectRect)
                { width = categoryLabelWidth, x = objectRect.x + memberLabelWidth };
            var propRects = new Rect[4];
            for (var i = 0; i < propRects.Length; i++)
            {
                var nextRect = new Rect(objectRect);
                var indent = (memberLabelWidth + categoryLabelWidth) * Mathf.Clamp01(i);
                nextRect.x += indent;
                nextRect.width -= indent;
                nextRect.y += (EditorGUIUtility.singleLineHeight + BLOCK_MARGIN_Y) * (i + 1);
                propRects[i] = nextRect;
            }

            var noTargetSet = props.SpComponentRef.objectReferenceValue == null;
            var noTypeSet = string.IsNullOrWhiteSpace(props.SpPropertyType.stringValue);
            
            EditorGUI.HelpBox(propRects[0], TargetedBindRecipeEditorProxy.PropertyTypeLabel.text + props.PropertyTypeContent.text,
                noTypeSet ? MessageType.Warning : MessageType.Info);
            
            using (new EditorGUI.DisabledGroupScope(true))
                EditorGUI.PropertyField(objectRect, props.SpComponentRef, GUIContent.none);
            
            using (new EditorGUI.DisabledGroupScope(noTargetSet))
            {
                var spawnRect = propRects[1];
                var content = props.PropertySetterContent;
                EditorGUI.LabelField(new Rect(memberLabelRect) { y = spawnRect.y }, TargetedBindRecipeEditorProxy.PropertySetterLabel);
                EditorGUI.LabelField(new Rect(categoryLabelRect) { y = spawnRect.y }, content.catLabel, EditorStyles.centeredGreyMiniLabel);
                if (EditorGUI.DropdownButton(spawnRect, content.propLabel, 
                        FocusType.Passive, EditorStyles.popup))
                {
                    CreateDropdownMenu(props, props.Component.GetType().ExtractPublicMemberInfo(), 
                        MemberCategory.Setter).DropDown(spawnRect);
                }

                using (new EditorGUI.DisabledScope(noTypeSet))
                {
                    spawnRect = propRects[2];
                    content = props.PropertyGetterContent;
                    EditorGUI.LabelField(new Rect(memberLabelRect) { y = spawnRect.y }, TargetedBindRecipeEditorProxy.PropertyGetterLabel);
                    EditorGUI.LabelField(new Rect(categoryLabelRect) { y = spawnRect.y }, content.catLabel, EditorStyles.centeredGreyMiniLabel);
                    if (EditorGUI.DropdownButton(propRects[2], content.propLabel, 
                            FocusType.Passive, EditorStyles.popup))
                    {
                        CreateDropdownMenu(props, props.Component.GetType().ExtractPublicMemberInfo(), 
                            MemberCategory.Getter).DropDown(spawnRect);
                    }

                    spawnRect = propRects[3];
                    content = props.PropertyChangedEventContent;
                    EditorGUI.LabelField(new Rect(memberLabelRect) { y = spawnRect.y }, TargetedBindRecipeEditorProxy.PropertyValueChangedEventLabel);
                    EditorGUI.LabelField(new Rect(categoryLabelRect) { y = spawnRect.y }, content.catLabel, EditorStyles.centeredGreyMiniLabel);
                    if (EditorGUI.DropdownButton(propRects[3], content.propLabel, 
                            FocusType.Passive, EditorStyles.popup))
                    {
                        CreateDropdownMenu(props, props.Component.GetType().ExtractPublicMemberInfo(), 
                            MemberCategory.Event).DropDown(spawnRect);
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Debug.Log("Changes detected!");
            }
        }
        
        private static Func<Type, bool> CreateEventChecker(string allowedTypeAssemblyQualifiedName)
        {
            if (allowedTypeAssemblyQualifiedName != null)
            {
                return (type) =>
                {
                    if (type.IsSubclassOf(typeof(UnityEventBase)))
                    {
                        var addListenerMethod = type.GetMethod(nameof(UnityEvent.AddListener));
                        var unityActionType = addListenerMethod?.GetParameters().FirstOrDefault()?.ParameterType;
                        if (unityActionType == null)
                            return false;
                        var paramType = unityActionType.GenericTypeArguments.FirstOrDefault();
                        Debug.Assert(paramType != null,
                            "UnityEvent appears to have no generic type argument. Was looking for " + allowedTypeAssemblyQualifiedName);
                        return paramType.AssemblyQualifiedName == allowedTypeAssemblyQualifiedName;
                    }
                    
                    if (type.MemberType.HasFlag(MemberTypes.Event))
                    {
                        var paramType = type.GenericTypeArguments.FirstOrDefault();
                        return paramType?.AssemblyQualifiedName == allowedTypeAssemblyQualifiedName;
                    }

                    return false;
                };
            }

            throw new NotImplementedException();
        }
        
        private static Func<Type, bool> CreateNonEventChecker(string allowedTypeAssemblyQualifiedName = null)
        {
            return (type) => (allowedTypeAssemblyQualifiedName == null || type.AssemblyQualifiedName == allowedTypeAssemblyQualifiedName) 
                             && !type.IsSubclassOf(typeof(UnityEventBase)) && !type.MemberType.HasFlag(MemberTypes.Event);
        }
        
        public GenericMenu CreateDropdownMenu(TargetedBindRecipeEditorProxy props, ReflectionExtensions.PublicMembers members, MemberCategory category)
        {
            var menu = new GenericMenu();

            UnityEditor.GenericMenu.MenuFunction2 choiceDelegate = category switch
            {
                MemberCategory.Setter => props.ChooseSetterAndValueType,
                MemberCategory.Getter => props.ChooseGetter,
                MemberCategory.Event => props.ChooseOnChangedEvent,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, 
                    "Accepted values: setter, getter, event")
            };
            
            menu.AddItem(new GUIContent("Unset"), string.IsNullOrWhiteSpace(props.SpPropertyType.stringValue), choiceDelegate, null);

            if (props.SpComponentRef.objectReferenceValue == (UnityEngine.Object)null)
                return menu;

            menu.AddSeparator("");
            
            Func<Type, bool> validTypeDelegate = category is MemberCategory.Event 
                ? CreateEventChecker(props.SpPropertyType.stringValue)
                : category is MemberCategory.Setter
                    ? CreateNonEventChecker()
                    : CreateNonEventChecker(props.SpPropertyType.stringValue);

            StringBuilder typeDisplayLabel = new StringBuilder();

            foreach (var valueType in members.Types.Where(validTypeDelegate))
            {
                typeDisplayLabel.Clear();
                typeDisplayLabel.Append(valueType.Name.Split('`').First());

                if (valueType.GenericTypeArguments.Any())
                {
                    typeDisplayLabel.Append('<');
                    typeDisplayLabel.AppendJoin(',', valueType.GenericTypeArguments.Select(t => t.Name));
                    typeDisplayLabel.Append('>');
                }
                if (valueType.HasElementType)
                {
                    typeDisplayLabel.Append('<');
                    typeDisplayLabel.Append(valueType.GetElementType()!.Name);
                    typeDisplayLabel.Append('>');
                }
                if (valueType.IsArray)
                {
                    typeDisplayLabel.Append("(Array)");
                }
                
                if (members.Fields.TryGetValue(valueType, out var fields))
                {
                    menu.AddSeparator($"{typeDisplayLabel}/Fields");
                    foreach (var field in fields)
                        menu.AddItem(new GUIContent($"{typeDisplayLabel}/{field.Name}"),
                            props.SpSetter.stringValue == field.Name,
                            choiceDelegate, TargetedBindRecipeEditorProxy.MemberChoice.FromInfo(field));
                }
                if (members.Properties.TryGetValue(valueType, out var properties))
                {
                    menu.AddSeparator($"{typeDisplayLabel}/Properties");
                    foreach (var property in properties)
                        menu.AddItem(new GUIContent($"{typeDisplayLabel}/{property.Name}"), props.SpSetter.stringValue == property.Name, 
                            choiceDelegate, TargetedBindRecipeEditorProxy.MemberChoice.FromInfo(property));
                }

                if (category is MemberCategory.Event)
                    continue;
                var methodsByType = category is MemberCategory.Setter 
                    ? members.SetterMethods 
                    : members.GetterMethods;
                if (methodsByType.TryGetValue(valueType, out var methods))
                {
                    menu.AddSeparator($"{typeDisplayLabel}/Setter Methods");
                    foreach (var method in methods)
                        menu.AddItem(new GUIContent($"{typeDisplayLabel}/{method.Name}"), props.SpSetter.stringValue == method.Name + "()", 
                            choiceDelegate, TargetedBindRecipeEditorProxy.MemberChoice.FromInfo(method));
                }
            }

            return menu;
        }
    }
}