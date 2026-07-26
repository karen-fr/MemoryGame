using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip matchClip;
    [SerializeField] private AudioClip mismatchClip;
    [SerializeField] private AudioClip winClip;

    public void PlayJumpSound()
    {
        PlayClip(jumpClip);
    }

    public void PlayMatchSound()
    {
        PlayClip(matchClip);
    }

    public void PlayMismatchSound()
    {
        PlayClip(mismatchClip);
    }

    public void PlayWinSound()
    {
        PlayClip(winClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }
}
