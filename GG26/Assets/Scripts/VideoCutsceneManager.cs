
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoCutsceneManager : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("UI")]
    [SerializeField] private GameObject buttonExit;

    private bool videoFinished = false;

    private void Start()
    {
        Time.timeScale = 1f;

        buttonExit.SetActive(false);

        // Registra o evento de finalização
        videoPlayer.loopPointReached += OnVideoFinished;

        // Inicia o vídeo
        videoPlayer.Play();

        Debug.Log("Cutscene iniciada!");
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (videoFinished)
            return;

        videoFinished = true;

        Debug.Log("Cutscene finalizada!");

        buttonExit.SetActive(true);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("Main Menu");
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
