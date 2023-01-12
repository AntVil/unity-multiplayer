using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;
using System.Net.Sockets;

public class MainMenu : MonoBehaviour
{
    //This script handles all the UI elements logic in the main menu
    public GameObject UserNameHost;
    public GameObject UserNameJoin;
    public GameObject HostnameFild;
    private string validIP;
    private string validUsername;
    public void HostGame()
    {
        //get text from imput field
        string username = UserNameHost.GetComponent<UnityEngine.UI.InputField>().text;
        //check if username is not empty
        if (username != "")
        {
            // Check if the username is only letters and max 10 characters
            if (System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z]{1,10}$"))
            {
                Debug.Log("Username is valid");
                validUsername = username;
            }
            else
            {
                Debug.Log("Username is not containing only letters or is longer than 10 characters");
                validUsername = "Host";
            }

        }
        else
        {
            Debug.Log("Username is empty");
            username = "Host";
        }

        // find out own ip address
        string hostName = System.Net.Dns.GetHostName();
        foreach (IPAddress ip in Dns.GetHostAddresses(hostName))
        {
            //find out if it is ipv4 and print it in console (ipv6 gets ignored)
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Debug.Log("ipv4 found");
                validIP = ip.ToString();
            }
            else
            {
                Debug.Log("ipv4 not found defaulting to localhost");
                validIP = "127.0.0.1";
            }
        }

        //the username and isHost is now stored in the VariableStorage class this is needed to transfer the data to the next scene
        VariableStorage.isHost = true;
        VariableStorage.validUsername = validUsername;
        VariableStorage.validIP = validIP;
        SceneManager.LoadScene("MainScene");
    }

    public void JoinGame()
    {
        //get text from imput field
        string username = UserNameJoin.GetComponent<UnityEngine.UI.InputField>().text;

        //check if username is not empty
        if (username != "")
        {
            // Check if the username is only letters and max 10 characters
            if (System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z]{1,10}$"))
            {
                Debug.Log("Username is valid");
                validUsername = username;
            }
            else
            {
                Debug.Log("Username is not containing only letters or is longer than 10 characters");
                validUsername = "Guest";
            }
        }
        else
        {
            Debug.Log("Username is empty");
            validUsername = "Guest";
        }

        //get text from Hostnameimput field
        string HostnameOrIP = HostnameFild.GetComponent<UnityEngine.UI.InputField>().text;

        //check if HostnameOrIP is an IPv4 Address
        if (System.Text.RegularExpressions.Regex.IsMatch(HostnameOrIP, @"^([0-9]{1,3}\.){3}[0-9]{1,3}$"))
        {
            Debug.Log("HostnameOrIP is an IPv4 Address");
            validIP = HostnameOrIP;
        }
        else
        {
            Debug.Log("HostnameOrIP is not an IPv4 Address");
            //check if DNS gives entry for HostnameOrIP

            //get all ip addresses of the host
            foreach (IPAddress ip in Dns.GetHostAddresses(HostnameOrIP))
            {
                //find out if it is ipv4 and print it in console (ipv6 gets ignored)
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    Debug.Log("ipv4 found");
                    validIP = HostnameOrIP;
                }
                else
                {
                    Debug.Log("ipv4 not found defaulting to localhost");
                    validIP = "127.0.0.1";
                }

            }

        }
        //the valid ip und the valid username are now stored in the VariableStorage class this is needed to transfer the data to the next scene
        VariableStorage.isHost = false;
        VariableStorage.validIP = validIP;
        VariableStorage.validUsername = validUsername;
        SceneManager.LoadScene("MainScene");
    }
    public void QuitGame()
    {
        Debug.Log("Quit was pressed");
        Application.Quit();
    }
}
