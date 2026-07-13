using UnityEngine;

public class ComodinTrap : MonoBehaviour
{
    [SerializeField] private float lockDuration = 2f;

    public float LockDuration => lockDuration;

    public void NotifyDetected()
    {
        Debug.Log("[ComodinTrap] Detectado - bloqueando " + lockDuration + "s");
    }
}
