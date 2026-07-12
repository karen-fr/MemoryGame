using System.Collections;
using UnityEngine;

public enum CardState
{
    Hidden,
    Revealed,
    Matched
}

public class MemoryCard : MonoBehaviour
{
    [SerializeField] private int pairId;
    [SerializeField] private float flipDuration = 0.2f;
    [SerializeField] private float matchBounceHeight = 0.15f;

    public int PairId => pairId;
    public CardState State { get; private set; } = CardState.Hidden;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private Coroutine reactionCoroutine;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialLocalScale = transform.localScale;
    }

    public void Reveal()
    {
        if (State == CardState.Matched) return;

        State = CardState.Revealed;
        PlayReaction(FlipRoutine());
    }

    public void Hide()
    {
        if (State == CardState.Matched) return;

        State = CardState.Hidden;
        PlayReaction(FlipRoutine());
    }

    public void SetMatched()
    {
        State = CardState.Matched;
        PlayReaction(MatchRoutine());
    }

    private void PlayReaction(IEnumerator routine)
    {
        if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);
        ResetTransform();
        reactionCoroutine = StartCoroutine(routine);
    }

    private void ResetTransform()
    {
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        transform.localScale = initialLocalScale;
    }

    private IEnumerator FlipRoutine()
    {
        float half = flipDuration / 2f;

        float elapsed = 0f;
        while (elapsed < half)
        {
            float scaleX = Mathf.Lerp(1f, 0f, elapsed / half);
            transform.localScale = new Vector3(initialLocalScale.x * scaleX, initialLocalScale.y, initialLocalScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            float scaleX = Mathf.Lerp(0f, 1f, elapsed / half);
            transform.localScale = new Vector3(initialLocalScale.x * scaleX, initialLocalScale.y, initialLocalScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetTransform();
        reactionCoroutine = null;
    }

    private IEnumerator MatchRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            float t = elapsed / flipDuration;
            transform.localPosition = initialLocalPosition + Vector3.up * Mathf.Sin(t * Mathf.PI) * matchBounceHeight;
            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetTransform();
        reactionCoroutine = null;
    }
}
