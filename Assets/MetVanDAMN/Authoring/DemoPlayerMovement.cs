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
        private Rigidbody2D rb2D;
        private Rigidbody rb3D;
        private bool isGrounded;
        private bool canCoyoteJump;
        private float coyoteTimeCounter;
        private bool isDashing;
        private float dashTimeCounter;
        private float dashCooldownCounter;
        private bool isLedgeGrabbing;
        private Vector3 ledgeGrabPosition;
        private Collider nearbyInteractable;

        // Movement state
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool runPressed;
        private bool dashPressed;
        private bool interactPressed;

        private void Awake()
        {
            // Get appropriate rigidbody component
            rb2D = GetComponent<Rigidbody2D>();
            rb3D = GetComponent<Rigidbody>();
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
            float currentSpeed = runPressed ? runSpeed : walkSpeed;
            
            if (rb2D)
            {
                // 2D movement
                Vector2 velocity = rb2D.velocity;
                velocity.x = moveInput.x * currentSpeed;
                rb2D.velocity = velocity;
            }
            else if (rb3D)
            {
                // 3D movement
                Vector3 velocity = rb3D.velocity;
                velocity.x = moveInput.x * currentSpeed;
                velocity.z = moveInput.y * currentSpeed; // Forward/backward in 3D
                rb3D.velocity = velocity;
            }

            // Handle sprite flipping for 2D
            if (moveInput.x != 0 && rb2D)
            {
                transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
            }
        }

        private void HandleJump()
        {
            if (jumpPressed && (isGrounded || canCoyoteJump) && !isLedgeGrabbing)
            {
                if (rb2D)
                {
                    rb2D.velocity = new Vector2(rb2D.velocity.x, jumpForce);
                }
                else if (rb3D)
                {
                    rb3D.velocity = new Vector3(rb3D.velocity.x, jumpForce, rb3D.velocity.z);
                }

                canCoyoteJump = false;
                isGrounded = false;
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
                    rb2D.velocity = dashDirection * dashForce;
                }
                else if (rb3D)
                {
                    rb3D.velocity = new Vector3(dashDirection.x * dashForce, rb3D.velocity.y, dashDirection.y * dashForce);
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
                    rb2D.velocity = Vector2.zero;
                    rb2D.gravityScale = 0;
                }
                else if (rb3D)
                {
                    rb3D.velocity = Vector3.zero;
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
                        rb2D.velocity = new Vector2(transform.localScale.x * walkSpeed, jumpForce * 0.8f);
                    }
                    else if (rb3D)
                    {
                        rb3D.useGravity = true;
                        rb3D.velocity = new Vector3(transform.localScale.x * walkSpeed, jumpForce * 0.8f, 0);
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
            if (interactPressed && nearbyInteractable)
            {
                // Trigger interaction with nearby object
                var interactable = nearbyInteractable.GetComponent<IDemoInteractable>();
                interactable?.Interact(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckForInteractable(other.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckForInteractable(other.gameObject);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == nearbyInteractable)
            {
                nearbyInteractable = null;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == nearbyInteractable)
            {
                nearbyInteractable = null;
            }
        }

        private void CheckForInteractable(GameObject obj)
        {
            if (obj.GetComponent<IDemoInteractable>() != null)
            {
                nearbyInteractable = obj.GetComponent<Collider>() ?? obj.GetComponent<Collider2D>();
            }
        }

        // Public API for other systems
        public bool IsGrounded => isGrounded;
        public bool IsDashing => isDashing;
        public bool IsLedgeGrabbing => isLedgeGrabbing;
        public Vector2 MoveInput => moveInput;
        public float CurrentSpeed => runPressed ? runSpeed : walkSpeed;

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
    }

    /// <summary>
    /// Interface for objects that can be interacted with
    /// </summary>
    public interface IDemoInteractable
    {
        void Interact(DemoPlayerMovement player);
    }
}