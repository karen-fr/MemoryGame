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
        ComodinTrap nearestTrap = FindNearest(FindObjectsByType<ComodinTrap>(FindObjectsSortMode.None), t => t.transform.position, transform.position);

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
        Vector3 feetPosition = GetCharacterFeetPosition();
        MemoryCard[] allCards = FindObjectsByType<MemoryCard>(FindObjectsSortMode.None);

        MemoryCard insideCard = null;
        float insideDistance = 0f;

        MemoryCard nearestCard = null;
        float nearestDistance = cardCheckRadius;

        foreach (MemoryCard card in allCards)
        {
            CardFootprint footprint = GetCardFootprint(card);
            float distance = HorizontalDistance(feetPosition, footprint.Center);

            bool insideArea = feetPosition.x >= footprint.MinX && feetPosition.x <= footprint.MaxX
                            && feetPosition.z >= footprint.MinZ && feetPosition.z <= footprint.MaxZ;

            if (insideArea && (insideCard == null || distance < insideDistance))
            {
                insideCard = card;
                insideDistance = distance;
            }

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestCard = card;
            }
        }

        MemoryCard selected = insideCard != null ? insideCard : nearestCard;

        if (selected != null)
        {
            string reason = insideCard != null ? "dentro del area" : "mas cercana";
            float selectedDistance = insideCard != null ? insideDistance : nearestDistance;

            Debug.Log(string.Format("[CatGridController] Seleccion X:{0:F2} Z:{1:F2} -> Baldosa: {2} (distancia {3:F2}, motivo: {4})",
                feetPosition.x, feetPosition.z, selected.name, selectedDistance, reason));
            CardSelected?.Invoke(selected);
        }
        else
        {
            Debug.Log(string.Format("[CatGridController] Seleccion X:{0:F2} Z:{1:F2} -> No hay baldosa bajo el personaje",
                feetPosition.x, feetPosition.z));
        }
    }

    private Vector3 GetCharacterFeetPosition()
    {
        Transform source = visualRoot != null ? visualRoot : transform;
        Renderer[] renderers = source.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0) return transform.position;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private readonly struct CardFootprint
    {
        public readonly Vector3 Center;
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinZ;
        public readonly float MaxZ;

        public CardFootprint(Vector3 center, float minX, float maxX, float minZ, float maxZ)
        {
            Center = center;
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
        }
    }

    private static CardFootprint GetCardFootprint(MemoryCard card)
    {
        Renderer[] renderers = card.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Vector3 fallback = card.transform.position;
            return new CardFootprint(fallback, fallback.x, fallback.x, fallback.z, fallback.z);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return new CardFootprint(bounds.center, bounds.min.x, bounds.max.x, bounds.min.z, bounds.max.z);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private T FindNearest<T>(T[] candidates, Func<T, Vector3> getPosition, Vector3 origin) where T : class
    {
        T nearest = null;
        float nearestDistance = cardCheckRadius;

        foreach (T candidate in candidates)
        {
            Vector3 position = getPosition(candidate);
            float distance = HorizontalDistance(origin, position);

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }
}
