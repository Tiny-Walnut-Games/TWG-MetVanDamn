#nullable enable
using UnityEngine;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    /// <summary>
    /// Complete player movement system supporting all MetVanDAMN movement capabilities.
    /// Implements walk, run, jump, coyote time, dash/evade, ledge grab, and context actions.
    /// </summary>
    public class DemoPlayerMovement : MonoBehaviour
        {
        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float runSpeed = 8f;
        public float jumpForce = 12f;
        public float dashForce = 15f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 1f;

        [Header("Coyote Time")]
        public float coyoteTime = 0.2f;

        [Header("Ledge Grab")]
        public float ledgeGrabRange = 1.5f;
        public LayerMask ledgeLayer = 1;

        [Header("Ground Detection")]
        public LayerMask groundLayer = 1;
        public float groundCheckDistance = 0.1f;

        [Header("Input")]
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode runKey = KeyCode.LeftShift;
        public KeyCode dashKey = KeyCode.LeftControl;
        public KeyCode interactKey = KeyCode.E;

        // Private state
        private Rigidbody2D rb2D = null!; // assigned in Awake if present
        private Rigidbody rb3D = null!;    // assigned in Awake if present
        private bool isGrounded;
        private bool canCoyoteJump;
        private float coyoteTimeCounter;
        private bool isDashing;
        private float dashTimeCounter;
        private float dashCooldownCounter;
        private bool isLedgeGrabbing;
        private Vector3 ledgeGrabPosition;
        // Nullability annihilation: represent absence of interactable with a dedicated placeholder GameObject
        private GameObject _interactablePlaceholder = null!;
        private GameObject interactableTarget = null!; // never null after Awake (points to placeholder when none)

        // Movement state
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool runPressed;
        private bool dashPressed;
        private bool interactPressed;

        private void Awake()
            {
            // Get appropriate rigidbody component(s); keep one or both (hybrid support)
            rb2D = GetComponent<Rigidbody2D>();
            rb3D = GetComponent<Rigidbody>();

            // Create placeholder interactable to avoid null checks everywhere
            var placeholder = new GameObject("__MovementInteractablePlaceholder__");
            placeholder.hideFlags = HideFlags.HideAndDontSave;
            _interactablePlaceholder = placeholder;
            interactableTarget = _interactablePlaceholder;
            }

        private void Update()
            {
            HandleInput();
            UpdateMovementState();
            HandleMovement();
            HandleJump();
            HandleDash();
            HandleLedgeGrab();
            HandleInteraction();
            }

        private void HandleInput()
            {
            // Get movement input
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical"); // For 3D movement

            // Get button inputs
            jumpPressed = Input.GetKeyDown(jumpKey);
            runPressed = Input.GetKey(runKey);
            dashPressed = Input.GetKeyDown(dashKey);
            interactPressed = Input.GetKeyDown(interactKey);
            }

        private void UpdateMovementState()
            {
            // Ground detection
            Vector3 groundCheckPos = transform.position - Vector3.up * groundCheckDistance;
            isGrounded = Physics2D.OverlapCircle(groundCheckPos, 0.2f, groundLayer) ||
                        (rb3D && Physics.CheckSphere(groundCheckPos, 0.2f, groundLayer));

            // Coyote time logic
            if (isGrounded)
                {
                canCoyoteJump = true;
                coyoteTimeCounter = coyoteTime;
                }
            else
                {
                coyoteTimeCounter -= Time.deltaTime;
                if (coyoteTimeCounter <= 0)
                    {
                    canCoyoteJump = false;
                    }
                }

            // Dash cooldown
            if (dashCooldownCounter > 0)
                {
                dashCooldownCounter -= Time.deltaTime;
                }

            // Dash duration
            if (isDashing)
                {
                dashTimeCounter -= Time.deltaTime;
                if (dashTimeCounter <= 0)
                    {
                    isDashing = false;
                    }
                }
            }

        private void HandleMovement()
            {
            if (isDashing || isLedgeGrabbing) return;

            // Determine movement speed
            float currentSpeed = GetModifiedSpeed(runPressed ? runSpeed : walkSpeed);

            if (rb2D)
                {
                // 2D movement
                Vector2 velocity = rb2D.linearVelocity;
                velocity.x = moveInput.x * currentSpeed;
                rb2D.linearVelocity = velocity;
                }
            else if (rb3D)
                {
                // 3D movement
                Vector3 velocity = rb3D.linearVelocity;
                velocity.x = moveInput.x * currentSpeed;
                velocity.z = moveInput.y * currentSpeed; // Forward/backward in 3D
                rb3D.linearVelocity = velocity;
                }

            // Handle sprite flipping for 2D
            if (moveInput.x != 0 && rb2D)
                {
                transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
                }
            }

        private void HandleJump()
            {
            if (jumpPressed && !isLedgeGrabbing)
                {
                // Regular jump (ground or coyote time)
                if (isGrounded || canCoyoteJump)
                    {
                    if (rb2D)
                        {
                        rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, jumpForce);
                        }
                    else if (rb3D)
                        {
                        rb3D.linearVelocity = new Vector3(rb3D.linearVelocity.x, jumpForce, rb3D.linearVelocity.z);
                        }

                    canCoyoteJump = false;
                    isGrounded = false;
                    hasUsedDoubleJump = false; // Reset double jump on ground jump
                    }
                // Double jump
                else if (CanDoubleJump())
                    {
                    PerformDoubleJump();
                    }
                // Wall jump
                else if (CanWallJump())
                    {
                    PerformWallJump();
                    hasUsedDoubleJump = false; // Reset double jump on wall jump
                    }
                }
            }

        private void HandleDash()
            {
            if (dashPressed && dashCooldownCounter <= 0 && !isLedgeGrabbing)
                {
                Vector2 dashDirection = moveInput.normalized;
                if (dashDirection.magnitude < 0.1f)
                    {
                    // Default dash direction (forward)
                    dashDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                    }

                isDashing = true;
                dashTimeCounter = dashDuration;
                dashCooldownCounter = dashCooldown;

                if (rb2D)
                    {
                    rb2D.linearVelocity = dashDirection * dashForce;
                    }
                else if (rb3D)
                    {
                    rb3D.linearVelocity = new Vector3(dashDirection.x * dashForce, rb3D.linearVelocity.y, dashDirection.y * dashForce);
                    }
                }
            }

        private void HandleLedgeGrab()
            {
            if (isGrounded || isDashing) return;

            // Check for ledge grab opportunity
            Vector3 ledgeCheckPos = transform.position + Vector3.right * transform.localScale.x * ledgeGrabRange;

            bool canGrabLedge = false;
            RaycastHit2D hit2D = Physics2D.Raycast(ledgeCheckPos, Vector2.down, ledgeGrabRange, ledgeLayer);
            RaycastHit hit3D;

            if (rb2D && hit2D.collider)
                {
                canGrabLedge = true;
                ledgeGrabPosition = hit2D.point;
                }
            else if (rb3D && Physics.Raycast(ledgeCheckPos, Vector3.down, out hit3D, ledgeGrabRange, ledgeLayer))
                {
                canGrabLedge = true;
                ledgeGrabPosition = hit3D.point;
                }

            if (canGrabLedge && moveInput.x != 0 && !isLedgeGrabbing)
                {
                // Start ledge grab
                isLedgeGrabbing = true;

                if (rb2D)
                    {
                    rb2D.linearVelocity = Vector2.zero;
                    rb2D.gravityScale = 0;
                    }
                else if (rb3D)
                    {
                    rb3D.linearVelocity = Vector3.zero;
                    rb3D.useGravity = false;
                    }

                // Position player at ledge
                transform.position = ledgeGrabPosition + Vector3.up * 0.5f;
                }
            else if (isLedgeGrabbing)
                {
                // Handle ledge grab input
                if (jumpPressed)
                    {
                    // Ledge climb
                    isLedgeGrabbing = false;

                    if (rb2D)
                        {
                        rb2D.gravityScale = 1;
                        rb2D.linearVelocity = new Vector2(transform.localScale.x * GetModifiedSpeed(walkSpeed), jumpForce * 0.8f);
                        }
                    else if (rb3D)
                        {
                        rb3D.useGravity = true;
                        rb3D.linearVelocity = new Vector3(transform.localScale.x * GetModifiedSpeed(walkSpeed), jumpForce * 0.8f, 0);
                        }
                    }
                else if (moveInput.x == 0 || Mathf.Sign(moveInput.x) != transform.localScale.x)
                    {
                    // Release ledge
                    isLedgeGrabbing = false;

                    if (rb2D)
                        {
                        rb2D.gravityScale = 1;
                        }
                    else if (rb3D)
                        {
                        rb3D.useGravity = true;
                        }
                    }
                }
            }

        private void HandleInteraction()
            {
            if (interactPressed && interactableTarget != _interactablePlaceholder)
                {
                if (interactableTarget.TryGetComponent<IDemoInteractable>(out IDemoInteractable? interactable))
                    {
                    interactable.Interact(this);
                    }
                }
            }

        private void OnTriggerEnter2D(Collider2D other) => CheckForInteractable(other.gameObject);

        private void OnTriggerEnter(Collider other) => CheckForInteractable(other.gameObject);

        private void OnTriggerExit2D(Collider2D other)
            {
            if (interactableTarget != _interactablePlaceholder && other.gameObject == interactableTarget)
                {
                interactableTarget = _interactablePlaceholder;
                }
            }

        private void OnTriggerExit(Collider other)
            {
            if (interactableTarget != _interactablePlaceholder && other.gameObject == interactableTarget)
                {
                interactableTarget = _interactablePlaceholder;
                }
            }

        private void CheckForInteractable(GameObject obj)
            {
            if (obj.GetComponent<IDemoInteractable>() != null)
                {
                interactableTarget = obj;
                }
            }

        // Public API for other systems
        public bool IsGrounded => isGrounded;
        public bool IsDashing => isDashing;
        public bool IsLedgeGrabbing => isLedgeGrabbing;
        public bool HasInteractable => interactableTarget != _interactablePlaceholder;
        public Vector2 MoveInput => moveInput;
        public float CurrentSpeed => GetModifiedSpeed(runPressed ? runSpeed : walkSpeed);

        private void OnDrawGizmosSelected()
            {
            // Draw ground check
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 groundCheckPos = transform.position - Vector3.up * groundCheckDistance;
            Gizmos.DrawWireSphere(groundCheckPos, 0.2f);

            // Draw ledge grab range
            Gizmos.color = Color.yellow;
            Vector3 ledgeCheckPos = transform.position + Vector3.right * transform.localScale.x * ledgeGrabRange;
            Gizmos.DrawRay(ledgeCheckPos, Vector3.down * ledgeGrabRange);

            // Draw dash direction
            if (isDashing)
                {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, (Vector3)moveInput.normalized * 2f);
                }
            }

        // Speed multiplier system for inventory buffs
        private float speedMultiplier = 1f;

        // Upgrade system support
        private bool doubleJumpEnabled = false;
        private bool wallJumpEnabled = false;
        private bool aimedDashEnabled = false;
        private bool hasUsedDoubleJump = false;

        public void ApplySpeedMultiplier(float multiplier)
            {
            speedMultiplier *= multiplier;
            Debug.Log($"💨 Speed multiplier applied: x{multiplier} (Total: x{speedMultiplier})");
            }

        public void RemoveSpeedMultiplier(float multiplier)
            {
            if (speedMultiplier > 0)
                {
                speedMultiplier /= multiplier;
                speedMultiplier = Mathf.Max(0.1f, speedMultiplier); // Prevent negative speed
                Debug.Log($"💨 Speed multiplier removed: ÷{multiplier} (Total: x{speedMultiplier})");
                }
            }

        public float GetCurrentSpeedMultiplier()
            {
            return speedMultiplier;
            }

        // Helper method for applying speed multiplier to movement
        private float GetModifiedSpeed(float baseSpeed)
            {
            return baseSpeed * speedMultiplier;
            }

        /// <summary>
        /// Set movement stats from upgrade system
        /// </summary>
        public void SetStats(float newWalkSpeed, float newRunSpeed, float newJumpForce, float newDashForce, float newDashCooldown)
            {
            walkSpeed = newWalkSpeed;
            runSpeed = newRunSpeed;
            jumpForce = newJumpForce;
            dashForce = newDashForce;
            dashCooldown = newDashCooldown;
            }

        /// <summary>
        /// Enable/disable double jump capability
        /// </summary>
        public void EnableDoubleJump(bool enabled)
            {
            doubleJumpEnabled = enabled;
            if (!enabled)
                {
                hasUsedDoubleJump = false;
                }
            }

        /// <summary>
        /// Enable/disable wall jump capability
        /// </summary>
        public void EnableWallJump(bool enabled)
            {
            wallJumpEnabled = enabled;
            }

        /// <summary>
        /// Enable/disable aimed dash capability
        /// </summary>
        public void EnableAimedDash(bool enabled)
            {
            aimedDashEnabled = enabled;
            }

        /// <summary>
        /// Check if player can double jump
        /// </summary>
        private bool CanDoubleJump()
            {
            return doubleJumpEnabled && !isGrounded && !hasUsedDoubleJump && !isLedgeGrabbing;
            }

        /// <summary>
        /// Check if player can wall jump
        /// </summary>
        private bool CanWallJump()
            {
            if (!wallJumpEnabled || isGrounded) return false;

            // Simple wall detection - check for collision to either side
            Vector3 leftCheck = transform.position + Vector3.left * 0.6f;
            Vector3 rightCheck = transform.position + Vector3.right * 0.6f;

            bool leftWall = rb2D ? Physics2D.OverlapCircle(leftCheck, 0.3f, groundLayer) :
                                 Physics.CheckSphere(leftCheck, 0.3f, groundLayer);
            bool rightWall = rb2D ? Physics2D.OverlapCircle(rightCheck, 0.3f, groundLayer) :
                                  Physics.CheckSphere(rightCheck, 0.3f, groundLayer);

            return leftWall || rightWall;
            }

        /// <summary>
        /// Perform double jump
        /// </summary>
        private void PerformDoubleJump()
            {
            hasUsedDoubleJump = true;

            if (rb2D)
                {
                rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, jumpForce * 0.9f); // Slightly less powerful than ground jump
                }
            else if (rb3D)
                {
                rb3D.linearVelocity = new Vector3(rb3D.linearVelocity.x, jumpForce * 0.9f, rb3D.linearVelocity.z);
                }

            Debug.Log("🦘 Double jump!");
            }

        /// <summary>
        /// Perform wall jump
        /// </summary>
        private void PerformWallJump()
            {
            Vector3 leftCheck = transform.position + Vector3.left * 0.6f;
            Vector3 rightCheck = transform.position + Vector3.right * 0.6f;

            bool leftWall = rb2D ? Physics2D.OverlapCircle(leftCheck, 0.3f, groundLayer) :
                                 Physics.CheckSphere(leftCheck, 0.3f, groundLayer);
            bool rightWall = rb2D ? Physics2D.OverlapCircle(rightCheck, 0.3f, groundLayer) :
                                  Physics.CheckSphere(rightCheck, 0.3f, groundLayer);

            Vector2 wallJumpDirection = Vector2.zero;
            if (leftWall) wallJumpDirection = new Vector2(1, 1).normalized;
            else if (rightWall) wallJumpDirection = new Vector2(-1, 1).normalized;

            if (wallJumpDirection != Vector2.zero)
                {
                if (rb2D)
                    {
                    rb2D.linearVelocity = wallJumpDirection * jumpForce * 0.8f;
                    }
                else if (rb3D)
                    {
                    rb3D.linearVelocity = new Vector3(wallJumpDirection.x * jumpForce * 0.8f, wallJumpDirection.y * jumpForce * 0.8f, 0);
                    }

                Debug.Log("🧗 Wall jump!");
                }
            }
        }

    /// <summary>
    /// Interface for objects that can be interacted with
    /// </summary>
    public interface IDemoInteractable
        {
        void Interact(DemoPlayerMovement player);
        }
    }
