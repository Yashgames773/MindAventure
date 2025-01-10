using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroMovementScript : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 12f;
    public float climbSpeed = 4f;

    [Header("Jump Settings")]
    public int maxJumpCount = 2;
    private bool isJUmp;
    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Ladder Detection")]
    public LayerMask ladderLayer;

    [Header("Bullets")]
    public GameObject bullets;
    public Transform bulletSpawnPoint;
    public float bulletSpeed;
    private Rigidbody2D rb;
    public Animator animator;
    public bool isGrounded;
    private bool isClimbing;
    private int jumpCount;

    private float horizontalInput;
    private float verticalInput;
    private bool isrun;
    [SerializeField] private bool isFlip;
    private bool isInvincible;
    public delegate void HealthDelegate(int val);
    public static event HealthDelegate healthDelegate;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    private void OnEnable()
    {
        FireBallScripts.onfireBallHit += PlayerHurt;
        TankBullets.onBulletHit += PlayerHurt;
        Robot.onRobotHit += PlayerHurt;
        DroneAI.onDroneHit += PlayerHurt;
        InvincibilityCells.cellPicked += Invincible;
        MIne.onExplosion += PlayerHurt;
        HealthManager.OnHealthZero += PlayerDeath;
    }

    private void Invincible(bool obj)
    {
        isInvincible = obj;
    }

    private void OnDisable()
    {
        FireBallScripts.onfireBallHit -= PlayerHurt;
        TankBullets.onBulletHit -= PlayerHurt;
        Robot.onRobotHit -= PlayerHurt;
        DroneAI.onDroneHit -= PlayerHurt;
        InvincibilityCells.cellPicked -= Invincible;
        MIne.onExplosion -= PlayerHurt;
        HealthManager.OnHealthZero -= PlayerDeath;
    }

    private void PlayerDeath()
    { 
        if(isInvincible == true)
        {
            return;
        }
        animator.SetTrigger("Death");

    }


    private void PlayerHurt(int val)
    {
        if (isInvincible == true)
        {
            return;
        }
        healthDelegate?.Invoke(val); 
        animator.SetTrigger("Hurt");
    }

    private void Update()
    {
        // Get input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

       

        if (isGrounded)
        {
            jumpCount = 0; // Reset jump count on ground
           
        }
        if(isGrounded == true) { isJUmp = false; }
        // Handle movement
        HandleMovement();

        // Handle climbing
        HandleClimbing();
      
        if(Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Fire");
            SpawnBullets();
        }
        if(Input.GetKeyDown(KeyCode.P))
        {
            animator.SetTrigger("Death");
            rb.simulated = false;
        }
        // Update animations
        UpdateAnimations();

        // Handle jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
         //   Debug.Log("Space");

            HandleJump();
        }
    }
    private void FixedUpdate()
    {
        // Apply horizontal movement
        if (!isClimbing)
        {
            float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            rb.velocity = new Vector2(horizontalInput * moveSpeed * Time.deltaTime, rb.velocity.y);
        }

        // Check if grounded
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Ground buffer logic to prevent jump inconsistency
        if (!wasGrounded && isGrounded)
        {
            jumpCount = 0; // Reset jump count when landing
            isJUmp = false;
        }
    }
    void SpawnBullets()
    {
        GameObject Bullets = Instantiate(bullets, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullets.transform.localScale = isFlip == true ? new Vector3(1f, 1f, 1f) : new Vector3(-1f, 1f, 1f);
        Bullets.GetComponent<Bullet>().speed = isFlip == true ? 10 : -10;
    }
    private void HandleMovement()
    {
        if (isClimbing) return; // Skip horizontal movement while climbing

        // Flip character sprite based on movement direction
        if (horizontalInput != 0)
        { 
            isFlip = horizontalInput != 1 ?  false : true;
            transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1, 1);
        }
    }

    private void HandleClimbing()
    {
        bool onLadder = Physics2D.OverlapCircle(transform.position, 0.1f, ladderLayer);

        if (onLadder && Mathf.Abs(verticalInput) > 0)
        {
            isClimbing = true;
           
            rb.gravityScale = 0; // Disable gravity while climbing
            rb.velocity = new Vector2(rb.velocity.x, verticalInput * climbSpeed*Time.deltaTime);
        }
        else if (isClimbing && !onLadder)
        {
            isClimbing = false;
            rb.gravityScale = 1; // Restore gravity when leaving ladder
        }
    }

    private void HandleJump()
    {
      //  Debug.Log($"Jump Attempt: isGrounded={isGrounded}, jumpCount={jumpCount}");

        // Allow jumping if grounded or jump count is within allowed range
        if (isGrounded || jumpCount < maxJumpCount)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpCount++;
         //   Debug.Log($"Jump Successful: jumpCount={jumpCount}");
        }
        else
        {
          //  Debug.Log("Jump Failed: Not grounded or jump limit reached");
        }
    }

    private void UpdateAnimations()
    {
        animator.SetFloat("xSpeed", Mathf.Abs(horizontalInput));
     //   animator.SetBool("IsClimbing", isClimbing);
        animator.SetBool("Jump", !isGrounded);
        animator.SetFloat("yVelocity", rb.velocity.y);
       // animator.SetFloat("VerticalVelocity", rb.velocity.y);
        animator.SetBool("Run", Input.GetKey(KeyCode.LeftShift) && Mathf.Abs(horizontalInput) > 0);
    }

    private void OnDrawGizmos()
    {
        // Visualize ground check
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
