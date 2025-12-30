using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using HG.CompBindChef.Bindings;
using HG.CompBindChef.Utils;
using HG.CompBindChef.Editor.Utils;


namespace HG.CompBindChef.Editor.Bindings
{
    /// <summary>
    /// Encapsulates the process of accessing and modifying serialized properties on a Bind Recipe.
    /// </summary>
    public class TargetedBindRecipeEditorProxy
    {
        public static readonly GUIContent UnsetLabelContent = new GUIContent("Unset");
        public static readonly GUIContent PropertyTypeLabel = new GUIContent("Bound Property Type: ");
        public static readonly GUIContent PropertySetterLabel = new GUIContent("Setter");
        public static readonly GUIContent PropertyGetterLabel = new GUIContent("Getter");
        public static readonly GUIContent PropertyValueChangedEventLabel = new GUIContent("OnChanged Event");

        private SerializedProperty _root;
        private SerializedProperty _recipeRoot;
        
        public SerializedProperty SpComponentRef;
        public Component Component => (Component)SpComponentRef.objectReferenceValue;

        //public SerializedProperty SpDirtyFlag;
        
        public SerializedProperty SpComponentType;
        public SerializedProperty SpPropertyType;
        public GUIContent PropertyTypeContent => string.IsNullOrWhiteSpace(SpPropertyType.stringValue)
            ? UnsetLabelContent
            : new GUIContent(TypeNameConverter.AssemblyQualifiedNameToCodeReadyTypeString(SpPropertyType.stringValue));
        
        
        public SerializedProperty SpSetter;
        public (GUIContent catLabel, GUIContent propLabel) PropertySetterContent => GUIContentForStringProperty(SpSetter);
        
        public SerializedProperty SpGetter;
        public (GUIContent catLabel, GUIContent propLabel) PropertyGetterContent => GUIContentForStringProperty(SpGetter);
        
        public SerializedProperty SpChangedEvent;
        public (GUIContent catLabel, GUIContent propLabel) PropertyChangedEventContent => GUIContentForStringProperty(SpChangedEvent);

        private static (GUIContent catLabel, GUIContent propLabel) GUIContentForStringProperty(SerializedProperty property)
        {
            if (string.IsNullOrWhiteSpace(property.stringValue))
                return (GUIContent.none, UnsetLabelContent);
            var sections = property.stringValue.Split(':');
            var categoryLabel = sections.Length > 1 ? new GUIContent($"[{sections[0]}]") : GUIContent.none;
            return (categoryLabel, new GUIContent(sections[^1]));
        }
        
        public static TargetedBindRecipeEditorProxy Create(SerializedProperty property)
        {
            var recipeRoot = property.FindPropertyRelative(nameof(BindRecipeSource.TargetedBindRecipe.recipe));
            return new TargetedBindRecipeEditorProxy
            {
                _root = property,
                //SpDirtyFlag = property.FindPropertyRelative(nameof(BindRecipeSource.TargetedBindRecipe.isDirty)),
                SpComponentRef = property.FindPropertyRelative(nameof(BindRecipeSource.TargetedBindRecipe.targetRef)),
                _recipeRoot = recipeRoot,
                SpComponentType = recipeRoot.FindPropertyRelative(nameof(BindRecipe.componentType)),
                SpPropertyType = recipeRoot.FindPropertyRelative(nameof(BindRecipe.propertyType)),
                SpGetter = recipeRoot.FindPropertyRelative(nameof(BindRecipe.propGetter)),
                SpSetter = recipeRoot.FindPropertyRelative(nameof(BindRecipe.propSetter)),
                SpChangedEvent = recipeRoot.FindPropertyRelative(nameof(BindRecipe.propChangedEvent)),
            };
        }

        public void ApplyValuesFromOtherViewProperty(BindRecipe bindRecipe)
        {
            Debug.Log("ApplyValuesFromOtherViewProperty: " + bindRecipe.ToString());
            SpComponentType.stringValue = bindRecipe.componentType;
            SpPropertyType.stringValue = bindRecipe.propertyType;
            SpSetter.stringValue = bindRecipe.propSetter;
            SpGetter.stringValue = bindRecipe.propGetter;
            SpChangedEvent.stringValue = bindRecipe.propChangedEvent;
            //SpDirtyFlag.boolValue = true;
        }
        
        public class MemberChoice
        {
            public static MemberChoice FromInfo(FieldInfo info)
            {
                return new MemberChoice
                {
                    Name = info.Name,
                    Category = MemberCategory.Field,
                    Type = info.FieldType
                };
            }
            
            public static MemberChoice FromInfo(PropertyInfo info)
            {
                var category = info.PropertyType.IsSubclassOf(typeof(UnityEventBase))
                    ? MemberCategory.UnityEvent
                    : info.MemberType.HasFlag(MemberTypes.Event)
                    ? MemberCategory.Event
                    : MemberCategory.Property;
                
                return new MemberChoice
                {
                    Name = info.Name,
                    Category = category,
                    Type = info.PropertyType
                };
            }
            
            public static MemberChoice FromInfo(MethodInfo info)
            {
                var parameters = info.GetParameters();
                var category = parameters.Length > 0
                               && !parameters[0].HasDefaultValue
                    ? MemberCategory.Setter
                    : MemberCategory.Getter;
                var type = category is MemberCategory.Setter 
                    ? parameters[0].ParameterType 
                    : info.ReturnType;
                
                return new MemberChoice
                {
                    Name = info.Name,
                    Category = category,
                    Type = type
                };
            }
            
            // Equivalent to "recipe type" for fields and properties.
            // Should only be different for events
            public Type Type;
            public MemberCategory Category;
            public string Name;
        }
        
        public void ChooseSetterAndValueType(object value)
        {
            if (value is MemberChoice choice && !string.IsNullOrWhiteSpace(choice.Name))
            {
                SpComponentType.stringValue = Component.GetType().AssemblyQualifiedName;
                Undo.RecordObject(_root.serializedObject.targetObject, "Choose Setter and Value Type");
                if (choice.Type.AssemblyQualifiedName != SpPropertyType.stringValue)
                {
                    SpGetter.stringValue = "";
                    SpChangedEvent.stringValue = "";
                }
                SpSetter.stringValue = $"{choice.Category}:{choice.Name}";
                SpPropertyType.stringValue = choice.Type.AssemblyQualifiedName;
                //SpDirtyFlag.boolValue = true;
                _root.serializedObject.ApplyModifiedProperties();
                return;
            }

            Undo.RecordObject(_root.serializedObject.targetObject, "Clear Setter choice");
            SpPropertyType.stringValue = "";
            SpSetter.stringValue = "";
            SpGetter.stringValue = "";
            SpChangedEvent.stringValue = "";
            //SpDirtyFlag.boolValue = true;
            _root.serializedObject.ApplyModifiedProperties();
        }

        public void ChooseGetter(object value)
        {
            if (value is MemberChoice choice && !string.IsNullOrWhiteSpace(choice.Name))
            {
                Undo.RecordObject(_root.serializedObject.targetObject, "Choose Getter");
                SpGetter.stringValue = $"{choice.Category}:{choice.Name}";
                Debug.Assert(SpPropertyType.stringValue == choice.Type.AssemblyQualifiedName, "Bound recipe name should be same as name found for getter");
                //SpDirtyFlag.boolValue = true;
                _root.serializedObject.ApplyModifiedProperties();
                return;
            }

            Undo.RecordObject(_root.serializedObject.targetObject, "Clear Getter choice");
            SpGetter.stringValue = "";
            //SpDirtyFlag.boolValue = true;
            _root.serializedObject.ApplyModifiedProperties();
        }


        public void ChooseOnChangedEvent(object value)
        {
            if (value is MemberChoice choice && !string.IsNullOrWhiteSpace(choice.Name))
            {
                Undo.RecordObject(_root.serializedObject.targetObject, "Choose OnChangedEvent");
                SpChangedEvent.stringValue = $"{choice.Category}:{choice.Name}";
                //SpDirtyFlag.boolValue = true;
                _root.serializedObject.ApplyModifiedProperties();
                return;
            }
            
            Undo.RecordObject(_root.serializedObject.targetObject, "Clear OnChangedEvent choice");
            SpChangedEvent.stringValue = "";
            //SpDirtyFlag.boolValue = true;
            _root.serializedObject.ApplyModifiedProperties();
        }
    }
}