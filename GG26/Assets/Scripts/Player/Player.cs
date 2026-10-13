
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
    [SerializeField] private float hitInvulnTime = 1f;

    [Header("SFX - Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX - Movement")]
    [SerializeField] private AudioClip walkSFX;
    [SerializeField] private AudioClip sprintSFX;
    [SerializeField] private float walkStepInterval = 0.4f;
    [SerializeField] private float sprintStepInterval = 0.25f;

    [Header("SFX - Actions")]
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip healSFX;

    [Header("SFX - Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera mainCam;

    private float currentStamina;
    private bool isSprinting;
    private bool isExhausted;
    private float cooldownTimer;

    private float collisionInvulnTimer = 0f;
    private float hitInvulnTimer = 0f;
    private float footstepTimer = 0f;

    private bool isDead = false;

    private static Player instance;

    private void Awake()
    {
        instance = this;

        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        if (anim == null)
            anim = GetComponent<Animator>();

        // Cria um AudioSource caso nenhum tenha sido atribuído.
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
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

        isDead = GameManager.Instance.currentHealth <= 0;

        if (anim != null)
            anim.SetBool("Dead", isDead);

        if (isDead && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (collisionInvulnTimer > 0f)
            collisionInvulnTimer -= Time.deltaTime;

        if (hitInvulnTimer > 0f)
            hitInvulnTimer -= Time.deltaTime;

        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        HandleSprint();
        HandleFootsteps();
        RotateHandToMouse();
        HandleShoot();
        UpdateAnimations();
        HandleHealInput();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        float speed = isSprinting
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

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
                currentStamina = Mathf.Clamp(
                    currentStamina, 0f, maxStamina
                );
            }
        }
    }

    // ====== SFX DE MOVIMENTO ======

    private void HandleFootsteps()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.0001f;

        if (!isMoving)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer > 0f)
            return;

        if (isSprinting)
        {
            PlaySFX(sprintSFX);
            footstepTimer = sprintStepInterval;
        }
        else
        {
            PlaySFX(walkSFX);
            footstepTimer = walkStepInterval;
        }
    }

    // ====== ANIMAÇÕES ======

    private void UpdateAnimations()
    {
        if (anim == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.0001f;

        anim.SetBool("Idle", !isMoving);
        anim.SetBool("Front", isMoving);

        if (!isMoving)
            anim.speed = 1f;
        else
            anim.speed = isSprinting
                ? sprintAnimFps / walkAnimFps
                : 1f;
    }

    // ====== MIRA ======

    private void RotateHandToMouse()
    {
        if (handPivot == null || mainCam == null || isDead)
            return;

        Vector3 mouseWorld =
            mainCam.ScreenToWorldPoint(Input.mousePosition);

        Vector2 dir = mouseWorld - handPivot.position;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        handPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ====== TIRO ======

    private void HandleShoot()
    {
        if (isDead) return;
        if (bulletPrefab == null || muzzle == null) return;
        if (!Input.GetMouseButton(0)) return;
        if (Time.time < nextFireTime) return;
        if (fireRate <= 0f) return;

        nextFireTime = Time.time + (1f / fireRate);

        GameObject bullet = Instantiate(
            bulletPrefab,
            muzzle.position,
            muzzle.rotation
        );

        Rigidbody2D brb = bullet.GetComponent<Rigidbody2D>();

        if (brb != null)
            brb.linearVelocity = (Vector2)muzzle.right * bulletSpeed;

        // Som de disparo
        PlaySFX(shootSFX);
    }

    // ====== CURA ======

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

        GameManager.Instance.UpdateUI();

        if (GameManager.Instance.currentHealth > before)
        {
            GameManager.Instance.healCharges = 0;

            // Som de cura somente se recuperou vida
            PlaySFX(healSFX);
        }
    }

    // ====== DANO / VIDA ======

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (isDead) return;
        if (GameManager.Instance.currentHealth <= 0) return;
        if (hitInvulnTimer > 0f) return;

        GameManager.Instance.currentHealth -= amount;

        GameManager.Instance.currentHealth = Mathf.Max(
            0, GameManager.Instance.currentHealth
        );

        GameManager.Instance.UpdateUI();

        hitInvulnTimer = hitInvulnTime;

        if (GameManager.Instance.currentHealth <= 0)
        {
            Die();
            Invoke(nameof(ShowGameOver), 1.5f);
        }
        else
        {
            if (anim != null)
                anim.SetTrigger("Hit");

            PlaySFX(damageSFX);
        }
    }

    private void ShowGameOver()
    {
        GameManager.Instance.TriggerGameOver();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        rb.linearVelocity = Vector2.zero;
        footstepTimer = 0f;

        if (anim != null)
            anim.SetBool("Dead", true);

        // Som de morte apenas uma vez
        PlaySFX(deathSFX);
    }

    // ====== COLISÃO COM INIMIGOS ======

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (collisionInvulnTimer > 0f) return;
        if (hitInvulnTimer > 0f) return;
        if (collision.collider == null) return;

        if (collision.collider.CompareTag("Enemy"))
        {
            TakeDamage(collisionDamage);
            collisionInvulnTimer = collisionInvulnTime;
        }
    }

    // ====== KILLS -> RECARGA DA CURA ======

    public static void RegisterEnemyKill()
    {
        if (instance == null) return;

        if (GameManager.Instance.healCharges >= 1)
            return;

        GameManager.Instance.killsSinceRestore++;

        if (GameManager.Instance.killsSinceRestore >=
            instance.killsToRestoreHeal)
        {
            GameManager.Instance.killsSinceRestore = 0;
            GameManager.Instance.healCharges = 1;
        }
    }

    // ====== SISTEMA CENTRAL DE SFX ======

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
