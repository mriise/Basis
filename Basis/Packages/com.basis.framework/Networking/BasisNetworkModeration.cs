using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using static BasisNetworkCore.Serializable.SerializableBasis;

public static class BasisNetworkModeration
{
    public static void SendBan()
    {

    }
    public static void SendIPBan()
    {

    }
    public static void SendKick()
    {

    }
    public static void UnBan()
    {

    }
    public static void UnIpBan()
    {

    }
    public static void TeleportAll()
    {

    }
    public static void DisplayMessage(string Message)
    {
        BasisUINotification.OpenNotification(Message);
    }
    public static void AdminMessage(NetDataReader reader)
    {
        AdminRequest AdminRequest = new AdminRequest();
        AdminRequest.Deserialize(reader);
        AdminRequestMode Mode = AdminRequest.GetAdminRequestMode();
        switch (Mode)
        {
            // case AdminRequestMode.Ban:
            //    break;
            // case AdminRequestMode.Kick:
            //    break;
            //   case AdminRequestMode.IpAndBan:
            //     break;
            case AdminRequestMode.Message:
                DisplayMessage(reader.GetString());
                break;
            case AdminRequestMode.MessageAll:
                DisplayMessage(reader.GetString());
                break;
            // case AdminRequestMode.UnBanIP:
            //    break;
            //  case AdminRequestMode.UnBan:
            //   break;
            //  case AdminRequestMode.RequestBannedPlayers:
            //      break;
            // case AdminRequestMode.TeleportTo:
            //    break;
            case AdminRequestMode.TeleportAll:
                ushort PlayerID = reader.GetUShort();
                if (BasisNetworkManagement.Players.TryGetValue(PlayerID, out Basis.Scripts.Networking.NetworkedAvatar.BasisNetworkPlayer player))
                {
                    if (player.Player != null && player.Player.BasisAvatar != null && player.Player.BasisAvatar.Animator != null)
                    {
                        Transform Trans = player.Player.BasisAvatar.Animator.GetBoneTransform(UnityEngine.HumanBodyBones.Hips);
                        BasisLocalPlayer.Instance.Teleport(Trans.position, Trans.rotation);
                    }
                    else
                    {
                        BasisDebug.LogError("Missing Teleport To Player ");
                    }
                }
                else
                {
                    BasisDebug.LogError("Trying to teleport to null player for id " + PlayerID);
                }
                break;
            //  case AdminRequestMode.AddAdmin:
            //   break;
            //   case AdminRequestMode.RemoveAdmin:
            //  break;
            default:
                BasisDebug.LogError("Missing Command " + Mode.ToString(), BasisDebug.LogTag.Networking);
                break;
        }
    }
}
