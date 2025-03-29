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
     //   avatar = BasisLocalPlayer.Instance.BasisAvatar;
        avatar.OnServerReductionSystemMessageReceived += OnServerReductionSystemMessageReceived;
    }
    private void OnServerReductionSystemMessageReceived(byte MessageIndex, byte[] buffer)
    {
        Debug.Log($"received {MessageIndex} {buffer.Length}");
    }
    public void OnDisable()
    {
      //  avatar = BasisLocalPlayer.Instance.BasisAvatar;
        avatar.OnServerReductionSystemMessageReceived -= OnServerReductionSystemMessageReceived;
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
