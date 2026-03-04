using UnityEngine;

public class PlacaInteracao : MonoBehaviour
{
    [Header("UI da placa ampliada")]
    public GameObject placaUI;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            placaUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            placaUI.SetActive(false);
        }
    }
}

