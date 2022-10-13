using System;
using UnityEngine;
using Valve.VR;

public class VRController : MonoBehaviour
{
    private const float WILL_RETURN_HOME = 0;
    private const float CAN_RETURN_HOME = -1;
    private const float HAS_RETURNED_HOME = -2;

    public GameObject Player;
    public BlackScreen blackScreen;
    public float returnHomeSeconds;
    private float returnHomeCounter = CAN_RETURN_HOME;
    

    public void Update()
    {
        // teleport home logic
        if (SteamVR_Actions._default.ReturnHome.GetStateDown(SteamVR_Input_Sources.Any))
        {
            if (returnHomeCounter == CAN_RETURN_HOME)
            {
                // initiate return home
                returnHomeCounter = WILL_RETURN_HOME;
            }
            else if (returnHomeCounter > returnHomeSeconds)
            {
                // return home if counter is full
                Player.transform.position = new Vector3(0, 0, 0);
                returnHomeCounter = HAS_RETURNED_HOME;
                updateBlackScreen();
            }
            else if (returnHomeCounter != HAS_RETURNED_HOME)
            {
                // count up
                returnHomeCounter += Time.deltaTime;
                updateBlackScreen();
            }
        }
        else if (returnHomeCounter != CAN_RETURN_HOME)
        {
            returnHomeCounter = CAN_RETURN_HOME;
            updateBlackScreen();
        }
    }

    public void updateBlackScreen()
    {
        if (returnHomeCounter > 0)
        {
            blackScreen.SetText($"zurück in {(int)Math.Ceiling(returnHomeSeconds - returnHomeCounter)}");
            blackScreen.SetAlpha(returnHomeCounter / returnHomeSeconds);
        }
        else
        {
            blackScreen.SetAlpha(0);
        }
    }
}
