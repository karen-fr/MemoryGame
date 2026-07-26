using UnityEngine;
using UnityEngine.UI;

public class OptionsPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button soundOnButton;
    [SerializeField] private Button soundOffButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private CatGridController catController;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (catController == null) catController = FindFirstObjectByType<CatGridController>();

        panelRoot.SetActive(false);

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (soundOnButton != null) soundOnButton.onClick.AddListener(MuteAudio);
        if (soundOffButton != null) soundOffButton.onClick.AddListener(UnmuteAudio);
        if (exitButton != null) exitButton.onClick.AddListener(Quit);

        UpdateSoundButtons();
    }

    private void Open()
    {
        panelRoot.SetActive(true);
        if (catController != null) catController.SetInputLocked(true);
    }

    private void Close()
    {
        panelRoot.SetActive(false);
        if (catController != null) catController.SetInputLocked(false);
    }

    private void MuteAudio()
    {
        AudioListener.pause = true;
        UpdateSoundButtons();
    }

    private void UnmuteAudio()
    {
        AudioListener.pause = false;
        UpdateSoundButtons();
    }

    private void UpdateSoundButtons()
    {
        bool muted = AudioListener.pause;
        if (soundOnButton != null) soundOnButton.gameObject.SetActive(!muted);
        if (soundOffButton != null) soundOffButton.gameObject.SetActive(muted);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        Debug.Log("[OptionsPanelController] Salida solicitada (Editor) - no se cierra Unity.");
#else
        Application.Quit();
#endif
    }
}
