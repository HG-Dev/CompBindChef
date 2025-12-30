#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace HG.CompBindChef.Bindings
{
    [ExecuteAlways]
    public sealed partial class BindRecipeSource : MonoBehaviour
    {
        /// <summary>
        /// Ties a specific component on a GameObject to a bind recipe object.
        /// </summary>
        [Serializable]
        public struct TargetedBindRecipe
        {
            public Component targetRef;
            public BindRecipe recipe;
        }
        
        [SerializeField] List<TargetedBindRecipe> bindRecipes = new();

        public static string ViewPropertySourceListFieldName => nameof(bindRecipes);
        public IReadOnlyList<TargetedBindRecipe> TargetedBindRecipes => bindRecipes;


        public static string BindArrayFieldName => nameof(BindRecipeSource.bindCodes);

        public void OnDestroy()
        {
            bindRecipes?.Clear();
        }
        
        public static BindRecipeSource AddTo([NotNull] Component component)
        {
            component.gameObject.TryGetComponent<BindRecipeSource>(out var bindCreator);
            
            if (!bindCreator)
                bindCreator = Undo.AddComponent<BindRecipeSource>(component.gameObject);
            
            Debug.Assert(bindCreator, "Failed to add BindRecipeSource to GameObject");

            return bindCreator;
        }
        
        [ContextMenu("Print recipes")]
        private void PrintRecipeInformation()
        {
            foreach (var pair in TargetedBindRecipes)
            {
                Debug.Log($"Component: {pair.targetRef.GetType().Name}  Recipe: {pair.recipe}\n{pair.recipe.componentType}");
            }
        }
    }
}
#endif