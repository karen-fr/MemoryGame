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

    public int PairId => pairId;
    public CardState State { get; private set; } = CardState.Hidden;

    public void Reveal()
    {
        if (State == CardState.Matched) return;

        State = CardState.Revealed;
    }

    public void Hide()
    {
        if (State == CardState.Matched) return;

        State = CardState.Hidden;
    }

    public void SetMatched()
    {
        State = CardState.Matched;
    }
}
