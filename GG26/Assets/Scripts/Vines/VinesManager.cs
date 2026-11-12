
using UnityEngine;
using System.Collections;

public class VinesManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private int requiredItems = 4;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collectSFX;
    [SerializeField] private float sfxVolume = 1f;

    [Header("Background Music")]
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private float duckVolume = 0.3f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float returnDuration = 0.6f;

    private float originalMusicVolume;
    private Coroutine musicCoroutine;

    [Header("Tilemap das vinhas")]
    [SerializeField] private GameObject vinesTilemap;
    [SerializeField] private GameObject Gate;

    private int collectedItems = 0;
    private bool vinesDestroyed = false;

    public void CollectItem()
    {
        if (vinesDestroyed)
            return;

        collectedItems++;

        // Reproduz o som de coleta.
        if (audioSource != null && collectSFX != null)
        {
            audioSource.PlayOneShot(collectSFX, sfxVolume);
            DuckBackgroundMusic();
        }

        Debug.Log("Objetos coletados: " +
            collectedItems + "/" + requiredItems);

        if (collectedItems >= requiredItems)
        {
            DestroyVines();
        }
    }

    private void DuckBackgroundMusic()
    {
        if (backgroundMusic == null)
            return;

        if (musicCoroutine != null)
            StopCoroutine(musicCoroutine);

        musicCoroutine = StartCoroutine(
            ChangeMusicVolume()
        );
    }

    private IEnumerator ChangeMusicVolume()
    {
        originalMusicVolume = backgroundMusic.volume;

        // Abaixa a música.
        yield return StartCoroutine(
            FadeMusic(
                originalMusicVolume * duckVolume,
                fadeDuration
            )
        );

        // Aguarda o efeito sonoro terminar.
        if (collectSFX != null)
            yield return new WaitForSeconds(collectSFX.length);

        // Recupera o volume original.
        yield return StartCoroutine(
            FadeMusic(
                originalMusicVolume,
                returnDuration
            )
        );

        musicCoroutine = null;
    }

    private IEnumerator FadeMusic(float targetVolume, float duration)
    {
        float startVolume = backgroundMusic.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            backgroundMusic.volume = Mathf.Lerp(
                startVolume,
                targetVolume,
                timer / duration
            );

            yield return null;
        }

        backgroundMusic.volume = targetVolume;
    }

    private void DestroyVines()
    {
        vinesDestroyed = true;

        if (vinesTilemap != null)
        {
            vinesTilemap.SetActive(false);
        }

        Debug.Log("As vinhas foram removidas!");
    }
}
