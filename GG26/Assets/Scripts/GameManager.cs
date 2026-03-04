using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Persistent Player Data")]
    public int currentHealth = 12;
    public int maxHealth = 12;

    public int healCharges = 1;        // 0 ou 1
    public int killsSinceRestore = 0;  // progresso da recarga

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
