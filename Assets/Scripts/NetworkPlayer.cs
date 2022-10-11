using UnityEngine;

public class NetworkPlayer : Unity.Netcode.NetworkBehaviour
{
    // synced objects
    public GameObject leftHandNetwork;
    public GameObject rightHandNetwork;
    public GameObject headNetwork;

    // actual objects
    private GameObject leftHand;
    private GameObject rightHand;
    private GameObject head;

    public Color color;

    public Color[] colors;

    public void Start()
    {
        if (IsOwner)
        {
            // rendering of own bodyparts not required
            Destroy(leftHandNetwork.GetComponent<MeshRenderer>());
            Destroy(rightHandNetwork.GetComponent<MeshRenderer>());
            Destroy(headNetwork.GetComponent<MeshRenderer>());
        }

        leftHand = getActiveGameObjectWithTag("LeftHand");
        rightHand = getActiveGameObjectWithTag("RightHand");
        head = getActiveGameObjectWithTag("MainCamera");
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner) return;

        ulong playerId = OwnerClientId % (ulong)colors.Length;

        leftHandNetwork.GetComponent<MeshRenderer>().material.color = colors[playerId];
        rightHandNetwork.GetComponent<MeshRenderer>().material.color = colors[playerId];
        headNetwork.GetComponent<MeshRenderer>().material.color = colors[playerId];
    }

    private GameObject getActiveGameObjectWithTag(string tag)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag(tag))
        {
            if (obj.activeInHierarchy)
            {
                return obj;
            }
        }

        // edge case
        return null;
    }

    public void Update()
    {
        if (IsOwner)
        {
            leftHandNetwork.transform.position = leftHand.transform.position;
            rightHandNetwork.transform.position = rightHand.transform.position;
            headNetwork.transform.position = head.transform.position;
        }
    }
}
