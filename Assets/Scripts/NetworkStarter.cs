using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkStarter : MonoBehaviour
{
    public bool Activate;

    void Start()
    {
        StartCoroutine(StartNetwork());
    }

    IEnumerator StartNetwork()
    {
        yield return new WaitForSeconds(1);

        if (Activate)
        {
            //get the ip in Unity Transport
            var unityTransportIPAdress = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address.ToString();

            // find out own ip address
            string hostName = System.Net.Dns.GetHostName();
            System.Net.IPAddress[] ipAddresses = System.Net.Dns.GetHostAddresses(hostName);
            var IPfound = false;
            foreach (System.Net.IPAddress ipAddress in ipAddresses)
            {
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    //if it is activated, check if unityTransportIPAdress is host IP adress
                    if (ipAddress.ToString() == unityTransportIPAdress)
                    {
                        IPfound = true;
                        NetworkManager.Singleton.StartHost();
                        break;
                    }
                }
            }
            if (!IPfound)
            {
                NetworkManager.Singleton.StartClient();
            }
        }
    }
}

