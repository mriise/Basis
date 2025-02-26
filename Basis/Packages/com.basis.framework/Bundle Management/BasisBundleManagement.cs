using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
public static class BasisBundleManagement
{
    public static string MetaLinkBasisSuffix = ".MetaLinkBasis";
    public static string EncryptedMetaBasisSuffix = ".EncryptedMetaBasis";
    public static string EncryptedBundleBasisSuffix = ".EncryptedBundleBasis";
    public static string AssetBundlesFolder = "AssetBundles";
    public static string LockedBundlesFolder = "LockedBundles";

    // Dictionary to track ongoing downloads keyed by MetaURL
    public static BasisProgressReport FindAllBundlesReport = new BasisProgressReport();
    public static async Task<bool> DownloadStoreMetaAndBundle(BasisTrackedBundleWrapper BasisTrackedBundleWrapper, BasisProgressReport progressCallback, CancellationToken cancellationToken)
    {
        if (BasisTrackedBundleWrapper == null)
        {
            BasisDebug.LogError("Basis Tracked Bundle Wrapper is null.");
            return false;
        }

        if (BasisTrackedBundleWrapper.LoadableBundle == null || BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted == null)
        {
            BasisDebug.LogError("Loadable Bundle or Basis Remote Bundle Encrypted is null.");
            return false;
        }

        string metaUrl = BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL;

        if (string.IsNullOrEmpty(metaUrl))
        {
            BasisDebug.LogError("MetaURL is null or empty.");
            return false;
        }

        BasisDebug.Log($"Starting download process for {metaUrl}");

        try
        {
            BasisDebug.Log($"Downloading meta file for {metaUrl}");

            string UniqueDownload = BasisGenerateUniqueID.GenerateUniqueID();

            string UniqueFilePath = BasisIOManagement.GenerateFilePath($"Temp_{UniqueDownload}{EncryptedMetaBasisSuffix}", LockedBundlesFolder);
            if (string.IsNullOrEmpty(UniqueFilePath))
            {
                BasisDebug.LogError("Failed to generate file path for the unique file.");
                return false;
            }

            if (File.Exists(UniqueFilePath))
            {
                File.Delete(UniqueFilePath);
            }

            if (BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.IsLocal)
            {
                if (!File.Exists(BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL))
                {
                    BasisDebug.LogError($"Local meta file not found: {metaUrl}");
                    return false;
                }

                File.Copy(BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL, UniqueFilePath); // The goal here is just to get the data out
            }
            else
            {
                await BasisIOManagement.DownloadBEE(metaUrl, UniqueFilePath, progressCallback, cancellationToken);
            }

            BasisDebug.Log($"Successfully downloaded meta file for {metaUrl} Decrypting meta file...");

            BasisTrackedBundleWrapper.LoadableBundle = await BasisEncryptionToData.GenerateMetaFromFile(BasisTrackedBundleWrapper.LoadableBundle, UniqueFilePath, progressCallback);

            if (BasisTrackedBundleWrapper.LoadableBundle == null)
            {
                BasisDebug.LogError("Failed to decrypt meta file, Loadable Bundle is null.");
                return false;
            }

            // Step 4: Download the bundle file
            string bundleUrl = BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL;

            if (string.IsNullOrEmpty(bundleUrl))
            {
                BasisDebug.LogError("BundleURL is null or empty.");
                return false;
            }

            BasisDebug.Log($"Downloading bundle file from {bundleUrl}");
            if (BasisTrackedBundleWrapper.LoadableBundle.BasisBundleConnector == null)
            {
                BasisDebug.LogError("Missing Basis Bundle Information for Loaded Bundle ", BasisDebug.LogTag.System);
                return false;
            }
            if (BasisTrackedBundleWrapper.LoadableBundle.BasisBundleConnector.BasisBundleGenerated == null)
            {
                BasisDebug.LogError("Missing Basis Bundle Generated for Loaded Bundle ", BasisDebug.LogTag.System);
                return false;
            }
            if (BasisTrackedBundleWrapper.LoadableBundle.BasisBundleConnector.GetPlatform(out BasisBundleGenerated Platform))
            {
                string FilePathMeta = BasisIOManagement.GenerateFilePath($"{Platform.AssetToLoadName}{EncryptedMetaBasisSuffix}", AssetBundlesFolder);
                string FilePathBundle = BasisIOManagement.GenerateFilePath($"{Platform.AssetToLoadName}{EncryptedBundleBasisSuffix}", AssetBundlesFolder);

                if (string.IsNullOrEmpty(FilePathMeta) || string.IsNullOrEmpty(FilePathBundle))
                {
                    BasisDebug.LogError("Failed to generate file paths for meta or bundle.");
                    return false;
                }

                if (File.Exists(FilePathMeta))
                {
                    File.Delete(FilePathMeta);
                }

                File.Move(UniqueFilePath, FilePathMeta); // Move encrypted file to match new name.

                if (BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.IsLocal)
                {
                    if (!File.Exists(bundleUrl))
                    {
                        BasisDebug.LogError($"Local bundle file not found: {bundleUrl}");
                        return false;
                    }

                    if (File.Exists(FilePathBundle))
                    {
                        File.Delete(FilePathBundle);
                    }

                    File.Copy(bundleUrl, FilePathBundle); // The goal here is just to get the data out
                }
                else
                {
                    await BasisIOManagement.DownloadBEE(bundleUrl, FilePathBundle, progressCallback, cancellationToken);
                }

                BasisDebug.Log($"Successfully downloaded bundle file for {bundleUrl}");
               // BasisTrackedBundleWrapper.LoadableBundle.BasisLocalEncryptedBundle.LocalConnectorPath = FilePathBundle;
                BasisTrackedBundleWrapper.LoadableBundle.BasisLocalEncryptedBundle.LocalConnectorPath = FilePathMeta;
                return true;
            }
            else
            {
                BasisDebug.LogError("Missing Platform in Bundle!");
                return false;
            }
        }
        catch (Exception ex)
        {
            BasisDebug.LogError($"Error during download and processing of meta: {ex.Message} {ex.StackTrace}");
            return false;
        }
    }
    public static async Task<bool> DownloadAndSaveMetaFile(BasisTrackedBundleWrapper BasisTrackedBundleWrapper, BasisProgressReport progressCallback, CancellationToken cancellationToken)
    {
        if (BasisTrackedBundleWrapper == null)
        {
            BasisDebug.LogError("BasisTrackedBundleWrapper is null.");
            return false;
        }

        if (BasisTrackedBundleWrapper.LoadableBundle == null || BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted == null)
        {
            BasisDebug.LogError("LoadableBundle or BasisRemoteBundleEncrypted is null.");
            return false;
        }

        string metaUrl = BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL;

        if (string.IsNullOrEmpty(metaUrl))
        {
            BasisDebug.LogError("MetaURL is null or empty.");
            return false;
        }

        BasisDebug.Log($"Starting download process for {metaUrl}");

        try
        {
            BasisDebug.Log($"Downloading meta file for {metaUrl}");

            string UniqueDownload = BasisGenerateUniqueID.GenerateUniqueID();
            if (string.IsNullOrEmpty(UniqueDownload))
            {
                BasisDebug.LogError("Failed to generate a unique ID.");
                return false;
            }

            string UniqueFilePath = BasisIOManagement.GenerateFilePath($"{UniqueDownload}{EncryptedMetaBasisSuffix}", LockedBundlesFolder);
            if (string.IsNullOrEmpty(UniqueFilePath))
            {
                BasisDebug.LogError("Failed to generate file path for the unique file.");
                return false;
            }

            if (File.Exists(UniqueFilePath))
            {
                File.Delete(UniqueFilePath);
            }

            if (BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.IsLocal)
            {
                if (!File.Exists(BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL))
                {
                    BasisDebug.LogError($"Local meta file not found: {metaUrl}");
                    return false;
                }

                File.Copy(BasisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL, UniqueFilePath); // The goal here is just to get the data out
            }
            else
            {
                await BasisIOManagement.DownloadBEE(metaUrl, UniqueFilePath, progressCallback, cancellationToken);
            }

            BasisDebug.Log($"Successfully downloaded meta file for {metaUrl}. Decrypting meta file...");

            BasisTrackedBundleWrapper.LoadableBundle = await BasisEncryptionToData.GenerateMetaFromFile(BasisTrackedBundleWrapper.LoadableBundle, UniqueFilePath, progressCallback);

            if (BasisTrackedBundleWrapper.LoadableBundle == null)
            {
                BasisDebug.LogError("Failed to decrypt meta file, LoadableBundle is null.");
                return false;
            }
            if (BasisTrackedBundleWrapper.LoadableBundle.BasisBundleConnector.GetPlatform(out BasisBundleGenerated Generated))
            {
                // Move the meta file to its final destination
                string FilePathMeta = BasisIOManagement.GenerateFilePath($"{Generated.AssetToLoadName}{EncryptedMetaBasisSuffix}", AssetBundlesFolder);

                if (string.IsNullOrEmpty(FilePathMeta))
                {
                    BasisDebug.LogError("Failed to generate file path for the meta file.");
                    return false;
                }

                if (File.Exists(FilePathMeta))
                {
                    File.Delete(FilePathMeta);
                }

                File.Move(UniqueFilePath, FilePathMeta); // Move encrypted file to match new name.

                BasisTrackedBundleWrapper.LoadableBundle.BasisLocalEncryptedBundle.LocalConnectorPath = FilePathMeta;

                BasisDebug.Log($"Meta file saved successfully at {FilePathMeta}");
                return true;
            }
            else
            {
                BasisDebug.LogError("Missing Platform");
                return false;
            }
        }
        catch (Exception ex)
        {
            BasisDebug.LogError($"Error during download and processing of meta: {ex.Message}");
            return false;
        }
    }
    public static async Task ProcessOnDiscMetaDataAsync(BasisTrackedBundleWrapper basisTrackedBundleWrapper, BasisStoredEncryptedBundle BasisStoredEncyptedBundle, BasisProgressReport progressCallback, CancellationToken cancellationToken)
    {
        // Log entry point
        BasisDebug.Log("Starting DataOnDiscProcessMeta method...");

        // Parameter validation with detailed logging
        if (BasisStoredEncyptedBundle == null)
        {
            BasisDebug.LogError("BasisTrackedBundleWrapper is null. Exiting method.");
            return;
        }
        // Parameter validation with detailed logging
        if (basisTrackedBundleWrapper == null)
        {
            BasisDebug.LogError("BasisTrackedBundleWrapper is null. Exiting method.");
            return;
        }

        // Validate nested objects in BasisTrackedBundleWrapper
        if (basisTrackedBundleWrapper.LoadableBundle == null)
        {
            BasisDebug.LogError("LoadableBundle inside BasisTrackedBundleWrapper is null. Exiting method.");
            return;
        }

        if (basisTrackedBundleWrapper.LoadableBundle.BasisLocalEncryptedBundle == null)
        {
            BasisDebug.LogError("BasisStoredEncyptedBundle inside LoadableBundle is null. Exiting method.");
            return;
        }

        if (basisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted == null)
        {
            BasisDebug.LogError("BasisRemoteBundleEncypted inside LoadableBundle is null. Exiting method.");
            return;
        }

        // Set local paths
        BasisDebug.Log($"Setting local bundle file: {BasisStoredEncyptedBundle.LocalConnectorPath} Setting local meta file: {BasisStoredEncyptedBundle.LocalConnectorPath}");

        basisTrackedBundleWrapper.LoadableBundle.BasisLocalEncryptedBundle = BasisStoredEncyptedBundle;

        // Fetching the meta URL
        string metaUrl = basisTrackedBundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL;
        if (string.IsNullOrEmpty(metaUrl))
        {
            BasisDebug.LogError("MetaURL is null or empty. Exiting method.");
            return;
        }

        BasisDebug.Log($"Fetched meta URL: {metaUrl}");

        // Create and assign the download task
        BasisDebug.Log("Creating BasisTrackedBundleWrapper... at path: " + BasisStoredEncyptedBundle.LocalConnectorPath);

        var loadableBundle = basisTrackedBundleWrapper.LoadableBundle;
        if (loadableBundle == null)
        {
            BasisDebug.LogError("Failed to retrieve LoadableBundle from BasisTrackedBundleWrapper.");
            return;
        }

        basisTrackedBundleWrapper.LoadableBundle = await BasisEncryptionToData.GenerateMetaFromFile(loadableBundle, BasisStoredEncyptedBundle.LocalConnectorPath, progressCallback);

        if (basisTrackedBundleWrapper.LoadableBundle == null)
        {
            BasisDebug.LogError("Failed to generate meta from file.");
        }
        else
        {
            BasisDebug.Log("Successfully processed the meta file.");
        }
    }
}
