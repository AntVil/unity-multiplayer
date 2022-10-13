using UnityEngine;
using Valve.VR;

public class VRController : MonoBehaviour
{
    public ReturnHome returnHome;

    public void Update()
    {
        if (SteamVR_Actions._default.ReturnHome.GetStateDown(SteamVR_Input_Sources.Any))
        {
            returnHome.StartReturn();
            print("jo geht");
        }
        else
        {
            returnHome.StopReturn();
        }
    }
}