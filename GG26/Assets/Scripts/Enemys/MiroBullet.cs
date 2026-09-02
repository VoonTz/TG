using UnityEngine;

public class MiroBullet : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 2;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 5f;

    private bool hasHit;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit)
            return;

        // Acertou o Player
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();

            if (player != null)
            {
                player.TakeDamage(damage);
            }

            hasHit = true;

            Destroy(gameObject);

            return;
        }

        // Bateu no cenário
        hasHit = true;

        Destroy(gameObject);
    }
}