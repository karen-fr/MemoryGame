using System;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private float startTime = 45f;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private int tickThreshold = 10;

    public event Action TimeUp;

    private float currentTime;
    private bool isRunning;
    private int lastTickSecond = -1;

    private void Start()
    {
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        if (audioManager == null) audioManager = FindFirstObjectByType<AudioManager>();

        // Do not auto-start: the countdown should not run before the player presses the
        // start button. GameManager.StartGame() calls StartTimer() at the right moment.
        currentTime = startTime;
        UpdateTimeText();
    }

    public void StartTimer()
    {
        Debug.Log("[GameTimer] Temporizador iniciado");

        currentTime = startTime;
        isRunning = true;
        lastTickSecond = -1;
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

        CheckTickSound();
        UpdateTimeText();
    }

    private void CheckTickSound()
    {
        int currentSecond = Mathf.CeilToInt(currentTime);

        // 🔊 Suena una vez por cada segundo dentro de los últimos "tickThreshold" segundos
        if (currentSecond <= tickThreshold && currentSecond != lastTickSecond)
        {
            lastTickSecond = currentSecond;
            if (audioManager != null) audioManager.PlayTickSound();
        }
    }

    private void UpdateTimeText()
    {
        if (uiManager != null) uiManager.UpdateTimeText(currentTime);
    }
}