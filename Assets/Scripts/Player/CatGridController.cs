using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CatGridController : MonoBehaviour
{
    [SerializeField] private float moveDistance = 0.5f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float jumpDuration = 0.3f;
    [SerializeField] private float cardCheckRadius = 0.4f;
    [SerializeField] private Transform visualRoot;

    public event Action<MemoryCard> CardSelected;
    public event Action<float> TrapTriggered;

    private Vector3 targetPosition;
    private bool isMoving;
    private bool isJumping;
    private bool isLocked;

    private Vector3 screenUpDirection = Vector3.forward;
    private Vector3 screenRightDirection = Vector3.right;

    private void Start()
    {
        targetPosition = transform.position;
        CalculateScreenRelativeAxes();

        if (visualRoot == null)
        {
            Transform found = FindChildRecursive(transform, "CharacterVisual");
            visualRoot = found != null ? found : transform;
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;

            Transform result = FindChildRecursive(child, childName);
            if (result != null) return result;
        }

        return null;
    }

    private void CalculateScreenRelativeAxes()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camForward = cam.transform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude > 0.0001f)
        {
            screenUpDirection = SnapToNearestGridAxis(camForward.normalized);
        }

        Vector3 camRight = cam.transform.right;
        camRight.y = 0f;
        if (camRight.sqrMagnitude > 0.0001f)
        {
            screenRightDirection = SnapToNearestGridAxis(camRight.normalized);
        }
    }

    private static Vector3 SnapToNearestGridAxis(Vector3 direction)
    {
        Vector3[] axes = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        Vector3 nearestAxis = Vector3.forward;
        float bestDot = float.NegativeInfinity;

        foreach (Vector3 axis in axes)
        {
            float dot = Vector3.Dot(direction, axis);
            if (dot > bestDot)
            {
                bestDot = dot;
                nearestAxis = axis;
            }
        }

        return nearestAxis;
    }

    private void Update()
    {
        if (isMoving)
        {
            MoveTowardsTarget();
            return;
        }

        if (isJumping) return;
        if (isLocked) return;

        ReadMovementInput();
        ReadActionInput();
    }

    private void ReadMovementInput()
    {
        if (Keyboard.current == null) return;

        Vector3 direction = Vector3.zero;

        if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
            direction = screenUpDirection;
        else if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
            direction = -screenUpDirection;
        else if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            direction = -screenRightDirection;
        else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            direction = screenRightDirection;

        if (direction != Vector3.zero)
        {
            targetPosition = transform.position + direction * moveDistance;
            isMoving = true;
            RotateTowards(direction);
        }
    }

    private void RotateTowards(Vector3 direction)
    {
        if (visualRoot == null || direction == Vector3.zero) return;

        visualRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void ReadActionInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(JumpAndSelect());
        }
    }

    private void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (transform.position == targetPosition)
        {
            isMoving = false;
            CheckForTrap();
        }
    }

    private void CheckForTrap()
    {
        TryTriggerTrap();
    }

    private bool TryTriggerTrap()
    {
        ComodinTrap nearestTrap = FindNearest(FindObjectsByType<ComodinTrap>(FindObjectsSortMode.None), t => t.transform.position);

        if (nearestTrap == null) return false;

        nearestTrap.NotifyDetected();
        TrapTriggered?.Invoke(nearestTrap.LockDuration);
        StartCoroutine(LockMovement(nearestTrap.LockDuration));
        return true;
    }

    private IEnumerator LockMovement(float duration)
    {
        isLocked = true;
        yield return new WaitForSeconds(duration);
        isLocked = false;
    }

    private IEnumerator JumpAndSelect()
    {
        isJumping = true;

        Vector3 startPosition = transform.position;
        Vector3 peakPosition = startPosition + Vector3.up * jumpHeight;
        float halfDuration = jumpDuration / 2f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            transform.position = Vector3.Lerp(startPosition, peakPosition, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            transform.position = Vector3.Lerp(peakPosition, startPosition, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPosition;
        isJumping = false;

        if (!TryTriggerTrap())
        {
            TrySelectCard();
        }
    }

    private void TrySelectCard()
    {
        MemoryCard nearestCard = FindNearest(FindObjectsByType<MemoryCard>(FindObjectsSortMode.None), c => c.transform.position);

        if (nearestCard != null)
        {
            Debug.Log("[CatGridController] Ficha seleccionada: " + nearestCard.name);
            CardSelected?.Invoke(nearestCard);
        }
    }

    private T FindNearest<T>(T[] candidates, Func<T, Vector3> getPosition) where T : class
    {
        T nearest = null;
        float nearestDistance = cardCheckRadius;

        foreach (T candidate in candidates)
        {
            Vector3 position = getPosition(candidate);
            float dx = transform.position.x - position.x;
            float dz = transform.position.z - position.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }
}
