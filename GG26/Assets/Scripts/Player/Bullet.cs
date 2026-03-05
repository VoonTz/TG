using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private int damage = 1;

    [Header("Explosion")]
    [SerializeField] private Animator anim;
    [SerializeField] private float destroyDelay = 0.7f;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isDestroyed = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (anim == null)
            anim = GetComponent<Animator>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // ✅ Bala é Trigger → usa isso
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;

        // Ignora player
        if (other.CompareTag("Player")) return;

        // 🔴 Qualquer Enemy recebe 1 de dano
        if (other.CompareTag("Enemy"))
        {
            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        Explode();
    }

    private void Explode()
    {
        isDestroyed = true;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        col.enabled = false;

        if (anim != null)
            anim.SetBool("destroyed", true);

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}