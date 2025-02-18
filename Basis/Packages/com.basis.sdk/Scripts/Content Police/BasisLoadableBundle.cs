[System.Serializable]
public class BasisLoadableBundle
{
    public string UnlockPassword;
    //encrypted state
    public BasisRemoteEncyptedBundle BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle();
    public BasisStoredEncyptedBundle BasisLocalEncryptedBundle= new BasisStoredEncyptedBundle();
    public BasisBundleConnector BasisBundleConnector;
}
