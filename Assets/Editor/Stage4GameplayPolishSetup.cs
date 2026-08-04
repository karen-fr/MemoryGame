using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Stage4GameplayPolishSetup
{
    private const string CanvasName = "GameplayCanvas";
    private const string UIControllerName = "UIController";
    private const string GameControllerName = "GameController";
    private const string EventSystemName = "EventSystem";

    private const string TimeTextName = "TimeText";
    private const string MatchesTextName = "MatchesText";
    private const string MessageTextName = "MessageText";
    private const string RestartButtonName = "RestartButton";

    private const string InterfazFolder = "Assets/interfaz";

    private static readonly Vector2 TimeBackgroundSize = new Vector2(280f, 140f);
    private static readonly Vector2 MatchesBackgroundSize = new Vector2(280f, 140f);
    private static readonly Vector2 StartButtonSize = new Vector2(430f, 160f);

    // RestartButton lives as a direct child of GameplayCanvas so its visibility never depends
    // on EndPanel/StartPanel/OptionsPanel being active.
    private static readonly Vector2 RestartButtonAnchorMin = new Vector2(0.5f, 0f);
    private static readonly Vector2 RestartButtonAnchorMax = new Vector2(0.5f, 0f);
    private static readonly Vector2 RestartButtonPivot = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 RestartButtonAnchoredPosition = new Vector2(0f, 70f);
    private static readonly Vector2 RestartButtonSize = new Vector2(220f, 70f);

    private static readonly List<string> Created = new List<string>();
    private static readonly List<string> Wired = new List<string>();
    private static readonly List<string> Warnings = new List<string>();
    private static readonly List<string> RaycastTargetsActive = new List<string>();

    [MenuItem("Tools/Memory Game/Setup Gameplay UI")]
    public static void SetupGameplayUI()
    {
        Created.Clear();
        Wired.Clear();
        Warnings.Clear();
        RaycastTargetsActive.Clear();

        GameObject canvasGO = FindInActiveScene(CanvasName);
        GameObject uiControllerGO = FindInActiveScene(UIControllerName);
        GameObject gameControllerGO = FindInActiveScene(GameControllerName);
        GameObject eventSystemGO = FindInActiveScene(EventSystemName);

        if (canvasGO == null || uiControllerGO == null || gameControllerGO == null || eventSystemGO == null)
        {
            Debug.LogError(
                "[Stage4GameplayPolishSetup] Abortado. No se creó nada. " +
                $"GameplayCanvas={(canvasGO != null ? "OK" : "FALTA")}, UIController={(uiControllerGO != null ? "OK" : "FALTA")}, " +
                $"GameController={(gameControllerGO != null ? "OK" : "FALTA")}, EventSystem={(eventSystemGO != null ? "OK" : "FALTA")}.");
            return;
        }

        EnsureUiInputActions(eventSystemGO);

        Transform canvasTransform = canvasGO.transform;

        Transform timeText = FindChildRecursive(canvasTransform, TimeTextName);
        Transform matchesText = FindChildRecursive(canvasTransform, MatchesTextName);
        Transform messageText = FindChildRecursive(canvasTransform, MessageTextName);
        Transform restartButton = FindChildRecursive(canvasTransform, RestartButtonName);

        if (timeText == null || matchesText == null || messageText == null || restartButton == null)
        {
            Debug.LogError(
                "[Stage4GameplayPolishSetup] Abortado. No se creó nada. Faltan elementos existentes que debían reutilizarse: " +
                $"TimeText={(timeText != null ? "OK" : "FALTA")}, MatchesText={(matchesText != null ? "OK" : "FALTA")}, " +
                $"MessageText={(messageText != null ? "OK" : "FALTA")}, RestartButton={(restartButton != null ? "OK" : "FALTA")}. " +
                "Ejecuta primero Tools > Memory Game > Setup Stage 4 UI.");
            return;
        }

        RectTransform hudPanel = FindOrCreateStretchPanel(canvasTransform, "HUDPanel");

        BuildHud(hudPanel, timeText, matchesText, messageText);

        RectTransform startPanel = FindOrCreateStretchPanel(canvasTransform, "StartPanel");
        GameObject startButtonGO = BuildStartPanel(startPanel);

        RectTransform endPanel = FindOrCreateStretchPanel(canvasTransform, "EndPanel");
        TMP_Text resultText = BuildEndPanel(endPanel);

        RectTransform optionsPanel = FindOrCreateStretchPanel(canvasTransform, "OptionsPanel");
        GameObject optionsOpenButton = FindOrInstantiate(hudPanel, "OptionsButton", "button_configuracion.prefab");
        BuildOptionsPanel(optionsPanel, optionsOpenButton);

        // Wire the panel controllers.
        GameManager gameManager = gameControllerGO.GetComponent<GameManager>();
        UIManager uiManager = uiControllerGO.GetComponent<UIManager>();

        // RestartButton must not depend on any panel's active state to be visible: pull it out
        // of whatever panel it is currently nested in (EndPanel from a previous setup pass, or
        // anywhere else) and make it a direct child of GameplayCanvas.
        ConfigureRestartButton(canvasTransform, restartButton, uiManager);

        WireComponent<StartPanelController>(startPanel.gameObject, controller =>
        {
            var so = new SerializedObject(controller);
            SetRef(so, "panelRoot", startPanel.gameObject);
            SetRef(so, "startButton", startButtonGO != null ? startButtonGO.GetComponent<Button>() : null);
            SetRef(so, "gameManager", gameManager);
            so.ApplyModifiedProperties();
        });

        WireComponent<EndPanelController>(endPanel.gameObject, controller =>
        {
            var so = new SerializedObject(controller);
            SetRef(so, "panelRoot", endPanel.gameObject);
            SetRef(so, "resultText", resultText);
            so.ApplyModifiedProperties();
        });

        CatGridController catController = gameControllerGO.GetComponent<CatGridController>();
        if (catController == null) catController = Object.FindFirstObjectByType<CatGridController>();

        WireComponent<OptionsPanelController>(optionsPanel.gameObject, controller =>
        {
            var so = new SerializedObject(controller);
            SetRef(so, "panelRoot", optionsPanel.gameObject);
            SetRef(so, "openButton", optionsOpenButton != null ? optionsOpenButton.GetComponent<Button>() : null);
            SetRef(so, "closeButton", FindButton(optionsPanel, "CloseButton"));
            SetRef(so, "soundOnButton", FindButton(optionsPanel, "SoundOnButton"));
            SetRef(so, "soundOffButton", FindButton(optionsPanel, "SoundOffButton"));
            SetRef(so, "exitButton", FindButton(optionsPanel, "ExitButton"));
            SetRef(so, "catController", catController);
            so.ApplyModifiedProperties();
        });

        // Connect GameManager.endPanelController if it isn't wired yet.
        if (gameManager != null)
        {
            SerializedObject gmSerialized = new SerializedObject(gameManager);
            SerializedProperty endPanelProp = gmSerialized.FindProperty("endPanelController");
            if (endPanelProp != null && endPanelProp.objectReferenceValue == null)
            {
                Undo.RecordObject(gameManager, "Stage4GameplayPolishSetup: connect endPanelController");
                endPanelProp.objectReferenceValue = endPanel.GetComponent<EndPanelController>();
                gmSerialized.ApplyModifiedProperties();
                Wired.Add("GameManager.endPanelController -> EndPanel");
            }
        }
        else
        {
            Warnings.Add("GameController no tiene un componente GameManager; no se conectó endPanelController.");
        }

        ApplyFont(resultText);
        CollectActiveRaycastTargets(canvasTransform);

        SetActiveWithUndo(startPanel.gameObject, true);
        SetActiveWithUndo(endPanel.gameObject, false);
        SetActiveWithUndo(optionsPanel.gameObject, false);
        SetActiveWithUndo(restartButton.gameObject, false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Button startButtonComponent = startButtonGO != null ? startButtonGO.GetComponent<Button>() : null;
        RectTransform startButtonRect = startButtonGO != null ? startButtonGO.GetComponent<RectTransform>() : null;
        RectTransform timeBgRect = hudPanel.Find("TimeBackground") as RectTransform;
        RectTransform matchesBgRect = hudPanel.Find("MatchesBackground") as RectTransform;

        Debug.Log(
            "[Stage4GameplayPolishSetup] Completado.\n" +
            $"Creados ({Created.Count}): {(Created.Count > 0 ? string.Join(", ", Created) : "ninguno (ya existía todo)")}\n" +
            $"Referencias conectadas ({Wired.Count}): {(Wired.Count > 0 ? string.Join(", ", Wired) : "ninguna nueva")}\n" +
            $"StartButton encontrado: {(startButtonGO != null ? startButtonGO.name : "NINGUNO")}\n" +
            $"GameManager encontrado: {(gameManager != null ? gameManager.name : "NINGUNO")}\n" +
            $"Button.interactable: {(startButtonComponent != null ? startButtonComponent.interactable.ToString() : "N/A")}\n" +
            $"Objetos con Raycast Target activo ({RaycastTargetsActive.Count}): {string.Join(", ", RaycastTargetsActive)}\n" +
            $"TimeBackground: size={(timeBgRect != null ? timeBgRect.sizeDelta.ToString() : "N/A")} anchor=top-left\n" +
            $"MatchesBackground: size={(matchesBgRect != null ? matchesBgRect.sizeDelta.ToString() : "N/A")} anchor=top-right\n" +
            $"StartButton: size={(startButtonRect != null ? startButtonRect.sizeDelta.ToString() : "N/A")} anchor=centro\n" +
            $"RestartButton: parent={restartButton.parent.name} activeSelf={restartButton.gameObject.activeSelf} anchoredPos={(restartButton as RectTransform)?.anchoredPosition}\n" +
            (Warnings.Count > 0 ? $"Advertencias:\n  {string.Join("\n  ", Warnings)}" : "Sin advertencias."));
    }

    // The scene already had an EventSystem + InputSystemUIInputModule, but with no actions
    // asset assigned at all (Point/Click bindings empty) — UI clicks were never reaching any
    // button, with no error, because the module had no way to detect pointer input.
    private static void EnsureUiInputActions(GameObject eventSystemGO)
    {
        if (!eventSystemGO.activeSelf)
        {
            Undo.RecordObject(eventSystemGO, "Stage4GameplayPolishSetup: activate EventSystem");
            eventSystemGO.SetActive(true);
            Wired.Add("EventSystem estaba inactivo -> activado");
        }

        InputSystemUIInputModule inputModule = eventSystemGO.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            Warnings.Add("EventSystem no tiene InputSystemUIInputModule; los clics de UI no funcionarán.");
            return;
        }

        if (!inputModule.enabled)
        {
            Undo.RecordObject(inputModule, "Stage4GameplayPolishSetup: enable InputSystemUIInputModule");
            inputModule.enabled = true;
            Wired.Add("InputSystemUIInputModule estaba deshabilitado -> habilitado");
        }

        if (inputModule.actionsAsset == null)
        {
            Undo.RecordObject(inputModule, "Stage4GameplayPolishSetup: assign default UI input actions");
            inputModule.AssignDefaultActions();
            Wired.Add("EventSystem.InputSystemUIInputModule -> acciones UI por defecto asignadas (estaban vacías: Point/Click sin configurar)");
        }

        EditorUtility.SetDirty(inputModule);
    }

    private static readonly Vector2 HudTextOffset = new Vector2(0f, -15f);
    private static readonly Vector2 HudTextSize = new Vector2(210f, 55f);

    private static void BuildHud(RectTransform hudPanel, Transform timeText, Transform matchesText, Transform messageText)
    {
        GameObject timeBackground = FindOrInstantiate(hudPanel, "TimeBackground", "static_tiempo.prefab");
        if (timeBackground != null)
        {
            RectTransform rect = timeBackground.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = TimeBackgroundSize;
            NoRaycast(timeBackground, true);
            PositionInsideFrame(timeText, timeBackground.transform, HudTextOffset, HudTextSize);
        }

        GameObject matchesBackground = FindOrInstantiate(hudPanel, "MatchesBackground", "static_parejas.prefab");
        if (matchesBackground != null)
        {
            RectTransform rect = matchesBackground.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = MatchesBackgroundSize;
            NoRaycast(matchesBackground, true);
            PositionInsideFrame(matchesText, matchesBackground.transform, HudTextOffset, HudTextSize);
        }

        if (messageText.parent != hudPanel)
        {
            messageText.SetParent(hudPanel, false);
        }
    }

    // Applied every run (not only on first reparent) so a previous bad position/size is
    // always corrected, matching the requested exact anchoring inside its frame.
    private static void PositionInsideFrame(Transform child, Transform newParent, Vector2 anchoredPosition, Vector2 size)
    {
        if (child.parent != newParent) child.SetParent(newParent, false);

        RectTransform rect = child as RectTransform;
        if (rect == null) return;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text != null) text.alignment = TextAlignmentOptions.Center;
    }

    private static GameObject BuildStartPanel(RectTransform startPanel)
    {
        CanvasGroup canvasGroup = startPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = startPanel.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Image dim = startPanel.GetComponent<Image>();
        if (dim == null && startPanel.childCount == 0)
        {
            dim = startPanel.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
        }

        if (dim != null) dim.raycastTarget = false;

        GameObject startButtonGO = FindOrInstantiate(startPanel, "StartButton", "static_inicio.prefab");
        if (startButtonGO != null)
        {
            if (!startButtonGO.activeSelf) startButtonGO.SetActive(true);

            RectTransform rect = startButtonGO.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = StartButtonSize;

            // The Button must live on this exact GameObject — the same one carrying the
            // visible Image — never on a separate transparent object layered over/under it.
            Image image = startButtonGO.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;

            Button button = startButtonGO.GetComponent<Button>();
            if (button == null) button = startButtonGO.AddComponent<Button>();
            button.interactable = true;
            if (image != null) button.targetGraphic = image;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            startButtonGO.transform.SetAsLastSibling();

            int strayVisuals = 0;
            foreach (Transform sibling in startPanel)
            {
                if (sibling == startButtonGO.transform) continue;
                if (sibling.GetComponent<Graphic>() != null) strayVisuals++;
            }

            if (strayVisuals > 0)
            {
                Warnings.Add($"StartPanel tiene {strayVisuals} elemento(s) visual(es) adicional(es) además de StartButton; revisar que ninguno tape el botón.");
            }
        }
        else
        {
            Warnings.Add("No se pudo crear/encontrar StartButton (static_inicio.prefab).");
        }

        return startButtonGO;
    }

    private static void ConfigureRestartButton(Transform canvasTransform, Transform restartButton, UIManager uiManager)
    {
        if (restartButton.parent != canvasTransform)
        {
            restartButton.SetParent(canvasTransform, true);
            Wired.Add("RestartButton reparentado: hijo directo de GameplayCanvas (ya no depende de EndPanel/StartPanel/OptionsPanel)");
        }

        RectTransform rect = restartButton as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = RestartButtonAnchorMin;
            rect.anchorMax = RestartButtonAnchorMax;
            rect.pivot = RestartButtonPivot;
            rect.anchoredPosition = RestartButtonAnchoredPosition;
            rect.sizeDelta = RestartButtonSize;
            rect.localScale = Vector3.one;
        }

        restartButton.SetAsLastSibling();

        Button button = restartButton.GetComponent<Button>();
        if (button != null) button.interactable = true;

        Image image = restartButton.GetComponent<Image>();
        if (image != null) image.raycastTarget = true;

        CanvasGroup canvasGroup = restartButton.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (uiManager != null)
        {
            var so = new SerializedObject(uiManager);
            SetRef(so, "restartButton", button);
            so.ApplyModifiedProperties();
        }
        else
        {
            Warnings.Add("UIManager no encontrado; no se pudo confirmar la referencia restartButton.");
        }
    }

    private static TMP_Text BuildEndPanel(RectTransform endPanel)
    {
        Image dim = endPanel.GetComponent<Image>();
        if (dim == null && endPanel.childCount == 0)
        {
            dim = endPanel.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
        }

        if (dim != null) dim.raycastTarget = false;

        GameObject background = FindOrInstantiate(endPanel, "EndBackground", "static_final.prefab");
        if (background != null)
        {
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            background.transform.SetAsFirstSibling();
            NoRaycast(background, true);
        }

        Transform resultTextTransform = endPanel.Find("ResultText");
        TMP_Text resultText;
        if (resultTextTransform == null)
        {
            GameObject go = new GameObject("ResultText", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Stage4GameplayPolishSetup: create ResultText");
            go.layer = endPanel.gameObject.layer;
            go.transform.SetParent(endPanel, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 80f);
            rect.sizeDelta = new Vector2(600f, 100f);

            resultText = go.AddComponent<TextMeshProUGUI>();
            resultText.text = "";
            resultText.fontSize = 54f;
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.color = Color.white;

            Created.Add("ResultText");
        }
        else
        {
            resultText = resultTextTransform.GetComponent<TMP_Text>();
        }

        return resultText;
    }

    private static void BuildOptionsPanel(RectTransform optionsPanel, GameObject openButton)
    {
        Image dim = optionsPanel.GetComponent<Image>();
        if (dim == null && optionsPanel.childCount == 0)
        {
            dim = optionsPanel.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
        }

        if (dim != null) dim.raycastTarget = false;

        GameObject background = FindOrInstantiate(optionsPanel, "OptionsBackground", "static_opciones.prefab");
        if (background != null)
        {
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            background.transform.SetAsFirstSibling();
            NoRaycast(background, true);
        }

        GameObject soundOn = FindOrInstantiate(optionsPanel, "SoundOnButton", "button_sonido on.prefab");
        PositionRelative(soundOn, new Vector2(-80f, -60f));

        GameObject soundOff = FindOrInstantiate(optionsPanel, "SoundOffButton", "button_sonido off.prefab");
        PositionRelative(soundOff, new Vector2(-80f, -60f));

        GameObject exit = FindOrInstantiate(optionsPanel, "ExitButton", "button_salida.prefab");
        PositionRelative(exit, new Vector2(80f, -60f));

        GameObject close = FindOrInstantiate(optionsPanel, "CloseButton", "button_cerrar.prefab");
        PositionRelative(close, new Vector2(0f, 120f));

        if (openButton != null)
        {
            RectTransform rect = openButton.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -160f);
        }
    }

    // Decorative-only frames should never intercept clicks meant for whatever sits on top of
    // or behind them. Real buttons (button_*.prefab instances) keep their default raycastTarget.
    private static void NoRaycast(GameObject go, bool disableRaycast)
    {
        if (go == null) return;
        Image image = go.GetComponent<Image>();
        if (image != null) image.raycastTarget = !disableRaycast;
    }

    private static void CollectActiveRaycastTargets(Transform canvasRoot)
    {
        foreach (Graphic graphic in canvasRoot.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.raycastTarget)
            {
                RaycastTargetsActive.Add(graphic.gameObject.name);
            }
        }
    }

    private static void PositionRelative(GameObject go, Vector2 anchoredPosition)
    {
        if (go == null) return;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
    }

    private static void ApplyFont(TMP_Text text)
    {
        if (text == null) return;

        string[] guids = AssetDatabase.FindAssets("Chango-Regular SDF t:TMP_FontAsset");
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("Chango t:TMP_FontAsset");
        if (guids.Length == 0) return;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (font != null) text.font = font;
    }

    private static void SetActiveWithUndo(GameObject go, bool active)
    {
        if (go == null || go.activeSelf == active) return;

        Undo.RecordObject(go, "Stage4GameplayPolishSetup: set active state");
        go.SetActive(active);
    }

    private static Button FindButton(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static void WireComponent<T>(GameObject target, System.Action<T> configure) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = Undo.AddComponent<T>(target);
            Created.Add(typeof(T).Name + " on " + target.name);
        }

        configure(component);
    }

    private static void SetRef(SerializedObject so, string fieldName, Object value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Warnings.Add($"No existe el campo '{fieldName}' en {so.targetObject.GetType().Name}.");
            return;
        }

        if (prop.objectReferenceValue == value) return;

        prop.objectReferenceValue = value;
        Wired.Add($"{so.targetObject.GetType().Name}.{fieldName} -> {(value != null ? value.name : "null")}");
    }

    private static GameObject FindOrInstantiate(Transform parent, string childName, string prefabFileName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null) return existing.gameObject;

        string path = $"{InterfazFolder}/{prefabFileName}";
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefabAsset == null)
        {
            Warnings.Add($"No se encontró el prefab '{path}' para crear '{childName}'.");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
        Undo.RegisterCreatedObjectUndo(instance, "Stage4GameplayPolishSetup: create " + childName);
        instance.name = childName;
        instance.transform.SetParent(parent, false);

        Created.Add(childName);
        return instance;
    }

    private static RectTransform FindOrCreateStretchPanel(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing as RectTransform;

        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Stage4GameplayPolishSetup: create " + name);
        go.layer = parent.gameObject.layer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Created.Add(name);
        return rect;
    }

    private static GameObject FindInActiveScene(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            GameObject result = FindChildRecursiveGO(root.transform, objectName);
            if (result != null) return result;
        }

        return null;
    }

    private static GameObject FindChildRecursiveGO(Transform current, string objectName)
    {
        if (current.name == objectName) return current.gameObject;

        for (int i = 0; i < current.childCount; i++)
        {
            GameObject result = FindChildRecursiveGO(current.GetChild(i), objectName);
            if (result != null) return result;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }

        return null;
    }
}
