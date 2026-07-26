using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    private const string CanvasObjectName = "GameplayCanvas";

    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text matchesText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (timeText == null) timeText = FindTextByName("TimeText");
        if (matchesText == null) matchesText = FindTextByName("MatchesText");
        if (messageText == null) messageText = FindTextByName("MessageText");
        if (restartButton == null) restartButton = FindButtonByName("RestartButton");

        EnsureVisible(timeText);
        EnsureVisible(matchesText);
        EnsureVisible(messageText);

        if (timeText != null) timeText.text = "00:45";
        if (matchesText != null) matchesText.text = "0 / 4";
        if (messageText != null) messageText.text = "Encuentra las parejas";
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        Debug.Log("[UIManager] Awake - UI inicializada");
    }

    public void SetRestartCallback(UnityAction callback)
    {
        if (restartButton == null) return;

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(callback);
    }

    public void ShowRestartButton()
    {
        if (restartButton != null) restartButton.gameObject.SetActive(true);
    }

    public void HideRestartButton()
    {
        if (restartButton != null) restartButton.gameObject.SetActive(false);
    }

    private Button FindButtonByName(string objectName)
    {
        GameObject canvasObject = GameObject.Find(CanvasObjectName);
        if (canvasObject == null) return null;

        Transform found = FindChildByName(canvasObject.transform, objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    public void UpdateTimeText(float timeInSeconds)
    {
        if (timeText == null) return;

        EnsureVisible(timeText);

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void UpdateMatchesText(int matchedPairs, int totalPairs)
    {
        if (matchesText == null) return;

        EnsureVisible(matchesText);
        matchesText.text = matchedPairs + " / " + totalPairs;
        Debug.Log("[UIManager] Update - Matches: " + matchesText.text);
    }

    public void ShowMessage(string message)
    {
        if (messageText == null) return;

        EnsureVisible(messageText);
        messageText.text = message;
        Debug.Log("[UIManager] Update - Message: " + message);
    }

    private TMP_Text FindTextByName(string objectName)
    {
        GameObject canvasObject = GameObject.Find(CanvasObjectName);
        if (canvasObject == null) return null;

        Transform found = FindChildByName(canvasObject.transform, objectName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildByName(child, childName);
            if (result != null) return result;
        }

        return null;
    }

    private static void EnsureVisible(TMP_Text text)
    {
        if (text == null) return;

        if (!text.gameObject.activeSelf) text.gameObject.SetActive(true);
        if (!text.enabled) text.enabled = true;

        Color color = text.color;
        if (color.a < 1f)
        {
            color.a = 1f;
            text.color = color;
        }
    }
}
