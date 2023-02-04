using UnityEngine;
using TMPro;

public class NetworkColoring : Unity.Netcode.NetworkBehaviour{
    // colored objects
    public MeshRenderer leftHandNetwork;
    public MeshRenderer rightHandNetwork;
    public MeshRenderer headNetwork;
    public TextMeshProUGUI usernameText;

    public Color[] colors;
 
    public override void OnNetworkSpawn(){
        if(IsOwner){
            // rendering of own bodyparts not required
            Destroy(leftHandNetwork);
            Destroy(rightHandNetwork);
            Destroy(headNetwork);
        }else{
            Color color = colors[OwnerClientId % (ulong)colors.Length];

            leftHandNetwork.material.color = color;
            rightHandNetwork.material.color = color;
            headNetwork.material.color = color;
            usernameText.color = color;
        }
    }
}
