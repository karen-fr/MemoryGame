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
    [SerializeField] private GameObject hiddenVisual;
    [SerializeField] private GameObject revealedVisual;

    public int PairId => pairId;
    public CardState State { get; private set; } = CardState.Hidden;

    private void Awake()
    {
        Hide();
    }

    public void Reveal()
    {
        if (State == CardState.Matched) return;

        State = CardState.Revealed;

        if (hiddenVisual != null) hiddenVisual.SetActive(false);
        if (revealedVisual != null) revealedVisual.SetActive(true);
    }

    public void Hide()
    {
        if (State == CardState.Matched) return;

        State = CardState.Hidden;

        if (hiddenVisual != null) hiddenVisual.SetActive(true);
        if (revealedVisual != null) revealedVisual.SetActive(false);
    }

    public void SetMatched()
    {
        State = CardState.Matched;

        if (hiddenVisual != null) hiddenVisual.SetActive(false);
        if (revealedVisual != null) revealedVisual.SetActive(true);
    }
}
