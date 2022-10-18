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
    public void UpdateModelClientRpc(string modelId)
    {
        if (IsServer) return;
        if (!IsOwner) return;

        // TODO: Request/Download Model from server
        string url = webserver.getRequestURL();
        // getModelName
        // getModelImage
        // getModelObj
        // getModelMtl
    }
}
