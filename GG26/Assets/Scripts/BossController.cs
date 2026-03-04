using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    EnemyBoss BossCode;

    [Header("Referências")]
    public Transform player;

    [Header("Fases")]
    public int faseAtual = 1;

    [Header("Vida")]
    public float vidaMax = 300f;
    private float vidaAtual;

    [Header("Ataque Normal")]
    public GameObject tiroPrefab;
    public float tempoEntreTiros = 1.5f;
    public float velocidadeTiro = 5f;

    [Header("Fase 2 - Escudo Giratório")]
    public GameObject tiroGiratorioPrefab;
    public int quantidadeTiros = 8;
    public float duracaoEscudo = 5f;
    public float velocidadeRotacao = 90f;

    [Header("Ataque Shotgun (Arco com Brecha)")]
    
    int direcaoBrecha = -1; // -1 = esquerda | 1 = direita

    public GameObject shotgunBulletPrefab;
    public int totalTirosShotgun = 20;
    public float anguloTotal = 120f;
    public int tamanhoBrecha = 3;
    public float cooldownShotgun = 10f;
    public float velocidadeShotgun = 6f;

    [Header("Ataque Espinho")]
    public GameObject espinhoPrefab;
    public float delayInicialEspinho = 60f;
    public float intervaloMinEspinho = 10f;
    public float intervaloMaxEspinho = 20f;



    void Start()
    {
        BossCode = FindAnyObjectByType<EnemyBoss>();

        vidaAtual = vidaMax;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        StartCoroutine(AtaqueNormal());
        StartCoroutine(ShotgunAttack());
        StartCoroutine(AtaqueEspinho());

    }

    void Update()
    {
        AtualizarFase();
    }

    // ===================== FASES =====================
    void AtualizarFase()
    {
        if (vidaAtual <= vidaMax * 0.66f && faseAtual == 1)
        {
            faseAtual = 2;
            tempoEntreTiros *= 0.8f;
        }
        else if (vidaAtual <= vidaMax * 0.33f && faseAtual == 2)
        {
            faseAtual = 3;
            tempoEntreTiros *= 0.7f;
            velocidadeTiro *= 1.3f;

            tamanhoBrecha = 1;
            velocidadeShotgun = 8f;
        }
    }

    // ===================== ATAQUE NORMAL =====================
    IEnumerator AtaqueNormal()
    {
        while (true)
        {
            Atirar();

            if (faseAtual >= 2)
                StartCoroutine(EscudoGiratorio());

            yield return new WaitForSeconds(tempoEntreTiros);
        }
    }

    void Atirar()
    {
        Vector2 direcao = (player.position - transform.position).normalized;

        GameObject tiro = Instantiate(
            tiroPrefab,
            transform.position,
            Quaternion.identity
        );

        tiro.GetComponent<Rigidbody2D>().linearVelocity =
            direcao * velocidadeTiro;
    }

    // ===================== ESCUDO GIRATÓRIO =====================
    IEnumerator EscudoGiratorio()
    {
        GameObject escudo = new GameObject("Escudo");
        escudo.transform.position = transform.position;
        escudo.transform.parent = transform;

        for (int i = 0; i < quantidadeTiros; i++)
        {
            float angulo = i * (360f / quantidadeTiros);
            Vector2 pos = new Vector2(
                Mathf.Cos(angulo * Mathf.Deg2Rad),
                Mathf.Sin(angulo * Mathf.Deg2Rad)
            ) * 1.5f;

            GameObject tiro = Instantiate(
                tiroGiratorioPrefab,
                transform.position + (Vector3)pos,
                Quaternion.identity
            );

            tiro.transform.parent = escudo.transform;
        }

        float tempo = 0f;
        while (tempo < duracaoEscudo)
        {
            escudo.transform.Rotate(
                Vector3.forward * velocidadeRotacao * Time.deltaTime
            );

            tempo += Time.deltaTime;
            yield return null;
        }

        Destroy(escudo);
    }

    // ===================== SHOTGUN =====================
    IEnumerator ShotgunAttack()
    {
        while (true)
        {
            yield return new WaitForSeconds(cooldownShotgun);

            DisparoShotgun();
        }
    }

    void DisparoShotgun()
    {
        Vector2 direcaoBase =
            (player.position - transform.position).normalized;

        float anguloInicial = -anguloTotal / 2f;
        float passo = anguloTotal / (totalTirosShotgun - 1);

        int centro = totalTirosShotgun / 2;

        // desloca a brecha pro lado
        int deslocamento = direcaoBrecha * (totalTirosShotgun / 4);
        int inicioBrecha = centro + deslocamento - tamanhoBrecha;
        int fimBrecha = centro + deslocamento + tamanhoBrecha;

        for (int i = 0; i < totalTirosShotgun; i++)
        {
            if (i >= inicioBrecha && i <= fimBrecha)
                continue;

            float angulo = anguloInicial + passo * i;
            Vector2 direcao =
                Quaternion.Euler(0, 0, angulo) * direcaoBase;

            GameObject tiro = Instantiate(
                shotgunBulletPrefab,
                transform.position,
                Quaternion.identity
            );

            tiro.GetComponent<Rigidbody2D>().linearVelocity =
                direcao * velocidadeShotgun;
        }

        // alterna lado da brecha
        direcaoBrecha *= -1;
    }

    //============== ESPINHO ===========
    IEnumerator AtaqueEspinho()
    {
        Debug.Log("Espinho: aguardando 1 minuto...");
        yield return new WaitForSeconds(delayInicialEspinho);

        Debug.Log("Espinho ATIVADO");

        while (true)
        {
            StartCoroutine(SpawnRastroEspinho());

            float proximo = Random.Range(intervaloMinEspinho, intervaloMaxEspinho);
            Debug.Log("Próximo espinho em " + proximo + "s");

            yield return new WaitForSeconds(proximo);
        }
    }

    IEnumerator SpawnRastroEspinho()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 pos = player.position;
            pos.z = -1;

            Instantiate(espinhoPrefab, pos, Quaternion.identity);

            Debug.Log("Espinho do rastro " + (i + 1));

            yield return new WaitForSeconds(1f);
        }
    }


    void SpawnEspinho()
    {
        Vector3 pos = player.position;
        pos.z = 0;

        Instantiate(espinhoPrefab, pos, Quaternion.identity);
    }




    // ===================== DANO =====================
    public void TomarDano(float dano)
    {
        vidaAtual -= dano;

        if (vidaAtual <= 0)
        {
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }
}
