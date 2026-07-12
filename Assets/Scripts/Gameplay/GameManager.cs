using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private CatGridController catController;
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private int totalPairs = 4;
    [SerializeField] private float compareDelay = 0.6f;

    private MemoryCard firstCard;
    private MemoryCard secondCard;
    private int matchedPairsCount;
    private bool isInputLocked;
    private bool isGameOver;

    private void OnEnable()
    {
        if (catController != null) catController.CardSelected += HandleCardSelected;
        if (gameTimer != null) gameTimer.TimeUp += HandleTimeUp;
    }

    private void OnDisable()
    {
        if (catController != null) catController.CardSelected -= HandleCardSelected;
        if (gameTimer != null) gameTimer.TimeUp -= HandleTimeUp;
    }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        matchedPairsCount = 0;
        isGameOver = false;
        isInputLocked = false;
        firstCard = null;
        secondCard = null;

        if (uiManager != null)
        {
            uiManager.UpdateMatchesText(matchedPairsCount, totalPairs);
            uiManager.ShowMessage("Start");
        }

        if (gameTimer != null) gameTimer.StartTimer();
    }

    private void HandleCardSelected(MemoryCard card)
    {
        if (isInputLocked || isGameOver) return;
        if (card.State != CardState.Hidden) return;

        card.Reveal();
        if (audioManager != null) audioManager.PlayJumpSound();

        if (firstCard == null)
        {
            firstCard = card;
            return;
        }

        secondCard = card;
        isInputLocked = true;
        StartCoroutine(CompareCards());
    }

    private IEnumerator CompareCards()
    {
        yield return new WaitForSeconds(compareDelay);

        if (firstCard.PairId == secondCard.PairId)
        {
            firstCard.SetMatched();
            secondCard.SetMatched();
            matchedPairsCount++;

            if (audioManager != null) audioManager.PlayMatchSound();
            if (uiManager != null) uiManager.UpdateMatchesText(matchedPairsCount, totalPairs);

            if (matchedPairsCount >= totalPairs)
            {
                WinGame();
            }
        }
        else
        {
            firstCard.Hide();
            secondCard.Hide();

            if (audioManager != null) audioManager.PlayMismatchSound();
            if (uiManager != null) uiManager.ShowMessage("Try Again");
        }

        firstCard = null;
        secondCard = null;
        isInputLocked = false;
    }

    private void WinGame()
    {
        isGameOver = true;

        if (gameTimer != null) gameTimer.StopTimer();
        if (uiManager != null) uiManager.ShowMessage("Win");
        if (audioManager != null) audioManager.PlayWinSound();
    }

    private void HandleTimeUp()
    {
        if (isGameOver) return;

        isGameOver = true;
        isInputLocked = true;

        if (uiManager != null) uiManager.ShowMessage("Finish");
    }
}
