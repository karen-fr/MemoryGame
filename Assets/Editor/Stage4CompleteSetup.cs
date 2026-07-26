using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage4CompleteSetup
{
    [MenuItem("Tools/Memory Game/Complete Stage 4 Setup")]
    public static void RunCompleteSetup()
    {
        Debug.Log("[Stage4CompleteSetup] Iniciando configuración completa (animaciones + interfaz)...");

        Stage4AnimatorSetup.SetupCharacterAnimations();
        Stage4GameplayPolishSetup.SetupGameplayUI();

        List<string> issues = ValidateReferences();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        if (issues.Count == 0)
        {
            Debug.Log("[Stage4CompleteSetup] Completado. Animator e interfaz configurados correctamente, sin advertencias de validación.");
        }
        else
        {
            Debug.LogWarning("[Stage4CompleteSetup] Completado con advertencias de validación:\n  " + string.Join("\n  ", issues));
        }
    }

    private static List<string> ValidateReferences()
    {
        var issues = new List<string>();

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        CatGridController catController = Object.FindFirstObjectByType<CatGridController>();
        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        StartPanelController startPanel = Object.FindFirstObjectByType<StartPanelController>(FindObjectsInactive.Include);
        EndPanelController endPanel = Object.FindFirstObjectByType<EndPanelController>(FindObjectsInactive.Include);
        OptionsPanelController optionsPanel = Object.FindFirstObjectByType<OptionsPanelController>(FindObjectsInactive.Include);

        if (gameManager == null) issues.Add("No se encontró GameManager en la escena.");
        if (catController == null) issues.Add("No se encontró CatGridController en la escena.");
        if (uiManager == null) issues.Add("No se encontró UIManager en la escena.");
        if (startPanel == null) issues.Add("No se encontró StartPanelController.");
        if (endPanel == null) issues.Add("No se encontró EndPanelController.");
        if (optionsPanel == null) issues.Add("No se encontró OptionsPanelController.");

        if (catController != null)
        {
            Animator animator = catController.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                issues.Add("El personaje no tiene un Animator accesible desde CatGridController.");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                issues.Add("El Animator del personaje no tiene un AnimatorController asignado.");
            }
        }

        return issues;
    }
}
