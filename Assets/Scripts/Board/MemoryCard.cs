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
    private const float RevealedAngle = 180f;
    private const float HiddenAngle = 0f;
    private const float OrientationCorrectionAngle = 180f;

    [SerializeField] private int pairId;
    [SerializeField] private float flipDuration = 0.3f;
    [SerializeField] private float matchBounceHeight = 0.15f;
    [SerializeField] private Vector3 flipRotationAxis = Vector3.right;
    [SerializeField] private float flipDirection = 1f;
    [SerializeField] private Renderer cardRenderer;

    // flipPivot is an EXTERNAL parent built by Stage4CardSetup: Parejas -> flipPivot ->
    // faceOrientationPivot -> this card's whole prefab instance (root, with pieza3 untouched
    // inside it). Neither pivot nor this card's own transform are ever touched directly by
    // hand — pieza3 stays exactly where it always was inside the prefab.
    [SerializeField] private Transform flipPivot;

    // Independent of the flip: the revealed face came out upside-down regardless of whether
    // flipPivot rotates around X or Z, which means the source mesh's back-face UVs carry a
    // fixed 180° twist. faceOrientationPivot corrects that twist by yawing around its own
    // Vector3.up the instant the card is edge-on (invisible), so the correction never reads
    // as a visible glitch.
    [SerializeField] private Transform faceOrientationPivot;

    public int PairId => pairId;
    public CardState State { get; private set; } = CardState.Hidden;
    public bool IsAnimating { get; private set; }
    public float FlipDuration => flipDuration;

    private Quaternion pivotInitialLocalRotation;
    private Vector3 pivotInitialLocalPosition;
    private Vector3 pivotInitialLocalScale;

    private Quaternion faceOrientationInitialLocalRotation;

    private float currentAngle;
    private Coroutine reactionCoroutine;

    private void Awake()
    {
        if (cardRenderer == null) cardRenderer = GetComponentInChildren<Renderer>();

        // Neither pivot is created here if the persisted external reference is missing —
        // building this hierarchy at runtime would restructure things other systems may
        // already depend on. That is Stage4CardSetup's job at edit time.
        if (flipPivot != null)
        {
            pivotInitialLocalPosition = flipPivot.localPosition;
            pivotInitialLocalRotation = flipPivot.localRotation;
            pivotInitialLocalScale = flipPivot.localScale;
        }

        if (faceOrientationPivot != null)
        {
            faceOrientationInitialLocalRotation = faceOrientationPivot.localRotation;
        }
    }

    public void Reveal()
    {
        if (State != CardState.Hidden || IsAnimating) return;

        State = CardState.Revealed;
        PlayReaction(RotateRoutine(RevealedAngle));
    }

    public void Hide()
    {
        if (State == CardState.Matched) return;

        State = CardState.Hidden;
        PlayReaction(RotateRoutine(HiddenAngle));
    }

    public void SetMatched()
    {
        State = CardState.Matched;
        currentAngle = RevealedAngle;
        ApplyPivotAngle(currentAngle);
        ApplyFaceOrientation(true);
        PlayReaction(MatchRoutine());
    }

    public void ResetCard()
    {
        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
            reactionCoroutine = null;
        }

        IsAnimating = false;
        State = CardState.Hidden;
        currentAngle = HiddenAngle;

        if (flipPivot != null)
        {
            flipPivot.localPosition = pivotInitialLocalPosition;
            flipPivot.localRotation = pivotInitialLocalRotation;
            flipPivot.localScale = pivotInitialLocalScale;
        }

        ApplyFaceOrientation(false);
    }

    private void PlayReaction(IEnumerator routine)
    {
        if (reactionCoroutine != null) StopCoroutine(reactionCoroutine);
        reactionCoroutine = StartCoroutine(routine);
    }

    private void ApplyPivotAngle(float angle)
    {
        if (flipPivot == null) return;
        flipPivot.localRotation = pivotInitialLocalRotation * Quaternion.AngleAxis(angle * flipDirection, flipRotationAxis.normalized);
    }

    private void ApplyFaceOrientation(bool corrected)
    {
        if (faceOrientationPivot == null) return;
        faceOrientationPivot.localRotation = corrected
            ? faceOrientationInitialLocalRotation * Quaternion.AngleAxis(OrientationCorrectionAngle, Vector3.up)
            : faceOrientationInitialLocalRotation;
    }

    private IEnumerator RotateRoutine(float targetAngle)
    {
        IsAnimating = true;
        float startAngle = currentAngle;
        bool revealing = targetAngle > startAngle;
        float midAngle = (startAngle + targetAngle) / 2f;
        float halfDuration = flipDuration / 2f;

        yield return AnimateSegment(startAngle, midAngle, halfDuration);

        // The card is edge-on (effectively invisible) right at the midpoint — the safe moment
        // to snap the orientation correction on or off without it reading as a visible pop.
        ApplyFaceOrientation(revealing);

        yield return AnimateSegment(midAngle, targetAngle, halfDuration);

        currentAngle = targetAngle;
        ApplyPivotAngle(currentAngle);

        IsAnimating = false;
        reactionCoroutine = null;
    }

    private IEnumerator AnimateSegment(float fromAngle, float toAngle, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            currentAngle = Mathf.Lerp(fromAngle, toAngle, elapsed / duration);
            ApplyPivotAngle(currentAngle);

            elapsed += Time.deltaTime;
            yield return null;
        }

        currentAngle = toAngle;
        ApplyPivotAngle(currentAngle);
    }

    private IEnumerator MatchRoutine()
    {
        IsAnimating = true;
        float elapsed = 0f;

        while (elapsed < flipDuration)
        {
            float t = elapsed / flipDuration;
            float bounce = Mathf.Sin(t * Mathf.PI);

            if (flipPivot != null)
            {
                flipPivot.localPosition = pivotInitialLocalPosition + Vector3.up * bounce * matchBounceHeight;
                flipPivot.localScale = pivotInitialLocalScale * (1f + bounce * 0.15f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (flipPivot != null)
        {
            flipPivot.localPosition = pivotInitialLocalPosition;
            flipPivot.localScale = pivotInitialLocalScale;
        }

        IsAnimating = false;
        reactionCoroutine = null;
    }
}
