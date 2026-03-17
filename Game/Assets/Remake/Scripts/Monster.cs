using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour, IDamageable
{
    public float hp = 50f;
    public void TakeDamage(float damage, Vector2 knockbackForce)
    {
        hp -= damage;
        Debug.Log($"몬스터가 {damage}만큼 맞았습니다. 남은 체력: {hp}");
        if (hp <= 0) Die();
    }
    void Die()
    {
        Debug.Log("몬스터 사망");
        Destroy(gameObject);
    }
}
