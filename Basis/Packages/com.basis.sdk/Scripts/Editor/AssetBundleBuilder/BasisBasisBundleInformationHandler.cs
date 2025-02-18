using BasisSerializer.OdinSerializer;
using System.IO;
using System.Threading.Tasks;
using static AssetBundleBuilder;

public static class BasisBasisBundleInformationHandler
{
    public static async Task<BasisBundleConnector> BasisBundleConnector(BasisAssetBundleObject BuildSettings, BasisBundleConnector BasisBundleConnector,string ConnectorPassword, InformationHash[] InformationHash, string[] AssetMode, string[] AssetBundlePath, string[] ExportFilePath, string[] Password, bool[] IsEncrypted, string[] Platform)
    {
        BasisBundleConnector.BasisBundleGenerated = new BasisBundleGenerated[InformationHash.Length];
        for (int Index = 0; Index < InformationHash.Length; Index++)
        {
            BasisBundleConnector.BasisBundleGenerated[Index] = new BasisBundleGenerated(InformationHash[Index].bundleHash.ToString(), AssetMode[Index], InformationHash[Index].File, InformationHash[Index].CRC, IsEncrypted[Index], Password[Index], Platform[Index], true, Platform[Index]);
        }
        BasisBundleConnector.UniqueVersion = BasisGenerateUniqueID.GenerateUniqueID();
        string filePath = Path.Combine(BuildSettings.AssetBundleDirectory, $"Connector{BuildSettings.BasisMetaExtension}");

        // If the file exists, delete it
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        await SaveBasisBundleConnector(BasisBundleConnector, filePath, BuildSettings, ConnectorPassword);
        return BasisBundleConnector;
    }
    private static async Task SaveBasisBundleConnector(BasisBundleConnector BasisBundleConnector, string filePath, BasisAssetBundleObject BuildSettings, string password)
    {
        byte[] Information = SerializationUtility.SerializeValue<BasisBundleConnector>(BasisBundleConnector, DataFormat.JSON);
        try
        {
            BasisDebug.Log("Saving Json " + Information.Length);
            // Write JSON data to the file
            await File.WriteAllBytesAsync(filePath, Information);
            BasisDebug.Log($"BasisBundleInformation saved to {filePath}");
            string EncryptedPath = Path.ChangeExtension(filePath, BuildSettings.BasisMetaEncryptedExtension);
            var BasisPassword = new BasisEncryptionWrapper.BasisPassword
            {
                VP = password
            };
            string UniqueID = BasisGenerateUniqueID.GenerateUniqueID();
            await BasisEncryptionWrapper.EncryptFileAsync(UniqueID, BasisPassword, filePath, EncryptedPath, Report);

            // Delete the bundle file if it exists
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (IOException ioEx)
        {
            BasisDebug.LogError($"Failed to save BasisBundleInformation to {filePath}: {ioEx.Message}");
        }
    }
    // Function to validate BasisBundleInformation
     private static BasisProgressReport Report = new BasisProgressReport();
}
