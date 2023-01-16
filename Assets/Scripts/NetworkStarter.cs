using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net.NetworkInformation;
using System.Linq;

public class NetworkStarter : MonoBehaviour
{
    public bool active;

    public void Start()
    {
        StartCoroutine(StartNetwork());
    }

    private IEnumerator StartNetwork()
    {
        yield return new WaitForSeconds(1);

        if (active)
        {

            Debug.Log(VariableStorage.isHost);
            Debug.Log(VariableStorage.validIP);
            Debug.Log(VariableStorage.validUsername);

            if (VariableStorage.isHost)
            {
                Debug.Log("Start as host");
                NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = VariableStorage.validIP;
                NetworkManager.Singleton.StartHost();
            }
            if (VariableStorage.isHost == false)
            {
                Debug.Log("Start as client");
                NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = VariableStorage.validIP;
                NetworkManager.Singleton.StartClient();
            }
        }
    }
}
