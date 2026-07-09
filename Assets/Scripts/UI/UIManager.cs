using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Text timeText;
    [SerializeField] private Text matchesText;
    [SerializeField] private Text messageText;

    public void UpdateTimeText(float timeInSeconds)
    {
        if (timeText == null) return;

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void UpdateMatchesText(int matchedPairs, int totalPairs)
    {
        if (matchesText == null) return;
        matchesText.text = matchedPairs + " / " + totalPairs;
    }

    public void ShowMessage(string message)
    {
        if (messageText == null) return;
        messageText.text = message;
    }
}
