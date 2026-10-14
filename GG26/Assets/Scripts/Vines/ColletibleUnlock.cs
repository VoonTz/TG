
using UnityEngine;

public class CollectibleUnlock : MonoBehaviour
{
    [Header("Gerenciador das vinhas")]
    [SerializeField] private VinesManager vinesManager;

    private bool collected = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        if (!other.CompareTag("Player"))
            return;

        collected = true;

        if (vinesManager != null)
        {
            vinesManager.CollectItem();
        }

        Destroy(gameObject);
    }
}
