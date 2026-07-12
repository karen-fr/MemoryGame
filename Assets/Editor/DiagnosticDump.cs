using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DiagnosticDump
{
    private static int frameCount;
    private static bool loggedAtFrame3;
    private static bool loggedAtFrame30;

    public static void RunPlayTest()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        frameCount = 0;
        loggedAtFrame3 = false;
        loggedAtFrame30 = false;
        EditorApplication.update += OnUpdate;
        EditorApplication.isPlaying = true;
    }

    private static void OnUpdate()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        frameCount++;

        if (frameCount == 3 && !loggedAtFrame3)
        {
            loggedAtFrame3 = true;
            LogState("FRAME3");
        }

        if (frameCount == 30 && !loggedAtFrame30)
        {
            loggedAtFrame30 = true;
            LogState("FRAME30");
            EditorApplication.update -= OnUpdate;
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }

    private static void LogState(string tag)
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        UIManager ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        CatGridController controller = Object.FindFirstObjectByType<CatGridController>(FindObjectsInactive.Include);

        if (gm == null || ui == null || controller == null)
        {
            Debug.Log($"[{tag}] MISSING refs gm={gm} ui={ui} controller={controller}");
            return;
        }

        FieldInfo messageTextField = typeof(UIManager).GetField("messageText", BindingFlags.NonPublic | BindingFlags.Instance);
        TMPro.TMP_Text messageText = (TMPro.TMP_Text)messageTextField.GetValue(ui);

        FieldInfo timeTextField = typeof(UIManager).GetField("timeText", BindingFlags.NonPublic | BindingFlags.Instance);
        TMPro.TMP_Text timeText = (TMPro.TMP_Text)timeTextField.GetValue(ui);

        FieldInfo matchesTextField = typeof(UIManager).GetField("matchesText", BindingFlags.NonPublic | BindingFlags.Instance);
        TMPro.TMP_Text matchesText = (TMPro.TMP_Text)matchesTextField.GetValue(ui);

        FieldInfo isGameOverField = typeof(GameManager).GetField("isGameOver", BindingFlags.NonPublic | BindingFlags.Instance);
        bool isGameOver = (bool)isGameOverField.GetValue(gm);

        Debug.Log($"[{tag}] messageText='{messageText.text}' timeText='{timeText.text}' matchesText='{matchesText.text}' isGameOver={isGameOver} playerPos={controller.transform.position}");
    }
}
