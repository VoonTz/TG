using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene (opcional, se usar Timeline)")]
    [SerializeField] private PlayableDirector director;

    [Header("Botão de saída")]
    [SerializeField] private Button exitButton;

    [Header("Cena de destino")]
    [SerializeField] private string menuSceneName = "Main Menu";

    private void Awake()
    {
        Time.timeScale = 1f;

        // Garante que existe um EventSystem na cena
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            Debug.LogWarning("Nenhum EventSystem na cena! Criando um.");
            new GameObject("EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        exitButton.gameObject.SetActive(false);
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(ReturnToMenu);
    }

    private void OnEnable()
    {
        if (director != null)
            director.stopped += OnDirectorStopped;
    }

    private void OnDisable()
    {
        if (director != null)
            director.stopped -= OnDirectorStopped;
    }

    private void OnDirectorStopped(PlayableDirector d)
    {
        CutsceneFinished();
    }

    // Pode ser chamado por Animation Event ou Signal do Timeline
    public void CutsceneFinished()
    {
        Debug.Log("Cutscene finalizada!");
        Time.timeScale = 1f;
        exitButton.gameObject.SetActive(true);
        exitButton.interactable = true;
    }

    public void ReturnToMenu()
    {
        Debug.Log("Botão Exit pressionado!");
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}