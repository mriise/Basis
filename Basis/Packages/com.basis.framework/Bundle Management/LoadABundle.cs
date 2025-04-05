using System.Threading;
using UnityEngine;
using static BundledContentHolder;
public class LoadABundle : MonoBehaviour
{
    public BasisProgressReport Report = new BasisProgressReport();
    public CancellationToken CancellationToken = new CancellationToken();
    public BasisLoadableBundle BasisLoadableBundle;
    public bool UseSafety = false;
    public Selector Selector;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void OnEnable()
    {
        // Load the GameObject asynchronously
        GameObject output = await BasisLoadHandler.LoadGameObjectBundle(BasisLoadableBundle, UseSafety, Report, CancellationToken, Vector3.zero, Quaternion.identity,Vector3.one,false, Selector, this.transform);
    }
}
