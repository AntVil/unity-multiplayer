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

    public void Start(){
        StartCoroutine(StartNetwork());
    }

    private IEnumerator StartNetwork(){
        yield return new WaitForSeconds(1);

        if(active){

        	Debug.Log(VariableStorage.isHost);
            Debug.Log(VariableStorage.validIP);
            Debug.Log(VariableStorage.validUsername);

            // get the ip in Unity Transport
            string unityTransportIPAdress = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address.ToString();

            // find out own ip address
            string hostName = System.Net.Dns.GetHostName();
            System.Net.IPAddress[] ipAddresses = System.Net.Dns.GetHostAddresses(hostName);

            // check if unityTransportIPAdress is host IP adress or localhost
            bool ipFound = false;
            foreach(System.Net.IPAddress ipAddress in ipAddresses){
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork){
                    if (ipAddress.ToString() == unityTransportIPAdress || unityTransportIPAdress == "127.0.0.1"){
                        ipFound = true;
                        NetworkManager.Singleton.StartHost();
                        break;
                    }
                }
            }

            if(!ipFound){
                NetworkManager.Singleton.StartClient();
            }
        }
    }
}

