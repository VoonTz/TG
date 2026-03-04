using UnityEngine;

public class BossBullet : MonoBehaviour
{
    public float tempoVida = 5f;

    void Start()
    {
        Destroy(gameObject, tempoVida);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // aplicar dano no player
            Destroy(gameObject);
        }
    }
}
