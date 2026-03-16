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

    [Header("Movement")]
    public float moveSpeed = 8.5f;
    public float gravity = -25f;
    public float airAcceleration = 12f;
    public float explosionHorizontalDamping = 4.5f;
    public float explosionUpwardDamping = 1.25f;
    public float rocketJumpVerticalBoostMultiplier = 1.45f;
    Vector3 velocity;
    Vector3 explosionVelocity;

    [Header("Jumping")]
    public float jumpForce = 9f;
    public int maxJumps = 2;
    int jumpCount = 0;

    [Header("Crouching")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;
    public float crouchSpeed = 5f;
    
    bool isCrouching = false;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        bool isGrounded = bodyController.isGrounded;

        if (isGrounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;

            jumpCount = 0;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 moveDirection = transform.right * x + transform.forward * z;

        // Apply air acceleration when airborne
        if (!isGrounded && moveDirection.sqrMagnitude > 0.01f)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 moveAccel = moveDirection.normalized * airAcceleration;
            horizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                horizontalVelocity + moveAccel * Time.deltaTime,
                Mathf.Clamp01(Time.deltaTime * 6f)
            );
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
        }

        float currentSpeed = moveSpeed;

        isCrouching = Input.GetKey(KeyCode.LeftControl);
        
        if (isCrouching)
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

        // Jump
        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            velocity.y = jumpForce;
            jumpCount++;
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;

        // Combine movement
        Vector3 finalMove =
            moveDirection * currentSpeed +
            explosionVelocity +
            velocity;

        bodyController.Move(finalMove * Time.deltaTime);

        // Decay explosion pushback
        Vector3 horizontalExplosionVelocity = new Vector3(
            explosionVelocity.x,
            0f,
            explosionVelocity.z
        );
        horizontalExplosionVelocity = Vector3.Lerp(
            horizontalExplosionVelocity,
            Vector3.zero,
            Mathf.Max(0f, explosionHorizontalDamping) * Time.deltaTime
        );

        float verticalExplosionVelocity = explosionVelocity.y;
        float verticalDamping = verticalExplosionVelocity > 0f
            ? Mathf.Max(0f, explosionUpwardDamping)
            : Mathf.Max(0f, explosionHorizontalDamping);
        verticalExplosionVelocity = Mathf.MoveTowards(
            verticalExplosionVelocity,
            0f,
            verticalDamping * Time.deltaTime
        );

        explosionVelocity = new Vector3(
            horizontalExplosionVelocity.x,
            verticalExplosionVelocity,
            horizontalExplosionVelocity.z
        );
    }

    public void AddExplosionForce(Vector3 explosionPosition, float force, float radius)
    {
        // Use player's feet position instead of center
        Vector3 playerFeet = transform.position;

        Vector3 direction = playerFeet - explosionPosition;
        float distance = direction.magnitude;

        if (distance > radius)
            return;

        float falloff = 1f - (distance / radius);

        // Point-blank rocket jumps can produce near-zero horizontal direction.
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.up;
        }

        Vector3 push = direction.normalized * force * falloff;

        // Strong vertical boost
        push.y = Mathf.Max(
            push.y,
            force * Mathf.Max(0f, rocketJumpVerticalBoostMultiplier) * falloff
        );

        explosionVelocity += push;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }
}
