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
    public float rocketJumpVerticalBoostMultiplier = 3.8f;
    public float maxExplosionUpwardVelocity = 32f;
    public float maxExplosionHorizontalVelocity = 12f;
    public float explosionForceScale = 1.35f;
    public float movementInputDeadzone = 0.05f;
    public float residualVelocityDeadzone = 0.03f;
    public float groundedHorizontalDamping = 20f;
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

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                0f,
                Mathf.Max(0f, groundedHorizontalDamping) * Time.deltaTime
            );
            velocity.z = Mathf.MoveTowards(
                velocity.z,
                0f,
                Mathf.Max(0f, groundedHorizontalDamping) * Time.deltaTime
            );

            jumpCount = 0;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        if (Mathf.Abs(x) < movementInputDeadzone)
        {
            x = 0f;
        }
        if (Mathf.Abs(z) < movementInputDeadzone)
        {
            z = 0f;
        }
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

        CollisionFlags collisionFlags = bodyController.Move(finalMove * Time.deltaTime);

        // Decay explosion pushback
        Vector3 horizontalExplosionVelocity = new Vector3(
            explosionVelocity.x,
            0f,
            explosionVelocity.z
        );

        bool hasMoveInput = moveDirection.sqrMagnitude > 0.0001f;
        if (isGrounded && !hasMoveInput)
        {
            if (Mathf.Abs(velocity.x) < residualVelocityDeadzone)
            {
                velocity.x = 0f;
            }
            if (Mathf.Abs(velocity.z) < residualVelocityDeadzone)
            {
                velocity.z = 0f;
            }
        }

        if ((collisionFlags & CollisionFlags.Sides) != 0 && !hasMoveInput)
        {
            // If blast pushes into a wall while idle, clear horizontal impulse quickly.
            horizontalExplosionVelocity = Vector3.MoveTowards(
                horizontalExplosionVelocity,
                Vector3.zero,
                Mathf.Max(0f, explosionHorizontalDamping) * 8f * Time.deltaTime
            );
        }

        horizontalExplosionVelocity = Vector3.MoveTowards(
            horizontalExplosionVelocity,
            Vector3.zero,
            Mathf.Max(0f, explosionHorizontalDamping) * Time.deltaTime
        );
        if (horizontalExplosionVelocity.sqrMagnitude < residualVelocityDeadzone * residualVelocityDeadzone)
        {
            horizontalExplosionVelocity = Vector3.zero;
        }

        float verticalExplosionVelocity = explosionVelocity.y;
        float verticalDamping = verticalExplosionVelocity > 0f
            ? Mathf.Max(0f, explosionUpwardDamping)
            : Mathf.Max(0f, explosionHorizontalDamping);
        verticalExplosionVelocity = Mathf.MoveTowards(
            verticalExplosionVelocity,
            0f,
            verticalDamping * Time.deltaTime
        );

        if (isGrounded || (collisionFlags & CollisionFlags.Below) != 0)
        {
            // Prevent repeated tiny hops after rocket jumps once feet touch down.
            verticalExplosionVelocity = Mathf.Min(0f, verticalExplosionVelocity);
        }

        if (Mathf.Abs(verticalExplosionVelocity) < residualVelocityDeadzone)
        {
            verticalExplosionVelocity = 0f;
        }

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
        float scaledForce = Mathf.Max(0f, force) * Mathf.Max(0f, explosionForceScale);

        Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z);
        float horizontalScale = 1f;
        if (horizontalDirection.sqrMagnitude < 0.0001f)
        {
            // Point-blank blast: prefer vertical lift over arbitrary sideways shove.
            horizontalDirection = Vector3.zero;
            horizontalScale = 0f;
        }

        Vector3 push = horizontalDirection.normalized * scaledForce * 1.15f * falloff * horizontalScale;

        float verticalBoost =
            scaledForce
            * Mathf.Max(0f, rocketJumpVerticalBoostMultiplier)
            * 0.42f
            * falloff;
        push.y = verticalBoost;

        explosionVelocity += push;

        Vector3 horizontalExplosionVelocity = new Vector3(explosionVelocity.x, 0f, explosionVelocity.z);
        horizontalExplosionVelocity = Vector3.ClampMagnitude(
            horizontalExplosionVelocity,
            Mathf.Max(0f, maxExplosionHorizontalVelocity)
        );
        explosionVelocity.x = horizontalExplosionVelocity.x;
        explosionVelocity.z = horizontalExplosionVelocity.z;
        explosionVelocity.y = Mathf.Min(explosionVelocity.y, Mathf.Max(0f, maxExplosionUpwardVelocity));
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }
}
