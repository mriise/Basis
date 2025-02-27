using UnityEngine;
using System.Threading;

public class DeleteMe : MonoBehaviour
{
    public CancellationToken CancellationToken;
    public BasisProgressReport BasisProgressReport = new BasisProgressReport();
    public string URL = "";
    public string Password = "";
    public BasisBundleConnector Connector = new BasisBundleConnector();
    public string WhereWasItSaved;
    async void Start()
    {
        BasisProgressReport = new BasisProgressReport();
        CancellationToken = new CancellationToken();
        var output = await BasisIOManagement.DownloadBEE(URL, Password, BasisProgressReport, CancellationToken);
        Connector = output.Item1;
        WhereWasItSaved = output.Item2;
    }
}
