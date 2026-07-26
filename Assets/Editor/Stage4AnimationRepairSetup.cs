using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage4AnimationRepairSetup
{
    private const string ControllerPath = "Assets/personaje/personaje.controller";
    private const string BaseModelPath = "Assets/personaje/base/personaje1.fbx";

    private static readonly string[] AnimationClipPaths =
    {
        "Assets/personaje/animaciones/idle.fbx",
        "Assets/personaje/animaciones/inicio-caminata.fbx",
        "Assets/personaje/animaciones/caminado.fbx",
        "Assets/personaje/animaciones/stop.fbx",
        "Assets/personaje/animaciones/salto.fbx",
        "Assets/personaje/animaciones/mareo.fbx",
        "Assets/personaje/animaciones/celebracion.fbx",
        "Assets/personaje/animaciones/perder.fbx",
    };

    [MenuItem("Tools/Memory Game/Repair Character Animations")]
    public static void RepairCharacterAnimations()
    {
        Debug.Log("[Stage4AnimationRepairSetup] Iniciando reparación de animaciones...");

        // 1. Re-affirm the baseline parameters/transitions (idempotent, safe to repeat).
        Stage4AnimatorSetup.SetupCharacterAnimations();

        // 2. Remove any transition still gated by "pass", wherever it currently points —
        // catches drift/leftovers from manual edits in the Animator window, not just the
        // exact transitions Stage4AnimatorSetup already knows about by name.
        int removedPassTransitions = CleanupPassTransitions();

        // 2b. Short pair-found / pair-mismatch reactions, as separate states so the existing
        // terminal "celebracion" (final win) and comodín-driven "mareo" keep working unchanged.
        string pairReactionReport = EnsurePairReactionStates();

        // 3. Each animation clip keeps its OWN Humanoid avatar (Create From This Model).
        // "Copy From Other Avatar" was tried and rejected: these FBX files don't share the
        // exact transform hierarchy as personaje1.fbx ("Group" not found), so copying the
        // rig configuration breaks import. Humanoid retargeting works across independent
        // avatars by design — the clips don't need to share the literal same Avatar asset
        // to animate personaje1's rig correctly.
        var reimported = new List<string>();
        var invalidAvatars = new List<string>();
        foreach (string path in AnimationClipPaths)
        {
            ClipFixResult result = EnsureCreateFromThisModelHumanoid(path);
            if (result == ClipFixResult.Reimported) reimported.Add(path);
            if (result == ClipFixResult.InvalidAvatar) invalidAvatars.Add(path);
        }

        // 4. Find the real Animator under PlayerRoot in the open scene and fix its settings.
        // This Animator keeps using personaje1Avatar — only the standalone clip FBX imports
        // were reverted, not the visible character's own Animator component.
        Avatar characterAvatar = FindSharedAvatar();
        GameObject playerRoot = FindInActiveScene("PlayerRoot");
        Animator[] animatorsUnderPlayer = playerRoot != null
            ? playerRoot.GetComponentsInChildren<Animator>(true)
            : System.Array.Empty<Animator>();

        Animator fixedAnimator = null;
        bool sceneChanged = false;

        if (animatorsUnderPlayer.Length == 0)
        {
            Debug.LogError("[Stage4AnimationRepairSetup] No se encontró ningún Animator bajo PlayerRoot. No se pudo conectar nada en la escena.");
        }
        else
        {
            if (animatorsUnderPlayer.Length > 1)
            {
                Debug.LogWarning(
                    $"[Stage4AnimationRepairSetup] Se encontraron {animatorsUnderPlayer.Length} Animator bajo PlayerRoot. " +
                    "Se usará el que controla un SkinnedMeshRenderer visible; los demás no se eliminan ni se tocan.");
            }

            fixedAnimator = animatorsUnderPlayer.FirstOrDefault(a => a.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                ?? animatorsUnderPlayer[0];

            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            Undo.RecordObject(fixedAnimator, "Stage4AnimationRepairSetup: fix Animator");
            bool animatorChanged = false;

            if (controller != null && fixedAnimator.runtimeAnimatorController != controller)
            {
                fixedAnimator.runtimeAnimatorController = controller;
                animatorChanged = true;
            }

            if (characterAvatar != null && fixedAnimator.avatar != characterAvatar)
            {
                fixedAnimator.avatar = characterAvatar;
                animatorChanged = true;
            }

            if (fixedAnimator.applyRootMotion)
            {
                fixedAnimator.applyRootMotion = false;
                animatorChanged = true;
            }

            if (fixedAnimator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                fixedAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animatorChanged = true;
            }

            if (!fixedAnimator.enabled)
            {
                fixedAnimator.enabled = true;
                animatorChanged = true;
            }

            if (fixedAnimator.speed <= 0f)
            {
                fixedAnimator.speed = 1f;
                animatorChanged = true;
            }

            if (animatorChanged)
            {
                EditorUtility.SetDirty(fixedAnimator);
                sceneChanged = true;
            }

            // 5. Explicitly wire CatGridController.animator to this exact instance, instead of
            // relying only on the runtime GetComponentInChildren fallback.
            CatGridController catController = Object.FindFirstObjectByType<CatGridController>();
            if (catController != null)
            {
                SerializedObject so = new SerializedObject(catController);
                SerializedProperty animatorProp = so.FindProperty("animator");
                if (animatorProp != null && animatorProp.objectReferenceValue != fixedAnimator)
                {
                    Undo.RecordObject(catController, "Stage4AnimationRepairSetup: connect Animator reference");
                    animatorProp.objectReferenceValue = fixedAnimator;
                    so.ApplyModifiedProperties();
                    sceneChanged = true;
                }
            }
            else
            {
                Debug.LogWarning("[Stage4AnimationRepairSetup] No se encontró CatGridController en la escena.");
            }
        }

        if (sceneChanged)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Stage4AnimationRepairSetup] Completado.\n" +
            $"Transiciones con condición 'pass' eliminadas: {removedPassTransitions}\n" +
            $"{pairReactionReport}\n" +
            $"Clips reimportados (Humanoid + Create From This Model): " +
            $"{(reimported.Count > 0 ? string.Join(", ", reimported) : "ninguno (ya estaban correctos)")}\n" +
            (invalidAvatars.Count > 0 ? $"Clips con Avatar inválido tras importar (revisar manualmente): {string.Join(", ", invalidAvatars)}\n" : "") +
            $"Animator conectado: {(fixedAnimator != null ? fixedAnimator.gameObject.name : "NINGUNO")}\n" +
            $"Avatar del personaje visible: {(characterAvatar != null ? characterAvatar.name : "NINGUNO")}\n" +
            $"Controller asignado: {(fixedAnimator != null && fixedAnimator.runtimeAnimatorController != null ? fixedAnimator.runtimeAnimatorController.name : "N/A")}\n" +
            $"Apply Root Motion: {(fixedAnimator != null ? fixedAnimator.applyRootMotion.ToString() : "N/A")}\n" +
            $"Culling Mode: {(fixedAnimator != null ? fixedAnimator.cullingMode.ToString() : "N/A")}");
    }

    private enum ClipFixResult
    {
        AlreadyCorrect,
        Reimported,
        InvalidAvatar,
        MissingImporter
    }

    private static ClipFixResult EnsureCreateFromThisModelHumanoid(string clipPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(clipPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[Stage4AnimationRepairSetup] No se encontró el ModelImporter de '{clipPath}'.");
            return ClipFixResult.MissingImporter;
        }

        bool alreadyCorrect = importer.animationType == ModelImporterAnimationType.Human
            && importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel
            && importer.sourceAvatar == null;

        if (alreadyCorrect) return ClipFixResult.AlreadyCorrect;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.sourceAvatar = null;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Avatar ownAvatar = AssetDatabase.LoadAllAssetsAtPath(clipPath).OfType<Avatar>().FirstOrDefault();
        if (ownAvatar == null || !ownAvatar.isValid || !ownAvatar.isHuman)
        {
            Debug.LogError($"[Stage4AnimationRepairSetup] '{clipPath}' quedó con un Avatar inválido tras reimportar; revisar el mapeo de huesos manualmente.");
            return ClipFixResult.InvalidAvatar;
        }

        return ClipFixResult.Reimported;
    }

    private static int CleanupPassTransitions()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) return 0;

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        int removed = 0;

        Undo.RegisterCompleteObjectUndo(controller, "Stage4AnimationRepairSetup: cleanup pass transitions");

        foreach (ChildAnimatorState child in stateMachine.states)
        {
            foreach (AnimatorStateTransition t in child.state.transitions.ToArray())
            {
                if (HasPassCondition(t))
                {
                    child.state.RemoveTransition(t);
                    removed++;
                }
            }
        }

        foreach (AnimatorStateTransition t in stateMachine.anyStateTransitions.ToArray())
        {
            if (HasPassCondition(t))
            {
                stateMachine.RemoveAnyStateTransition(t);
                removed++;
            }
        }

        if (removed > 0) EditorUtility.SetDirty(controller);

        return removed;
    }

    private static bool HasPassCondition(AnimatorStateTransition t)
    {
        foreach (AnimatorCondition c in t.conditions)
        {
            if (c.parameter == "pass") return true;
        }

        return false;
    }

    private const string PairMatchReactionStateName = "pair-match-reaction";
    private const string PairMismatchReactionStateName = "pair-mismatch-reaction";

    private static string EnsurePairReactionStates()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) return "Reacciones de pareja: no se pudo abrir el controller.";

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState celebracion = FindState(stateMachine, "celebracion");
        AnimatorState mareo = FindState(stateMachine, "mareo");
        AnimatorState idle = FindState(stateMachine, "idle");

        if (celebracion == null || mareo == null || idle == null)
        {
            return "Reacciones de pareja: no se encontraron los estados base 'celebracion'/'mareo'/'idle'; no se creó nada.";
        }

        bool changed = false;

        if (!controller.parameters.Any(p => p.name == "PairMatch"))
        {
            controller.AddParameter("PairMatch", AnimatorControllerParameterType.Trigger);
            changed = true;
        }

        if (!controller.parameters.Any(p => p.name == "PairMismatch"))
        {
            controller.AddParameter("PairMismatch", AnimatorControllerParameterType.Trigger);
            changed = true;
        }

        bool matchCreated = EnsureReactionState(
            stateMachine, PairMatchReactionStateName, celebracion.motion, idle, "PairMatch",
            new Vector3(650f, 480f, 0f), ref changed);

        bool mismatchCreated = EnsureReactionState(
            stateMachine, PairMismatchReactionStateName, mareo.motion, idle, "PairMismatch",
            new Vector3(650f, 600f, 0f), ref changed);

        if (changed) EditorUtility.SetDirty(controller);

        return "Reacciones de pareja: " +
            $"pair-match-reaction {(matchCreated ? "creado" : "ya existía")}, " +
            $"pair-mismatch-reaction {(mismatchCreated ? "creado" : "ya existía")}.";
    }

    // Creates (only if missing) a short reaction state that reuses an existing Motion, wires
    // an Any State -> state transition on the given trigger, and a state -> idle exit-time
    // transition back out. Safe to call repeatedly: every lookup is by name/destination first.
    private static bool EnsureReactionState(
        AnimatorStateMachine stateMachine, string stateName, Motion motion, AnimatorState idle,
        string triggerName, Vector3 position, ref bool changed)
    {
        AnimatorState reactionState = FindState(stateMachine, stateName);
        bool created = false;

        if (reactionState == null)
        {
            reactionState = stateMachine.AddState(stateName, position);
            reactionState.motion = motion;
            reactionState.writeDefaultValues = true;
            created = true;
            changed = true;
        }
        else if (reactionState.motion != motion)
        {
            reactionState.motion = motion;
            changed = true;
        }

        bool hasAnyStateEntry = stateMachine.anyStateTransitions.Any(t => t.destinationState == reactionState);
        if (!hasAnyStateEntry)
        {
            AnimatorStateTransition entry = stateMachine.AddAnyStateTransition(reactionState);
            entry.hasExitTime = false;
            entry.canTransitionToSelf = false;
            entry.duration = 0.15f;
            entry.AddCondition(AnimatorConditionMode.If, 0, triggerName);
            changed = true;
        }

        bool hasExitToIdle = reactionState.transitions.Any(t => t.destinationState == idle);
        if (!hasExitToIdle)
        {
            AnimatorStateTransition exit = reactionState.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 0.9f;
            exit.hasFixedDuration = true;
            exit.duration = 0.25f;
            changed = true;
        }

        return created;
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state.name == name) return child.state;
        }

        return null;
    }

    private static Avatar FindSharedAvatar()
    {
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(BaseModelPath))
        {
            if (obj is Avatar avatar && avatar.isValid && avatar.isHuman) return avatar;
        }

        return null;
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
