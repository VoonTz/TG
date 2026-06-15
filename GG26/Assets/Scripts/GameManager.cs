using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Persistent Player Data")]
    public int currentHealth = 12;
    public int maxHealth = 12;

    [Header("UI Health")]
    public Image healthImage;
    public Sprite[] healthSprites;

    public int healCharges = 1;        // 0 ou 1
    public int killsSinceRestore = 0;  // progresso da recarga

    //-------------
    [Header("Game Over Screen")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject playerHud;

    void Start()
    {
        
        UpdateUI();

        if(gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }
    }
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

    public void UpdateUI()
    {
        healthImage.sprite = healthSprites[currentHealth];
    }

    public void ReduceLives()
    {
        if (currentHealth <= 0)
        {
            TriggerGameOver();
        }
    }


    public void TriggerGameOver()
    {
        playerHud.SetActive(false);

        gameOverScreen.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
