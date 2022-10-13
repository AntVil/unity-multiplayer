using System;
using UnityEngine;
using System.Collections;

public class KeyboardController : MonoBehaviour
{
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private float rotationY = 0;

    private const float WILL_RETURN_HOME = 0;
    private const float CAN_RETURN_HOME = -1;
    private const float HAS_RETURNED_HOME = -2;

    public BlackScreen blackScreen;
    public float returnHomeSeconds;
    private float returnHomeCounter = CAN_RETURN_HOME;
    
    [HideInInspector]
    public bool canMove = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        updateBlackScreen();
    }

    void Update()
    {
        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        // Press Left Shift to run
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            rotationY += Input.GetAxis("Mouse X") * lookSpeed;
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }

        // teleport home logic
        if (Input.GetButton("ReturnHome"))
        {
            if (returnHomeCounter == CAN_RETURN_HOME)
            {
                // initiate return home
                returnHomeCounter = WILL_RETURN_HOME;
            }
            else if (returnHomeCounter > returnHomeSeconds)
            {
                // return home if counter is full
                characterController.enabled = false;
                transform.position = new Vector3(0, 0, 0);
                characterController.enabled = true;
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
        else if(returnHomeCounter != CAN_RETURN_HOME) {
            returnHomeCounter = CAN_RETURN_HOME;
            updateBlackScreen();
        }
    }

    public void updateBlackScreen()
    {
        if(returnHomeCounter > 0)
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