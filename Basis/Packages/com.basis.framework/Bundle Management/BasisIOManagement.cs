using System.IO;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public static class BasisIOManagement
{
    public static int HeaderSize = 8;//8 bytes
    public static async Task DownloadBEE(string url, string localFilePath, BasisProgressReport progressCallback, CancellationToken cancellationToken = default)
    {
      await DownloadFileRange(url, localFilePath, progressCallback, cancellationToken,0, HeaderSize);
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
    public static async Task DownloadFileRange(string url, string localFilePath, BasisProgressReport progressCallback, CancellationToken cancellationToken = default, long startByte = 0, long? endByte = null)
    {
        BasisDebug.Log($"Starting file download from {url} (Range: {startByte}-{(endByte.HasValue ? endByte.ToString() : "end")})");
        string uniqueID = BasisGenerateUniqueID.GenerateUniqueID();

        if (string.IsNullOrWhiteSpace(url))
        {
            BasisDebug.LogError("The provided URL is null or empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            BasisDebug.LogError("The provided local file path is null or empty.");
            return;
        }

        // Ensure directory exists
        string directory = Path.GetDirectoryName(localFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Set the range header (e.g., "bytes=500-999")
            string rangeHeader = endByte.HasValue ? $"bytes={startByte}-{endByte}" : $"bytes={startByte}-";
            request.SetRequestHeader("Range", rangeHeader);

            // Use a DownloadHandlerFile to stream the download
            request.downloadHandler = new DownloadHandlerFile(localFilePath, true) // Append mode
            {
                removeFileOnAbort = true,
            };

            UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    BasisDebug.Log("Download cancelled.");
                    request.Abort();
                    return;
                }

                progressCallback.ReportProgress(uniqueID, asyncOperation.webRequest.downloadProgress * 100, "Downloading data...");
                await Task.Yield();
            }

            // Handle potential download errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                BasisDebug.LogError($"Failed to download file: {request.error} for URL {url}");
                return;
            }

            // Validate response status code (206 means partial content)
            long responseCode = request.responseCode;
            if (responseCode != 206 && responseCode != 200)
            {
                BasisDebug.LogError($"Server did not support range requests. Response code: {responseCode} a workaround should be implemented");
                return;
            }

            // Check if the file exists
            if (!File.Exists(localFilePath))
            {
                BasisDebug.LogError("The file was not created.");
                return;
            }

            BasisDebug.Log($"Successfully downloaded range {startByte}-{(endByte.HasValue ? endByte.ToString() : "end")} from {url} to {localFilePath}");
            progressCallback.ReportProgress(uniqueID, 100, "Successfully Loaded data");
        }
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
