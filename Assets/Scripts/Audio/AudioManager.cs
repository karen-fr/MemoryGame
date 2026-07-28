using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Fuente de Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audios del Juego - Cartas")]
    [SerializeField] private AudioClip flipCardClip;   // Sonido al voltear carta
    [SerializeField] private AudioClip matchClip;      // Sonido al encontrar pareja
    [SerializeField] private AudioClip errorClip;      // Sonido al fallar (no coinciden)
    [SerializeField] private AudioClip winClip;        // Sonido al ganar (Bowser)

    // =============================================
    // MÉTODOS NUEVOS (llamados por el GameManager)
    // =============================================

    public void PlayJumpSound()
    {
        PlayClip(flipCardClip);  // Jump = Flip card
    }

    public void PlayMatchSound()
    {
        PlayClip(matchClip);
    }

    public void PlayMismatchSound()
    {
        PlayClip(errorClip);
    }

    public void PlayWinSound()
    {
        PlayClip(winClip);
    }

    // =============================================
    // MÉTODOS PRIVADOS
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

    // =============================================
    // MÉTODOS DE PRUEBA
    // =============================================

    [ContextMenu("Probar Jump")]
    public void TestJump() { PlayJumpSound(); }

    [ContextMenu("Probar Match")]
    public void TestMatch() { PlayMatchSound(); }

    [ContextMenu("Probar Mismatch")]
    public void TestMismatch() { PlayMismatchSound(); }

    [ContextMenu("Probar Win")]
    public void TestWin() { PlayWinSound(); }
}