using UnityEngine;
using System.Collections;

public class Boss : MonoBehaviour
{
    [Header("Boss Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int projectileDamage = 1;

    [Header("Fire Rate")]
    [SerializeField] private float normalFireRate = 1.5f;
    [SerializeField] private float shotgunCooldown = 4f;

    [Header("Shotgun Attack")]
    [SerializeField] private int shotgunBullets = 8;
    [SerializeField] private float shotgunSpread = 60f;

    [Header("Spike Attack")]
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private float spikeDelay = 0.5f;
    [SerializeField] private int spikeCount = 3;

    private int currentHealth;
    private Transform player;

    private float nextNormalShot;
    private float nextShotgunShot;

    Animator Tree;

    private bool isDead = false;

    private bool isSpiking = false;

    private void Start()
    {
        Tree = GetComponent<Animator>();

        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    private void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float hpPercent = (float)currentHealth / maxHealth;

        // ===== FASE 1 =====
        if (hpPercent > 0.5f)
        {
            NormalShoot();
        }

        // ===== FASE 2 =====
        else if (hpPercent > 0.25f)
        {
            NormalShoot();
            ShotgunAttack();
        }

        // ===== FASE 3 =====
        else
        {
            ShotgunAttack();

            if (!isSpiking)
                StartCoroutine(SpikeAttack());
        }
    }

    // ===== TIRO NORMAL =====
    private void NormalShoot()
    {
        if (Time.time < nextNormalShot) return;

        nextNormalShot = Time.time + (1f / normalFireRate);

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        Vector2 direction = (player.position - firePoint.position).normalized;

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = direction * projectileSpeed;

        BossProjectile proj = bullet.AddComponent<BossProjectile>();
        proj.damage = projectileDamage;
    }

    // ===== SHOTGUN =====
    private void ShotgunAttack()
    {
        if (Time.time < nextShotgunShot) return;

        nextShotgunShot = Time.time + shotgunCooldown;

        Vector2 baseDir = (player.position - firePoint.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        int gapIndex = Random.Range(0, shotgunBullets);

        for (int i = 0; i < shotgunBullets; i++)
        {
            if (i == gapIndex) continue;

            float angle = baseAngle - shotgunSpread / 2 + (shotgunSpread / shotgunBullets) * i;

            Quaternion rot = Quaternion.Euler(0, 0, angle);

            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, rot);

            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

            if (rb != null)
                rb.linearVelocity = (Vector2)(rot * Vector2.right) * projectileSpeed;

            BossProjectile proj = bullet.AddComponent<BossProjectile>();
            proj.damage = projectileDamage;
        }
    }

    // ===== ESPINHOS =====
    private IEnumerator SpikeAttack()
    {
        isSpiking = true;

        for (int i = 0; i < spikeCount; i++)
        {
            Vector3 spawnPos = player.position;

            GameObject spike = Instantiate(spikePrefab, spawnPos, Quaternion.identity);

            Spike spikeScript = spike.AddComponent<Spike>();
            spikeScript.damage = projectileDamage;

            yield return new WaitForSeconds(spikeDelay);
        }

        yield return new WaitForSeconds(2f);

        isSpiking = false;
    }

    // ===== VIDA =====
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
        if (isDead) return;
        isDead = true;
        Tree.SetTrigger("IsDead");
        Player.RegisterEnemyKill();
        Destroy(gameObject, 1.5f);
    }
}


// ===== PROJÉTIL =====
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
                player.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}


// ===== ESPINHO =====
public class Spike : MonoBehaviour
{
    public int damage;
    private float timer = 0.5f;

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.6f);

            if (hit != null && hit.CompareTag("Player"))
            {
                Player p = hit.GetComponent<Player>();

                if (p != null)
                    p.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}