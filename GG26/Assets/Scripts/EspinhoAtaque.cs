using UnityEngine;
using System.Collections;

public class EspinhoAtaque : MonoBehaviour
{
    public float tempoAviso = 1f;
    public float duracaoAtivo = 2f;
    public int dano = 1;

    Collider2D col;
    SpriteRenderer sr;

    void Start()
    {
        //  FORÇA Z = -1 DESDE O INÍCIO
        Vector3 pos = transform.position;
        pos.z = -1f;
        transform.position = pos;

        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        col.enabled = false;

        StartCoroutine(AtivarEspinho());
    }

    IEnumerator AtivarEspinho()
    {
        Debug.Log("Espinho apareceu (SEM dano)");

        // estado de aviso
        sr.color = Color.yellow;

        yield return new WaitForSeconds(tempoAviso);

        // estado ativo
        col.enabled = true;
        sr.color = Color.red;

        Debug.Log("Espinho ATIVO (COM dano)");

        yield return new WaitForSeconds(duracaoAtivo);

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("ESPINHO ACERTOU O PLAYER");

            // other.GetComponent<PlayerLife>()?.TomarDano(dano);
        }
    }
}
