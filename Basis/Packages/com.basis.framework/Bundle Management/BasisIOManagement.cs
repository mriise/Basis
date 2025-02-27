using System.IO;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using System;

public static class BasisIOManagement
{
    public static int HeaderSize = 8;//8 bytes
    public static async Task<BasisBundleConnector> DownloadBEE(string url, string vp, BasisProgressReport progressCallback, CancellationToken cancellationToken = default)
    {
        //this gives us the size as a long that we can then use to grab the next section with
        byte[] ConnectorSize = await DownloadFileRange(url, null, progressCallback, cancellationToken, 0, HeaderSize, true);
        long LengthOfSection = BitConverter.ToInt64(ConnectorSize, 0);
        byte[] ConnectorBytes = await DownloadFileRange(url, null, progressCallback, cancellationToken, HeaderSize, HeaderSize + LengthOfSection -1, true);
        BasisDebug.Log("downloaded Connector file size is " + ConnectorBytes.Length + " trying to decode with " + vp);
        BasisBundleConnector Connector = await BasisEncryptionToData.GenerateMetaFromBytes(vp, ConnectorBytes, progressCallback);
        /*
        long previousEnd = LengthOfSection; // Start one byte before the actual first byte
        for (int Index = 0; Index < Connector.BasisBundleGenerated.Length; Index++)
        {
            BasisBundleGenerated pair = Connector.BasisBundleGenerated[Index];
            long startPosition = previousEnd + 1;
            long endPosition = pair.EndByte;
            if (BasisBundleConnector.IsPlatform(pair))
            {
                Console.WriteLine($"Downloading from {startPosition} to {endPosition}");

                byte[] sectionData = await DownloadFileRange(url, null, progressCallback, cancellationToken, startPosition, endPosition - startPosition + 1, true);
                BasisDebug.Log("Length is " + sectionData.Length);
            }
            previousEnd = endPosition; // Update for the next iteration
        }
        */
        return Connector;
    }
    /// <summary>
    /// Downloads a file range in chunks.
    /// </summary>
    /// <param name="url">File URL</param>
    /// <param name="localFilePath">Local file path</param>
    /// <param name="progressCallback">Progress callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="startByte">Starting byte of the range</param>
    /// <param name="endByte">Ending byte of the range</param>
    public static async Task<byte[]> DownloadFileRange(string url, string localFilePath, BasisProgressReport progressCallback, CancellationToken cancellationToken = default, long startByte = 0, long? endByte = null, bool loadToMemory = false)
    {
        BasisDebug.Log($"Starting file download from {url} (Range: {startByte}-{(endByte.HasValue ? endByte.ToString() : "end")})");
        string uniqueID = BasisGenerateUniqueID.GenerateUniqueID();

        localFilePath = loadToMemory ? "memory" : localFilePath;

        if (!ValidateInputs(url, localFilePath))
        {
            return null;
        }

        using (UnityWebRequest request = CreateRequest(url, startByte, endByte, loadToMemory ? null : localFilePath))
        {
            return await ProcessDownload(request, uniqueID, progressCallback, cancellationToken, url, localFilePath, startByte, endByte, loadToMemory);
        }
    }
    private static bool ValidateInputs(string url, string localFilePath)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            BasisDebug.LogError("The provided URL is null or empty.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            BasisDebug.LogError("The provided local file path is null or empty.");
            return false;
        }
        return true;
    }

    private static void EnsureDirectoryExists(string localFilePath)
    {
        string directory = Path.GetDirectoryName(localFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static UnityWebRequest CreateRequest(string url, long startByte, long? endByte, string localFilePath = null)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        string rangeHeader = endByte.HasValue ? $"bytes={startByte}-{endByte}" : $"bytes={startByte}-";
        request.SetRequestHeader("Range", rangeHeader);

        if (localFilePath != null)
        {
            request.downloadHandler = new DownloadHandlerFile(localFilePath, true) { removeFileOnAbort = true };
        }
        else
        {
            request.downloadHandler = new DownloadHandlerBuffer();
        }

        return request;
    }
    private static async Task<byte[]> ProcessDownload(UnityWebRequest request, string uniqueID, BasisProgressReport progressCallback, CancellationToken cancellationToken, string url, string localFilePath, long startByte, long? endByte, bool loadToMemory)
    {
        UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();

        while (!asyncOperation.isDone)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                BasisDebug.Log("Download cancelled.");
                request.Abort();
                return null;
            }
            progressCallback.ReportProgress(uniqueID, asyncOperation.webRequest.downloadProgress * 100, "Downloading data...");
            await Task.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            BasisDebug.LogError($"Failed to download file: {request.error} for URL {url}");
            return null;
        }

        long responseCode = request.responseCode;
        if (responseCode != 206 && responseCode != 200)
        {
            BasisDebug.LogError($"Server did not support range requests. Response code: {responseCode}.");
            return null;
        }

        if (loadToMemory)
        {
            BasisDebug.Log($"Successfully downloaded range {startByte}-{(endByte.HasValue ? endByte.ToString() : "end")} to memory");
            return request.downloadHandler.data;  // Return as byte array
        }
        else
        {
            if (!File.Exists(localFilePath))
            {
                BasisDebug.LogError("The file was not created.");
                return null;
            }
            BasisDebug.Log($"Successfully downloaded range {startByte}-{(endByte.HasValue ? endByte.ToString() : "end")} from {url} to {localFilePath}");
            return null;  // No data returned for file-based download
        }
    }

    private static bool HandleDownloadCompletion(UnityWebRequest request,string url,string localFilePath,long startByte, long? endByte)
    {
        if (request.result != UnityWebRequest.Result.Success)
        {
            BasisDebug.LogError($"Failed to download file: {request.error} for URL {url}");
            return false;
        }

        long responseCode = request.responseCode;
        if (responseCode != 206 && responseCode != 200)
        {
            BasisDebug.LogError($"Server did not support range requests. Response code: {responseCode} a workaround should be implemented");
            return false;
        }

        if (!File.Exists(localFilePath))
        {
            BasisDebug.LogError("The file was not created.");
            return false;
        }

        BasisDebug.Log($"Successfully downloaded range {startByte}-{(endByte.HasValue ? endByte.ToString() : "end")} from {url} to {localFilePath}");
        return true;
    }

    public static string GenerateFilePath(string fileName, string subFolder)
    {
        BasisDebug.Log($"Generating folder path for {fileName} in subfolder {subFolder}");

        // Create the full folder path
        string folderPath = GenerateFolderPath(subFolder);
        // Create the full file path
        string localPath = Path.Combine(folderPath, fileName);
        BasisDebug.Log($"Generated folder path: {localPath}");

        // Return the local path
        return localPath;
    }
    public static string GenerateFolderPath(string subFolder)
    {
        BasisDebug.Log($"Generating folder path in subfolder {subFolder}");

        // Create the full folder path
        string folderPath = Path.Combine(Application.persistentDataPath, subFolder);

        // Check if the directory exists, and create it if it doesn't
        if (!Directory.Exists(folderPath))
        {
            BasisDebug.Log($"Directory {folderPath} does not exist. Creating directory.");
            Directory.CreateDirectory(folderPath);
        }
        return folderPath;
    }
}
