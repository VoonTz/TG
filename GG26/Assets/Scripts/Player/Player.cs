using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Sprint")]
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float maxStamina = 3f;
    [SerializeField] private float sprintCooldown = 10f;
    [SerializeField] private float slowRechargeTime = 45f;

    [Header("Aim")]
    [SerializeField] private Transform handPivot;

    [Header("Shoot")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private float fireRate = 1f;
    private float nextFireTime;

    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private float walkAnimFps = 10f;
    [SerializeField] private float sprintAnimFps = 16f;

    [Header("Health")]
    [SerializeField] private int healAmount = 6;

    [Header("Heal Charges")]
    [SerializeField] private int killsToRestoreHeal = 10;
    [SerializeField] private KeyCode healKey = KeyCode.E;

    [Header("Collision Damage")]
    [SerializeField] private int collisionDamage = 1;
    [SerializeField] private float collisionInvulnTime = 0.6f;

    [Header("Damage / Death Animation")]
    [SerializeField] private float hitInvulnTime = 1f; // 1s de invuln enquanto toca "dano"

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera mainCam;

    private float currentStamina;
    private bool isSprinting;

    private bool isExhausted;
    private float cooldownTimer;

    private float collisionInvulnTimer = 0f;
    private float hitInvulnTimer = 0f;

    private bool isDead = false;

    private static Player instance;

    private void Awake()
    {
        instance = this;

        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        if (anim == null) anim = GetComponent<Animator>();
    }

    private void Start()
    {
        currentStamina = maxStamina;
        isExhausted = false;
        cooldownTimer = 0f;

        if (GameManager.Instance == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        GameManager.Instance.currentHealth = Mathf.Clamp(
            GameManager.Instance.currentHealth,
            0,
            GameManager.Instance.maxHealth
        );

        isDead = (GameManager.Instance.currentHealth <= 0);
        if (anim != null)
            anim.SetBool("Dead", isDead);
    }

    private void Update()
    {
        // timers
        if (collisionInvulnTimer > 0f) collisionInvulnTimer -= Time.deltaTime;
        if (hitInvulnTimer > 0f) hitInvulnTimer -= Time.deltaTime;

        if (isDead)
        {
            // trava tudo quando morreu
            rb.linearVelocity = Vector2.zero;
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        HandleSprint();
        RotateHandToMouse();
        HandleShoot();
        UpdateAnimations();
        HandleHealInput();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        float speed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        rb.linearVelocity = moveInput * speed;
    }

    private void HandleSprint()
    {
        if (isExhausted)
        {
            isSprinting = false;
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)
            {
                isExhausted = false;
                currentStamina = maxStamina;
                cooldownTimer = 0f;
            }
            return;
        }

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);
        bool isMoving = moveInput.magnitude > 0f;

        if (shiftHeld && isMoving && currentStamina > 0f)
        {
            isSprinting = true;
            currentStamina -= Time.deltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isSprinting = false;
                isExhausted = true;
                cooldownTimer = sprintCooldown;
            }
        }
        else
        {
            isSprinting = false;

            if (currentStamina < maxStamina)
            {
                float rechargeRate = maxStamina / slowRechargeTime;
                currentStamina += rechargeRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            }
        }
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.0001f;

        anim.SetBool("Idle", !isMoving);
        anim.SetBool("Front", isMoving);

        if (!isMoving) anim.speed = 1f;
        else anim.speed = isSprinting ? (sprintAnimFps / walkAnimFps) : 1f;
    }

    private void RotateHandToMouse()
    {
        if (handPivot == null || mainCam == null) return;
        if (isDead) return;

        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (mouseWorld - handPivot.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        handPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void HandleShoot()
    {
        if (isDead) return;
        if (bulletPrefab == null || muzzle == null) return;
        if (!Input.GetMouseButton(0)) return;

        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + (1f / fireRate);

        GameObject bullet = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
        Rigidbody2D brb = bullet.GetComponent<Rigidbody2D>();
        if (brb != null)
            brb.linearVelocity = (Vector2)muzzle.right * bulletSpeed;
    }

    private void HandleHealInput()
    {
        if (isDead) return;

        if (!Input.GetKeyDown(healKey)) return;
        if (GameManager.Instance.healCharges <= 0) return;
        if (GameManager.Instance.currentHealth <= 0) return;

        int before = GameManager.Instance.currentHealth;
        GameManager.Instance.currentHealth = Mathf.Min(
            GameManager.Instance.maxHealth,
            GameManager.Instance.currentHealth + healAmount
        );

        if (GameManager.Instance.currentHealth > before)
            GameManager.Instance.healCharges = 0;
    }

    // ====== DANO / VIDA ======
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (isDead) return;
        if (GameManager.Instance.currentHealth <= 0) return;

        // invuln geral (dano + colisão)
        if (hitInvulnTimer > 0f) return;

        GameManager.Instance.currentHealth -= amount;
        GameManager.Instance.currentHealth = Mathf.Max(0, GameManager.Instance.currentHealth);

        // toca animação de dano e dá 1s invuln
        if (anim != null)
            anim.SetTrigger("Hit");

        hitInvulnTimer = hitInvulnTime;

        // morreu?
        if (GameManager.Instance.currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        // trava movimento instant
        rb.linearVelocity = Vector2.zero;

        if (anim != null)
            anim.SetBool("Dead", true);
    }

    // colisão com inimigo = 1 de dano (sem trigger)
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (collisionInvulnTimer > 0f) return;
        if (hitInvulnTimer > 0f) return; // enquanto toma dano, não toma mais nada
        if (collision.collider == null) return;

        if (collision.collider.CompareTag("Enemy"))
        {
            TakeDamage(collisionDamage);
            collisionInvulnTimer = collisionInvulnTime;
        }
    }

    // ====== KILLS -> RECARGA CURA ======
    public static void RegisterEnemyKill()
    {
        if (instance == null) return;

        if (GameManager.Instance.healCharges >= 1) return;

        GameManager.Instance.killsSinceRestore++;

        if (GameManager.Instance.killsSinceRestore >= instance.killsToRestoreHeal)
        {
            GameManager.Instance.killsSinceRestore = 0;
            GameManager.Instance.healCharges = 1;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 260, 20), "Velocidade: " + rb.linearVelocity.magnitude.ToString("F2"));
        GUI.Label(new Rect(10, 30, 260, 20), $"Vida: {GameManager.Instance.currentHealth}/{GameManager.Instance.maxHealth}");
        GUI.Label(new Rect(10, 50, 260, 20), "Stamina: " + currentStamina.ToString("F2") + " / " + maxStamina.ToString("F0"));

        if (isExhausted)
            GUI.Label(new Rect(10, 70, 260, 20), "Cooldown: " + cooldownTimer.ToString("F1") + "s");

        int charges = GameManager.Instance.healCharges;
        int killsLeft = (charges >= 1) ? 0 : Mathf.Max(0, killsToRestoreHeal - GameManager.Instance.killsSinceRestore);

        string healText = (charges >= 1)
            ? $"Cura (E): PRONTA (1/1)"
            : $"Cura (E): {killsLeft} kills p/ recarregar";

        string invText = (hitInvulnTimer > 0f) ? $"Invuln: {hitInvulnTimer:F1}s" : "";

        GUI.Label(new Rect(10, 90, 320, 20), healText);
        if (!string.IsNullOrEmpty(invText))
            GUI.Label(new Rect(10, 110, 260, 20), invText);
    }
}
