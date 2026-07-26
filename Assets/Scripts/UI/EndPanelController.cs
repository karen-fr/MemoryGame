using TMPro;
using UnityEngine;

public class EndPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text resultText;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        panelRoot.SetActive(false);
    }

    public void Show(string message)
    {
        if (resultText != null) resultText.text = message;
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
