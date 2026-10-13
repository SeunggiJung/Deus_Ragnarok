using UnityEngine;

// 캐릭터별 수치 데이터. 코드 수정 없이 에셋만 바꿔서 밸런스를 조절하기 위함
[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Deus/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Health")]
    public float maxHp = 100f;
    public float invincibleTime = 0.5f;   // 피격 후 무적 시간 (초)

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackRadius = 1f;       // attackCheck 기준 판정 범위 반지름
}
