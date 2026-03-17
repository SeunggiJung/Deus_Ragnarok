using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public List<Enemy> enemies = new List<Enemy>(); // 모든 몬스터를 담을 리스트

    // 모든 몬스터의 상태를 초기화 및 부활시키는 메서드
    public void RespawnAllMonsters()
    {
        foreach (Enemy enemy in enemies)
        {
            // 보스가 아닌 경우만 부활
            if (enemy.enemyName != "Boss" && enemy.isDead)
            {
                enemy.Respawn();
            }
        }
    }

    // 모든 Enemy를 리스트에 추가
    public void RegisterEnemy(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    // Enemy가 죽으면 리스트에 반영
    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemy.isDead = true; // 죽은 상태로 표시
        }
    }
}