using UnityEngine;

public class EnemyCullingReset : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Settings")]
    [SerializeField] private bool resetOnExit = true;

    private Vector3 startPosition;
    private bool wasVisible = true;

    private void Awake()
    {
        startPosition = transform.position;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        if (targetCamera == null) return;

        bool isVisible = IsVisible();

        if (!isVisible && wasVisible)
        {
            ResetEnemy();
        }

        wasVisible = isVisible;
    }

    private bool IsVisible()
    {
        Vector3 viewPos = targetCamera.WorldToViewportPoint(transform.position);

        return viewPos.z > 0 &&
               viewPos.x > 0 && viewPos.x < 1 &&
               viewPos.y > 0 && viewPos.y < 1;
    }

    private void ResetEnemy()
    {
        // RESETA A POSIÇÃO DO INIMIGO
        // transform.position = startPosition;

        var frok = GetComponent<Frok>();
        if (frok != null)
        {
            frok.ResetEnemyState();
        }
    }

}