using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;


public class MainMenu : MonoBehaviour
{
    //This script handles all the UI elements logic in the main menu
    public GameObject UserNameHost;
    public GameObject UserNameJoin;
    public GameObject HostnameFildJoin;
    public GameObject HostnameFildHost;
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

        //get text from Hostnameimput field
        string fildIP = HostnameFildHost.GetComponent<UnityEngine.UI.InputField>().text;


        if (System.Text.RegularExpressions.Regex.IsMatch(fildIP, @"^([0-9]{1,3}\.){3}[0-9]{1,3}$"))
        {
            Debug.Log("fildIP is an IPv4 Address");
            //check if the ip adress is own ip adress
            //find out own ip addresses
            string hostName = System.Net.Dns.GetHostName();

            try
            {
                foreach (IPAddress ip in Dns.GetHostAddresses(hostName))
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        if (ip.ToString() == fildIP || fildIP == "127.0.0.1")
                        {
                            validIP = fildIP;
                            break;
                        }
                    }
                }
                if (validIP == null)
                {
                    HostnameFildHost.GetComponent<UnityEngine.UI.InputField>().image.color = Color.red;
                }
            }
            catch (System.Exception)
            {
                HostnameFildHost.GetComponent<UnityEngine.UI.InputField>().image.color = Color.red;
            }
        }
        else
        {
            Debug.Log("HostnameOrIP is not an IPv4 Address");
            HostnameFildHost.GetComponent<UnityEngine.UI.InputField>().image.color = Color.red;
        }


        if (validIP != null)
        {
            //the username and isHost is now stored in the VariableStorage class this is needed to transfer the data to the next scene
            VariableStorage.isHost = true;
            VariableStorage.validIP = validIP;
            VariableStorage.validUsername = validUsername;
            SceneManager.LoadScene("MainScene");
        }
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
        string HostnameOrIP = HostnameFildJoin.GetComponent<UnityEngine.UI.InputField>().text;

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
            try
            {
                foreach (IPAddress ip in Dns.GetHostAddresses(HostnameOrIP))
                {
                    //find out if it is ipv4 and print it in console (ipv6 gets ignored)
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        Debug.Log("ipv4 found");
                        validIP = HostnameOrIP;
                    }
                }
                if(validIP == null)
                {
                    HostnameFildJoin.GetComponent<UnityEngine.UI.InputField>().image.color = Color.red;
                }
            }
            catch (System.Exception)
            {
                Debug.Log("HostnameOrIP is not a valid Hostname or IP");
                HostnameFildJoin.GetComponent<UnityEngine.UI.InputField>().image.color = Color.red;
            }

        }
        if (validIP != null)
        {
            //the valid ip und the valid username are now stored in the VariableStorage class this is needed to transfer the data to the next scene
            VariableStorage.isHost = false;
            VariableStorage.validIP = validIP;
            VariableStorage.validUsername = validUsername;
            SceneManager.LoadScene("MainScene");
        }
    }
    public void QuitGame()
    {
        Debug.Log("Quit was pressed");
        Application.Quit();
    }

    public void Help()
    {
        Application.OpenURL("https://support.microsoft.com/en-us/windows/find-your-ip-address-in-windows-f21a9bbc-c582-55cd-35e0-73431160a1b9#Category=Windows_10");
    }
}
