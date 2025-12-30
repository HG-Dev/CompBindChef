using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using HG.CompBindChef.Bindings;
// ReSharper disable PossibleMultipleEnumeration

namespace HG.CompBindChef.Editor.Bindings
{
    /// <summary>
    /// Extracts information serialized on a BindRecipeSource for centralization on a BindRecipeStore.
    /// </summary>
    [CustomEditor(typeof(BindRecipeSource))]
    public class BindCreatorEditor : UnityEditor.Editor
    {
        SerializedProperty _sources;
        SerializedProperty _bindCodes;
        
        GUID _behaviourGuid;
        bool _canSaveToStore;
        BindRecipeSource _bindRecipeSourceScript;

        static readonly GUIContent AddButtonContent = EditorGUIUtility.TrTextContent("Add New Bind Source", 
            "Create a set of member accessors for a local target that can be compiled to modify a recipe remotely.");
        static readonly GUIContent SourcesHeaderContent = new GUIContent("Binding Sources");

        static readonly float RemoveButtonWidth = EditorGUIUtility.singleLineHeight * 2.5f;
        
        static GUIContent RemoveButtonContent;
        //private static Vector2 RemoveButtonSize;

        const float GUI_ADD_BUTTON_WIDTH = 200f;

        
        private void OnEnable()
        {
            RemoveButtonContent ??= new GUIContent(EditorGUIUtility.IconContent("Toolbar Minus"))
            {
                tooltip = "Remove this bind source."
            };
            
            _sources = serializedObject.FindProperty(BindRecipeSource.ViewPropertySourceListFieldName);
            _bindCodes = serializedObject.FindProperty(BindRecipeSource.BindArrayFieldName);
            _bindRecipeSourceScript = (BindRecipeSource)serializedObject.targetObject;
            _canSaveToStore = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj: serializedObject.targetObject,
                guid: out string behaviourGuidString, localId: out _) && GUID.TryParse(behaviourGuidString, out _behaviourGuid);

            //enabled = (_sources?.isArray ?? false) && (_bindCodes?.isArray ?? false);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            RenderListInspector();
            RenderAddButton();

            if (serializedObject.ApplyModifiedProperties())
            {
                //_bindCodes.arraySize = 
                //_bindRecipeSourceScript.TouchStoreReferences();
                //Debug.Log("Touch store references");
            }
        }

        private void RenderListInspector()
        {
            //anyDirty = false;
            int removalIndex = -1;
            // Display each TargetedBindRecipe item
            for (int i = 0; i < _sources.arraySize; i++)
            {
                var viewProp = _sources.GetArrayElementAtIndex(i);
                var compType = viewProp.FindPropertyRelative(nameof(BindRecipeSource.TargetedBindRecipe.targetRef)).objectReferenceValue;
                //var dirtyProp = viewProp.FindPropertyRelative(nameof(BindRecipeSource.TargetedBindRecipe.isDirty));
                //anyDirty |= dirtyProp.boolValue;
                //dirtyProp.boolValue = false;
                
                using (_ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (_ = new EditorGUILayout.HorizontalScope())
                    {
                        var colorStripeRect = EditorGUILayout.GetControlRect(GUILayout.Width(8));
                        colorStripeRect.height = TargetedBindRecipeDrawer.Height - TargetedBindRecipeDrawer.BLOCK_MARGIN_Y;
                        colorStripeRect.y += TargetedBindRecipeDrawer.BLOCK_MARGIN_Y;

                        GUI.Box(colorStripeRect, GUIContent.none);
                        EditorGUILayout.PropertyField(viewProp, SourcesHeaderContent);
                        
                        var removeButtonRect = EditorGUILayout.GetControlRect(GUILayout.Width(RemoveButtonWidth + 8), GUILayout.Height(RemoveButtonWidth));
                        removeButtonRect.y += TargetedBindRecipeDrawer.BLOCK_MARGIN_Y;
                        removeButtonRect.width -= 8;
                        if (GUI.Button(removeButtonRect, RemoveButtonContent ?? GUIContent.none, EditorStyles.miniButton))
                        {
                            removalIndex = i;
                        }
                    }
                }
            }

            if (removalIndex >= 0)
            {
                _sources.DeleteArrayElementAtIndex(removalIndex);
            }
        }

        private void RenderAddButton()
        {
            Rect addButtonRect = GUILayoutUtility.GetRect(AddButtonContent, GUI.skin.button);
            addButtonRect.x += (addButtonRect.width - GUI_ADD_BUTTON_WIDTH) * 0.5f;
            addButtonRect.width = GUI_ADD_BUTTON_WIDTH;
            if (GUI.Button(addButtonRect, AddButtonContent))
            {
                ShowAddBindSourceMenu(addButtonRect);
            }
        }

        /// <summary>
        /// Create a TargetedBindRecipe from local components (not including any BindRecipeSource),
        /// or ViewPropertySources specific to GameObject / transform.
        /// </summary>
        private void ShowAddBindSourceMenu(Rect position)
        {
            GenericMenu menu = new GenericMenu();
            var components = _bindRecipeSourceScript.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var compType = components[i].GetType();
                if (typeof(BindRecipeSource).IsAssignableFrom(components[i].GetType()))
                    continue; // Don't bind to BindCreators
                var label = new GUIContent(compType.Name);
                menu.AddItem(label, false, PickComponentForNewBindSource, components[i]);
            }
            menu.DropDown(position);
        }

        private void PickComponentForNewBindSource(object boxedComponent)
        {
            var bindsInUse = _bindRecipeSourceScript.BindCodes;
            var serializedPropertyBindSource = GrowArray(_sources);
            var props = TargetedBindRecipeEditorProxy.Create(serializedPropertyBindSource);
            props.ChooseSetterAndValueType(null);

            props.SpComponentRef.objectReferenceValue = (UnityEngine.Component)boxedComponent;
            props.SpComponentType.stringValue = boxedComponent.GetType().AssemblyQualifiedName;
            serializedPropertyBindSource.serializedObject.ApplyModifiedProperties();

            return;
            /* TODO: Re-enable popular recipe suggestion
            if (!_bindRecipeStore)
                return;
            
            var compType = boxedComponent.GetType().AssemblyQualifiedName;
            var mostPopular =
                _bindRecipeStore.BindRecipes.ItemsWithComponentType(compType)
                    .Where(pair => !bindsInUse.Contains(pair.Key))
                    .OrderByDescending(pair => _bindRecipeStore.TemporaryRecipeCache
                        .TryGetValue(pair.Key, out var set) ? set.Count : 1)
                    .FirstOrDefault();
            
            if (_bindRecipeStore.BindRecipes.TryGetValue(mostPopular.Key, out var recipe))
            {
                props.ApplyValuesFromOtherViewProperty(recipe);
            }

            serializedPropertyBindSource.serializedObject.ApplyModifiedProperties();
            _bindRecipeSourceScript.TouchStoreReferences();*/
        }

        private static SerializedProperty GrowArray(SerializedProperty arrayProperty, bool onlyToInitialize = false)
        {
            if (!arrayProperty.isArray)
                return null;

            if (arrayProperty.arraySize < 1)
                arrayProperty.InsertArrayElementAtIndex(0);
            else if (!onlyToInitialize)
                arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);

            return arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);
        }
    }
}