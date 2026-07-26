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
    [SerializeField] private float flipDuration = 0.3f;
    [SerializeField] private float matchBounceHeight = 0.15f;
    [SerializeField] private Vector3 flipRotationAxis = Vector3.right;

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

        // Tip to 90 degrees while shrinking to near-zero on the flip axis's face scale.
        yield return AnimateFlipHalf(0f, 90f, 1f, 0.05f, half);

        // At this near-invisible point it is safe to snap the rotation back before the
        // never-rendered backface (90-180 range) would otherwise show through.
        yield return AnimateFlipHalf(0f, 0f, 0.05f, 1f, half);

        ResetTransform();
        reactionCoroutine = null;
    }

    private IEnumerator AnimateFlipHalf(float fromAngle, float toAngle, float fromScale, float toScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float angle = Mathf.Lerp(fromAngle, toAngle, t);
            float scaleX = Mathf.Lerp(fromScale, toScale, t);

            transform.localRotation = initialLocalRotation * Quaternion.AngleAxis(angle, flipRotationAxis);
            transform.localScale = new Vector3(initialLocalScale.x * scaleX, initialLocalScale.y, initialLocalScale.z);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator MatchRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flipDuration)
        {
            float t = elapsed / flipDuration;
            float bounce = Mathf.Sin(t * Mathf.PI);

            transform.localPosition = initialLocalPosition + Vector3.up * bounce * matchBounceHeight;
            transform.localScale = initialLocalScale * (1f + bounce * 0.15f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetTransform();
        reactionCoroutine = null;
    }
}
