
using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    [Header("Boss")]
    public Animator bossAnimator;

    [Header("Audio")]
    public AudioManager audioManager;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player"))
            return;

        triggered = true;

        Debug.Log("Trigger do Boss ativado!");

        // Inicia a animação de entrada.
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Entrar");
        }

        // Faz a transição das músicas.
        if (audioManager != null)
        {
            audioManager.PlayBossMusic();
        }
        else
        {
            Debug.LogWarning("AudioManager não foi configurado no BossTrigger!");
        }
    }
}
