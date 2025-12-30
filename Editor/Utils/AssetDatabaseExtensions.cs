using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace HG.CompBindChef.Editor.Utils;

public static class AssetDatabaseEx
{
    public static readonly char[] DirectorySeparatorChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    /// <summary>
    /// Given a directory (or asset) path, ensure all the folders found in the path exist.
    /// </summary>
    /// <param name="directoryPath">Path to create folders for, if any are missing</param>
    /// <returns>GUID of the final directory found in the given path.</returns>
    public static string CreateMissingFolders(string directoryPath)
    {
        // Assume root directory if directoryPath is null or empty
        if (string.IsNullOrWhiteSpace(directoryPath))
            return AssetDatabase.AssetPathToGUID("Assets");

        // Skip final asset name, this method only ensures folders exist
        if (Path.HasExtension(directoryPath))
            directoryPath = Path.GetDirectoryName(directoryPath);

        var directories = directoryPath.Split(DirectorySeparatorChars, StringSplitOptions.RemoveEmptyEntries).ToList();

        StringBuilder path = new StringBuilder("Assets");
        // Skip "Assets" if it's in the path; already included in path StringBuilder
        int i = directories[0] == "Assets" ? 1 : 0;
        string finalGuid = "";
        for (; i < directories.Count; i++)
        {
            var prevPath = path.ToString();
            path.Append(Path.DirectorySeparatorChar);
            path.Append(directories[i]);
            var nextPath = path.ToString();
            // Fast-forward while do not need to make directories
            if (string.IsNullOrEmpty(finalGuid) && AssetDatabase.IsValidFolder(nextPath))
                continue;

            finalGuid = AssetDatabase.CreateFolder(prevPath, directories[i]);
        }

        return finalGuid;
    }
}