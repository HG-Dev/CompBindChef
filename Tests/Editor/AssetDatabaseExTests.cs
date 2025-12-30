using HG.CompBindChef.Editor.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

[TestFixture]
public class AssetDatabaseExTests
{
    [SetUp]
    public void EnsureDeleteMeDirectoryDoesNotExist()
    {
        if (AssetDatabase.IsValidFolder("Assets/DELETEME"))
            throw new UnityException("DELETEME directory will be deleted if this test is run. Delete it or rename it before running tests.");
    }

    [TearDown]
    public void RemoveDeleteMeDirectory()
    {
        AssetDatabase.DeleteAsset("Assets/DELETEME");
    }

    [TestCase("Assets/DELETEME/", TestName = "Single directory ending with delimiter")]
    [TestCase("Assets/DELETEME/D1/D2", TestName = "Valid three-deep directory")]
    [TestCase("Assets/DELETEME/MyAsset.asset", TestName = "Asset path instead of directory path")]
    [TestCase("DELETEME/Inner/MyAsset.asset", TestName = "Asset path, no Assets")]
    [TestCase("DELETEME", TestName = "Single directory, does not start with Assets")]
    [TestCase("", TestName = "Empty path (Assets)")]
    [Description("Create a series of folders under Assets from a given path.")]
    public void CreateMissingFolders_ShouldCreateValidEndDirectory(string path)
    {
        TestContext.Out.WriteLine(path);
        var guid = AssetDatabaseEx.CreateMissingFolders(path);
        Assert.That(guid, Is.Not.Null);
    }
}
