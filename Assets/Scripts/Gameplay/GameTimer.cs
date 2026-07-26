using System;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float startTime = 45f;
    [SerializeField] private UIManager uiManager;

    public event Action TimeUp;

    private float currentTime;
    private bool isRunning;

    private void Start()
    {
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        Debug.Log("[GameTimer] Start - iniciando en " + startTime + "s");
        StartTimer();
    }

    public void StartTimer()
    {
        currentTime = startTime;
        isRunning = true;
        UpdateTimeText();
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    private void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            UpdateTimeText();
            TimeUp?.Invoke();
            return;
        }

        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        if (uiManager != null) uiManager.UpdateTimeText(currentTime);
    }
}
