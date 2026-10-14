using System;
using System.Collections;
using UnityEngine;

// 3스테이지 최종 보스 하데스. 공통 보스(BossController)에 하데스 전용 행동 트리를 적용하고, 2형태를 지원
// - 1형태가 쓰러지면 사라지지 않고 쓰러진 채로 남음 → 레버를 당기면(HadesEncounter) Revive : 더 거대한 2형태로 되살아남
// - 2형태까지 쓰러뜨리면 일반 보스처럼 사망 연출 후 제거
public class HadesBoss : BossController
{
    public static class HadesAnim
    {
        public const string Revived = "revived";   // 2형태 모션 세트 선택
        public const string Spawn = "spawn";       // 등장 / 되살아남 모션
    }

    [Tooltip("등장 연출(카메라 이동 + Spawn 모션)이 끝날 때까지 전투를 시작하지 않는 시간")]
    [SerializeField] private float spawnDelay = 3f;

    private float enabledTime;

    public HadesBossStats HadesStats => (HadesBossStats)Stats;
    public bool Revived { get; private set; }
    public event Action OnFirstFormDefeated;   // 1형태가 쓰러졌을 때 (레버 등장)

    // 형태별 이펙트 색 : 1형태(붉은 불꽃 마법사)는 주황, 2형태(검은 로브 + 보라 불꽃)는 보라
    public Color FxColor => Revived ? new Color(0.65f, 0.25f, 1f) : new Color(1f, 0.55f, 0.15f);
    public Color FxDarkColor => Revived ? new Color(0.3f, 0.05f, 0.5f) : new Color(0.9f, 0.2f, 0.05f);
    public Color ZoneIdleColor => Revived ? new Color(0.6f, 0.3f, 1f) : new Color(1f, 0.6f, 0.1f);
    public Color ZoneLockedColor => Revived ? new Color(0.75f, 0.15f, 1f) : new Color(1f, 0.15f, 0.1f);

    protected override bool HasMoreForms => !Revived;
    protected override bool CanEngage => Time.time - enabledTime >= spawnDelay;
    protected override float CounterStaggerTime => Stats.staggerTime * 2f;

    protected override BTNode BuildTree() => HadesBossBehaviorTree.Build(this);
    protected override bool IsValidStats(CharacterStats s) => s is HadesBossStats;
    protected override CharacterStats CreateDefaultStats() => ScriptableObject.CreateInstance<HadesBossStats>();

    private void OnEnable()
    {
        enabledTime = Time.time;
        StartCoroutine(IgnorePlayerBody());
    }

    // 보스가 거대해서 바짝 붙으면 몸 / 팔 쪽 콜라이더에 막혀 점프가 안 되는 문제 →
    // 플레이어와는 물리 충돌을 끔 (겹쳐 지나갈 수 있음). 피격 판정은 OverlapCircle / OverlapBox 질의라 영향 없음
    // 플레이어의 공격과 보스의 범위 공격은 그대로 몸통(콜라이더) 기준으로 맞음
    private IEnumerator IgnorePlayerBody()
    {
        yield return null;   // Awake / Start 이후 (Target 탐색 시점)
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null) yield break;

        var theirs = player.GetComponentsInChildren<Collider2D>(true);
        while (true)
        {
            // 플레이어의 구르기가 끝나면서 충돌 무시를 되돌릴 수 있어 주기적으로 다시 적용
            foreach (var mine in GetComponentsInChildren<Collider2D>(true))
                foreach (var other in theirs)
                    if (other != null) Physics2D.IgnoreCollision(mine, other, true);
            yield return new WaitForSeconds(0.2f);
        }
    }

    // 1형태 사망 : 쓰러진 모습을 잠시 보여준 뒤 모습만 숨김 (오브젝트는 유지해 Revive에서 재사용)
    protected override void OnFormDefeated()
    {
        StartCoroutine(HideBody(HadesStats.formSwitchDeathTime));
        OnFirstFormDefeated?.Invoke();
        Debug.Log("[Boss] 하데스 1형태 쓰러짐 : 레버로 되살릴 수 있음");
    }

    private IEnumerator HideBody(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var sr in spriteRenderers)
            if (sr != null) sr.enabled = false;
    }

    // 레버 : 더 거대한 2형태로 되살아남
    public void Revive()
    {
        if (Revived || !IsDead) return;
        Revived = true;
        StopAllCoroutines();

        HadesBossStats hs = HadesStats;
        bonusMaxHp = hs.form2MaxHp - hs.maxHp;
        bonusAttack = hs.form2AttackDamage - hs.attackDamage;
        ResetCharacter();       // 사망 상태 / 체력 초기화
        RestartFight();         // 행동 트리 / 그로기 / 전투 시작 상태 초기화

        foreach (var col in GetComponentsInChildren<Collider2D>(true)) col.enabled = true;
        rb.simulated = true;
        rb.velocity = Vector2.zero;
        foreach (var sr in spriteRenderers)
            if (sr != null) sr.enabled = true;

        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(s.x * hs.form2ScaleMultiplier, s.y * hs.form2ScaleMultiplier, s.z);
        ApplyForm2Hitbox();
        transform.position += Vector3.up * 0.5f;   // 바닥에 끼지 않도록 살짝 띄워 착지 (중력으로 내려앉음)

        enabledTime = Time.time;
        EnterPhase2();          // 쿨다운 / 이동 속도 / 모션 속도 강화 + 색 변화
        StartCoroutine(IgnorePlayerBody());   // StopAllCoroutines로 끊긴 충돌 무시 갱신 재시작
        anim.SetBool(HadesAnim.Revived, true);
        anim.ResetTrigger(AnimParams.Die);
        anim.SetTrigger(HadesAnim.Spawn);
        Debug.Log("[Boss] 하데스 2형태 부활");
    }

    // 2형태 스프라이트는 1형태와 크기 / 발 위치가 달라서 같은 콜라이더를 쓰면 몸이 바닥 아래로 내려가 보임
    // → 빌더가 계산한 2형태용 콜라이더(스프라이트의 보이는 영역 기준)로 교체
    private void ApplyForm2Hitbox()
    {
        HadesBossStats hs = HadesStats;
        var capsule = GetComponent<CapsuleCollider2D>();
        if (!hs.hasForm2Collider || capsule == null) return;
        capsule.offset = hs.form2ColliderOffset;
        capsule.size = hs.form2ColliderSize;
    }
}
