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
    public static async Task<(bool, string)> BuildBundle(BasisContentBase BasisContentBase, List<BuildTarget> Targets, Func<BasisContentBase, BasisAssetBundleObject, string, BuildTarget, Task<(bool, (BasisBundleGenerated, AssetBundleBuilder.InformationHash))>> buildFunction)
    {
        Debug.Log("Starting BuildBundle...");

        // Store the initial active build target
        BuildTarget originalActiveTarget = EditorUserBuildSettings.activeBuildTarget;



        if (ErrorChecking(BasisContentBase, out string Error) == false)
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
        if (Directory.Exists(Objects.AssetBundleDirectory))
        {
            Directory.Delete(Objects.AssetBundleDirectory, true);
        }
        Debug.Log("Generating random bytes for hex string...");
        byte[] randomBytes = GenerateRandomBytes(32);
        string hexString = ByteArrayToHexString(randomBytes);
        Debug.Log($"Generated hex string: {hexString}");

        BasisBundleGenerated[] Bundles = new BasisBundleGenerated[Targets.Count];
        List<string> Paths = new List<string>();
        for (int Index = 0; Index < Targets.Count; Index++)
        {
            BuildTarget Target = Targets[Index];
            (bool success, (BasisBundleGenerated Generated, AssetBundleBuilder.InformationHash Hash)) = await buildFunction(BasisContentBase, Objects, hexString, Target);
            if (!success)
            {
                return (false, "Failure While Building for " + Target);
            }
            Bundles[Index] = Generated;
            BasisDebug.Log("Adding " + Hash.EncyptedPath);
            string HashPath = PathConversion(Hash.EncyptedPath);
            Paths.Add(HashPath);
        }

        string GeneratedID = BasisGenerateUniqueID.GenerateUniqueID();
        BasisBundleConnector BasisBundleConnector = new BasisBundleConnector(GeneratedID, BasisContentBase.BasisBundleDescription, Bundles);
        (BasisBundleConnector, string) values = await BasisBasisBundleInformationHandler.BasisBundleConnector(Objects, BasisBundleConnector, hexString, true);
        BasisBundleConnector = values.Item1;

        string relativePath = values.Item2;
        string fullPath = PathConversion(relativePath);
        Paths.Insert(0, fullPath);



        Debug.Log("Successfully built asset bundle.");

        await CombineFiles(Path.Combine(Objects.AssetBundleDirectory, GeneratedID + Objects.BasisEncryptedExtension), Paths);
        await AssetBundleBuilder.SaveFileAsync(Objects.AssetBundleDirectory, Objects.ProtectedPasswordFileName, "txt", hexString);

        DeleteFolders(Objects.AssetBundleDirectory);
        File.Delete(fullPath);
        OpenRelativePath(Objects.AssetBundleDirectory);

        if (EditorUserBuildSettings.activeBuildTarget != originalActiveTarget)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(originalActiveTarget), originalActiveTarget);
            Debug.Log($"Switched back to original build target: {originalActiveTarget}");
        }
        return (true, "Success");
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
    public static async Task CombineFiles(string outputPath, List<string> bundlePaths,int bufferSize = 81920)
    {
        BasisDebug.Log("Combining files: " + bundlePaths.Count);

        try
        {
            using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
            {
                byte[] buffer = new byte[bufferSize]; // 80 KB buffer
                int totalFiles = bundlePaths.Count;

                // Reserve first 8 bytes for the size of the first file
                outputStream.Seek(8, SeekOrigin.Begin);

                long firstFileSize = 0;

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
                    using (FileStream inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                    {
                        if (i == 0)
                        {
                            firstFileSize = inputStream.Length;
                        }

                        int bytesRead;
                        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await outputStream.WriteAsync(buffer, 0, bytesRead);
                        }
                    }

                    // Ensure the written data is flushed before moving to the next file
                    await outputStream.FlushAsync();
                }

                // Write the first file size at the beginning
                outputStream.Seek(0, SeekOrigin.Begin);
                byte[] sizeBytes = BitConverter.GetBytes(firstFileSize);
                await outputStream.WriteAsync(sizeBytes, 0, sizeBytes.Length);
                BasisDebug.Log("Size of First file is " + firstFileSize);
            }

            BasisDebug.Log($"Files combined successfully into: {outputPath}");
        }
        catch (Exception ex)
        {
            BasisDebug.LogError($"Error combining files: {ex.Message}");
        }
        finally
        {
            // Ensure the progress bar is cleared
            EditorUtility.ClearProgressBar();
        }
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
