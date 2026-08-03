using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Fuente de Audio")]
    [SerializeField] private AudioSource audioSource;

    // Los audios se cargan automáticamente desde la carpeta Resources
    private AudioClip flipCardClip;
    private AudioClip matchClip;
    private AudioClip errorClip;
    private AudioClip winClip;
    private AudioClip stepsClip;
    private AudioClip jumpClip;
    private AudioClip startClip;
    private AudioClip finishClip;
    private AudioClip lossClip;
    private AudioClip tickClip;
    private AudioClip trapClip;

    [Header("Música de Fondo")]
    [SerializeField] private AudioSource backgroundMusicSource;

    private void Start()
    {
        // Cargar todos los audios desde la carpeta Resources
        flipCardClip = Resources.Load<AudioClip>("Flip card");
        matchClip = Resources.Load<AudioClip>("Match");
        errorClip = Resources.Load<AudioClip>("Error");
        winClip = Resources.Load<AudioClip>("Victory");
        stepsClip = Resources.Load<AudioClip>("Steps");
        jumpClip = Resources.Load<AudioClip>("Jump");
        startClip = Resources.Load<AudioClip>("Start");
        finishClip = Resources.Load<AudioClip>("Finish");
        lossClip = Resources.Load<AudioClip>("loss");
        tickClip = Resources.Load<AudioClip>("CountDown");
        trapClip = Resources.Load<AudioClip>("BOWSER");

        Debug.Log("🎵 AudioManager: Audios cargados desde Resources");
    }

    // =============================================
    // MÉTODOS PARA CARTAS
    // =============================================

    public void PlayFlipCardSound() { PlayClip(flipCardClip); }
    public void PlayMatchSound() { PlayClip(matchClip); }
    public void PlayErrorSound() { PlayClip(errorClip); }
    public void PlayWinSound() { PlayClip(winClip); }

    // =============================================
    // MÉTODOS PARA EL PERSONAJE
    // =============================================

    public void PlayStepsSound() { PlayClip(stepsClip); }
    public void PlayJumpSound() { PlayClip(jumpClip); }
    public void PlayTrapSound() { PlayClip(trapClip); }

    // =============================================
    // MÉTODOS PARA UI / JUEGO
    // =============================================

    public void PlayStartSound() { PlayClip(startClip); }
    public void PlayFinishSound() { PlayClip(finishClip); }
    public void PlayLossSound() { PlayClip(lossClip); }
    public void PlayTickSound() { PlayClip(tickClip); }

    // =============================================
    // MÚSICA DE FONDO
    // =============================================

    public void PlayBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.clip != null)
        {
            backgroundMusicSource.Play();
            Debug.Log("🎵 Música de fondo iniciada");
        }
    }

    public void StopBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Stop();
            Debug.Log("🔇 Música de fondo detenida");
        }
    }

    // =============================================
    // MÉTODO PRIVADO
    // =============================================

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("⚠️ AudioManager: No hay AudioSource asignado.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("⚠️ AudioManager: El clip de audio es null.");
            return;
        }

        audioSource.PlayOneShot(clip);
        Debug.Log("🔊 AudioManager reproduciendo: " + clip.name);
    }
}
