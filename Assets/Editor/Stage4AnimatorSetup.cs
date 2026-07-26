using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class Stage4AnimatorSetup
{
    private const string ControllerPath = "Assets/personaje/personaje.controller";

    [MenuItem("Tools/Memory Game/Setup Character Animations")]
    public static void SetupCharacterAnimations()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[Stage4AnimatorSetup] Abortado. No se encontró el AnimatorController en '{ControllerPath}'.");
            return;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState idle = FindState(stateMachine, "idle");
        AnimatorState inicioCaminata = FindState(stateMachine, "inicio-caminata");
        AnimatorState caminado = FindState(stateMachine, "caminado");
        AnimatorState stop = FindState(stateMachine, "stop");
        AnimatorState salto = FindState(stateMachine, "salto");
        AnimatorState mareo = FindState(stateMachine, "mareo");
        AnimatorState celebracion = FindState(stateMachine, "celebracion");
        AnimatorState perder = FindState(stateMachine, "perder");

        var missing = new List<string>();
        if (idle == null) missing.Add("idle");
        if (inicioCaminata == null) missing.Add("inicio-caminata");
        if (caminado == null) missing.Add("caminado");
        if (stop == null) missing.Add("stop");
        if (salto == null) missing.Add("salto");
        if (mareo == null) missing.Add("mareo");
        if (celebracion == null) missing.Add("celebracion");
        if (perder == null) missing.Add("perder");

        if (missing.Count > 0)
        {
            Debug.LogError(
                $"[Stage4AnimatorSetup] Abortado. Faltan estados esperados en '{ControllerPath}': {string.Join(", ", missing)}. " +
                "No se modificó nada (los estados de las compañeras no se tocan ni se recrean).");
            return;
        }

        Undo.RegisterCompleteObjectUndo(controller, "Stage4AnimatorSetup: reconfigure controller");

        var addedParams = new List<string>();
        EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool, addedParams);
        EnsureParameter(controller, "Jump", AnimatorControllerParameterType.Trigger, addedParams);
        EnsureParameter(controller, "Trap", AnimatorControllerParameterType.Trigger, addedParams);
        EnsureParameter(controller, "Win", AnimatorControllerParameterType.Trigger, addedParams);
        EnsureParameter(controller, "Lose", AnimatorControllerParameterType.Trigger, addedParams);

        // idle -> inicio-caminata: driven by IsMoving instead of the old blanket "pass" bool,
        // fires immediately (no exit time needed to start walking).
        Retarget(idle, "inicio-caminata", t =>
        {
            ClearConditions(t);
            t.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            t.hasExitTime = false;
        });

        // caminado -> stop: leave the walk loop once the player actually stops. Keep the
        // original exit-time value (a clean loop-point) that the clip's author already tuned.
        Retarget(caminado, "stop", t =>
        {
            ClearConditions(t);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        });

        // stop -> idle: unconditional once the stop clip finishes. Replaces the old
        // stop -> inicio-caminata loop and the extra pass-gated branches removed below.
        Retarget(stop, "idle", t => ClearConditions(t));

        // salto / mareo return to idle unconditionally once their clip nearly finishes; idle's
        // own IsMoving check immediately re-enters caminado if the player kept moving meanwhile.
        Retarget(salto, "idle", t => ClearConditions(t));
        Retarget(mareo, "idle", t => ClearConditions(t));

        // Remove the old pass-gated branches now superseded by the Any State transitions below
        // (Jump/Trap/Win/Lose should be able to interrupt from any state, not just from idle/stop/salto).
        RemoveTransitionTo(idle, "salto");
        RemoveTransitionTo(idle, "mareo");
        RemoveTransitionTo(idle, "celebracion");
        RemoveTransitionTo(idle, "perder");
        RemoveTransitionTo(stop, "salto");
        RemoveTransitionTo(stop, "inicio-caminata");
        RemoveTransitionTo(stop, "celebracion");
        RemoveTransitionTo(stop, "mareo");
        RemoveTransitionTo(salto, "mareo");
        RemoveTransitionTo(salto, "inicio-caminata");
        RemoveTransitionTo(salto, "perder");
        RemoveTransitionTo(salto, "celebracion");
        RemoveTransitionTo(mareo, "inicio-caminata");

        EnsureAnyStateTransition(stateMachine, salto, "Jump");
        EnsureAnyStateTransition(stateMachine, mareo, "Trap");
        EnsureAnyStateTransition(stateMachine, celebracion, "Win");
        EnsureAnyStateTransition(stateMachine, perder, "Lose");

        // celebracion / perder are intentionally left with no outgoing transitions (terminal):
        // movement stays blocked until CatGridController.ResetToInitialState() force-plays idle.

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Stage4AnimatorSetup] Completado sobre 'personaje.controller'.\n" +
            $"Parámetros agregados: {(addedParams.Count > 0 ? string.Join(", ", addedParams) : "ninguno (ya existían)")}\n" +
            "Transiciones reconfiguradas: idle->inicio-caminata (IsMoving), caminado->stop (!IsMoving), " +
            "stop->idle (incondicional), salto->idle (incondicional), mareo->idle (incondicional).\n" +
            "Transiciones Any State agregadas/confirmadas: Jump->salto, Trap->mareo, Win->celebracion, Lose->perder.\n" +
            "celebracion y perder se dejaron sin transiciones salientes (terminales) — el reinicio se hace por código.\n" +
            "El parámetro 'pass' se conservó sin usar, no se eliminó. Ningún clip, estado ni Avatar fue modificado.");
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type, List<string> addedLog)
    {
        if (controller.parameters.Any(p => p.name == name)) return;
        controller.AddParameter(name, type);
        addedLog.Add($"{name} ({type})");
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state.name == name) return child.state;
        }

        return null;
    }

    private static void Retarget(AnimatorState from, string toName, Action<AnimatorStateTransition> configure)
    {
        AnimatorStateTransition transition = FindTransition(from, toName);
        if (transition == null) return;
        configure(transition);
    }

    private static AnimatorStateTransition FindTransition(AnimatorState from, string toName)
    {
        foreach (AnimatorStateTransition t in from.transitions)
        {
            if (t.destinationState != null && t.destinationState.name == toName) return t;
        }

        return null;
    }

    private static void RemoveTransitionTo(AnimatorState from, string toName)
    {
        AnimatorStateTransition transition = FindTransition(from, toName);
        if (transition != null) from.RemoveTransition(transition);
    }

    private static void ClearConditions(AnimatorStateTransition transition)
    {
        foreach (AnimatorCondition condition in transition.conditions.ToArray())
        {
            transition.RemoveCondition(condition);
        }
    }

    private static void EnsureAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string triggerName)
    {
        foreach (AnimatorStateTransition existing in stateMachine.anyStateTransitions)
        {
            if (existing.destinationState == destination) return;
        }

        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.canTransitionToSelf = false;
        transition.duration = 0.15f;
        transition.AddCondition(AnimatorConditionMode.If, 0, triggerName);
    }
}
