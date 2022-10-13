using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReturnHome : MonoBehaviour
{
    public Image background;
    public TextMeshProUGUI text;

    public GameObject player;
    public CharacterController characterController;

    private const float WILL_RETURN_HOME = 0;
    private const float CAN_RETURN_HOME = -1;
    private const float HAS_RETURNED_HOME = -2;

    public float returnHomeSeconds;
    private float returnHomeCounter = CAN_RETURN_HOME;
    private bool returning = false;

    public float returnHomeCooldownSeconds;
    private float returnHomeCooldown = 0;

    public void Update()
    {
        // teleport home logic
        if (returnHomeCooldown <= 0)
        {
            if (returning)
            {
                if (returnHomeCounter == CAN_RETURN_HOME)
                {
                    // initiate return home
                    returnHomeCounter = WILL_RETURN_HOME;
                }
                else if (returnHomeCounter > returnHomeSeconds)
                {
                    // return home if counter is full

                    // vr
                    player.transform.position = new Vector3(0, 0, 0);

                    // keyboard
                    characterController.enabled = false;
                    characterController.transform.position = new Vector3(0, 0, 0);
                    characterController.enabled = true;

                    returnHomeCounter = HAS_RETURNED_HOME;
                    returnHomeCooldown = returnHomeCooldownSeconds;
                    UpdateScreen();
                }
                else if (returnHomeCounter != HAS_RETURNED_HOME)
                {
                    // count up
                    returnHomeCounter += Time.deltaTime;
                    UpdateScreen();
                }
            }
            else if (returnHomeCounter != CAN_RETURN_HOME)
            {
                returnHomeCounter = CAN_RETURN_HOME;
                UpdateScreen();
            }
        }
        else
        {
            returnHomeCooldown -= Time.deltaTime;
            UpdateScreen();
        }
    }

    private void UpdateScreen()
    {
        if(returnHomeCooldown <= 0)
        {
            if (returnHomeCounter > 0)
            {
                text.text = $"zurück in {(int)Math.Ceiling(returnHomeSeconds - returnHomeCounter)}";
                SetAlpha(returnHomeCounter / returnHomeSeconds);
            }
            else
            {
                SetAlpha(0);
            }
        }
        else
        {
            text.text = "willkommen zurück";
            SetAlpha(returnHomeCooldown / returnHomeCooldownSeconds);
        }
    }

    private void SetAlpha(float alpha)
    {
        Color tempColor = background.color;
        tempColor.a = alpha;
        background.color = tempColor;

        tempColor = text.color;
        tempColor.a = alpha;
        text.color = tempColor;
    }

    public void StartReturn()
    {
        returning = true;
    }

    public void StopReturn()
    {
        returning = false;
    }
}