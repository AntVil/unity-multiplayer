using UnityEngine;

public class NetworkPlayer : Unity.Netcode.NetworkBehaviour
{
    public GameObject leftHandNetwork;
    public GameObject rightHandNetwork;
    public GameObject headNetwork;

    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject head;

    public Color color;

    public Color[] colors;

    public uint playerId = 0;

    private static uint playerSession = 0;

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
        if (IsServer)
        {
            print("running server code");
            print(playerSession);

            playerSession += 1;
            UpdateUnsetColorClientRpc(playerSession);
        }
    }

    [Unity.Netcode.ClientRpc]
    private void UpdateUnsetColorClientRpc(uint serverPlayerSession)
    {
        print("running client code");
        if(playerId == 0)
        {
            playerId = serverPlayerSession % (uint)colors.Length;
            if (!IsOwner)
            {
                leftHandNetwork.GetComponent<MeshRenderer>().material.color = colors[playerId];
                rightHandNetwork.GetComponent<MeshRenderer>().material.color = colors[playerId];
                headNetwork.GetComponent<MeshRenderer>().material.color = colors[playerId];
            }
        }
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

        return null;
    }

    private Color calculateColor()
    {
        return new Color(0, 128, 255);
    }


    void Update()
    {
        if (IsOwner)
        {
            leftHandNetwork.transform.position = leftHand.transform.position;
            rightHandNetwork.transform.position = rightHand.transform.position;
            headNetwork.transform.position = head.transform.position;
        }
    }
}
