using System.Collections.Generic;
using UnityEngine;

// 이 오브젝트의 x 위치보다 왼쪽(= 보스 구역 이전)에서는 구르기를 막는다.
// 보스방 입구 기둥에 붙여서 사용 : 구르기는 보스 구역에 들어선 뒤부터 가능
public class DodgeLockZone : MonoBehaviour
{
    private static readonly List<DodgeLockZone> all = new List<DodgeLockZone>();

    private void OnEnable() { all.Add(this); }
    private void OnDisable() { all.Remove(this); }

    public static bool IsLocked(Vector2 position)
    {
        foreach (DodgeLockZone z in all)
        {
            if (position.x < z.transform.position.x) return true;
        }
        return false;
    }
}
