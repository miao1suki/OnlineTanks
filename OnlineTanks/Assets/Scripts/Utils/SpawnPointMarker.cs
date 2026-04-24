using UnityEngine;

public class SpawnPointMarker : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(
            transform.position,
            0.3f
        );
    }
}