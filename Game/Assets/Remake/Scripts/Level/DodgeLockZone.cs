using System.Collections.Generic;
using UnityEngine;

// 이 오브젝트의 x 위치보다 왼쪽(= 보스 구역 이전)에서는 구르기를 막는다.
// 보스방 입구 기둥에 붙여서 사용 : 구르기는 보스 구역에 들어선 뒤부터 가능
public class DodgeLockZone : MonoBehaviour
{
    private static readonly List<DodgeLockZone> all = new List<DodgeLockZone>();

    private void OnEnable() { all.Add(this); }
    private void OnDisable() { all.Remove(this); }

    // 기둥(타일맵)은 오브젝트 원점이 아니라 콜라이더 위치가 실제 경계 → 콜라이더 중심을 사용
    private float LimitX
    {
        get
        {
            Collider2D col = GetComponent<Collider2D>();
            return col != null ? col.bounds.center.x : transform.position.x;
        }
    }

    public static bool IsLocked(Vector2 position)
    {
        foreach (DodgeLockZone z in all)
        {
            if (position.x < z.LimitX) return true;
        }
        return false;
    }
}
