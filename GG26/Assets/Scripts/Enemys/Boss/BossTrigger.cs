using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    public Animator bossAnimator; // arrasta o boss aqui no inspector

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
           Debug.Log("Trigger ativado");
           bossAnimator.SetTrigger("Entrar");
        }
        
    }
}