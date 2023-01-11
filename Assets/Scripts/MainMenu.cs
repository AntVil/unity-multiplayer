using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    //This script handles all the UI elements logic in the main menu
    public GameObject UserNameHost;
    public GameObject UserNameJoin;
    public GameObject HostnameFild;
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
                string validUsername = username;
            }
          
        }else
        {
           Debug.Log("Username is empty or not valid");
           username = "Host"; 
        }

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
                string validUsername = username;
            }
          
        }else
        {
           Debug.Log("Username is empty or not valid");
           username = "Guest"; 
        }

        string HostnameOrIP = HostnameFild.GetComponent<UnityEngine.UI.InputField>().text;
    }
    public void QuitGame()
    {
        Debug.Log("Quit was pressed");
        Application.Quit();
    }
}
