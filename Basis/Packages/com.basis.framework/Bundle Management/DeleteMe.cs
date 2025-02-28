using UnityEngine;
using System.Threading;

public class DeleteMe : MonoBehaviour
{
    public BasisProgressReport Report = new BasisProgressReport();
    public CancellationToken CancellationToken = new CancellationToken();
    public BasisLoadableBundle BasisLoadableBundle;
    public bool UseSafety = false;
    public async void Start()
    {
        // BasisLoadHandler.LoadGameObjectBundle();
        GameObject output = await BasisLoadHandler.LoadGameObjectBundle(BasisLoadableBundle, UseSafety, Report, CancellationToken, Vector3.zero, Quaternion.identity, Vector3.one, false, this.transform);
    }
}
