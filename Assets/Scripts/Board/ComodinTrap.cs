using UnityEngine;

public class ComodinTrap : MonoBehaviour
{
    [SerializeField] private float lockDuration = 2f;

    public float LockDuration => lockDuration;

    private void Awake()
    {
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }
}
