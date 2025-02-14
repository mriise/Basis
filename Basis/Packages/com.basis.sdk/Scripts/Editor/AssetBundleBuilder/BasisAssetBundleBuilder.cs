using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static BasisEncryptionWrapper;
public static class AssetBundleBuilder
{
    public static async Task<List<BasisBundleInformation>> BuildAssetBundle(string targetDirectory, BasisAssetBundleObject settings, string assetBundleName, BasisBundleInformation BasisBundleInformation, string Mode, string Password, BuildTarget BuildTarget, bool IsEncrypted = true)
    {
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        // Start Progress Bar
        EditorUtility.DisplayProgressBar("Building Asset Bundles", "Initializing...", 0f);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(targetDirectory, settings.BuildAssetBundleOptions, BuildTarget);
        List<BasisBundleInformation> basisBundleInformation = new List<BasisBundleInformation>();

        if (manifest != null)
        {
            string[] Files = manifest.GetAllAssetBundles();
            int totalFiles = Files.Length;

            for (int Index = 0; Index < totalFiles; Index++)
            {
                string FileOutput = Files[Index];
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FileOutput);
                Hash128 bundleHash = manifest.GetAssetBundleHash(FileOutput);
                BuildPipeline.GetCRCForAssetBundle(FileOutput, out uint CRC);

                InformationHash informationHash = new InformationHash
                {
                    File = fileNameWithoutExtension,
                    bundleHash = bundleHash,
                    CRC = CRC,
                };

                string actualFilePath = Path.Combine(targetDirectory, informationHash.File) + ".bundle";

                // Update Progress
                float progress = (float)(Index + 1) / totalFiles;
                EditorUtility.DisplayProgressBar("Building Asset Bundles", $"Processing {FileOutput}...", progress);

                BasisBundleInformation Output = await BasisBasisBundleInformationHandler.CreateInformation(
                    settings, BasisBundleInformation, informationHash, Mode, assetBundleName, targetDirectory, Password, IsEncrypted);

                if (Output == null)
                {
                    EditorUtility.ClearProgressBar();
                    throw new Exception("Check Console for Error Message!");
                }

                basisBundleInformation.Add(Output);

                string EncryptedFilePath = actualFilePath;
                if (IsEncrypted)
                {
                    EncryptedFilePath = await EncryptBundle(Password, actualFilePath, settings, manifest);
                }
                else
                {
                    File.Copy(actualFilePath, EncryptedFilePath);
                    EncryptedFilePath = Path.ChangeExtension(EncryptedFilePath, settings.BasisBundleDecryptedExtension);
                }

                if (File.Exists(actualFilePath))
                {
                    File.Delete(actualFilePath);
                }

                string PathOut = Path.GetDirectoryName(EncryptedFilePath);
                await SaveFileAsync(PathOut, settings.ProtectedPasswordFileName, "txt", Password);
            }

            // Delete manifest files
            string[] manifestFiles = Directory.GetFiles(targetDirectory, "*.manifest");
            foreach (string manifestFile in manifestFiles)
            {
                if (File.Exists(manifestFile))
                {
                    File.Delete(manifestFile);
                    Debug.Log("Deleted manifest file: " + manifestFile);
                }
            }

            string[] AssetFiles = Directory.GetFiles(targetDirectory);
            foreach (string manifestFile in AssetFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(manifestFile);
                if (fileNameWithoutExtension == "AssetBundles")
                {
                    File.Delete(manifestFile);
                    Debug.Log("Deleted manifest file: " + manifestFile);
                }
            }
        }
        else
        {
            Debug.LogError("AssetBundle build failed.");
        }

        // Clear Progress Bar
        EditorUtility.ClearProgressBar();
        return basisBundleInformation;
    }
    public static async Task SaveFileAsync(string directoryPath, string fileName, string fileExtension, string fileContent)
    {
        // Combine directory path, file name, and extension
        string fullPath = Path.Combine(directoryPath, $"{fileName}.{fileExtension}");
        // Use asynchronous file writing
        using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 256, true))
        using (StreamWriter writer = new StreamWriter(fileStream))
        {
            await writer.WriteAsync(fileContent);
        }

        Debug.Log($"File saved asynchronously at: {fullPath}");
    }
    public struct InformationHash
    {
        public string File;
        public Hash128 bundleHash;
        public uint CRC;
    }

    private static BasisProgressReport Report = new BasisProgressReport();

    // Method to encrypt a file using a password
    public static async Task<string> EncryptBundle(string password, string actualFilePath, BasisAssetBundleObject buildSettings, AssetBundleManifest assetBundleManifest)
    {
        System.Diagnostics.Stopwatch encryptionTimer = System.Diagnostics.Stopwatch.StartNew();

        // Get all asset bundles from the manifest
        string[] bundles = assetBundleManifest.GetAllAssetBundles();
        if (bundles.Length == 0)
        {
            Debug.LogError("No asset bundles found in manifest.");
            return string.Empty;
        }
        string EncryptedPath = Path.ChangeExtension(actualFilePath, buildSettings.BasisBundleEncryptedExtension);

        // Delete existing encrypted file if present
        if (File.Exists(EncryptedPath))
        {
            File.Delete(EncryptedPath);
        }
        Debug.Log("Encrypting " + actualFilePath);
        BasisPassword BasisPassword = new BasisPassword
        {
            VP = password
        };
        string UniqueID = BasisGenerateUniqueID.GenerateUniqueID();
        await BasisEncryptionWrapper.EncryptFileAsync(UniqueID, BasisPassword, actualFilePath, EncryptedPath, Report);
        encryptionTimer.Stop();
        Debug.Log("Encryption took " + encryptionTimer.ElapsedMilliseconds + " ms for " + EncryptedPath);
        return EncryptedPath;
    }

    public static string SetAssetBundleName(string assetPath, string uniqueID, BasisAssetBundleObject settings)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
        string assetBundleName = $"{uniqueID}{settings.BundleExtension}";

        if (assetImporter != null)
        {
            assetImporter.assetBundleName = assetBundleName;
            return assetBundleName;
        }
        else
        {
            Debug.LogError("Missing Asset Import for path " + assetPath);
        }

        return null;
    }

    public static void ResetAssetBundleName(string assetPath)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
        if (assetImporter != null && !string.IsNullOrEmpty(assetImporter.assetBundleName))
        {
            assetImporter.assetBundleName = null;
        }
    }
}
