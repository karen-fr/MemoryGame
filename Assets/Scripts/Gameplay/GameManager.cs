using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private CatGridController catController;
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private EndPanelController endPanelController;
    [SerializeField] private int totalPairs = 4;
    [SerializeField] private float compareDelay = 0.6f;

    private MemoryCard firstCard;
    private MemoryCard secondCard;
    private int matchedPairsCount;
    private bool isInputLocked;
    private bool isGameOver;
    private bool hasGameStarted;

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
        if (uiManager != null) uiManager.SetRestartCallback(RestartGame);

        StartPanelController startPanel = FindFirstObjectByType<StartPanelController>();
        if (startPanel != null && startPanel.isActiveAndEnabled)
        {
            if (catController != null) catController.SetInputLocked(true);
            if (endPanelController != null) endPanelController.Hide();
        }
        else
        {
            StartGame();
        }
    }

    private void ResolveReferences()
    {
        if (catController == null) catController = FindFirstObjectByType<CatGridController>();
        if (gameTimer == null) gameTimer = FindFirstObjectByType<GameTimer>();
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        if (endPanelController == null) endPanelController = FindFirstObjectByType<EndPanelController>();
    }

    public void StartGame()
    {
        Debug.Log("[GameManager] StartGame ejecutado");

        if (hasGameStarted) return;
        hasGameStarted = true;

        matchedPairsCount = 0;
        isGameOver = false;
        isInputLocked = false;
        firstCard = null;
        secondCard = null;

        if (catController != null) catController.SetInputLocked(false);
        if (endPanelController != null) endPanelController.Hide();

        if (uiManager != null)
        {
            uiManager.UpdateMatchesText(matchedPairsCount, totalPairs);
            uiManager.ShowMessage("Encuentra las parejas");
            uiManager.HideRestartButton();
        }

        if (gameTimer != null) gameTimer.StartTimer();

        // Sonido de inicio + musica principal del juego
        if (audioManager != null)
        {
            audioManager.PlayStartSound();
            audioManager.PlayBackgroundMusic();
        }
    }

    private void HandleCardSelected(MemoryCard card)
    {
        if (isInputLocked || isGameOver) return;
        if (card.State != CardState.Hidden) return;

        card.Reveal();
        if (audioManager != null) audioManager.PlayFlipCardSound();

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
        while (firstCard.IsAnimating || secondCard.IsAnimating)
        {
            yield return null;
        }

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
            else
            {
                Debug.Log("[GameManager] Reaccion de pareja correcta");
                if (catController != null) catController.PlayPairMatchReaction();
            }
        }
        else
        {
            firstCard.Hide();
            secondCard.Hide();

            if (audioManager != null) audioManager.PlayErrorSound();
            if (uiManager != null) uiManager.ShowMessage("Intenta otra vez");

            Debug.Log("[GameManager] Reaccion de pareja incorrecta");
            if (catController != null) catController.PlayPairMismatchReaction();
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
        if (catController != null)
        {
            catController.SetInputLocked(true);
            catController.PlayWinCelebration();
        }

        const string message = "¡Ganaste!";
        Debug.Log("[GameManager] Mostrando reinicio por victoria");
        if (uiManager != null)
        {
            uiManager.ShowMessage(message);
            uiManager.ShowRestartButton();
        }
        if (endPanelController != null) endPanelController.Show(message);

        // Detiene la musica principal y reproduce Victory + Finish juntos
        if (audioManager != null)
        {
            audioManager.StopBackgroundMusic();
            audioManager.PlayWinSound();
            audioManager.PlayFinishSound();
        }
    }

    private void HandleTimeUp()
    {
        if (isGameOver) return;

        isGameOver = true;
        isInputLocked = true;
        if (catController != null)
        {
            catController.SetInputLocked(true);
            catController.PlayLoseReaction();
        }

        const string message = "Se acabo el tiempo";
        Debug.Log("[GameManager] Mostrando reinicio por derrota");
        if (uiManager != null)
        {
            uiManager.ShowMessage(message);
            uiManager.ShowRestartButton();
        }
        if (endPanelController != null) endPanelController.Show(message);

        // Detiene la musica principal y reproduce Loss + Finish juntos
        if (audioManager != null)
        {
            audioManager.StopBackgroundMusic();
            audioManager.PlayLossSound();
            audioManager.PlayFinishSound();
        }
    }

    public void RestartGame()
    {
        Debug.Log("[GameManager] RestartGame ejecutado");
        StopAllCoroutines();

        matchedPairsCount = 0;
        isGameOver = false;
        isInputLocked = false;
        firstCard = null;
        secondCard = null;

        foreach (MemoryCard card in FindObjectsByType<MemoryCard>(FindObjectsSortMode.None))
        {
            card.ResetCard();
        }

        if (catController != null) catController.ResetToInitialState();
        if (gameTimer != null) gameTimer.StartTimer();
        if (endPanelController != null) endPanelController.Hide();

        if (uiManager != null)
        {
            uiManager.UpdateMatchesText(matchedPairsCount, totalPairs);
            uiManager.ShowMessage("Encuentra las parejas");
            uiManager.HideRestartButton();
        }
    }
}
