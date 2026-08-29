using UnityEngine;
using Unity.Netcode;

public class BotController : NetworkBehaviour
{
    public PlayerMovement.TeamType team;

    public override void OnNetworkSpawn()
    {
        // الـBot يعمل عند السيرفر فقط
        enabled = IsServer;
    }
}