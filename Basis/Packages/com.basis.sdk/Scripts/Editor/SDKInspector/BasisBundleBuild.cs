using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class BasisBundleBuild
{
    public static async Task<(bool, string)> GameObjectBundleBuild(BasisContentBase BasisContentBase, List<BuildTarget> Targets)
    {
        return await BuildBundle(BasisContentBase, Targets,
            (content, obj, hex, target) => BasisAssetBundlePipeline.BuildAssetBundle(content.gameObject, obj, hex, target));
    }

    public static async Task<(bool, string)> SceneBundleBuild(BasisContentBase BasisContentBase, List<BuildTarget> Targets)
    {
        return await BuildBundle(BasisContentBase, Targets,
            (content, obj, hex, target) => BasisAssetBundlePipeline.BuildAssetBundle(content.gameObject.scene, obj, hex, target));
    }
    public static async Task<(bool, string)> BuildBundle(
        BasisContentBase BasisContentBase,
        List<BuildTarget> Targets,
        Func<BasisContentBase, BasisAssetBundleObject, string, BuildTarget, Task<(bool, BasisBundleGenerated)>> buildFunction)
    {
        Debug.Log("Starting BuildBundle...");

        // Store the initial active build target
        BuildTarget originalActiveTarget = EditorUserBuildSettings.activeBuildTarget;

        if (ErrorChecking(BasisContentBase, out string Error) == false)
        {
            return (false, Error);
        }

        if (CheckIfWeCanBuild(Targets, out Error) == false)
        {
            return (false, Error);
        }

        Debug.Log("Passed error checking for BuildBundle...");

        // Ensure active build target is first in the list
        BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
        if (!Targets.Contains(activeTarget))
        {
            Debug.LogWarning($"Active build target {activeTarget} not in list of targets.");
        }
        else
        {
            // Move active build target to the front
            Targets.Remove(activeTarget);
            Targets.Insert(0, activeTarget);
        }

        BasisAssetBundleObject Objects = AssetDatabase.LoadAssetAtPath<BasisAssetBundleObject>(BasisAssetBundleObject.AssetBundleObject);

        Debug.Log("Generating random bytes for hex string...");
        byte[] randomBytes = GenerateRandomBytes(32);
        string hexString = ByteArrayToHexString(randomBytes);
        Debug.Log($"Generated hex string: {hexString}");

        Debug.Log("IL2CPP is installed. Proceeding to build asset bundle...");

        BasisBundleGenerated[] Bundles = new BasisBundleGenerated[Targets.Count];
        for (int Index = 0; Index < Targets.Count; Index++)
        {
            BuildTarget Target = Targets[Index];
            (bool success, BasisBundleGenerated bundle) = await buildFunction(BasisContentBase, Objects, hexString, Target);
            if (!success)
            {
                return (false, "Failure While Building for " + Target);
            }
            Bundles[Index] = bundle;
        }

        BasisBundleConnector BasisBundleConnector = new BasisBundleConnector(
            BasisGenerateUniqueID.GenerateUniqueID(),
            BasisContentBase.BasisBundleDescription,
            Bundles
        );

        Debug.Log("Successfully built asset bundle.");

        // Restore the original build target
        if (EditorUserBuildSettings.activeBuildTarget != originalActiveTarget)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildPipeline.GetBuildTargetGroup(originalActiveTarget),
                originalActiveTarget
            );
            Debug.Log($"Switched back to original build target: {originalActiveTarget}");
        }

        OpenRelativePath(Objects.AssetBundleDirectory);
        return (true, "Success");
    }
    public static string OpenRelativePath(string relativePath)
    {
        // Get the root path of the project (up to the Assets folder)
        string projectRoot = Application.dataPath.Replace("/Assets", "");

        // If the relative path starts with './', remove it
        if (relativePath.StartsWith("./"))
        {
            relativePath = relativePath.Substring(2); // Remove './'
        }

        // Combine the root with the relative path
        string fullPath = Path.Combine(projectRoot, relativePath);

        // Open the folder or file in explorer
        OpenFolderInExplorer(fullPath);
        return fullPath;
    }
    // Convert a Unity path to a Windows-compatible path and open it in File Explorer
    public static void OpenFolderInExplorer(string folderPath)
    {
        // Convert Unity-style file path (forward slashes) to Windows-style (backslashes)
        string windowsPath = folderPath.Replace("/", "\\");

        // Check if the path exists
        if (Directory.Exists(windowsPath) || File.Exists(windowsPath))
        {
            // On Windows, use 'explorer' to open the folder or highlight the file
            System.Diagnostics.Process.Start("explorer.exe", windowsPath);
        }
        else
        {
            Debug.LogError("Path does not exist: " + windowsPath);
        }
    }
    public static bool ErrorChecking(BasisContentBase BasisContentBase, out string Error)
    {
        Error = string.Empty; // Initialize the error variable

        if (string.IsNullOrEmpty(BasisContentBase.BasisBundleDescription.AssetBundleName))
        {
            Error = "Name was empty! Please provide a name in the field.";
            return false;
        }

        if (string.IsNullOrEmpty(BasisContentBase.BasisBundleDescription.AssetBundleDescription))
        {
            Error = "Description was empty! Please provide a description in the field.";
            return false;
        }

        return true;
    }

    public static bool CheckIfWeCanBuild(List<BuildTarget> Targets, out string Error)
    {
        for (int Index = 0; Index < Targets.Count; Index++)
        {
            BuildTarget item = Targets[Index];

            //   if (IsPlatformInstalled(item) == false)
            //  {
            //   Error = "Missing Platform for " + item + " please install from the Unity Hub, make sure to include IL2CPP";
            //    return false;
            //  }

            var playbackEndingDirectory = BuildPipeline.GetPlaybackEngineDirectory(item, BuildOptions.None, false);
            bool isInstalled = !string.IsNullOrEmpty(playbackEndingDirectory) && Directory.Exists(Path.Combine(playbackEndingDirectory, "Variations", "il2cpp"));

            if (isInstalled == false)
            {
                Error = "IL2CPP is NOT installed for platform " + item + " please add it from the unity hub!";
                return false;
            }
        }
        Error = string.Empty;
        return true;
    }

    static bool IsPlatformInstalled(BuildTarget target)
    {
        // Use Unity's method to check for the platform installation
        string playbackEnginePath = BuildPipeline.GetPlaybackEngineDirectory(target, BuildOptions.None, false);

        if (string.IsNullOrEmpty(playbackEnginePath))
        {
            return false; // If no path returned, platform isn't installed
        }

        // Special check for StandaloneWindows64 which might be named differently in the folder structure
        if (target == BuildTarget.StandaloneWindows64)
        {
            // Check if the "windows64" folder exists in the PlaybackEngines directory
            return Directory.Exists(Path.Combine(playbackEnginePath, "windows64"));
        }

        // For all other platforms, we rely on the normal method
        return Directory.Exists(playbackEnginePath);
    }
    // Generates a random byte array of specified length
    public static byte[] GenerateRandomBytes(int length)
    {
        Debug.Log($"Generating {length} random bytes...");
        byte[] randomBytes = new byte[length];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(randomBytes);
        }
        Debug.Log("Random bytes generated successfully.");
        return randomBytes;
    }

    // Converts a byte array to a Base64 encoded string
    public static string ByteArrayToBase64String(byte[] byteArray)
    {
        Debug.Log("Converting byte array to Base64 string...");
        return Convert.ToBase64String(byteArray);
    }

    // Converts a byte array to a hexadecimal string
    public static string ByteArrayToHexString(byte[] byteArray)
    {
        Debug.Log("Converting byte array to hexadecimal string...");
        StringBuilder hex = new StringBuilder(byteArray.Length * 2);
        foreach (byte b in byteArray)
        {
            hex.AppendFormat("{0:x2}", b);
        }
        Debug.Log("Hexadecimal string conversion successful.");
        return hex.ToString();
    }
}
