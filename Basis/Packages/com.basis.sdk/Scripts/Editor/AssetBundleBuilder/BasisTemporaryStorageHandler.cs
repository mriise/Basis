using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
//taken from UnityEngine.Rendering.SceneExtensions
internal static class SceneExtensions
{
    static PropertyInfo s_SceneGUID = typeof(Scene).GetProperty("guid", BindingFlags.NonPublic | BindingFlags.Instance);
    public static string GetGUID(this Scene scene)
    {
        Debug.Assert(s_SceneGUID != null, "Reflection for scene GUID failed");
        return (string)s_SceneGUID.GetValue(scene);
    }
}

public static class TemporaryStorageHandler
{
    public static string SavePrefabToTemporaryStorage(GameObject prefab, BasisAssetBundleObject settings, ref bool wasModified, out string uniqueID)
    {
        EnsureDirectoryExists(settings.TemporaryStorage);
        uniqueID = BasisGenerateUniqueID.GenerateUniqueID();
        string prefabPath = Path.Combine(settings.TemporaryStorage, $"{uniqueID}.prefab");
        prefab = PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        wasModified = true;

        return prefabPath;
    }
    public static string SaveSceneToTemporaryStorage(Scene sceneToCopy, BasisAssetBundleObject settings, out string uniqueID)
    {
        string actualScenePath = sceneToCopy.path;
        EnsureDirectoryExists(settings.TemporaryStorage);

        uniqueID = BasisGenerateUniqueID.GenerateUniqueID();
        string tempScenePath = Path.Combine(settings.TemporaryStorage, $"{uniqueID}.unity");

        if (EditorSceneManager.SaveScene(sceneToCopy, tempScenePath, true))
        {
            Scene tempScene = EditorSceneManager.OpenScene(tempScenePath, OpenSceneMode.Single);
            ProcessSceneProbeVolume(tempScene);
            EditorSceneManager.SaveScene(tempScene);

            EditorSceneManager.OpenScene(actualScenePath, OpenSceneMode.Single);
            return tempScenePath;
        }

        return null;
    }
    private static void ProcessSceneProbeVolume(Scene scene)
    {
        List<GameObject> rootObjects = new List<GameObject>();
        scene.GetRootGameObjects(rootObjects);
        string GUID = scene.GetGUID();
        foreach (GameObject obj in rootObjects)
        {
            if (obj.TryGetComponent(out ProbeVolumePerSceneData probeVolumeData))
            {
                BasisDebug.Log("Found ProbeVolumePerSceneData");
                SetSceneGUID(probeVolumeData, GUID);
                CloneAndAssignBakingSet(probeVolumeData, GUID);
                EditorUtility.SetDirty(probeVolumeData);
            }
        }
    }
    private static void SetSceneGUID(ProbeVolumePerSceneData data, string GetGUID)
    {
        var guidField = typeof(ProbeVolumePerSceneData).GetField("sceneGUID", BindingFlags.NonPublic | BindingFlags.Instance);
        if (guidField != null)
        {
            guidField.SetValue(data, GetGUID);
            BasisDebug.Log($"Set ProbeVolumePerSceneData GUID To {GetGUID}");
        }
    }
    private static void CloneAndAssignBakingSet(ProbeVolumePerSceneData data, string ReplaceWithSceneGUID)
    {
        ProbeVolumeBakingSet originalSet = data.bakingSet;
        ProbeVolumeBakingSet duplicateSet = CloneBakingSet(originalSet, ReplaceWithSceneGUID);

        FieldInfo serializedBakingSetField = typeof(ProbeVolumePerSceneData).GetField("serializedBakingSet", BindingFlags.NonPublic | BindingFlags.Instance);
        if (serializedBakingSetField != null)
        {
            serializedBakingSetField.SetValue(data, duplicateSet);
            BasisDebug.Log("Successfully set internal serializedBakingSet");
        }
        else
        {
            BasisDebug.LogError("Failed to find serializedBakingSet field via reflection");
        }
    }
    private static ProbeVolumeBakingSet CloneBakingSet(ProbeVolumeBakingSet originalSet, string ReplaceWithSceneGUID)
    {
        ProbeVolumeBakingSet newSet = new ProbeVolumeBakingSet();
        FieldInfo[] fields = typeof(ProbeVolumeBakingSet).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(originalSet);
            field.SetValue(newSet, value);
        }
        // Now update the scene GUIDs in the cloned set
        for (int Index = 0; Index < newSet.sceneGUIDs.Count; Index++)
        {
            string SceneGUID = newSet.sceneGUIDs[Index];
            BasisDebug.Log("Scene ID was " + SceneGUID);

            // Use reflection to update the scene GUID list in the new set
            UpdateSceneGUIDUsingReflection(newSet, Index, ReplaceWithSceneGUID);
        }
        return newSet;
    }
    public static void UpdateSceneGUIDUsingReflection(object target, int index, string ReplaceWithSceneGUID)
    {
        // Get the type of the target object
        Type type = target.GetType();

        // Get the private field "m_SceneGUIDs" using reflection
        FieldInfo fieldInfo = type.GetField("m_SceneGUIDs", BindingFlags.NonPublic | BindingFlags.Instance);

        if (fieldInfo != null)
        {
            // Get the current value of the private field (the List<string>)
            var sceneGUIDsList = (List<string>)fieldInfo.GetValue(target);

            // Update the value in the list
            if (index >= 0 && index < sceneGUIDsList.Count)
            {
                sceneGUIDsList[index] = ReplaceWithSceneGUID;
            }
            else
            {
                BasisDebug.Log("Index out of bounds");
            }

            // Optionally, set the updated value back to the field (not needed in this case, because the list is already modified)
            fieldInfo.SetValue(target, sceneGUIDsList);
        }
        else
        {
            BasisDebug.Log("Field 'm_SceneGUIDs' not found");
        }
    }
    /// <summary>
    /// Deletes the scene from the temporary storage if it exists.
    /// </summary>
    /// <param name="uniqueID">The unique ID of the scene to delete.</param>
    /// <param name="settings">An object containing temporary storage path settings.</param>
    /// <returns>Returns true if the file was successfully deleted, false if it does not exist or failed to delete.</returns>
    public static bool DeleteTemporaryStorageScene(string uniqueID, BasisAssetBundleObject settings)
    {
        string tempScenePath = Path.Combine(settings.TemporaryStorage, $"{uniqueID}.unity");

        if (File.Exists(tempScenePath))
        {
            try
            {
                File.Delete(tempScenePath);
                return true;
            }
            catch (IOException ex)
            {
                Debug.LogError($"Failed to delete scene file: {ex.Message}");
                return false;
            }
        }

        Debug.LogWarning("Scene file not found.");
        return false;
    }
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
    public static void ClearTemporaryStorage(string tempStoragePath)
    {
        if (Directory.Exists(tempStoragePath))
        {
            Directory.Delete(tempStoragePath, true);
        }
    }
}
