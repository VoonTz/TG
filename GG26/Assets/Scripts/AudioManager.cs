using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("------ Audio Fonte ------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("------ Audio Clip ------")]
    public AudioClip background;
    public AudioClip death;
    public AudioClip boss;
    public AudioClip enemies;
    public AudioClip walk;

    private void Start()
    {
        musicSource.clip=background;
        musicSource.Play();
    }
}
