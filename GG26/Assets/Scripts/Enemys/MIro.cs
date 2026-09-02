using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Miro : MonoBehaviour
{
    public enum State
    {
        IDLE,
        WANDER,
        CHASE,
        ATTACK,
        DEAD
    }

    [Header("Stats")]
    [SerializeField] private int maxHealth = 15;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 1.5f;
    [SerializeField] private float aggroRange = 6f;

    [Header("Wander")]
    [SerializeField] private float wanderSpeed = 1f;
    [SerializeField] private float wanderChangeTime = 1f;
    [SerializeField] private float wanderIdleChance = 0.2f;

    [Header("Ranged Attack")]
    [SerializeField] private float shootingRange = 4f;

    // Distância mínima que o inimigo tenta manter do player
    [SerializeField] private float minimumDistance = 2.5f;

    [SerializeField] private float shootWindupTime = 0.3f;
    [SerializeField] private float shootCooldown = 1.5f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 7f;

    [Header("Collision / World")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float castSkin = 0.02f;
    [SerializeField] private bool changeDirOnHit = true;

    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private string paramIsMoving = "isMoving";
    [SerializeField] private string paramDoAttack = "doAttack";
    [SerializeField] private string paramIsDead = "isDead";
    [SerializeField] private float deathAnimTime = 0.5f;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip hurtSFX;

    [SerializeField] private float sfxVolume = 1f;

    private int currentHealth;

    private Rigidbody2D rb;
    private Transform player;

    private bool aggroed;
    private bool isAttacking;
    private bool isDead;

    private float attackTimer;
    private float cooldownTimer;

    private State state = State.WANDER;

    // Wander
    private Vector2 wanderDir;
    private float wanderTimer;

    // Animator
    private bool lastMoving;

    // Collision Cast
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private ContactFilter2D castFilter;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (anim == null)
            anim = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;

        PickNewWanderDirection();

        castFilter = new ContactFilter2D();
        castFilter.useTriggers = false;
        castFilter.SetLayerMask(obstacleMask);

        SetMoving(false);
        SetDead(false);
    }


    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
            player = p.transform;
    }


    private void Update()
    {
        if (isDead)
        {
            state = State.DEAD;
            return;
        }

        if (player == null)
            return;


        // ========================================================
        // COOLDOWN DO TIRO
        // ========================================================

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;


        // ========================================================
        // AGGRO
        // ========================================================

        if (!aggroed)
        {
            float dist =
                Vector2.Distance(
                    transform.position,
                    player.position
                );

            if (dist <= aggroRange)
                aggroed = true;
        }


        // ========================================================
        // ESTÁ ATACANDO
        // ========================================================

        if (isAttacking)
        {
            state = State.ATTACK;

            SetMoving(false);

            return;
        }


        // ========================================================
        // WANDER / COMBATE
        // ========================================================

        if (!aggroed)
        {
            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0f)
                PickNewWanderDirection();

            state =
                wanderDir == Vector2.zero
                    ? State.IDLE
                    : State.WANDER;
        }
        else
        {
            float dist =
                Vector2.Distance(
                    transform.position,
                    player.position
                );

            // Dentro do alcance de tiro
            if (dist <= shootingRange &&
                cooldownTimer <= 0f)
            {
                state = State.ATTACK;
            }
            else
            {
                state = State.CHASE;
            }
        }
    }


    private void FixedUpdate()
    {
        if (isDead)
            return;

        if (player == null)
            return;


        // ========================================================
        // ATAQUE
        // ========================================================

        if (isAttacking)
        {
            DoAttackCycle();

            SetMoving(false);

            return;
        }


        // ========================================================
        // WANDER
        // ========================================================

        if (!aggroed)
        {
            bool moved =
                TryMove(
                    wanderDir,
                    wanderSpeed
                );

            SetMoving(moved);

            return;
        }


        // ========================================================
        // DISTÂNCIA DO PLAYER
        // ========================================================

        float dist =
            Vector2.Distance(
                rb.position,
                player.position
            );


        // ========================================================
        // COMEÇA A ATACAR
        // ========================================================

        if (dist <= shootingRange &&
            cooldownTimer <= 0f)
        {
            StartAttack();

            SetMoving(false);

            return;
        }


        // ========================================================
        // DIREÇÃO DO PLAYER
        // ========================================================

        Vector2 dirToPlayer =
            ((Vector2)player.position - rb.position)
            .normalized;


        // ========================================================
        // PLAYER ESTÁ PERTO DEMAIS
        // ========================================================

        if (dist < minimumDistance)
        {
            // Direção oposta ao player
            Vector2 retreatDirection =
                -dirToPlayer;

            bool moved =
                TryMove(
                    retreatDirection,
                    chaseSpeed
                );

            SetMoving(moved);

            return;
        }


        // ========================================================
        // PLAYER ESTÁ LONGE DEMAIS
        // ========================================================

        if (dist > shootingRange)
        {
            bool moved =
                TryMove(
                    dirToPlayer,
                    chaseSpeed
                );

            SetMoving(moved);

            return;
        }


        // ========================================================
        // ESTÁ NA DISTÂNCIA IDEAL
        // ========================================================

        rb.linearVelocity = Vector2.zero;

        SetMoving(false);
    }


    // ============================================================
    // ATAQUE
    // ============================================================

    private void StartAttack()
    {
        if (isAttacking || isDead)
            return;

        isAttacking = true;

        attackTimer = 0f;

        rb.linearVelocity = Vector2.zero;


        // Animação de ataque
        if (anim != null)
            anim.SetTrigger(paramDoAttack);

        SetMoving(false);
    }


    private void DoAttackCycle()
    {
        rb.linearVelocity = Vector2.zero;

        attackTimer += Time.fixedDeltaTime;


        // Tempo de preparação
        if (attackTimer < shootWindupTime)
            return;


        // Atira
        Shoot();


        // Finaliza ataque
        isAttacking = false;

        attackTimer = 0f;

        cooldownTimer = shootCooldown;

        SetMoving(false);
    }


    // ============================================================
    // DISPARO
    // ============================================================

    private void Shoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning(
                $"{name}: Projectile Prefab não configurado."
            );

            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning(
                $"{name}: Fire Point não configurado."
            );

            return;
        }


        // ========================================================
        // DIREÇÃO DO TIRO
        // ========================================================

        Vector2 direction =
            (
                (Vector2)player.position -
                (Vector2)firePoint.position
            ).normalized;


        // ========================================================
        // CRIA PROJÉTIL
        // ========================================================

        GameObject projectile =
            Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.identity
            );


        // ========================================================
        // IGNORA O PRÓPRIO INIMIGO
        // ========================================================

        Collider2D[] enemyColliders =
            GetComponentsInChildren<Collider2D>();

        Collider2D[] projectileColliders =
            projectile.GetComponentsInChildren<Collider2D>();


        foreach (Collider2D enemyCollider in enemyColliders)
        {
            foreach (Collider2D projectileCollider in projectileColliders)
            {
                Physics2D.IgnoreCollision(
                    projectileCollider,
                    enemyCollider
                );
            }
        }


        // ========================================================
        // VELOCIDADE DO PROJÉTIL
        // ========================================================

        Rigidbody2D projectileRb =
            projectile.GetComponent<Rigidbody2D>();

        if (projectileRb != null)
        {
            projectileRb.linearVelocity =
                direction * projectileSpeed;
        }


        // ========================================================
        // ROTAÇÃO DO PROJÉTIL
        // ========================================================

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        projectile.transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );


        // ========================================================
        // SFX DO TIRO
        // ========================================================

        PlaySFX(shootSFX);
    }


    // ============================================================
    // MOVIMENTO
    // ============================================================

    private bool TryMove(
        Vector2 direction,
        float speed
    )
    {
        if (direction.sqrMagnitude < 0.0001f)
            return false;


        Vector2 delta =
            direction.normalized *
            speed *
            Time.fixedDeltaTime;


        int hitCount =
            rb.Cast(
                delta.normalized,
                castFilter,
                castHits,
                delta.magnitude + castSkin
            );


        if (hitCount > 0)
        {
            float minDist = float.MaxValue;


            for (int i = 0; i < hitCount; i++)
            {
                if (castHits[i].collider == null)
                    continue;

                minDist =
                    Mathf.Min(
                        minDist,
                        castHits[i].distance
                    );
            }


            float allowed =
                Mathf.Max(
                    0f,
                    minDist - castSkin
                );


            bool moved = false;


            if (allowed > 0f)
            {
                Vector2 safeDelta =
                    delta.normalized *
                    allowed;

                rb.MovePosition(
                    rb.position +
                    safeDelta
                );

                moved = true;
            }


            // Se estiver vagando e bater
            // em uma parede, troca direção
            if (changeDirOnHit && !aggroed)
                PickNewWanderDirection();


            return moved;
        }


        rb.MovePosition(
            rb.position +
            delta
        );

        return true;
    }


    // ============================================================
    // WANDER
    // ============================================================

    private void PickNewWanderDirection()
    {
        wanderTimer = wanderChangeTime;


        if (Random.value < wanderIdleChance)
        {
            wanderDir = Vector2.zero;

            return;
        }


        float angle =
            Random.Range(
                0f,
                360f
            ) * Mathf.Deg2Rad;


        wanderDir =
            new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ).normalized;
    }


    // ============================================================
    // DANO
    // ============================================================

    public void TakeDamage(int amount)
    {
        if (isDead)
            return;


        currentHealth -= amount;


        // Toma dano = fica agressivo
        if (!aggroed)
        {
            aggroed = true;

            wanderDir = Vector2.zero;

            wanderTimer = 0f;
        }


        // SFX de dano
        PlaySFX(hurtSFX);


        if (currentHealth <= 0)
            Die();
    }


    // ============================================================
    // MORTE
    // ============================================================

    private void Die()
    {
        if (isDead)
            return;


        isDead = true;

        state = State.DEAD;

        isAttacking = false;

        attackTimer = 0f;

        cooldownTimer = 0f;


        // Para o inimigo
        rb.linearVelocity = Vector2.zero;


        // Animação
        SetMoving(false);

        SetDead(true);


        // SFX de morte
        PlaySFX(deathSFX);


        // Conta kill
        Player.RegisterEnemyKill();


        // Espera animação
        Destroy(
            gameObject,
            deathAnimTime
        );
    }


    // ============================================================
    // SFX
    // ============================================================

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource == null)
            return;

        if (clip == null)
            return;


        audioSource.PlayOneShot(
            clip,
            sfxVolume
        );
    }


    // ============================================================
    // ANIMAÇÃO
    // ============================================================

    private void SetMoving(bool moving)
    {
        if (anim == null)
            return;


        if (moving == lastMoving)
            return;


        lastMoving = moving;


        anim.SetBool(
            paramIsMoving,
            moving
        );
    }


    private void SetDead(bool dead)
    {
        if (anim == null)
            return;


        anim.SetBool(
            paramIsDead,
            dead
        );
    }


    // ============================================================
    // RESET
    // ============================================================

    public void ResetEnemyState()
    {
        aggroed = false;

        isAttacking = false;

        isDead = false;


        attackTimer = 0f;

        cooldownTimer = 0f;


        currentHealth = maxHealth;


        rb.linearVelocity = Vector2.zero;

        rb.simulated = true;


        state = State.WANDER;


        PickNewWanderDirection();


        SetDead(false);

        SetMoving(false);
    }


    // ============================================================
    // DEBUG
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        // Aggro
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            aggroRange
        );


        // Alcance de tiro
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            shootingRange
        );


        // Distância mínima
        Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(
            transform.position,
            minimumDistance
        );
    }
}