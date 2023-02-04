using UnityEngine;

public class NetworkPlayer : Unity.Netcode.NetworkBehaviour{
    // synced objects
    public GameObject leftHandNetwork;
    public GameObject rightHandNetwork;
    public GameObject headNetwork;

    // actual objects
    private GameObject leftHand;
    private GameObject rightHand;
    private GameObject head;

    public void Start(){
        leftHand = GetActiveGameObjectWithTag("LeftHand");
        rightHand = GetActiveGameObjectWithTag("RightHand");
        head = GetActiveGameObjectWithTag("MainCamera");
    }

    private GameObject GetActiveGameObjectWithTag(string tag){
        foreach(GameObject obj in GameObject.FindGameObjectsWithTag(tag)){
            if(obj.activeInHierarchy){
                return obj;
            }
        }

        // edge case
        return null;
    }

    public void Update(){
        if(IsOwner){
            leftHandNetwork.transform.position = leftHand.transform.position;
            rightHandNetwork.transform.position = rightHand.transform.position;
            headNetwork.transform.position = head.transform.position;

            headNetwork.transform.rotation = head.transform.rotation;
        }
    }
}
