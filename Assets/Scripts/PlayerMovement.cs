using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public Transform graphicsTransform;
    public CharacterController bodyController;

    [Header("Settings")]
    public float cameraOffsetY = -0.35f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 150f;
    float xRotation = 0f;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -20f;
    Vector3 velocity;

    [Header("Jumping")]
    public float jumpForce = 8f;
    public int maxJumps = 2;
    int jumpCount = 0;

    [Header("Crouching")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;
    public float crouchSpeed = 3f;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        bool isGrounded = bodyController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            jumpCount = 0;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        float currentSpeed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            bodyController.height = crouchingHeight;
            bodyController.center = new Vector3(0, crouchingHeight / 2f, 0);

            cameraTransform.localPosition = new Vector3(0, crouchingHeight + cameraOffsetY, 0);
            graphicsTransform.localPosition = new Vector3(0, crouchingHeight / 2f, 0);

            currentSpeed = crouchSpeed;
        }
        else
        {
            bodyController.height = standingHeight;
            bodyController.center = new Vector3(0, standingHeight / 2f, 0);

            cameraTransform.localPosition = new Vector3(0, standingHeight + cameraOffsetY, 0);
            graphicsTransform.localPosition = new Vector3(0, standingHeight / 2f, 0);
        }

        bodyController.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            velocity.y = jumpForce;
            jumpCount++;
        }

        velocity.y += gravity * Time.deltaTime;
        bodyController.Move(velocity * Time.deltaTime);
    }
}
