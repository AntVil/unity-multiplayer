using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class VRController : MonoBehaviour
{
    GameObject Player;
    public void Update()
    {
        if (SteamVR_Actions._default.ReturnHome.GetStateDown(SteamVR_Input_Sources.Any))
        {
            Player.transform.position = Vector3.zero;
        }
    }
}
