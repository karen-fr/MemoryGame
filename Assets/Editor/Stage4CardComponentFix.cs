using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage4CardComponentFix
{
    private const string ParejasName = "Parejas";
    private const string TargetCardName = "pareja4 (1)";
    private const string ReferenceCardName = "pareja4";
    private const int ExpectedPairId = 4;
    private const int ExpectedCardCount = 8;

    [MenuItem("Tools/Memory Game/Fix Missing Card Component")]
    public static void FixMissingCardComponent()
    {
        GameObject parejasGO = FindInActiveScene(ParejasName);
        if (parejasGO == null)
        {
            Debug.LogError($"[Stage4CardComponentFix] Abortado. No se encontró el GameObject '{ParejasName}' en la escena activa.");
            return;
        }

        Transform targetTransform = parejasGO.transform.Find(TargetCardName);
        if (targetTransform == null)
        {
            Debug.LogError($"[Stage4CardComponentFix] Abortado. No se encontró el hijo '{TargetCardName}' dentro de '{ParejasName}'.");
            return;
        }

        Transform referenceTransform = parejasGO.transform.Find(ReferenceCardName);
        MemoryCard referenceCard = referenceTransform != null ? referenceTransform.GetComponent<MemoryCard>() : null;

        if (referenceCard == null)
        {
            Debug.LogError(
                $"[Stage4CardComponentFix] Abortado. No se encontró '{ReferenceCardName}' con un componente MemoryCard válido " +
                "para confirmar que el pairId 4 existe antes de tocar su pareja. No se modificó nada.");
            return;
        }

        int referencePairId = GetPairId(referenceCard);
        if (referencePairId != ExpectedPairId)
        {
            Debug.LogError(
                $"[Stage4CardComponentFix] Abortado. '{ReferenceCardName}' tiene pairId={referencePairId}, se esperaba {ExpectedPairId}. " +
                "No se modificó nada.");
            return;
        }

        MemoryCard targetCard = targetTransform.GetComponent<MemoryCard>();

        if (targetCard == null)
        {
            targetCard = Undo.AddComponent<MemoryCard>(targetTransform.gameObject);

            Renderer renderer = targetTransform.GetComponent<Renderer>();
            if (renderer == null) renderer = targetTransform.GetComponentInChildren<Renderer>();

            SerializedObject serializedCard = new SerializedObject(targetCard);
            SerializedProperty pairIdProp = serializedCard.FindProperty("pairId");
            SerializedProperty rendererProp = serializedCard.FindProperty("cardRenderer");

            if (pairIdProp != null) pairIdProp.intValue = ExpectedPairId;
            if (rendererProp != null && renderer != null) rendererProp.objectReferenceValue = renderer;

            serializedCard.ApplyModifiedProperties();

            if (renderer == null)
            {
                Debug.LogWarning($"[Stage4CardComponentFix] '{TargetCardName}' no tiene ningún Renderer en su jerarquía; Card Renderer quedó sin asignar.");
            }

            Debug.Log($"[Stage4CardComponentFix] Se agregó MemoryCard a '{TargetCardName}' con pairId={ExpectedPairId}.");
        }
        else
        {
            Debug.Log($"[Stage4CardComponentFix] '{TargetCardName}' ya tenía MemoryCard; no se duplicó nada (idempotente).");
        }

        MemoryCard[] allCards = parejasGO.GetComponentsInChildren<MemoryCard>(true);

        if (allCards.Length != ExpectedCardCount)
        {
            Debug.LogWarning(
                $"[Stage4CardComponentFix] Se esperaban {ExpectedCardCount} componentes MemoryCard bajo '{ParejasName}' " +
                $"pero se encontraron {allCards.Length}. Revisa manualmente la jerarquía.");
        }

        var summary = new List<string>();
        foreach (MemoryCard card in allCards)
        {
            summary.Add($"{card.gameObject.name} (pairId={GetPairId(card)})");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            $"[Stage4CardComponentFix] Estado final: {allCards.Length} MemoryCard bajo '{ParejasName}'.\n  " +
            string.Join("\n  ", summary));
    }

    private static int GetPairId(MemoryCard card)
    {
        SerializedObject serializedCard = new SerializedObject(card);
        SerializedProperty prop = serializedCard.FindProperty("pairId");
        return prop != null ? prop.intValue : -1;
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
