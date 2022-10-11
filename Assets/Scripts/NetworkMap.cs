using UnityEngine;

public class NetworkMap : Unity.Netcode.NetworkBehaviour
{
    public WebServer webserver;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // TODO: Request/Download Model from server
        }
    }

    [Unity.Netcode.ClientRpc]
    private void UpdateModelClientRpc(ulong modelId)
    {
        if (IsServer) return;
        if (!IsOwner) return;

        // TODO: Request/Download Model from server
    }
}
