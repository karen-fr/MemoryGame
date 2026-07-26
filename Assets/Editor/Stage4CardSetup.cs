using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage4CardSetup
{
    private const string ParejasName = "Parejas";
    private const string PivotPrefix = "FlipPivot_";
    private const string OrientationPivotPrefix = "FaceOrientationPivot_";
    private const string StalePivotName = "FlipPivot";
    private static readonly Vector3 DesiredFlipAxis = Vector3.right;

    [MenuItem("Tools/Memory Game/Setup Card Materials")]
    public static void SetupCardMaterials()
    {
        GameObject parejasGO = FindInActiveScene(ParejasName);
        if (parejasGO == null)
        {
            Debug.LogError($"[Stage4CardSetup] Abortado. No se encontró el GameObject '{ParejasName}' en la escena activa.");
            return;
        }

        int removedStale = CleanupStaleEmptyPivots(parejasGO.transform);

        MemoryCard[] cards = parejasGO.GetComponentsInChildren<MemoryCard>(true);

        if (cards.Length != 8)
        {
            Debug.LogWarning(
                $"[Stage4CardSetup] Se esperaban 8 cartas con MemoryCard dentro de '{ParejasName}' pero se encontraron {cards.Length}. " +
                "Se continúa configurando solo las encontradas.");
        }

        var report = new List<string>();
        var skippedNoRenderer = new List<string>();
        var skippedNoFields = new List<string>();
        var warnings = new List<string>();

        foreach (MemoryCard card in cards)
        {
            Transform cardRoot = card.transform;
            string cardName = cardRoot.name;
            string pivotName = PivotPrefix + cardName;
            string orientationPivotName = OrientationPivotPrefix + cardName;

            SerializedObject serializedCard = new SerializedObject(card);
            SerializedProperty rendererProp = serializedCard.FindProperty("cardRenderer");
            SerializedProperty pivotProp = serializedCard.FindProperty("flipPivot");
            SerializedProperty orientationProp = serializedCard.FindProperty("faceOrientationPivot");
            SerializedProperty pairIdProp = serializedCard.FindProperty("pairId");
            SerializedProperty axisProp = serializedCard.FindProperty("flipRotationAxis");

            if (rendererProp == null || pivotProp == null || orientationProp == null)
            {
                skippedNoFields.Add(cardName);
                Debug.LogWarning($"[Stage4CardSetup] '{cardName}' no expone los campos esperados (cardRenderer/flipPivot/faceOrientationPivot) en MemoryCard; se omite.");
                continue;
            }

            int pairId = pairIdProp != null ? pairIdProp.intValue : -1;

            Renderer renderer = rendererProp.objectReferenceValue as Renderer;
            if (renderer == null) renderer = card.GetComponentInChildren<Renderer>();

            if (renderer == null)
            {
                skippedNoRenderer.Add(cardName);
                Debug.LogWarning($"[Stage4CardSetup] '{cardName}' no tiene ningún Renderer en su jerarquía.");
                continue;
            }

            // Reuse the FlipPivot that already works — never recreated, repositioned or moved.
            Transform flipPivot = pivotProp.objectReferenceValue as Transform;
            if (flipPivot == null) flipPivot = parejasGO.transform.Find(pivotName);

            if (flipPivot == null)
            {
                warnings.Add($"{cardName}: no se encontró su FlipPivot ('{pivotName}'). No se creó nada; revisa manualmente antes de repetir.");
                continue;
            }

            // Find (or create, only if missing) FaceOrientationPivot as a direct child of the
            // existing FlipPivot — same center, identity local transform, so it starts exactly
            // where FlipPivot already is.
            Transform orientationPivot = flipPivot.Find(orientationPivotName);
            bool orientationPivotCreated = false;

            if (orientationPivot == null)
            {
                GameObject pivotObject = new GameObject(orientationPivotName);
                Undo.RegisterCreatedObjectUndo(pivotObject, "Stage4CardSetup: create " + orientationPivotName);
                orientationPivot = pivotObject.transform;
                orientationPivot.SetParent(flipPivot, false);
                orientationPivotCreated = true;
            }

            bool reparented = false;
            if (cardRoot.parent != orientationPivot)
            {
                Vector3 worldPos = cardRoot.position;
                Quaternion worldRot = cardRoot.rotation;

                // cardRoot is the ROOT of its own nested Prefab Instance — reparenting the
                // instance root as a whole is always allowed (unlike reparenting pieza3, which
                // is internal to that instance and is never touched here).
                Undo.SetTransformParent(cardRoot, orientationPivot, "Stage4CardSetup: parent " + cardName + " under " + orientationPivotName);
                cardRoot.position = worldPos;
                cardRoot.rotation = worldRot;
                reparented = true;
            }

            bool changed = orientationPivotCreated;

            if (rendererProp.objectReferenceValue != renderer)
            {
                rendererProp.objectReferenceValue = renderer;
                changed = true;
            }

            if (pivotProp.objectReferenceValue != flipPivot)
            {
                pivotProp.objectReferenceValue = flipPivot;
                changed = true;
            }

            if (orientationProp.objectReferenceValue != orientationPivot)
            {
                orientationProp.objectReferenceValue = orientationPivot;
                changed = true;
            }

            if (axisProp != null && axisProp.vector3Value != DesiredFlipAxis)
            {
                axisProp.vector3Value = DesiredFlipAxis;
                changed = true;
            }

            if (changed) serializedCard.ApplyModifiedProperties();

            // Verification: pieza3 still internal to the card, renderer still alive.
            bool visualStillInternal = renderer.transform.IsChildOf(cardRoot);
            bool rendererAlive = renderer != null && renderer.gameObject.activeInHierarchy && renderer.enabled;

            if (!visualStillInternal)
            {
                warnings.Add($"{cardName}: el Renderer ya no es descendiente de la raíz de la carta tras el reparentado (revisar manualmente).");
            }

            if (!rendererAlive)
            {
                warnings.Add($"{cardName}: el Renderer quedó inactivo o deshabilitado tras el reparentado.");
            }

            report.Add(
                $"{cardName} (pairId={pairId}, flipPivot={flipPivot.name}, faceOrientationPivot={orientationPivotName}, renderer={renderer.name}, " +
                $"flipRotationAxis={(axisProp != null ? axisProp.vector3Value.ToString() : "n/a")}, " +
                $"{(orientationPivotCreated ? "orientationPivot creado" : "orientationPivot existente reutilizado")}, " +
                $"{(reparented ? "raíz reparentada bajo faceOrientationPivot" : "ya estaba bajo su pivote de orientación")}, " +
                $"visualInterno={visualStillInternal}, rendererActivo={rendererAlive})");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            "[Stage4CardSetup] Completado. Jerarquía forzada a: Parejas -> FlipPivot_<carta> (sin tocar) -> FaceOrientationPivot_<carta> -> <carta> (instancia de prefab completa, pieza3 intacto adentro).\n" +
            (removedStale > 0 ? $"Pivotes '{StalePivotName}' vacíos de una versión anterior eliminados: {removedStale}\n" : "") +
            $"Cartas configuradas ({report.Count}):\n  " + (report.Count > 0 ? string.Join("\n  ", report) : "ninguna") + "\n" +
            (warnings.Count > 0 ? $"Advertencias:\n  {string.Join("\n  ", warnings)}\n" : "") +
            (skippedNoRenderer.Count > 0 ? $"Sin Renderer válido: {string.Join(", ", skippedNoRenderer)}\n" : "") +
            (skippedNoFields.Count > 0 ? $"Sin campos esperados en MemoryCard: {string.Join(", ", skippedNoFields)}\n" : ""));
    }

    // Removes leftover objects literally named "FlipPivot" (the old, broken internal-pivot
    // attempt) anywhere under Parejas, but only when they are empty (no children, no
    // components besides Transform) — never touches anything with actual content.
    private static int CleanupStaleEmptyPivots(Transform parejas)
    {
        Transform[] all = parejas.GetComponentsInChildren<Transform>(true);
        int removed = 0;

        foreach (Transform t in all)
        {
            if (t == parejas) continue;
            if (t.name != StalePivotName) continue;
            if (t.childCount > 0) continue;
            if (t.GetComponents<Component>().Length > 1) continue; // has more than just Transform

            Undo.DestroyObjectImmediate(t.gameObject);
            removed++;
        }

        return removed;
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
