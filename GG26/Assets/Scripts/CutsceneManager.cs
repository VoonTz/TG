using UnityEngine;
using UnityEngine.SceneManagement;
public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene")]
    [SerializeField] private GameObject cutsceneCanvas;
    [SerializeField] private Animator cutsceneAnimator;

    [Header("Botão")]
    [SerializeField] private GameObject buttonMenu;

    private bool cutsceneStarted = false;

    void Start()
    {
        buttonMenu.SetActive(false);
        cutsceneCanvas.SetActive(false);
    }

    public void PlayCutscene()
    {
        if (cutsceneStarted)
            return;

        cutsceneStarted = true;

        Debug.Log("Cutscene iniciada!");

        Time.timeScale = 0f;

        cutsceneCanvas.SetActive(true);

        buttonMenu.SetActive(false);

        cutsceneAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        cutsceneAnimator.Play("Cutscene", 0, 0f);
    }

    public void CutsceneFinished()
    {
        Debug.Log("Cutscene finalizada!");

        // Exibe o botão de retorno
        buttonMenu.SetActive(true);
    }

    public void ReturnToMenu()
    {
        // Retoma o tempo normal antes de trocar de cena
        Time.timeScale = 1f;

        // Carrega o menu principal
        SceneManager.LoadScene("Main Menu");
    }
}
