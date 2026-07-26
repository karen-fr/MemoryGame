using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage4UISetup
{
    private const string CanvasName = "GameplayCanvas";
    private const string UIControllerName = "UIController";
    private const string GameControllerName = "GameController";
    private const string TimeTextName = "TimeText";
    private const string MatchesTextName = "MatchesText";
    private const string MessageTextName = "MessageText";
    private const string RestartButtonName = "RestartButton";
    private const string FontSearchTerm = "Chango-Regular SDF";

    [MenuItem("Tools/Memory Game/Setup Stage 4 UI")]
    public static void SetupStage4UI()
    {
        GameObject canvasGO = FindInActiveScene(CanvasName);
        GameObject uiControllerGO = FindInActiveScene(UIControllerName);
        GameObject gameControllerGO = FindInActiveScene(GameControllerName);

        if (canvasGO == null || uiControllerGO == null || gameControllerGO == null)
        {
            Debug.LogError(
                "[Stage4UISetup] Abortado, no se creó ni modificó nada. " +
                $"GameplayCanvas {(canvasGO != null ? "OK" : "NO ENCONTRADO")}, " +
                $"UIController {(uiControllerGO != null ? "OK" : "NO ENCONTRADO")}, " +
                $"GameController {(gameControllerGO != null ? "OK" : "NO ENCONTRADO")}. " +
                "Verifica que MainScene esté abierta y contenga esos objetos.");
            return;
        }

        var created = new List<string>();
        var existing = new List<string>();
        var wired = new List<string>();

        ConfigureCanvas(canvasGO);

        UIManager uiManager = uiControllerGO.GetComponent<UIManager>();
        if (uiManager == null)
        {
            uiManager = Undo.AddComponent<UIManager>(uiControllerGO);
            created.Add("UIManager (componente en UIController)");
        }
        else
        {
            existing.Add("UIManager (componente en UIController)");
        }

        TMP_FontAsset font = FindProjectFont();

        TextMeshProUGUI timeText = GetOrCreateText(
            canvasGO.transform, TimeTextName, created, existing,
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f), pivot: new Vector2(0f, 1f),
            anchoredPosition: new Vector2(50f, -50f), sizeDelta: new Vector2(420f, 60f),
            initialText: "00:45", fontSize: 42f, alignment: TextAlignmentOptions.TopLeft,
            color: Color.white, font: font, wrap: false);

        TextMeshProUGUI matchesText = GetOrCreateText(
            canvasGO.transform, MatchesTextName, created, existing,
            anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            anchoredPosition: new Vector2(-50f, -50f), sizeDelta: new Vector2(420f, 60f),
            initialText: "0 / 4", fontSize: 42f, alignment: TextAlignmentOptions.TopRight,
            color: Color.white, font: font, wrap: false);

        TextMeshProUGUI messageText = GetOrCreateText(
            canvasGO.transform, MessageTextName, created, existing,
            anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPosition: new Vector2(0f, -150f), sizeDelta: new Vector2(800f, 140f),
            initialText: "Encuentra las parejas", fontSize: 48f, alignment: TextAlignmentOptions.Top,
            color: Color.black, font: font, wrap: true);

        Button restartButton = GetOrCreateButton(
            canvasGO.transform, RestartButtonName, created, existing,
            anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(0.5f, 0f), pivot: new Vector2(0.5f, 0f),
            anchoredPosition: new Vector2(0f, 60f), sizeDelta: new Vector2(260f, 80f),
            label: "Reiniciar", font: font);

        WireReference(uiManager, "timeText", timeText, wired);
        WireReference(uiManager, "matchesText", matchesText, wired);
        WireReference(uiManager, "messageText", messageText, wired);
        WireReference(uiManager, "restartButton", restartButton, wired);

        GameManager gameManager = gameControllerGO.GetComponent<GameManager>();
        GameTimer gameTimer = gameControllerGO.GetComponent<GameTimer>();

        if (gameManager != null)
        {
            WireReference(gameManager, "uiManager", uiManager, wired);
        }
        else
        {
            Debug.LogWarning("[Stage4UISetup] GameController no tiene un componente GameManager; no se conectó uiManager ahí.");
        }

        if (gameTimer != null)
        {
            WireReference(gameTimer, "uiManager", uiManager, wired);
        }
        else
        {
            Debug.LogWarning("[Stage4UISetup] GameController no tiene un componente GameTimer; no se conectó uiManager ahí.");
        }

        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log(
            "[Stage4UISetup] Completado.\n" +
            $"Creados: {(created.Count > 0 ? string.Join(", ", created) : "ninguno")}\n" +
            $"Ya existían: {(existing.Count > 0 ? string.Join(", ", existing) : "ninguno")}\n" +
            $"Referencias conectadas: {(wired.Count > 0 ? string.Join(", ", wired) : "ninguna (ya estaban asignadas o los campos no existen)")}");
    }

    private static void ConfigureCanvas(GameObject canvasGO)
    {
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        if (canvas == null) canvas = Undo.AddComponent<Canvas>(canvasGO);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvasGO);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasGO.GetComponent<GraphicRaycaster>() == null)
        {
            Undo.AddComponent<GraphicRaycaster>(canvasGO);
        }
    }

    private static TextMeshProUGUI GetOrCreateText(
        Transform parent, string objectName, List<string> created, List<string> existing,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta,
        string initialText, float fontSize, TextAlignmentOptions alignment, Color color, TMP_FontAsset font, bool wrap)
    {
        Transform existingChild = parent.Find(objectName);
        if (existingChild != null)
        {
            existing.Add(objectName);
            return existingChild.GetComponent<TextMeshProUGUI>();
        }

        GameObject go = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Stage4UISetup: create " + objectName);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        if (font != null) text.font = font;

        created.Add(objectName);
        return text;
    }

    private static Button GetOrCreateButton(
        Transform parent, string objectName, List<string> created, List<string> existing,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta,
        string label, TMP_FontAsset font)
    {
        Transform existingChild = parent.Find(objectName);
        if (existingChild != null)
        {
            existing.Add(objectName);
            return existingChild.GetComponent<Button>();
        }

        GameObject go = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Stage4UISetup: create " + objectName);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject labelGO = new GameObject("Text", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(labelGO, "Stage4UISetup: create " + objectName + " label");
        labelGO.layer = go.layer;
        labelGO.transform.SetParent(go.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.black;
        labelText.fontSize = 32f;
        if (font != null) labelText.font = font;

        created.Add(objectName);
        return button;
    }

    private static void WireReference(Component target, string fieldName, Object value, List<string> wired)
    {
        if (target == null || value == null) return;

        SerializedObject serializedTarget = new SerializedObject(target);
        SerializedProperty property = serializedTarget.FindProperty(fieldName);
        if (property == null) return;
        if (property.objectReferenceValue != null) return;

        Undo.RecordObject(target, "Stage4UISetup: connect " + fieldName);
        property.objectReferenceValue = value;
        serializedTarget.ApplyModifiedProperties();
        wired.Add($"{target.GetType().Name}.{fieldName} -> {value.name}");
    }

    private static TMP_FontAsset FindProjectFont()
    {
        string[] guids = AssetDatabase.FindAssets($"{FontSearchTerm} t:TMP_FontAsset");
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("Chango t:TMP_FontAsset");
        if (guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
    }

    private static GameObject FindInActiveScene(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            GameObject result = FindChildRecursive(root.transform, objectName);
            if (result != null) return result;
        }

        return null;
    }

    private static GameObject FindChildRecursive(Transform current, string objectName)
    {
        if (current.name == objectName) return current.gameObject;

        for (int i = 0; i < current.childCount; i++)
        {
            GameObject result = FindChildRecursive(current.GetChild(i), objectName);
            if (result != null) return result;
        }

        return null;
    }
}
