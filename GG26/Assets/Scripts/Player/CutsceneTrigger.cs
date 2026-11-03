using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    [SerializeField] private CutsceneManager cutsceneManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BossDead"))
        {
            Debug.Log("Jogador tocou no chefão derrotado!");

            cutsceneManager.PlayCutscene();
        }
    }
}
