using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Component.Transforming;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5.0f;
    public Transform cameraTransform;
    public float jumpForce = 5f;
    public float slopeLimit = 45.0f; // Slope limit in degrees
    public float airControlForce = 5.0f; 
    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isJumpPressed = false;
    private bool isGrounded = false;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return; // Only process for the local player

        isGrounded = IsGrounded();
        UpdateMoveDirection();

        if (IsServer)
        {
            if (CanMoveOnSlope())
            {
                Vector3 desiredVelocity = moveDirection * moveSpeed;
                if (isGrounded)
                {
                    rb.velocity = new Vector3(desiredVelocity.x, rb.velocity.y, desiredVelocity.z);
                }
                else
                {
                    rb.velocity = Vector3.Lerp(rb.velocity, new Vector3(desiredVelocity.x, rb.velocity.y, desiredVelocity.z), Time.fixedDeltaTime * airControlForce);
                }
            }

            if (isJumpPressed && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isJumpPressed = false;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
        {
            // Disable components that should only be active on the owner
            // For example, disable input-related components or scripts
        }
    }


    private bool IsGrounded()
    {
        RaycastHit hit;
        float distance = 5f;
        Vector3 offset = Vector3.down;

        return Physics.Raycast(transform.position, offset, out hit, distance) && hit.collider.CompareTag("Ground");
    }

    private bool CanMoveOnSlope()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            return slopeAngle <= slopeLimit;
        }
        return true; // If no ground detected, assume movement is okay
    }

    public void Move(Vector2 newMoveInput)
    {
        moveInput = newMoveInput;
    }

    private void UpdateMoveDirection()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        if (moveDirection != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, moveSpeed * Time.deltaTime);
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && IsGrounded())
        {
            isJumpPressed = true;
        }
    }
}
