using UnityEngine;

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

    public ReturnHome returnHome;
    public float autoReturnHomeBelowY = 0.0f;
    
    [HideInInspector]
    public bool canMove = true;

    void Start(){
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update(){
        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = playerCamera.transform.TransformDirection(Vector3.forward);
        Vector3 right = playerCamera.transform.TransformDirection(Vector3.right);

        // Press Left Shift to run
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if(Input.GetButton("Jump") && canMove && characterController.isGrounded){
            moveDirection.y = jumpSpeed;
        }else{
            moveDirection.y = movementDirectionY;
        }

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        if(!characterController.isGrounded){
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if(canMove){
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            rotationY += Input.GetAxis("Mouse X") * lookSpeed;
            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }

        // teleport home logic
        if(Input.GetButton("ReturnHome") || transform.position.y < autoReturnHomeBelowY){
            returnHome.StartReturn();
        }else{
            returnHome.StopReturn();
        }
    }
}
