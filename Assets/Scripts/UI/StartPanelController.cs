using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StartPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button startButton;
    [SerializeField] private GameManager gameManager;

    private bool started;

    private void OnEnable()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();

        started = false;

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStart);
            startButton.onClick.AddListener(HandleStart);
            Debug.Log($"[StartPanelController] Button conectado: {startButton.name}");
        }
        else
        {
            Debug.LogWarning("[StartPanelController] startButton no está asignado.");
        }
    }

    private void OnDisable()
    {
        if (startButton != null) startButton.onClick.RemoveListener(HandleStart);
    }

    private void Update()
    {
        if (started) return;
        if (panelRoot == null || !panelRoot.activeInHierarchy) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool confirmPressed = keyboard.enterKey.wasPressedThisFrame
            || keyboard.numpadEnterKey.wasPressedThisFrame
            || keyboard.spaceKey.wasPressedThisFrame;

        if (confirmPressed) HandleStart();
    }

    private void HandleStart()
    {
        if (started) return;
        started = true;

        Debug.Log("[StartPanelController] Inicio pulsado");

        if (gameManager != null) gameManager.StartGame();
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
