using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class BasisPlayerSettingsManager
{
    private static readonly string settingsDirectory = Path.Combine(Application.persistentDataPath, "PlayerSettings");

    static BasisPlayerSettingsManager()
    {
        // Ensure the directory exists
        if (!Directory.Exists(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }
    }

    /// <summary>
    /// Requests player settings from file. If not found or corrupted, creates and returns default settings.
    /// </summary>
    public static async Task<BasisPlayerSettingsData> RequestPlayerSettings(string uuid)
    {
        string filePath = GetFilePath(uuid);

        if (File.Exists(filePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonUtility.FromJson<BasisPlayerSettingsData>(json);
                }
            }
            catch (Exception ex)
            {
                BasisDebug.LogError($"Failed to load settings for {uuid}: {ex.Message}. Resetting file.");
            }

            // If file is empty, corrupted, or unreadable, delete it and create a new one
            File.Delete(filePath);
        }

        // Create default settings if file does not exist or was deleted
        BasisPlayerSettingsData defaultData = new BasisPlayerSettingsData(uuid, 1.0f, true);
        await SetPlayerSettings(defaultData);
        return defaultData;
    }

    /// <summary>
    /// Saves player settings to file.
    /// </summary>
    public static async Task SetPlayerSettings(BasisPlayerSettingsData settings)
    {
        string filePath = GetFilePath(settings.UUID);
        string json = JsonUtility.ToJson(settings, false); // Minimized JSON
        await File.WriteAllTextAsync(filePath, json);
    }
    private static string GetFilePath(string uuid)
    {
        string sanitizedUuid = SanitizeFileName(uuid);
        return Path.Combine(settingsDirectory, $"{sanitizedUuid}.json");
    }

    /// <summary>
    /// Removes invalid characters from a filename.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_'); // Replace invalid characters with underscore
        }
        return fileName;
    }
}
