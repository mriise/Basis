using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static BasisEncryptionWrapper;
public static class AssetBundleBuilder
{
    public static async Task<BasisBundleConnector> BuildAssetBundle(string targetDirectory,BasisAssetBundleObject settings,string assetBundleName,BasisBundleConnector basisBundleConnector,string mode,string password,BuildTarget buildTarget,bool isEncrypted = true)
    {
        EnsureDirectoryExists(targetDirectory);
        EditorUtility.DisplayProgressBar("Building Asset Bundles", "Initializing...", 0f);
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(targetDirectory, settings.BuildAssetBundleOptions, buildTarget);
        if (manifest != null)
        {
            await ProcessAssetBundles(targetDirectory, settings, manifest, password, isEncrypted,password);
            DeleteManifestFiles(targetDirectory);
        }
        else
        {
            BasisDebug.LogError("AssetBundle build failed.");
        }
        EditorUtility.ClearProgressBar();
        return basisBundleConnector;
    }
    private static async Task<InformationHash> ProcessAssetBundles(string targetDirectory,BasisAssetBundleObject settings,AssetBundleManifest manifest,string password,bool isEncrypted,string PasswordTextFileFolderPath)
    {
        string[] files = manifest.GetAllAssetBundles();
        int totalFiles = files.Length;
        List<InformationHash> InformationHashes = new List<InformationHash>();
        for (int index = 0; index < totalFiles; index++)
        {
            string fileOutput = files[index];
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileOutput);
            Hash128 bundleHash = manifest.GetAssetBundleHash(fileOutput);
            BuildPipeline.GetCRCForAssetBundle(fileOutput, out uint crc);
            InformationHash informationHash = new InformationHash
            {
                File = fileNameWithoutExtension,
                bundleHash = bundleHash,
                CRC = crc
            };
            string actualFilePath = Path.Combine(targetDirectory, informationHash.File) + ".bundle";
            float progress = (float)(index + 1) / totalFiles;
            EditorUtility.DisplayProgressBar("Building Asset Bundles", $"Processing {fileOutput}...", progress);
            string encryptedFilePath = await HandleEncryption(actualFilePath, password, settings, manifest, isEncrypted);
            CleanupOriginalFile(actualFilePath);
            InformationHashes.Add(informationHash);
        }
        await SaveFileAsync(PasswordTextFileFolderPath, settings.ProtectedPasswordFileName, "txt", password);
        if (InformationHashes.Count == 1)
        {
            return InformationHashes[0];
        }
        else
        {
            if (InformationHashes.Count > 1)
            {
                BasisDebug.LogError("More then a single Bundle is being built, please check what bundles your additionally building");
                return InformationHashes[0];
            }
            else
            {
                BasisDebug.LogError("No bundles where built, this is a massive issue!");
                return new InformationHash();
            }
        }
    }

    private static async Task<string> HandleEncryption(string filePath,string password,BasisAssetBundleObject settings,AssetBundleManifest manifest, bool isEncrypted)
    {
        if (isEncrypted)
        {
            return await EncryptBundle(password, filePath, settings, manifest);
        }
        else
        {
            string decryptedFilePath = Path.ChangeExtension(filePath, settings.BasisBundleDecryptedExtension);
            File.Copy(filePath, decryptedFilePath);
            return decryptedFilePath;
        }
    }

    private static void CleanupOriginalFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static void DeleteManifestFiles(string targetDirectory)
    {
        string[] Files = Directory.GetFiles(targetDirectory, "*.manifest");
        foreach (string manifestFile in Files)
        {
            if (File.Exists(manifestFile))
            {
                File.Delete(manifestFile);
                BasisDebug.Log("Deleted manifest file: " + manifestFile);
            }
        }

        string[] BundlesFiles = Directory.GetFiles(targetDirectory);
        foreach (string assetFile in BundlesFiles)
        {
            if (Path.GetFileNameWithoutExtension(assetFile) == "AssetBundles")
            {
                File.Delete(assetFile);
                BasisDebug.Log("Deleted manifest file: " + assetFile);
            }
        }
    }
    public static async Task SaveFileAsync(string directoryPath, string fileName, string fileExtension, string fileContent,int BufferSize = 256)
    {
        // Combine directory path, file name, and extension
        string fullPath = Path.Combine(directoryPath, $"{fileName}.{fileExtension}");
        // Use asynchronous file writing
        using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
        {
            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                await writer.WriteAsync(fileContent);
            }
        }

        BasisDebug.Log($"File saved asynchronously at: {fullPath}");
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
            BasisDebug.LogError("No asset bundles found in manifest.");
            return string.Empty;
        }
        string EncryptedPath = Path.ChangeExtension(actualFilePath, buildSettings.BasisBundleEncryptedExtension);

        // Delete existing encrypted file if present
        if (File.Exists(EncryptedPath))
        {
            File.Delete(EncryptedPath);
        }
        BasisDebug.Log("Encrypting " + actualFilePath);
        BasisPassword BasisPassword = new BasisPassword
        {
            VP = password
        };
        string UniqueID = BasisGenerateUniqueID.GenerateUniqueID();
        await BasisEncryptionWrapper.EncryptFileAsync(UniqueID, BasisPassword, actualFilePath, EncryptedPath, Report);
        encryptionTimer.Stop();
        BasisDebug.Log("Encryption took " + encryptionTimer.ElapsedMilliseconds + " ms for " + EncryptedPath);
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
            BasisDebug.LogError("Missing Asset Import for path " + assetPath);
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
    private static void EnsureDirectoryExists(string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }
    }

}
