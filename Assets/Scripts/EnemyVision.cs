using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public float viewDistance = 10f;
    public float viewAngle = 60f;
    public float viewAngleVertical = 45f;
    public LayerMask obstacleMask;
    public bool canSeePlayer { get; private set; }

    public void DetectPlayer(Transform player)
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer >= viewDistance)
        {
            canSeePlayer = false;
            return;
        }

        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle >= viewAngle / 2f)
        {
            canSeePlayer = false;
            return;
        }

        float verticalAngle = Vector3.Angle(
            new Vector3(directionToPlayer.x, 0, directionToPlayer.z),
            directionToPlayer                                          
        );
        if (verticalAngle >= viewAngleVertical / 2f)
        {
            canSeePlayer = false;
            return;
        }

        // Có v?t c?n không?
        canSeePlayer = !Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Gizmos.color = Color.blue;
        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + right * viewDistance);

        Gizmos.color = Color.red;
        Vector3 up = Quaternion.Euler(-viewAngleVertical / 2f, 0, 0) * transform.forward;
        Vector3 down = Quaternion.Euler(viewAngleVertical / 2f, 0, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + up * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + down * viewDistance);
    }
}