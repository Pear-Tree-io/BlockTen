using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [Tooltip("Seconds until this GameObject is destroyed")]
    public float lifetime = 1f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
