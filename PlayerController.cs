using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("State")]
    public bool canMove = true; 
    [Header("Movement")]
    public float speed = 5.0f;
    public float jumpSpeed = 5.0f;

    [Header("Camera")]
    public Transform playerCamera; 
    public float sensitivity = 0.5f;
    private float xRotation = 0f; 

    private Rigidbody rigid_body;

    void Start()
    {
        rigid_body = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * sensitivity;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = mouseDelta.y * sensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        if (GetComponent<Rigidbody>().isKinematic)
        {
            return; 
        }
        if (canMove)
        {
            Vector3 input = Vector3.zero;
            if (Keyboard.current.wKey.isPressed) input.z = 1;
            if (Keyboard.current.sKey.isPressed) input.z = -1;
            if (Keyboard.current.aKey.isPressed) input.x = -1;
            if (Keyboard.current.dKey.isPressed) input.x = 1;

            Vector3 moveDir = transform.right * input.x + transform.forward * input.z;
            Vector3 currentVelocity = rigid_body.linearVelocity; 
            Vector3 targetVelocity = moveDir.normalized * speed;

            rigid_body.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);

            if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded())
            {
                rigid_body.AddForce(Vector3.up * jumpSpeed, ForceMode.Impulse);
            }
        }
        else
        {
            rigid_body.linearVelocity = Vector3.zero;
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
}