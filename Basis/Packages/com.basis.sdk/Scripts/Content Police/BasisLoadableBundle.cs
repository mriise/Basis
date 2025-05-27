using UnityEngine;

[System.Serializable]
public class BasisLoadableBundle
{
    public string UnlockPassword;
    //encrypted state
    public BasisRemoteEncyptedBundle BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle();
    public BasisStoredEncryptedBundle BasisLocalEncryptedBundle= new BasisStoredEncryptedBundle();
    public BasisBundleConnector BasisBundleConnector;
    /// <summary>
    /// only used to submit data.
    /// </summary>
    public BasisLoadableGameobject LoadableGameobject = null;
    public class BasisLoadableGameobject
    {
        public GameObject InSceneItem;
    }
}

