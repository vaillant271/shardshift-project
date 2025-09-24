using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InputHandler))]
public class PlayerController : MonoBehaviour
{
    // Ссылки
    private Rigidbody2D rb;
    private InputHandler input;

    [Header("Movement")]
    public float moveSpeed = 7f;
    public float maxGroundSpeed = 10f;
    public float acceleration = 60f;
    public float deceleration = 70f;

    [Header("Jump")]
    public float jumpForce = 12f;
    public float coyoteTime = 0.1f;         // время после схода с платформы, когда прыжок ещё разрешён
    public float jumpBuffer = 0.12f;         // буфержатие перед приземлением
    public float lowJumpGravityMultiplier = 2f; // усиление гравитации при отпускании прыжка
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpHeld;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashTime = 0.18f;
    public float dashCooldown = 0.35f;
    private bool isDashing;
    private float dashEndTime;
    private float dashReadyTime;
    private Vector2 dashDir = Vector2.right;

    [Header("Parry")]
    public float parryDuration = 0.2f;
    private bool isParrying;
    private float parryEndTime;

    [Header("Phase Shift")]
    public PhaseShiftSystem phaseShiftSystem; // опционально, можно оставить пустым

    [Header("Ground Check")]
    public Transform groundCheck;            // пустой объект у ног
    public float groundCheckRadius = 0.15f;
    public LayerMask groundMask;             // слой земли
    private bool isGrounded;
    private bool wasGroundedLastFrame;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<InputHandler>();
    }

    private void Update()
    {
        UpdateGrounded();
        UpdateTimers();
        HandleBufferedJumpInput();
        HandleDashInput();
        HandleParryInput();
        HandlePhaseShiftInput();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            rb.velocity = dashDir * dashSpeed;
            return;
        }

        HandleHorizontalMotion();
        ApplyBetterJumpGravity();
    }

    // -------- Grounding --------
    private void UpdateGrounded()
    {
        wasGroundedLastFrame = isGrounded;
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
        }
        else
        {
            // Fallback по нижней точке коллайдера
            isGrounded = rb.velocity.y == 0f;
        }

        if (isGrounded) coyoteTimer = coyoteTime;
    }

    private void UpdateTimers()
    {
        if (coyoteTimer > 0f) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;

        if (isDashing && Time.time >= dashEndTime)
            isDashing = false;

        if (Time.time >= parryEndTime)
            isParrying = false;
    }

    // -------- Movement --------
    private void HandleHorizontalMotion()
    {
        float target = input.MoveInput.x * moveSpeed;
        float diff = target - rb.velocity.x;
        float accel = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
        float movement = Mathf.Clamp(diff, -accel * Time.fixedDeltaTime, accel * Time.fixedDeltaTime);

        // запрещать усиление горизонтали во время парри
        if (isParrying) movement = 0f;

        rb.velocity = new Vector2(
            Mathf.Clamp(rb.velocity.x + movement, -maxGroundSpeed, maxGroundSpeed),
            rb.velocity.y
        );

        // Обновление направления для рывка
        if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            dashDir = new Vector2(Mathf.Sign(input.MoveInput.x), 0f);
    }

    // -------- Jump --------
    private void HandleBufferedJumpInput()
    {
        if (input.JumpPressed) jumpBufferTimer = jumpBuffer;

        // прыжок, если есть буфер и доступен coyote
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            PerformJump();
            jumpBufferTimer = 0f;
        }

        // фиксация удержания для лучшей гравитации
        if (InputSystem.settings != null)
        {
            // если используется перформанс без прямого доступа — достаточно применения lowJumpGravityMultiplier
        }
    }

    private void PerformJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void ApplyBetterJumpGravity()
    {
        // Усиленная гравитация при отпускании прыжка
        if (rb.velocity.y > 0f && !Keyboard.current.spaceKey.isPressed)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    // -------- Dash --------
    private void HandleDashInput()
    {
        if (input.DashPressed && !isDashing && Time.time >= dashReadyTime)
        {
            isDashing = true;
            dashEndTime = Time.time + dashTime;
            dashReadyTime = Time.time + dashCooldown;

            // направление по вводу; если нет ввода — по последнему направлению
            Vector2 dir = input.MoveInput.sqrMagnitude > 0.01f
                ? new Vector2(Mathf.Sign(input.MoveInput.x == 0 ? dashDir.x : input.MoveInput.x), 0f)
                : dashDir;

            dashDir = dir == Vector2.zero ? Vector2.right : dir;

            // обнулить вертикаль для стабильного рывка по земле
            rb.velocity = new Vector2(0f, 0f);
        }
    }

    // -------- Parry --------
    private void HandleParryInput()
    {
        if (input.ParryPressed)
        {
            isParrying = true;
            parryEndTime = Time.time + parryDuration;
            // Здесь можно активировать временную неуязвимость/слои коллизий
        }
    }

    // -------- Phase Shift --------
    private void HandlePhaseShiftInput()
    {
        if (input.PhaseShiftPressed)
        {
            // Безопасный вызов опциональной системы
            if (phaseShiftSystem != null) phaseShiftSystem.TogglePhase();
        }
    }

    // -------- Gizmos для отладки приземления --------
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}