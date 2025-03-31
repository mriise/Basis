using Basis.Scripts.BasisSdk;
using Basis.Scripts.BasisSdk.Players;
using LiteNetLib;
using UnityEngine;
public class BasisTestNetwork : MonoBehaviour
{
    public BasisAvatar avatar;
    public bool Send = false;
    public ushort[] Players;
    public byte[] SendingOutBytes = new byte[3];
    public void OnEnable()
    {
        avatar.OnServerReductionSystemMessageReceived += OnServerReductionSystemMessageReceived;
    }
    public void OnDisable()
    {
        avatar.OnServerReductionSystemMessageReceived -= OnServerReductionSystemMessageReceived;
    }
    private void OnServerReductionSystemMessageReceived(byte MessageIndex, byte[] buffer)
    {
        Debug.Log($"received {MessageIndex} {buffer.Length}");
    }
    public void LateUpdate()
    {
        if (Send)
        {
            avatar.ServerReductionSystemMessageSend(16, SendingOutBytes);
            Send = false;
        }
    }
}
