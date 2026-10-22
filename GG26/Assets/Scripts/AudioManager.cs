
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource bossMusicSource;
    [SerializeField] private AudioSource SFXSource;

    [Header("Audio Clip")]
    public AudioClip background;
    public AudioClip death;
    public AudioClip boss;
    public AudioClip enemies;
    public AudioClip walk;

    [Header("Music Fade")]
    [SerializeField] private float fadeOutDuration = 2f;
    [SerializeField] private float fadeInDuration = 3f;

    private float normalMusicVolume;
    private float bossMusicVolume;
    private bool bossMusicStarted = false;
    private Coroutine musicTransition;

    private void Start()
    {
        // Guarda os volumes configurados no Inspector.
        normalMusicVolume = musicSource.volume;
        bossMusicVolume = bossMusicSource.volume;

        // Inicia a música normal.
        musicSource.clip = background;
        musicSource.loop = true;
        musicSource.Play();

        // Deixa a música do boss preparada, mas silenciosa.
        bossMusicSource.clip = boss;
        bossMusicSource.loop = true;
        bossMusicSource.volume = 0f;
        bossMusicSource.Stop();
    }

    public void PlayBossMusic()
    {
        if (bossMusicStarted)
            return;

        bossMusicStarted = true;

        if (musicTransition != null)
            StopCoroutine(musicTransition);

        musicTransition = StartCoroutine(CrossfadeMusic());
    }

    private IEnumerator CrossfadeMusic()
    {
        // Começa a música do boss em volume zero.
        bossMusicSource.volume = 0f;
        bossMusicSource.Play();

        float elapsed = 0f;
        float duration = Mathf.Max(fadeOutDuration, fadeInDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float outProgress = fadeOutDuration > 0f
                ? Mathf.Clamp01(elapsed / fadeOutDuration)
                : 1f;

            float inProgress = fadeInDuration > 0f
                ? Mathf.Clamp01(elapsed / fadeInDuration)
                : 1f;

            musicSource.volume = Mathf.Lerp(
                normalMusicVolume, 0f, outProgress);

            bossMusicSource.volume = Mathf.Lerp(
                0f, bossMusicVolume, inProgress);

            yield return null;
        }

        // Garante os volumes finais.
        musicSource.volume = 0f;
        musicSource.Stop();

        bossMusicSource.volume = bossMusicVolume;

        musicTransition = null;
    }
}
