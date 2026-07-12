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
        if (catController != null) catController.TrapTriggered += HandleTrapTriggered;
        if (gameTimer != null) gameTimer.TimeUp += HandleTimeUp;
    }

    private void OnDisable()
    {
        if (catController != null) catController.CardSelected -= HandleCardSelected;
        if (catController != null) catController.TrapTriggered -= HandleTrapTriggered;
        if (gameTimer != null) gameTimer.TimeUp -= HandleTimeUp;
    }

    private void Awake()
    {
        ResolveReferences();
        Debug.Log("[GameManager] Awake");
    }

    private void Start()
    {
        Debug.Log("[GameManager] Start");
        StartGame();
    }

    private void ResolveReferences()
    {
        if (catController == null) catController = FindFirstObjectByType<CatGridController>();
        if (gameTimer == null) gameTimer = FindFirstObjectByType<GameTimer>();
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
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
            uiManager.ShowMessage("Encuentra las parejas");
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
            if (uiManager != null) uiManager.ShowMessage("Ficha seleccionada, busca su pareja");
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
            if (uiManager != null)
            {
                uiManager.UpdateMatchesText(matchedPairsCount, totalPairs);
                uiManager.ShowMessage("Pareja encontrada");
            }

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
            if (uiManager != null) uiManager.ShowMessage("Intenta otra vez");
        }

        firstCard = null;
        secondCard = null;
        isInputLocked = false;
    }

    private void HandleTrapTriggered(float lockDuration)
    {
        if (isGameOver) return;

        if (uiManager != null) uiManager.ShowMessage("Trampa, espera...");
        StartCoroutine(ClearTrapMessage(lockDuration));
    }

    private IEnumerator ClearTrapMessage(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isGameOver) yield break;
        if (uiManager != null) uiManager.ShowMessage("Encuentra las parejas");
    }

    private void WinGame()
    {
        isGameOver = true;

        if (gameTimer != null) gameTimer.StopTimer();
        if (uiManager != null) uiManager.ShowMessage("¡Ganaste!");
        if (audioManager != null) audioManager.PlayWinSound();
    }

    private void HandleTimeUp()
    {
        if (isGameOver) return;

        isGameOver = true;
        isInputLocked = true;

        if (uiManager != null) uiManager.ShowMessage("Se acabó el tiempo");
    }
}
