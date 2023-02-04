using UnityEngine;
using TMPro;

public class NetworkUsername : Unity.Netcode.NetworkBehaviour{
    public TextMeshProUGUI usernameText;
    
    private string username;

    public override void OnNetworkSpawn(){
        if(IsOwner){
            username = VariableStorage.validUsername;
            ShareUsernameServerRpc(username);
        }else{
            RequestUsernameServerRpc();
        }
    }

    [Unity.Netcode.ServerRpc]
    private void ShareUsernameServerRpc(string shareUsername){
        if(!IsServer) return;

        username = shareUsername;

        ShareUsernameClientRpc(shareUsername);
    }

    [Unity.Netcode.ServerRpc(RequireOwnership = false)]
    private void RequestUsernameServerRpc(){
        if(!IsServer) return;

        // potential improvment possible by specifying TargetClientIds (ClientRpcParams )
        ShareUsernameClientRpc(username);
    }

    [Unity.Netcode.ClientRpc]
    private void ShareUsernameClientRpc(string shareUsername){
        if(IsOwner) return;

        username = shareUsername;
        usernameText.text = shareUsername;
    }
}
