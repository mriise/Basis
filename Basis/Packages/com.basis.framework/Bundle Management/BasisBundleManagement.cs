using System;
using System.Threading;
using System.Threading.Tasks;

public static class BasisBundleManagement
{
    public static string MetaLinkBasisSuffix = ".BML";
    public static string EncryptedMetaBasisSuffix = ".BEE";
    public static string AssetBundlesFolder = "BEEData";
    public static async Task<(BasisBundleGenerated, byte[])> DownloadStoreMetaAndBundle(BasisTrackedBundleWrapper bundleWrapper, BasisProgressReport progressCallback, CancellationToken cancellationToken)
    {
        if (!IsValidBundleWrapper(bundleWrapper)) return new(null, null);

        string metaUrl = bundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL;
        if (!IsValidUrl(metaUrl)) return new(null, null);

        try
        {
            BasisDebug.Log($"Starting download process for {metaUrl}");

            (BasisBundleConnector connector, string localPath, byte[] Bytes) = await BasisIOManagement.DownloadBEE(metaUrl, bundleWrapper.LoadableBundle.UnlockPassword, progressCallback, cancellationToken);
            bundleWrapper.LoadableBundle.BasisBundleConnector = connector;
            bundleWrapper.LoadableBundle.BasisLocalEncryptedBundle.LocalConnectorPath = localPath;

            if (!IsValidConnector(bundleWrapper.LoadableBundle.BasisBundleConnector)) return new (null, null);

            BasisDebug.Log($"Downloading bundle file from {metaUrl}");
            if (bundleWrapper.LoadableBundle.BasisBundleConnector.GetPlatform(out BasisBundleGenerated Generated))
            {
                return (Generated,Bytes);
            }
            else
            {
                return (null,null);
            }
        }
        catch (Exception ex)
        {
            BasisDebug.LogError($"Error during download and processing of meta: {ex.Message} {ex.StackTrace}");
            return new(null, null);
        }
    }

    public static async Task<(BasisBundleGenerated, byte[])> ProcessOnDiscMetaDataAsync(BasisTrackedBundleWrapper bundleWrapper, BasisStoredEncryptedBundle storedBundle, BasisProgressReport progressCallback, CancellationToken cancellationToken)
    {
        if (!IsValidBundleWrapper(bundleWrapper) || storedBundle == null)
        {
            BasisDebug.LogError("Invalid bundle data. Exiting method.");
            return new(null, null);
        }

        bundleWrapper.LoadableBundle.BasisLocalEncryptedBundle = storedBundle;
        string metaUrl = bundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted.CombinedURL;
        if (!IsValidUrl(metaUrl)) return new(null, null);

        BasisDebug.Log($"Processing on-disk meta at {storedBundle.LocalConnectorPath}");

        (BasisBundleConnector, byte[]) value = await BasisIOManagement.ReadBEEFile(storedBundle.LocalConnectorPath, bundleWrapper.LoadableBundle.UnlockPassword, progressCallback, cancellationToken);

        if (bundleWrapper.LoadableBundle == null)
        {
            BasisDebug.LogError("Failed to process the Connector and related files.");
        }
        else
        {
            BasisDebug.Log("Successfully processed the Connector and related files.");
        }
        if (bundleWrapper.LoadableBundle.BasisBundleConnector.GetPlatform(out BasisBundleGenerated Generated))
        {
            return (Generated, value.Item2);
        }
        else
        {
            return new(null, null);
        }
    }

    private static bool IsValidBundleWrapper(BasisTrackedBundleWrapper bundleWrapper)
    {
        if (bundleWrapper?.LoadableBundle == null || bundleWrapper.LoadableBundle.BasisRemoteBundleEncrypted == null)
        {
            BasisDebug.LogError("Invalid BasisTrackedBundleWrapper.");
            return false;
        }
        return true;
    }

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            BasisDebug.LogError("MetaURL is null or empty.");
            return false;
        }
        return true;
    }

    private static bool IsValidConnector(BasisBundleConnector connector)
    {
        if (connector == null)
        {
            BasisDebug.LogError("Failed to decrypt meta file, Basis Bundle Connector is null.");
            return false;
        }
        return true;
    }
}
