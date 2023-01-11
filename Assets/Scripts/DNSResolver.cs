using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class DNSResolver : MonoBehaviour
{
   public string hostName;
    void Start()
    {
        //get all ip addresses of the host
        foreach (IPAddress ip in Dns.GetHostAddresses(hostName))
        {
            //find out if it is ipv4 and print it in console (ipv6 gets ignored)
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Debug.Log("ipv4");
                Debug.Log(ip.ToString());
            }

        }
    }


}
