
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

        // Configura o Animator para funcionar sem depender do Time.timeScale
        cutsceneAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        cutsceneAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        cutsceneAnimator.speed = 1f;

        // Pausa o jogo
        Time.timeScale = 0f;

        // Ativa a cutscene
        cutsceneCanvas.SetActive(true);

        buttonMenu.SetActive(false);

        // Reinicia e reproduz a animação
        cutsceneAnimator.Rebind();
        cutsceneAnimator.Play("Cutscene", 0, 0f);
        cutsceneAnimator.Update(0f);
    }

    public void CutsceneFinished()
    {
        Debug.Log("Cutscene finalizada!");

        buttonMenu.SetActive(true);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("Main Menu");
    }
}
