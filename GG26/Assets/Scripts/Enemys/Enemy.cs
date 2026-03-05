using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 5;

    [Header("Aggro / Chase")]
    [SerializeField] private float chaseSpeed = 2f;     // velocidade quando persegue
    [SerializeField] private float aggroRange = 5f;     // distância pra "agarrar" o player

    [Header("Wander")]
    [SerializeField] private float wanderSpeed = 1f;        // velocidade andando aleatório
    [SerializeField] private float wanderChangeTime = 1f;   // troca direção a cada X segundos
    [SerializeField] private float wanderIdleChance = 0.2f; // chance de ficar parado ao trocar (0 a 1)

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 0.6f;

    [Header("Debug GUI")]
    [SerializeField] private Vector2 guiOffset = new Vector2(0f, 0.8f);
    [SerializeField] private bool debugInfo = true;

    private int currentHealth;
    private Rigidbody2D rb;
    private Transform player;
    private bool aggroed;
    private Camera mainCam;

    // wander
    private Vector2 wanderDir;
    private float wanderTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        currentHealth = maxHealth;

        PickNewWanderDirection();
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        // Checa aggro (uma vez só)
        if (!aggroed && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= aggroRange)
                aggroed = true;
        }

        // Atualiza wander timer só se ainda não aggroou
        if (!aggroed)
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
                PickNewWanderDirection();
        }
    }

    private void FixedUpdate()
    {
        if (aggroed && player != null)
        {
            // CHASE
            Vector2 dir = ((Vector2)player.position - rb.position).normalized;
            rb.linearVelocity = dir * chaseSpeed;
        }
        else
        {
            // WANDER
            rb.linearVelocity = wanderDir * wanderSpeed;
        }
    }

    private void PickNewWanderDirection()
    {
        wanderTimer = wanderChangeTime;

        // chance de ficar parado um pouquinho
        if (Random.value < wanderIdleChance)
        {
            wanderDir = Vector2.zero;
            return;
        }

        // direção aleatória 360°
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        wanderDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
    }

    // Chamado pela bala
    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        currentHealth -= damage;

        // knockback pequeno
        if (knockbackForce > 0f)
            rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);

        if (currentHealth <= 0)
            Destroy(gameObject);
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

        string state = aggroed ? "CHASE" : "WANDER";
        GUI.Label(new Rect(x, y, 200f, 40f), $"HP: {currentHealth}/{maxHealth}\nState: {state}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}
