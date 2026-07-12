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

    public int PairId => pairId;
    public CardState State { get; private set; } = CardState.Hidden;

    private Quaternion hiddenRotation;
    private Quaternion revealedRotation;

    private void Awake()
    {
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }

        hiddenRotation = transform.localRotation;
        revealedRotation = hiddenRotation * Quaternion.Euler(0f, 180f, 0f);
    }

    public void Reveal()
    {
        if (State == CardState.Matched) return;

        State = CardState.Revealed;
        StopAllCoroutines();
        StartCoroutine(FlipTo(revealedRotation));
    }

    public void Hide()
    {
        if (State == CardState.Matched) return;

        State = CardState.Hidden;
        StopAllCoroutines();
        StartCoroutine(FlipTo(hiddenRotation));
    }

    public void SetMatched()
    {
        State = CardState.Matched;
        StopAllCoroutines();
        StartCoroutine(FlipTo(revealedRotation));
    }

    private IEnumerator FlipTo(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < flipDuration)
        {
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / flipDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = targetRotation;
    }
}
