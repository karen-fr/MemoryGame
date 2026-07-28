using UnityEngine;

public class CartaAudio : MonoBehaviour
{
    private AudioClip sonidoVoltear;
    private AudioClip sonidoError;
    private AudioClip sonidoMatch;
    private AudioClip sonidoBowser;

    private AudioSource audioSource;

    void Start()
    {
        // Cargar los audios desde la carpeta Resources
        sonidoVoltear = Resources.Load<AudioClip>("Flip card");
        sonidoError = Resources.Load<AudioClip>("Error");
        sonidoMatch = Resources.Load<AudioClip>("match_01");
        sonidoBowser = Resources.Load<AudioClip>("Bowser");

        // ==========================================
        // MENSAJES DE DEPURACIÓN (PASO 3)
        // ==========================================
        Debug.Log("🎵 CartaAudio iniciado en: " + gameObject.name);
        Debug.Log("Flip card: " + (sonidoVoltear != null ? "✅ CARGADO" : "❌ NO ENCONTRADO"));
        Debug.Log("Error: " + (sonidoError != null ? "✅ CARGADO" : "❌ NO ENCONTRADO"));
        Debug.Log("match_01: " + (sonidoMatch != null ? "✅ CARGADO" : "❌ NO ENCONTRADO"));
        Debug.Log("Bowser: " + (sonidoBowser != null ? "✅ CARGADO" : "❌ NO ENCONTRADO"));

        // Configurar el Audio Source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("🔊 AudioSource agregado automáticamente a: " + gameObject.name);
        }

        audioSource.spatialBlend = 0;    // Sonido 2D
        audioSource.playOnAwake = false; // Lo controlamos con el script
        audioSource.volume = 1f;
    }

    // Sonido al voltear una carta
    public void ReproducirVoltear()
    {
        if (sonidoVoltear != null)
        {
            audioSource.PlayOneShot(sonidoVoltear);
            Debug.Log("🔊 Reproduciendo: Flip card en " + gameObject.name);
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró el audio 'Flip card' en " + gameObject.name);
        }
    }

    // Sonido cuando dos cartas coinciden (MATCH)
    public void ReproducirMatch()
    {
        if (sonidoMatch != null)
        {
            audioSource.PlayOneShot(sonidoMatch);
            Debug.Log("🔊 Reproduciendo: match_01 en " + gameObject.name);
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró el audio 'match_01' en " + gameObject.name);
        }
    }

    // Sonido cuando dos cartas NO coinciden (ERROR)
    public void ReproducirError()
    {
        if (sonidoError != null)
        {
            audioSource.PlayOneShot(sonidoError);
            Debug.Log("🔊 Reproduciendo: Error en " + gameObject.name);
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró el audio 'Error' en " + gameObject.name);
        }
    }

    // Sonido del comodín malo (BOWSER)
    public void ReproducirBowser()
    {
        if (sonidoBowser != null)
        {
            audioSource.PlayOneShot(sonidoBowser);
            Debug.Log("🔊 Reproduciendo: Bowser en " + gameObject.name);
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró el audio 'Bowser' en " + gameObject.name);
        }
    }
}