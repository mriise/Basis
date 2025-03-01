using BasisSerializer.OdinSerializer;
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
        return await BuildBundle(BasisContentBase, Targets, (content, obj, hex, target) => BasisAssetBundlePipeline.BuildAssetBundle(content.gameObject, obj, hex, target));
    }

    public static async Task<(bool, string)> SceneBundleBuild(BasisContentBase BasisContentBase, List<BuildTarget> Targets)
    {
        return await BuildBundle(BasisContentBase, Targets,(content, obj, hex, target) => BasisAssetBundlePipeline.BuildAssetBundle(content.gameObject.scene, obj, hex, target));
    }
    public static async Task<(bool, string)> BuildBundle(BasisContentBase basisContentBase, List<BuildTarget> targets, Func<BasisContentBase, BasisAssetBundleObject, string, BuildTarget, Task<(bool, (BasisBundleGenerated, AssetBundleBuilder.InformationHash))>> buildFunction)
    {
        Debug.Log("Starting BuildBundle...");
        EditorUtility.DisplayProgressBar("Starting Bundle Build", "Starting Bundle Build", 0);
        // Store the original active build target
        BuildTarget originalActiveTarget = EditorUserBuildSettings.activeBuildTarget;

        // Perform error checking
        if (!ErrorChecking(basisContentBase, out string error))
        {
            return (false, error);
        }

        Debug.Log("Passed error checking for BuildBundle...");

        // Ensure active build target is the first in the list
        AdjustBuildTargetOrder(targets);

        // Load asset bundle object and delete existing directory
        BasisAssetBundleObject assetBundleObject = AssetDatabase.LoadAssetAtPath<BasisAssetBundleObject>(BasisAssetBundleObject.AssetBundleObject);
        ClearAssetBundleDirectory(assetBundleObject.AssetBundleDirectory);

        // Generate random bytes for hex string
        string hexString = GenerateHexString(32);

        BasisBundleGenerated[] bundles = new BasisBundleGenerated[targets.Count];
        List<string> paths = new List<string>();
        int cachedcount = targets.Count;
        for (int i = 0; i < cachedcount; i++)
        {
            BuildTarget target = targets[i];
            var (success, result) = await buildFunction(basisContentBase, assetBundleObject, hexString, target);
            if (!success)
            {
                return (false, $"Failure While Building for {target}");
            }

            bundles[i] = result.Item1;
            string hashPath = PathConversion(result.Item2.EncyptedPath);
            paths.Add(hashPath);

            BasisDebug.Log("Adding " + result.Item2.EncyptedPath);
        }
        EditorUtility.DisplayProgressBar("Starting Bundle Build", "Starting Bundle Build", 10);
        // Generate a unique ID and prepare for saving
        string generatedID = BasisGenerateUniqueID.GenerateUniqueID();
        BasisBundleConnector basisBundleConnector = new BasisBundleConnector(generatedID, basisContentBase.BasisBundleDescription, bundles);
        byte[] BasisbundleconnectorUnEncrypted = BasisSerializer.OdinSerializer.SerializationUtility.SerializeValue<BasisBundleConnector>(basisBundleConnector, DataFormat.JSON);
        var BasisPassword = new BasisEncryptionWrapper.BasisPassword
        {
            VP = hexString
        };
        string UniqueID = BasisGenerateUniqueID.GenerateUniqueID();
        BasisProgressReport report = new BasisProgressReport();
        byte[] EncryptedConnector = await BasisEncryptionWrapper.EncryptDataAsync(UniqueID, BasisbundleconnectorUnEncrypted, BasisPassword, report);
        EditorUtility.DisplayProgressBar("Starting Bundle Combining", "Starting Bundle Combining", 100);
        await CombineFiles(Path.Combine(assetBundleObject.AssetBundleDirectory, $"{generatedID}{assetBundleObject.BasisEncryptedExtension}"), paths, EncryptedConnector);
        await AssetBundleBuilder.SaveFileAsync(assetBundleObject.AssetBundleDirectory, assetBundleObject.ProtectedPasswordFileName, "txt", hexString);

        // Clean up
        DeleteFolders(assetBundleObject.AssetBundleDirectory);
        OpenRelativePath(assetBundleObject.AssetBundleDirectory);

        // Restore the original build target if necessary
        RestoreOriginalBuildTarget(originalActiveTarget);

        Debug.Log("Successfully built asset bundle.");
        EditorUtility.ClearProgressBar();
        return (true, "Success");
    }

    private static void AdjustBuildTargetOrder(List<BuildTarget> targets)
    {
        BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
        if (!targets.Contains(activeTarget))
        {
            Debug.LogWarning($"Active build target {activeTarget} not in list of targets.");
        }
        else
        {
            targets.Remove(activeTarget);
            targets.Insert(0, activeTarget);
        }
    }

    private static void ClearAssetBundleDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }

    private static string GenerateHexString(int length)
    {
        byte[] randomBytes = GenerateRandomBytes(length);
        return ByteArrayToHexString(randomBytes);
    }

    private static void RestoreOriginalBuildTarget(BuildTarget originalTarget)
    {
        if (EditorUserBuildSettings.activeBuildTarget != originalTarget)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(originalTarget), originalTarget);
            Debug.Log($"Switched back to original build target: {originalTarget}");
        }
    }

    public static async Task CombineFiles(string outputPath, List<string> bundlePaths, byte[] EncryptedConnector, int bufferSize = 81920)
    {
        BasisDebug.Log("Combining files: " + bundlePaths.Count);

        try
        {
            long headerLength = EncryptedConnector.Length;
            using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
            {
                byte[] buffer = new byte[bufferSize];
                int totalFiles = bundlePaths.Count;

                // Write header length and EncryptedConnector to the output stream
                await outputStream.WriteAsync(BitConverter.GetBytes(headerLength), 0, 8);
                await outputStream.WriteAsync(EncryptedConnector, 0, EncryptedConnector.Length);

                for (int i = 0; i < totalFiles; i++)
                {
                    string path = bundlePaths[i];

                    if (!File.Exists(path))
                    {
                        BasisDebug.LogError($"File not found: {path}");
                        throw new FileNotFoundException($"ERROR File not found: {path}");
                    }

                    // Update the progress bar
                    float progress = (float)i / totalFiles;
                    EditorUtility.DisplayProgressBar("Combining Files", $"Processing: {Path.GetFileName(path)}", progress);

                    BasisDebug.Log("Combining " + path);
                    // Append file content to output file
                    await AppendFileToOutput(path, buffer, outputStream, bufferSize);
                }
            }

            BasisDebug.Log($"Files combined successfully into: {outputPath}");
        }
        catch (Exception ex)
        {
            BasisDebug.LogError($"Error combining files: {ex.Message}");
            EditorUtility.ClearProgressBar();
        }
    }
    private static async Task AppendFileToOutput(string path, byte[] buffer, FileStream outputStream, int bufferSize = 4 * 1024 * 1024)
    {
        using (FileStream inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous))
        {
            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await outputStream.WriteAsync(buffer, 0, bytesRead);
            }
        }
    }
    public static string PathConversion(string relativePath)
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
        return fullPath;
    }
    static void DeleteFolders(string parentDir)
    {
        if (!Directory.Exists(parentDir))
        {
            BasisDebug.Log("Directory does not exist.");
            return;
        }

        foreach (string subDir in Directory.GetDirectories(parentDir))
        {
            try
            {
                /*
                foreach (string file in Directory.GetFiles(subDir))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(parentDir, fileName);

                    // Ensure unique filenames
                    destFile = GetUniqueFileName(destFile);

                    File.Move(file, destFile);
                    BasisDebug.Log($"Moved: {file} -> {destFile}");
                }
                */

                // Delete the empty folder
                Directory.Delete(subDir, true);
                BasisDebug.Log($"Deleted folder: {subDir}");
            }
            catch (Exception ex)
            {
                BasisDebug.LogError($"Error processing {subDir}: {ex.Message}");
            }
        }
    }
    static string GetUniqueFileName(string path)
    {
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);
        int count = 1;

        while (File.Exists(path))
        {
            path = Path.Combine(directory, $"{fileName} ({count}){extension}");
            count++;
        }

        return path;
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
