using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DashEnemy : MonoBehaviour
{
    public enum State
    {
        IDLE,
        CHASE,
        PREPARE_DASH,
        DASH,
        COOLDOWN,
        DEAD
    }

    [Header("Stats")]
    [SerializeField] private int maxHealth = 15;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 1.5f;
    [SerializeField] private float aggroRange = 6f;

    [Header("Dash")]
    [SerializeField] private float dashRange = 4f;
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private float dashPrepareTime = 0.5f;

    [Header("Dash Damage")]
    [SerializeField] private int dashDamage = 4;
    [SerializeField] private float damageRange = 0.8f;

    [Header("Collision / World")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float castSkin = 0.02f;

    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private string paramIsMoving = "isMoving";
    [SerializeField] private string paramDoDash = "doDash";
    [SerializeField] private string paramIsDead = "isDead";

    private int currentHealth;

    private Rigidbody2D rb;
    private Transform player;

    private State state = State.IDLE;

    private Vector2 dashDirection;

    private float dashTimer;
    private float cooldownTimer;
    private float prepareTimer;

    private bool aggroed;
    private bool isDead;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private ContactFilter2D castFilter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (anim == null)
            anim = GetComponent<Animator>();

        currentHealth = maxHealth;

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

        // =====================================================
        // COOLDOWN
        // =====================================================

        if (state == State.COOLDOWN)
        {
            cooldownTimer -= Time.deltaTime;

            SetMoving(false);

            if (cooldownTimer <= 0f)
            {
                state = State.CHASE;
            }

            return;
        }

        // =====================================================
        // PREPARAÇÃO DO DASH
        // =====================================================

        if (state == State.PREPARE_DASH)
        {
            prepareTimer -= Time.deltaTime;

            // Para completamente enquanto prepara
            rb.linearVelocity = Vector2.zero;

            SetMoving(false);

            if (prepareTimer <= 0f)
            {
                StartDash();
            }

            return;
        }

        // =====================================================
        // DURANTE O DASH
        // =====================================================

        if (state == State.DASH)
        {
            SetMoving(true);
            return;
        }

        // =====================================================
        // DETECTAR PLAYER
        // =====================================================

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (!aggroed)
        {
            if (distance <= aggroRange)
            {
                aggroed = true;
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

        // =====================================================
        // DASH
        // =====================================================

        if (state == State.DASH)
        {
            DoDash();
            return;
        }

        // =====================================================
        // PREPARAÇÃO / COOLDOWN
        // =====================================================

        if (state == State.COOLDOWN ||
            state == State.PREPARE_DASH)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // =====================================================
        // AINDA NÃO DETECTOU O PLAYER
        // =====================================================

        if (!aggroed)
        {
            rb.linearVelocity = Vector2.zero;
            SetMoving(false);
            return;
        }

        // =====================================================
        // PERSEGUIÇÃO
        // =====================================================

        Vector2 direction =
            ((Vector2)player.position - rb.position).normalized;

        float distance = Vector2.Distance(
            rb.position,
            player.position
        );

        // =====================================================
        // CHEGOU NA DISTÂNCIA DO DASH
        // =====================================================

        if (distance <= dashRange)
        {
            StartPrepareDash();
            return;
        }

        // =====================================================
        // ANDA ATÉ O PLAYER
        // =====================================================

        bool moved = TryMove(
            direction,
            chaseSpeed
        );

        SetMoving(moved);
    }

    // =========================================================
    // PREPARA O DASH
    // =========================================================

    private void StartPrepareDash()
    {
        if (state != State.CHASE)
            return;

        state = State.PREPARE_DASH;

        prepareTimer = dashPrepareTime;

        rb.linearVelocity = Vector2.zero;

        SetMoving(false);
    }

    // =========================================================
    // COMEÇA O DASH
    // =========================================================

    private void StartDash()
    {
        if (isDead)
            return;

        state = State.DASH;

        dashTimer = dashDuration;

        // Guarda a posição atual do player.
        // A direção não muda durante o dash.

        dashDirection =
            ((Vector2)player.position - rb.position).normalized;

        rb.linearVelocity = Vector2.zero;

        if (anim != null)
            anim.SetTrigger(paramDoDash);
    }

    // =========================================================
    // EXECUTA O DASH
    // =========================================================

    private void DoDash()
    {
        dashTimer -= Time.fixedDeltaTime;

        bool moved = TryMove(
            dashDirection,
            dashSpeed
        );

        // Bateu em uma parede
        if (!moved)
        {
            EndDash();
            return;
        }

        // Tempo do dash acabou
        if (dashTimer <= 0f)
        {
            EndDash();
            return;
        }

        // Verifica dano
        CheckDashDamage();
    }

    // =========================================================
    // FINALIZA DASH
    // =========================================================

    private void EndDash()
    {
        rb.linearVelocity = Vector2.zero;

        state = State.COOLDOWN;

        cooldownTimer = dashCooldown;

        SetMoving(false);
    }

    // =========================================================
    // DANO DO DASH
    // =========================================================

    private void CheckDashDamage()
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (distance <= damageRange)
        {
            Player p = player.GetComponent<Player>();

            if (p != null)
            {
                p.TakeDamage(dashDamage);

                // Garante que o dano acontece
                // apenas uma vez por dash.

                EndDash();
            }
        }
    }

    // =========================================================
    // MOVIMENTO COM COLISÃO
    // =========================================================

    private bool TryMove(Vector2 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return false;

        Vector2 delta =
            direction.normalized *
            speed *
            Time.fixedDeltaTime;

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

            float allowed =
                Mathf.Max(
                    0f,
                    minDist - castSkin
                );

            if (allowed > 0f)
            {
                Vector2 safeDelta =
                    delta.normalized * allowed;

                rb.MovePosition(
                    rb.position + safeDelta
                );

                return true;
            }

            return false;
        }

        rb.MovePosition(
            rb.position + delta
        );

        return true;
    }

    // =========================================================
    // RECEBER DANO
    // =========================================================

    public void TakeDamage(int amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;

        // Se tomar dano, fica agressivo
        aggroed = true;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // =========================================================
    // MORTE
    // =========================================================

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        state = State.DEAD;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        SetMoving(false);
        SetDead(true);

        Destroy(gameObject, 0.5f);
    }

    // =========================================================
    // ANIMAÇÃO
    // =========================================================

    private void SetMoving(bool moving)
    {
        if (anim == null)
            return;

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

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            aggroRange
        );

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            dashRange
        );

        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            transform.position,
            damageRange
        );
    }
}