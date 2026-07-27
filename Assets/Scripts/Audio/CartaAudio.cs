using UnityEngine;

public class CartaAudio : MonoBehaviour
{
    private AudioClip sonidoVoltear;
    private AudioClip sonidoError;
    private AudioClip sonidoMatch;
    private AudioClip sonidoBowser; // ← NUEVO: para el comodín malo

    private AudioSource audioSource;

    void Start()
    {
        sonidoVoltear = Resources.Load<AudioClip>("Flip card");
        sonidoError = Resources.Load<AudioClip>("Error");
        sonidoMatch = Resources.Load<AudioClip>("match_01");
        sonidoBowser = Resources.Load<AudioClip>("BOWSER"); // ← NUEVO

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.spatialBlend = 0;
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
    }

    // Sonido al voltear una carta
    public void ReproducirVoltear()
    {
        if (sonidoVoltear != null)
        {
            audioSource.PlayOneShot(sonidoVoltear);
        }
    }

    // Sonido cuando dos cartas coinciden (MATCH)
    public void ReproducirMatch()
    {
        if (sonidoMatch != null)
        {
            audioSource.PlayOneShot(sonidoMatch);
        }
    }

    // Sonido cuando dos cartas NO coinciden (ERROR)
    public void ReproducirError()
    {
        if (sonidoError != null)
        {
            audioSource.PlayOneShot(sonidoError);
        }
    }

    // Sonido del comodín malo (BOWSER)
    public void ReproducirBowser()
    {
        if (sonidoBowser != null)
        {
            audioSource.PlayOneShot(sonidoBowser);
        }
    }
}