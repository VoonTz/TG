using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Frok : MonoBehaviour
{
    public enum State { IDLE, WANDER, CHASE, ATTACK, DEAD }

    [Header("Stats")]
    [SerializeField] private int maxHealth = 15;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 1.5f;
    [SerializeField] private float aggroRange = 5f;

    [Header("Wander")]
    [SerializeField] private float wanderSpeed = 1f;
    [SerializeField] private float wanderChangeTime = 1f;
    [SerializeField] private float wanderIdleChance = 0.2f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 0.7f;          // entra em ATTACK
    [SerializeField] private float attackDamageRange = 1.0f;    // range para acertar o hit
    [SerializeField] private float attackWindupTime = 0.3f;     // tempo do ataque
    [SerializeField] private float postAttackIdleTime = 1f;     // pausa após atacar
    [SerializeField] private int attackDamage = 4;

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


    [Header("Debug GUI")]
    [SerializeField] private Vector2 guiOffset = new Vector2(0f, 0.8f);
    [SerializeField] private bool debugInfo = true;

    private int currentHealth;
    private Rigidbody2D rb;
    private Transform player;
    private Camera mainCam;

    private bool aggroed;
    private State state = State.WANDER;

    // wander
    private Vector2 wanderDir;
    private float wanderTimer;

    // attack lock
    private bool isAttacking;
    private float attackTimer;
    private float postAttackTimer;

    // animator helper
    private bool lastMoving;
    private bool isDead;

    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private ContactFilter2D castFilter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        if (anim == null) anim = GetComponent<Animator>();

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
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (isDead) { state = State.DEAD; return; }
        if (player == null) return;

        // aggro por proximidade (uma vez)
        if (!aggroed)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= aggroRange) aggroed = true;
        }

        // pós-ataque idle trava tudo
        if (postAttackTimer > 0f)
        {
            postAttackTimer -= Time.deltaTime;
            state = State.IDLE;
            SetMoving(false);
            return;
        }

        // ✅ enquanto estiver atacando, não pode virar CHASE
        if (isAttacking)
        {
            state = State.ATTACK;
            SetMoving(false);
            return;
        }

        if (!aggroed)
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f) PickNewWanderDirection();

            state = (wanderDir == Vector2.zero) ? State.IDLE : State.WANDER;
        }
        else
        {
            float dist = Vector2.Distance(transform.position, player.position);
            state = (dist <= attackRange) ? State.ATTACK : State.CHASE;
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (player == null) return;

        if (postAttackTimer > 0f)
        {
            SetMoving(false);
            return;
        }

        if (!aggroed)
        {
            bool moved = TryMove(wanderDir, wanderSpeed);
            SetMoving(moved);
            return;
        }

        // ✅ se começou ataque, executa ele inteiro
        if (isAttacking)
        {
            DoAttackCycle();
            SetMoving(false);
            return;
        }

        float dist = Vector2.Distance(rb.position, player.position);

        if (dist <= attackRange)
        {
            // inicia ataque (trava)
            StartAttack();
            DoAttackCycle();
            SetMoving(false);
            return;
        }

        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        bool movedChase = TryMove(dir, chaseSpeed);
        SetMoving(movedChase);
    }

    private void StartAttack()
    {
        if (isAttacking || isDead) return;

        isAttacking = true;
        attackTimer = 0f;
        rb.linearVelocity = Vector2.zero;

        // 🔥 dispara animação de ataque UMA VEZ
        if (anim != null) anim.SetTrigger(paramDoAttack);

        SetMoving(false);
    }

    // ✅ ataque "travado": windup -> hit (se estiver no range) -> idle 1s
    private void DoAttackCycle()
    {
        rb.linearVelocity = Vector2.zero;

        attackTimer += Time.fixedDeltaTime;

        if (attackTimer < attackWindupTime)
            return;

        // chegou a hora do hit
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackDamageRange)
        {
            Player p = player.GetComponent<Player>();
            if (p != null) p.TakeDamage(attackDamage);
        }

        // finaliza ataque e entra no idle pós-ataque
        isAttacking = false;
        attackTimer = 0f;
        postAttackTimer = postAttackIdleTime;

        SetMoving(false);
    }

    /// <summary>
    /// Move sem atravessar cenário (Cast). Retorna TRUE se moveu alguma coisa.
    /// </summary>
    private bool TryMove(Vector2 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.0001f) return false;

        Vector2 delta = direction.normalized * speed * Time.fixedDeltaTime;

        int hitCount = rb.Cast(delta.normalized, castFilter, castHits, delta.magnitude + castSkin);

        if (hitCount > 0)
        {
            float minDist = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (castHits[i].collider == null) continue;
                minDist = Mathf.Min(minDist, castHits[i].distance);
            }

            float allowed = Mathf.Max(0f, minDist - castSkin);

            bool moved = false;
            if (allowed > 0f)
            {
                Vector2 safeDelta = delta.normalized * allowed;
                rb.MovePosition(rb.position + safeDelta);
                moved = true;
            }

            // bateu no cenário enquanto vagando -> muda direção
            if (changeDirOnHit && !aggroed)
                PickNewWanderDirection();

            return moved;
        }

        rb.MovePosition(rb.position + delta);
        return true;
    }

    private void PickNewWanderDirection()
    {
        wanderTimer = wanderChangeTime;

        if (Random.value < wanderIdleChance)
        {
            wanderDir = Vector2.zero;
            return;
        }

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        wanderDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
    }

    // Chamado pela bala (1 de dano etc.)
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        // tomou tiro = aggro instantâneo
        if (!aggroed)
        {
            aggroed = true;
            wanderDir = Vector2.zero;
            wanderTimer = 0f;
        }

        // se estava vagando/parado, força chase a partir de agora
        // (se estiver atacando, ele termina o ciclo e depois volta pra chase naturalmente)
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        state = State.DEAD;

        SetMoving(false);
        SetDead(true);

        // trava lógica
        isAttacking = false;
        postAttackTimer = 0f;
        attackTimer = 0f;

        // para mover/colidir (opcional mas recomendado)
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // congela física do inimigo

        // conta kill pro player
        Player.RegisterEnemyKill();

        // espera a animação tocar 1 vez e some
        Destroy(gameObject, deathAnimTime);
    }


    private void SetMoving(bool moving)
    {
        if (anim == null) return;
        if (moving == lastMoving) return;

        lastMoving = moving;
        anim.SetBool(paramIsMoving, moving);
    }

    private void SetDead(bool dead)
    {
        if (anim == null) return;
        anim.SetBool(paramIsDead, dead);
    }

    private void OnGUI()
    {
        if (!debugInfo) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 worldPos = transform.position + (Vector3)guiOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0f) return;

        float x = screenPos.x - 55f;
        float y = Screen.height - screenPos.y - 10f;

        GUI.Label(new Rect(x, y, 260f, 55f),
            $"HP: {currentHealth}/{maxHealth}\nState: {state}\nAggro: {aggroed}  Attacking: {isAttacking}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackDamageRange);
    }
}
