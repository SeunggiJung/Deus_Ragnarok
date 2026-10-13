using UnityEngine;

// 플레이어가 지나가면 낙사 시 돌아올 위치를 이 지점으로 갱신
[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn != null)
            respawn.SetCheckpoint(transform.position);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 3f, 0f));
    }
}
