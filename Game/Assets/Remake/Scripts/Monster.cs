using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 일반 몬스터. 체력/피격/무적 처리는 BaseCharacter에서 공통으로 담당
public class Monster : BaseCharacter
{
    protected override void Die()
    {
        Debug.Log($"{gameObject.name} 사망");
        Destroy(gameObject);
    }
}
