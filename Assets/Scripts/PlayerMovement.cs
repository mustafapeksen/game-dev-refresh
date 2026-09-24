using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rigidbody2D;
    [SerializeField]
    private InputActionAsset inputActions;
    private InputActionMap movementMap;
    private InputAction moveAction;
    private Vector2 moveValue;

    [Header("Movement")]
    [SerializeField]
    [Min(0f)]
    private float speed = 5f;
    [SerializeField]
    [Min(0f)]
    private float acceleration = 15f;
    [SerializeField]
    [Min(0f)]
    private float deceleration = 30f;
    [SerializeField]
    [Min(0f)]
    private float turnAcceleration = 25f;
    [SerializeField]
    [Range(0f, 1f)]
    private float airControlMultiplier = 0.5f;

    private InputAction jumpAction;
    [Header("Jump")]
    [SerializeField]
    [Min(0f)]
    private float jumpVelocity = 5f;
    [SerializeField]
    [Range(0f, 1f)]
    private float jumpCutMultiplier = 0.5f;

    private InputAction fastFallAction;
    [Header("Fast Fall")]
    [SerializeField]
    [Min(0f)]
    private float fastFallAcceleration = 15f;

    [SerializeField]
    [Min(0f)]
    private float maxFallSpeed = 8f;
    [SerializeField]
    private bool isFastFallActive;

    [Header("Ground Check")]
    [SerializeField]
    private Transform groundCheck;
    [SerializeField]
    private bool isGrounded;
    [SerializeField]
    private bool wasGrounded;
    [SerializeField]
    [Min(0f)]
    private float groundCheckRadius;
    [SerializeField]
    private LayerMask groundLayer;

    [Header("Coyote Time")]
    [SerializeField]
    [Min(0f)]
    private float coyoteTime;
    [SerializeField]
    private float coyoteTimeCounter;
    [SerializeField]
    private bool groundJumpAvailable = true;

    [Header("Jump Buffer")]
    [SerializeField]
    [Min(0f)]
    private float jumpBufferTime = 0.1f;
    [SerializeField]
    [Min(0f)]
    private float jumpBufferTimeCounter = 0f;
    [SerializeField]
    private bool isJumpHeld;

    [Header("Air Jump")]

    [SerializeField]
    [Min(0f)]
    private int maxAirJumps = 1;
    [SerializeField]
    private int remainingAirJumps;

    private void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();

        movementMap = inputActions.FindActionMap("Movement");
        moveAction = movementMap.FindAction("Move");

        jumpAction = movementMap.FindAction("Jump");
        fastFallAction = movementMap.FindAction("FastFall");

    }
    private void OnEnable()
    {
        jumpAction.performed += JumpPerformed;
        jumpAction.canceled += JumpCanceled;

        fastFallAction.performed += FastFallPerformed;
        fastFallAction.canceled += FastFallCanceled;

        movementMap.Enable();
    }

    private void Update()
    {
        moveValue = moveAction.ReadValue<Vector2>();
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;

        if (isGrounded && !wasGrounded)
        {
            groundJumpAvailable = true;
            remainingAirJumps = maxAirJumps;
        }


        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter = Mathf.Max(coyoteTimeCounter - Time.deltaTime, 0);
        }
        if (coyoteTimeCounter <= 0)
        {
            groundJumpAvailable = false;
        }

        jumpBufferTimeCounter = Mathf.Max(jumpBufferTimeCounter - Time.deltaTime, 0);

        wasGrounded = isGrounded;
    }

    private void FixedUpdate()
    {
        TryJump();
        float accelerationRate;
        if (moveValue.x == 0)
        {
            accelerationRate = deceleration;
        }
        else if (rigidbody2D.linearVelocity.x * moveValue.x < 0)
        {
            accelerationRate = turnAcceleration;
        }
        else
        {
            accelerationRate = acceleration;
        }

        if (!isGrounded)
        {
            accelerationRate *= airControlMultiplier;
        }

        Vector2 velocityValue = new Vector2(Mathf.MoveTowards(
            rigidbody2D.linearVelocity.x,
            moveValue.x * speed,
            accelerationRate * Time.fixedDeltaTime),
            rigidbody2D.linearVelocity.y);

        rigidbody2D.linearVelocity = velocityValue;

        if (isFastFallActive &&
    !isGrounded &&
    rigidbody2D.linearVelocityY < 0)
        {
            float newYVelocity = Mathf.MoveTowards(
                rigidbody2D.linearVelocityY,
                -maxFallSpeed,
                fastFallAcceleration * Time.fixedDeltaTime
            );

            rigidbody2D.linearVelocity = new Vector2(
                rigidbody2D.linearVelocityX,
                newYVelocity
            );
        }

        if (rigidbody2D.linearVelocityY < -maxFallSpeed)
        {
            rigidbody2D.linearVelocity = new Vector2(
                rigidbody2D.linearVelocityX,
                -maxFallSpeed
            );
        }
    }

    private void TryJump()
    {
        if (jumpBufferTimeCounter <= 0)
            return;

        if (groundJumpAvailable && coyoteTimeCounter > 0)
        {
            groundJumpAvailable = false;
            coyoteTimeCounter = 0;
        }
        else if (remainingAirJumps > 0 && !isGrounded)
        {
            remainingAirJumps--;
        }
        else
        {
            return;
        }

        float currentJumpVelocity = isJumpHeld
             ? jumpVelocity
             : jumpVelocity * jumpCutMultiplier;

        rigidbody2D.linearVelocityY = currentJumpVelocity;
        jumpBufferTimeCounter = 0;
    }

    private void JumpPerformed(InputAction.CallbackContext _)
    {
        isJumpHeld = true;
        jumpBufferTimeCounter = jumpBufferTime;
    }

    private void JumpCanceled(InputAction.CallbackContext _)
    {
        isJumpHeld = false;

        if (rigidbody2D.linearVelocityY <= 0)
            return;

        rigidbody2D.linearVelocityY *= jumpCutMultiplier;
    }
    private void FastFallPerformed(InputAction.CallbackContext _)
    {
        isFastFallActive = true;
    }

    private void FastFallCanceled(InputAction.CallbackContext _)
    {
        isFastFallActive = false;
    }

    private void OnDisable()
    {
        jumpAction.performed -= JumpPerformed;
        jumpAction.canceled -= JumpCanceled;

        coyoteTimeCounter = 0;
        jumpBufferTimeCounter = 0;
        groundJumpAvailable = true;
        isJumpHeld = false;
        wasGrounded = false;
        remainingAirJumps = 0;

        isFastFallActive = false;
        fastFallAction.performed -= FastFallPerformed;
        fastFallAction.canceled -= FastFallCanceled;

        movementMap.Disable();
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
