using HG.CompBindChef.Collections;
using HG.CompBindChef.Editor;
using HG.CompBindChef.Editor.Utils;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HG.CompBindChef.Editor;

public class CompBindChefSettings : ScriptableObject
{
    const string AssetFolder = "Assets/DataBinding";
    const string AssetFilename = "CompBindRecipes.asset";

    [field: SerializeField] public BindRecipeDictionary Recipes { get; private set; }

    internal static CompBindChefSettings GetOrCreate()
    {
        var settings = AssetDatabase.LoadAssetAtPath<CompBindChefSettings>(Path.Combine(AssetFolder, AssetFilename));
        if (ReferenceEquals(settings, null))
        {
            settings = CreateInstance<CompBindChefSettings>();
            AssetDatabaseEx.CreateMissingFolders(AssetFolder);
            AssetDatabase.CreateAsset(settings, Path.Combine(AssetFolder, AssetFilename));
            AssetDatabase.SaveAssets();
        }

        Debug.Assert(EditorUtility.IsPersistent(settings));
        return settings;
    }

    internal static SerializedObject GetSerializedObject() => new SerializedObject(GetOrCreate());
}

static class CompBindChefSettingsProvider
{
    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        return new SettingsProvider("Project/Data Binding", SettingsScope.Project)
        {
            label = "(HG) Data Binding",
            activateHandler = (_, root) =>
            {
                VisualElement container = new VisualElement();

                try
                {
                    SerializedObject so = CompBindChefSettings.GetSerializedObject();
                    container.Add(new Label("Hello world"));
                    root.Bind(so);
                }
                catch (System.Exception soException)
                {
                    container.Add(new HelpBox(soException.Message, HelpBoxMessageType.Error));
                }
                
                root.Add(container);
            },
            keywords = new string[] { "Recipes" }
        };
    }
}