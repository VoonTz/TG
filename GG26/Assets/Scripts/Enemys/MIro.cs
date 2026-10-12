
using System.Collections;
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

    [Header("Lose Aggro")]
    [Tooltip("Distância além da qual o Miro perde o aggro.")]
    [SerializeField] private float loseAggroRange = 8f;

    [Header("Movement Reaction Delay")]
    [SerializeField] private float closeReactionDelay = 0.25f;

    [Header("Wander")]
    [SerializeField] private float wanderSpeed = 1f;
    [SerializeField] private float wanderChangeTime = 1f;
    [SerializeField] private float wanderIdleChance = 0.2f;

    [Header("Ranged Attack")]
    [SerializeField] private float shootingRange = 4f;
    [SerializeField] private float minimumDistance = 2.5f;
    [SerializeField] private float shootWindupTime = 0.3f;
    [SerializeField] private float shootCooldown = 1.5f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private Transform playerCenter;

    [Header("Collision / World")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float castSkin = 0.02f;
    [SerializeField] private bool changeDirOnHit = true;

    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private string paramIsMoving = "IsWalking";
    [SerializeField] private string paramDoAttack = "IsAttacking";
    [SerializeField] private string paramIsDead = "isDead";
    [SerializeField] private float deathAnimTime = 1f;

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

    private float closeReactionTimer;
    private bool waitingToRetreat;

    private bool isRetreating;

    private State state = State.WANDER;

    private Vector2 wanderDir;
    private float wanderTimer;

    private bool lastMoving;

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
        {
            player = p.transform;

            Transform center = player.Find("Center");

            if (center != null)
            {
                playerCenter = center;
            }
            else
            {
                Debug.LogWarning(
                    "O objeto Center não foi encontrado dentro do Player."
                );
            }
        }
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
        // DISTÂNCIA E AGGRO
        // ========================================================

        float dist = Vector2.Distance(
            transform.position,
            player.position
        );

        // Se estiver agressivo e o Player sair do alcance,
        // perde o aggro e volta ao comportamento de patrulha.
        float effectiveLoseRange = Mathf.Max(
            loseAggroRange,
            aggroRange
        );

        if (aggroed && dist > effectiveLoseRange)
        {
            aggroed = false;

            // Cancela um ataque que ainda não terminou.
            isAttacking = false;
            attackTimer = 0f;

            waitingToRetreat = false;
            closeReactionTimer = 0f;

            rb.linearVelocity = Vector2.zero;

            if (anim != null)
                anim.ResetTrigger(paramDoAttack);

            PickNewWanderDirection();
        }

        // Só adquire aggro quando o Player entra no alcance inicial.
        if (!aggroed && dist <= aggroRange)
        {
            aggroed = true;
            wanderDir = Vector2.zero;
            wanderTimer = 0f;
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

            state = wanderDir == Vector2.zero
                ? State.IDLE
                : State.WANDER;
        }
        else
        {
            if (dist <= shootingRange && cooldownTimer <= 0f)
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
            bool moved = TryMove(wanderDir, wanderSpeed);
            SetMoving(moved);
            return;
        }

        // ========================================================
        // DISTÂNCIA DO PLAYER
        // ========================================================

        float dist = Vector2.Distance(
            rb.position,
            player.position
        );

        // ========================================================
        // COMEÇA A ATACAR
        // ========================================================

        if (dist <= shootingRange &&
            cooldownTimer <= 0f &&
            !isRetreating)
        {
            StartAttack();
            SetMoving(false);
            return;
        }

        // ========================================================
        // DIREÇÃO DO PLAYER
        // ========================================================

        Vector2 dirToPlayer =
            ((Vector2)player.position - rb.position).normalized;

        // ========================================================
        // PLAYER ESTÁ PERTO DEMAIS
        // ========================================================

        if (dist < minimumDistance)
        {
            if (!waitingToRetreat)
            {
                isRetreating = false;

                waitingToRetreat = true;
                closeReactionTimer = closeReactionDelay;

                rb.linearVelocity = Vector2.zero;
                SetMoving(false);
                return;
            }

            if (closeReactionTimer > 0f)
            {
                closeReactionTimer -= Time.fixedDeltaTime;

                rb.linearVelocity = Vector2.zero;
                SetMoving(false);
                return;
            }

            Vector2 retreatDirection = -dirToPlayer;

            isRetreating = true;

            bool moved = TryMove(retreatDirection, chaseSpeed);

            SetMoving(moved);
            return;
        }

        // O Player saiu da distância mínima.
        waitingToRetreat = false;
        closeReactionTimer = 0f;
        isRetreating = false;

        // ========================================================
        // PLAYER ESTÁ LONGE DEMAIS PARA ATIRAR
        // ========================================================

        if (dist > shootingRange)
        {
            bool moved = TryMove(dirToPlayer, chaseSpeed);
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
    // ACHAR O CENTRO DO PLAYER
    // ============================================================

    private Vector2 GetPlayerCenter()
    {
        if (player == null)
            return Vector2.zero;

        Collider2D playerCollider =
            player.GetComponentInChildren<Collider2D>();

        if (playerCollider != null)
            return playerCollider.bounds.center;

        SpriteRenderer sprite =
            player.GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            return sprite.bounds.center;

        return player.position;
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

        if (anim != null)
            anim.SetTrigger(paramDoAttack);

        SetMoving(false);
    }

    private void DoAttackCycle()
    {
        rb.linearVelocity = Vector2.zero;
        attackTimer += Time.fixedDeltaTime;

        // Tempo de preparação do disparo.
        if (attackTimer < shootWindupTime)
            return;

        Shoot();

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
        if (firePoint == null || projectilePrefab == null)
            return;

        Transform targetTransform =
            playerCenter != null ? playerCenter : player;

        if (targetTransform == null)
            return;

        Vector2 origin = firePoint.position;
        Vector2 target = targetTransform.position;

        Vector2 direction = (target - origin).normalized;

        Debug.DrawRay(origin, direction * 5f, Color.red, 1f);

        // Cria o projétil no eixo Z = -1.
        Vector3 spawnPosition = firePoint.position;
        spawnPosition.z = -1f;

        GameObject projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        // Ignora colisão com os colliders do próprio Miro.
        Collider2D bulletCollider =
            projectile.GetComponent<Collider2D>();

        Collider2D[] enemyColliders =
            GetComponentsInChildren<Collider2D>();

        if (bulletCollider != null)
        {
            foreach (Collider2D enemyCollider in enemyColliders)
            {
                if (enemyCollider != null)
                {
                    Physics2D.IgnoreCollision(
                        bulletCollider,
                        enemyCollider
                    );
                }
            }
        }

        // Define a direção e a velocidade do projétil.
        Rigidbody2D projectileRb =
            projectile.GetComponent<Rigidbody2D>();

        if (projectileRb != null)
        {
            projectileRb.linearVelocity =
                direction * projectileSpeed;
        }

        // Rotação visual do projétil.
        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        projectile.transform.rotation =
            Quaternion.Euler(0f, 0f, angle);

        PlaySFX(shootSFX);
    }

    // ============================================================
    // MOVIMENTO
    // ============================================================

    private bool TryMove(Vector2 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return false;

        Vector2 delta =
            direction.normalized * speed * Time.fixedDeltaTime;

        int hitCount = rb.Cast(
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

                minDist = Mathf.Min(
                    minDist,
                    castHits[i].distance
                );
            }

            float allowed = Mathf.Max(
                0f,
                minDist - castSkin
            );

            bool moved = false;

            if (allowed > 0f)
            {
                Vector2 safeDelta =
                    delta.normalized * allowed;

                rb.MovePosition(rb.position + safeDelta);
                moved = true;
            }

            // Se estiver vagando e bater em uma parede, troca direção.
            if (changeDirOnHit && !aggroed)
                PickNewWanderDirection();

            return moved;
        }

        rb.MovePosition(rb.position + delta);
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

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        wanderDir = new Vector2(
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

        // Receber dano deixa o Miro agressivo.
        if (!aggroed)
        {
            aggroed = true;

            wanderDir = Vector2.zero;
            wanderTimer = 0f;
        }

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

        if (anim != null)
            anim.SetTrigger("IsDead");

        rb.linearVelocity = Vector2.zero;

        Collider2D ownCollider = GetComponent<Collider2D>();

        if (ownCollider != null)
            ownCollider.enabled = false;

        gameObject.tag = "Untagged";

        PlaySFX(deathSFX);

        Player.RegisterEnemyKill();

        StartCoroutine(AtrasoDelay());
    }

    private IEnumerator AtrasoDelay()
    {
        yield return new WaitForSeconds(deathAnimTime);
        Destroy(gameObject);
    }

    // ============================================================
    // SFX
    // ============================================================

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, sfxVolume);
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
        anim.SetBool(paramIsMoving, moving);
    }

    private void SetDead(bool dead)
    {
        if (anim == null)
            return;

        anim.SetBool(paramIsDead, dead);
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

        closeReactionTimer = 0f;
        waitingToRetreat = false;

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
        // Alcance inicial para detectar o Player.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            aggroRange
        );

        // Alcance máximo antes de perder o aggro.
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(
            transform.position,
            Mathf.Max(loseAggroRange, aggroRange)
        );

        // Alcance de tiro.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            shootingRange
        );

        // Distância mínima.
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(
            transform.position,
            minimumDistance
        );
    }
}
