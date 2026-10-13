
using UnityEngine;

public class VinesManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private int requiredItems = 4;

    [Header("Tilemap das vinhas")]
    [SerializeField] private GameObject vinesTilemap;

    private int collectedItems = 0;
    private bool vinesDestroyed = false;

    public void CollectItem()
    {
        if (vinesDestroyed)
            return;

        collectedItems++;

        Debug.Log("Objetos coletados: " +
            collectedItems + "/" + requiredItems);

        if (collectedItems >= requiredItems)
        {
            DestroyVines();
        }
    }

    private void DestroyVines()
    {
        vinesDestroyed = true;

        if (vinesTilemap != null)
        {
            vinesTilemap.SetActive(false);
        }

        Debug.Log("As vinhas foram removidas!");
    }
}
