using UnityEngine;

public class StepSoundController : MonoBehaviour
{
    [Header("Configuración de Pasos")]
    [SerializeField] private AudioClip stepClip1;
    [SerializeField] private AudioClip stepClip2;
    [SerializeField] private AudioClip stepClip3;
    [SerializeField] private AudioClip stepClip4;
    [SerializeField] private AudioClip stepClip5;
    [SerializeField] private float stepInterval = 0.3f;
    [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;
    private float stepTimer = 0f;
    private bool isMoving = false;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        // 🔥 USANDO EL INPUT MANAGER CLÁSICO (100% compatible)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);

        if (isMoving)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                PlayStepSound();
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private void PlayStepSound()
    {
        AudioClip[] clips = { stepClip1, stepClip2, stepClip3, stepClip4, stepClip5 };
        System.Collections.Generic.List<AudioClip> validClips = new System.Collections.Generic.List<AudioClip>();
        foreach (AudioClip clip in clips)
        {
            if (clip != null) validClips.Add(clip);
        }

        if (validClips.Count == 0)
        {
            Debug.LogWarning("⚠️ StepSoundController: No hay clips de pasos asignados.");
            return;
        }

        int randomIndex = Random.Range(0, validClips.Count);
        AudioClip selectedClip = validClips[randomIndex];

        if (audioSource != null && selectedClip != null)
        {
            audioSource.PlayOneShot(selectedClip);
            Debug.Log("👣 Paso reproducido: " + selectedClip.name);
        }
    }
}