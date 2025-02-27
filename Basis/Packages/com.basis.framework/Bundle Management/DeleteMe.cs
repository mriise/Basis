using UnityEngine;
using System.Threading;

public class DeleteMe : MonoBehaviour
{
    public CancellationToken CancellationToken;
    public BasisProgressReport BasisProgressReport = new BasisProgressReport();
    public string URL = "";
    public string Password = "";
    public BasisBundleConnector Connector = new BasisBundleConnector();
    async void Start()
    {
        BasisProgressReport = new BasisProgressReport();
        CancellationToken = new CancellationToken();
        Connector = await BasisIOManagement.DownloadBEE(URL, Password, BasisProgressReport, CancellationToken);
    }
}
