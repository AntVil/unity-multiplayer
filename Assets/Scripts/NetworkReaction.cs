using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class NetworkReaction : Unity.Netcode.NetworkBehaviour
{
    public ParticleSystem positiveReaction;
    public ParticleSystem negativeReaction;

    private bool hasSharedPositiveReaction;
    private bool hasSharedNegativeReaction;

    public void Start(){
        hasSharedPositiveReaction = false;
        hasSharedNegativeReaction = false;
    }

    public override void OnNetworkSpawn(){
        //Destroy(GetComponent<NetworkReaction>());
    }

    void Update(){
        if(!IsOwner) return;

        if(Input.GetButton("PositiveReaction") || SteamVR_Actions._default.PositiveReaction.GetStateDown(SteamVR_Input_Sources.Any)){
            if(!hasSharedPositiveReaction){
                ShareReactionServerRpc(OwnerClientId, true);
                hasSharedPositiveReaction = true;
            }
        }else{
            hasSharedPositiveReaction = false;
        }

        if(Input.GetButton("NegativeReaction") || SteamVR_Actions._default.NegativeReaction.GetStateDown(SteamVR_Input_Sources.Any)){
            if(!hasSharedNegativeReaction){
                ShareReactionServerRpc(OwnerClientId, false);
                hasSharedNegativeReaction = true;
            }
        }else{
            hasSharedNegativeReaction = false;
        }
    }

    [Unity.Netcode.ServerRpc]
    private void ShareReactionServerRpc(ulong client, bool isPositive){
        if (!IsServer) return;

        ShareReactionClientRpc(client, isPositive);
    }

    [Unity.Netcode.ClientRpc]
    private void ShareReactionClientRpc(ulong client, bool isPositive){
        if (IsOwner || OwnerClientId != client) return;

        if(isPositive){
            positiveReaction.Play();
        }else{
            negativeReaction.Play();
        }
    }
}
