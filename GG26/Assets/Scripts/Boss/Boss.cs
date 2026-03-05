using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Boss Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int projectileDamage = 1;

    private int currentHealth;
    private float nextFireTime;
    private Transform player;

    private void Start()
    {
        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    private void Update()
    {
        if (player == null) return;

        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + (1f / fireRate);
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        Vector2 direction = (player.position - firePoint.position).normalized;

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * projectileSpeed;

        BossProjectile proj = bullet.AddComponent<BossProjectile>();
        proj.damage = projectileDamage;
    }

    // Player pode dar dano apenas se o boss tiver tag "Enemy"
    public void TakeDamage(int damage)
    {
        if (!CompareTag("Enemy")) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Player.RegisterEnemyKill(); // conta kill para recarregar cura
        Destroy(gameObject);
    }
}


// ===== PROJÉTIL DO BOSS =====
public class BossProjectile : MonoBehaviour
{
    public int damage;

    private void Start()
    {
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}