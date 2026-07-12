using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CatGridController : MonoBehaviour
{
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float jumpDuration = 0.3f;
    [SerializeField] private LayerMask cardLayerMask = ~0;
    [SerializeField] private float cardCheckRadius = 0.4f;

    public event Action<MemoryCard> CardSelected;

    private Vector3 targetPosition;
    private bool isMoving;
    private bool isJumping;

    private void Start()
    {
        targetPosition = transform.position;
    }

    private void Update()
    {
        if (isMoving)
        {
            MoveTowardsTarget();
            return;
        }

        if (isJumping) return;

        ReadMovementInput();
        ReadActionInput();
    }

    private void ReadMovementInput()
    {
        if (Keyboard.current == null) return;

        Vector3 direction = Vector3.zero;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            direction = Vector3.forward;
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            direction = Vector3.back;
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            direction = Vector3.left;
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            direction = Vector3.right;

        if (direction != Vector3.zero)
        {
            targetPosition = transform.position + direction * cellSize;
            isMoving = true;
        }
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
        }
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

        TrySelectCard();
    }

    private void TrySelectCard()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, cardCheckRadius, cardLayerMask);

        foreach (Collider hit in hits)
        {
            MemoryCard card = hit.GetComponent<MemoryCard>();
            if (card != null)
            {
                CardSelected?.Invoke(card);
                break;
            }
        }
    }
}
