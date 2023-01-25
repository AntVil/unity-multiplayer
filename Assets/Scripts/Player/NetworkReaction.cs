using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class NetworkReaction : Unity.Netcode.NetworkBehaviour{
    public ParticleSystem positiveReaction;
    public ParticleSystem negativeReaction;

    private bool hasSharedPositiveReaction;
    private bool hasSharedNegativeReaction;

    public void Start(){
        hasSharedPositiveReaction = false;
        hasSharedNegativeReaction = false;
    }

    public override void OnNetworkSpawn(){
        
    }

    void Update(){
        if(!IsOwner) return;

        bool positiveVR;
        bool negativeVR;
        bool positiveKeyboard;
        bool negativeKeyboard;

        try{
            positiveVR = SteamVR_Actions._default.PositiveReaction.GetStateDown(SteamVR_Input_Sources.Any);
            negativeVR = SteamVR_Actions._default.NegativeReaction.GetStateDown(SteamVR_Input_Sources.Any);

        }catch{
            positiveVR = false;
            negativeVR = false;
        }

        try{
            positiveKeyboard = Input.GetButton("PositiveReaction");
            negativeKeyboard = Input.GetButton("NegativeReaction");
        }catch{
            positiveKeyboard = false;
            negativeKeyboard = false;
        }

        if(positiveVR || positiveKeyboard){
            if(!hasSharedPositiveReaction){
                ShareReactionServerRpc(OwnerClientId, true);
                hasSharedPositiveReaction = true;
            }
        }else{
            hasSharedPositiveReaction = false;
        }

        if(negativeVR || negativeKeyboard){
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
        if(!IsServer) return;

        ShareReactionClientRpc(client, isPositive);
    }

    [Unity.Netcode.ClientRpc]
    private void ShareReactionClientRpc(ulong client, bool isPositive){
        if(IsOwner || OwnerClientId != client) return;

        if(isPositive){
            positiveReaction.Play();
        }else{
            negativeReaction.Play();
        }
    }
}
