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

    private enum WallSide
    {
        None,
        Left,
        Right
    }

    [Header("Wall Jump")]
    [SerializeField]
    private Transform leftWallCheck;
    [SerializeField]
    private float leftWallCheckRadius;
    [SerializeField]
    private Transform rightWallCheck;
    [SerializeField]
    private float rightWallCheckRadius;
    [SerializeField]
    private LayerMask wallLayer;

    [SerializeField]
    private WallSide currentWallSide = WallSide.None;
    [SerializeField]
    private WallSide previousWallSide = WallSide.None;
    [SerializeField]
    private WallSide lastWallJumpSide = WallSide.None;

    [SerializeField]
    [Min(0f)]
    private float wallHangTime = 3f;
    [SerializeField]
    [Min(0f)]
    private float wallHangTimeCounter;
    [SerializeField]
    private bool isWallHanging = false;
    [SerializeField]
    private bool isTouchingLeftWall = false;
    [SerializeField]
    private bool isTouchingRightWall = false;

    [SerializeField]
    [Min(0f)]
    private float wallJumpHorizontalVelocity = 5f;

    [SerializeField]
    [Min(0f)]
    private float wallJumpVerticalVelocity = 5f;

    [SerializeField]
    [Min(0f)]
    private float wallJumpControlLockTime = 0.15f;
    private float wallJumpControlLockCounter;

    private float defaultGravityScale;

    private void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        defaultGravityScale = rigidbody2D.gravityScale;

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
        isTouchingLeftWall = Physics2D.OverlapCircle(leftWallCheck.position, leftWallCheckRadius, wallLayer) != null;
        isTouchingRightWall = Physics2D.OverlapCircle(rightWallCheck.position, rightWallCheckRadius, wallLayer) != null;

        if (isGrounded && !wasGrounded)
        {
            groundJumpAvailable = true;
            remainingAirJumps = maxAirJumps;
            lastWallJumpSide = WallSide.None;
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


        if (isTouchingLeftWall && isTouchingRightWall)
        {
            currentWallSide = WallSide.None;
        }
        else if (isTouchingLeftWall)
        {
            currentWallSide = WallSide.Left;
        }
        else if (isTouchingRightWall)
        {
            currentWallSide = WallSide.Right;
        }
        else
        {
            currentWallSide = WallSide.None;
        }


        if (currentWallSide != previousWallSide && !isGrounded && currentWallSide != WallSide.None)
        {
            wallHangTimeCounter = wallHangTime;
        }
        else if (!isGrounded && currentWallSide != WallSide.None)
        {
            wallHangTimeCounter = Mathf.Max(wallHangTimeCounter - Time.deltaTime, 0);
        }
        else
        {
            wallHangTimeCounter = wallHangTime;
        }

        if (!isGrounded &&
    currentWallSide != WallSide.None &&
    wallHangTimeCounter > 0 && lastWallJumpSide != currentWallSide && rigidbody2D.linearVelocityY <= 0)
        {
            isWallHanging = true;
        }
        else
        {
            isWallHanging = false;
        }

        wallJumpControlLockCounter =
    Mathf.Max(wallJumpControlLockCounter - Time.deltaTime, 0);

        jumpBufferTimeCounter = Mathf.Max(jumpBufferTimeCounter - Time.deltaTime, 0);

        wasGrounded = isGrounded;
        previousWallSide = currentWallSide;
    }

    private void FixedUpdate()
    {
        TryJump();
        if (isWallHanging)
        {
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.gravityScale = 0;
            return;
        }

        rigidbody2D.gravityScale = defaultGravityScale;

        if (wallJumpControlLockCounter <= 0)
        {
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
        }

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
        Vector2 currentJumpVelocity = rigidbody2D.linearVelocity;
        if (jumpBufferTimeCounter <= 0)
            return;

        if (groundJumpAvailable && coyoteTimeCounter > 0)
        {
            groundJumpAvailable = false;
            coyoteTimeCounter = 0;
            currentJumpVelocity = new Vector2(rigidbody2D.linearVelocityX, jumpVelocity);
        }
        else if (currentWallSide != WallSide.None &&
          currentWallSide != lastWallJumpSide)
        {
            lastWallJumpSide = currentWallSide;
            if (currentWallSide == WallSide.Left)
            {
                currentJumpVelocity = new Vector2(wallJumpHorizontalVelocity, wallJumpVerticalVelocity);
            }
            else if (currentWallSide == WallSide.Right)
            {
                currentJumpVelocity = new Vector2(-wallJumpHorizontalVelocity, wallJumpVerticalVelocity);
            }

            wallJumpControlLockCounter = wallJumpControlLockTime;

            isWallHanging = false;
            wallHangTimeCounter = 0;
        }
        else if (remainingAirJumps > 0 && !isGrounded)
        {
            remainingAirJumps--;
            currentJumpVelocity = new Vector2(rigidbody2D.linearVelocityX, jumpVelocity);
        }
        else
        {
            return;
        }

        currentJumpVelocity.y = isJumpHeld
                   ? currentJumpVelocity.y
                   : currentJumpVelocity.y * jumpCutMultiplier;
        rigidbody2D.linearVelocity = currentJumpVelocity;

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

        currentWallSide = WallSide.None;
        previousWallSide = WallSide.None;
        lastWallJumpSide = WallSide.None;

        wallHangTimeCounter = 0;
        isWallHanging = false;

        isTouchingLeftWall = false;
        isTouchingRightWall = false;

        wallJumpControlLockCounter = 0;
        rigidbody2D.gravityScale = defaultGravityScale;

        isFastFallActive = false;
        fastFallAction.performed -= FastFallPerformed;
        fastFallAction.canceled -= FastFallCanceled;

        movementMap.Disable();
    }

    private void OnDrawGizmosSelected()
    {
        if (leftWallCheck != null)
            Gizmos.DrawWireSphere(leftWallCheck.position, leftWallCheckRadius);

        if (rightWallCheck != null)
            Gizmos.DrawWireSphere(rightWallCheck.position, rightWallCheckRadius);

        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
