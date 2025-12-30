using UnityEngine;
using UnityEditor;
using HG.CompBindChef.Bindings;

namespace HG.CompBindChef.Editor.Plugins
{
    [InitializeOnLoad]
    internal static class BindComponentMenuCommand
    {
        [MenuItem("CONTEXT/Component/Create Binding")]
        static void BindAnyComponent(MenuCommand command)
        {
            var component = command.context as Component;
            if (component == null)
            {
                Debug.LogWarning($"Create Binding failed: {command.context.GetType()} is not a target");
                return;
            }
            
            BindRecipeSource.AddTo(component);
        }
    }
}
