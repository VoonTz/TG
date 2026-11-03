using UnityEngine;

public class CutsceneAnimationEvent : MonoBehaviour
{
    [SerializeField] private CutsceneManager cutsceneManager;

    public void CutsceneFinished()
    {
        Debug.Log("Evento da cutscene executado!");

        cutsceneManager.CutsceneFinished();
    }
}
