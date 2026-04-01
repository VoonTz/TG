using Cinemachine;
using UnityEngine;


[RequireComponent(typeof(BoxCollider2D))]
public class RoomCameraScaler : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCam;
    private PolygonCollider2D col;

    private void Start()
    {
        col = GetComponent<PolygonCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            AjustarCamera();
        }
    }

    void AjustarCamera()
    {
        Bounds bounds = col.bounds;

        float screenRatio = (float)Screen.width / Screen.height;
        float targetRatio = bounds.size.x / bounds.size.y;

        float newSize;

        if (screenRatio >= targetRatio)
        {
            
            newSize = bounds.size.y / 2.5f;
        }
        else
        {
            float difference = targetRatio / screenRatio;
            newSize = (bounds.size.y / 2) * difference;
        }

        virtualCam.m_Lens.OrthographicSize = newSize;
    }
}