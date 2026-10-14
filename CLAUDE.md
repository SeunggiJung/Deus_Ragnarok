# Deus_Ragnarok 리팩토링 & 리마스터링 — 작업 인수인계 문서

> 다른 AI 모델 / 다른 세션이 이 프로젝트를 이어서 작업할 때 가장 먼저 읽는 문서.
> 마지막 갱신 : 2026-10-14 (3스테이지 리메이크 반영)

---

## 1. 프로젝트 개요

- 2D 횡스크롤 액션 RPG (Unity **2022.3.62f3 LTS**, C#, Built-in 2D 물리)
- 목표 : 원작의 비대한 MonoBehaviour(`Sword_Man.cs` 980줄)를 **FSM + 인터페이스 + 데이터(ScriptableObject) 기반**으로 리팩토링하고, 다크소울식 보스전(Behavior Tree)까지 리마스터링
- 사용자는 한국어로 소통. 답변 / 커밋 메시지 / 노션 기록 / 코드 주석 모두 **한국어**

### 경로

| 항목 | 경로 |
|---|---|
| Git 루트 | `C:\Unity\Deus_Ragnarok` (원격 `github.com/SeunggiJung/Deus_Ragnarok`, 브랜치 `main`) |
| Unity 프로젝트 | `C:\Unity\Deus_Ragnarok\Game` |
| 리메이크 코드 / 에셋 | `Game/Assets/Remake/` (Scripts, Editor, Data, Prefabs, Scenes, Animations) |
| 원작 코드 / 씬 | `Game/Assets/Legacy/` — **수정하지 않는다** (참고용) |
| 외부 에셋 팩 | `Game/Assets/External/` |
| 노션 기록 페이지 | `326953f3dde980938dc2c424fa6e6779` ("Deus_Ragnarok 리팩토링 & 리마스터링 기록") |
| 노션 작업 로그 (26.10.13, 1~27번) | `3f8953f3dde98132819ff31094f7ca14` |
| 노션 작업 로그 (26.10.14, 1~8번) | `3f8953f3dde981bbb41ed5369aa5921e` (자정이 지나면 날짜별 새 페이지를 기록 페이지 루트 아래에 만들어 1번부터) |
| 노션 트러블 슈팅 (26.10.13) | `3f8953f3dde9815bb914c5e6ec11f0d5` |
| 노션 트러블 슈팅 (26.10.14) | `3f8953f3dde981469d7ae62be423eb02` |

---

## 2. 작업 방식 (사용자와 합의된 규칙)

1. 작업 단위가 끝날 때마다 **git 커밋 + push** (push 자동 허용됨). 커밋 메시지는 "무엇을 / 왜"를 한국어로, 마지막 줄에 Co-Authored-By.
   - `Game/Assets/External/Low_Swordman/3.Prefab/sword_man.prefab`은 Unity 버전 업그레이드로 자동 재직렬화된 변경 → **커밋하지 않음**
   - 사용자가 직접 수정한 파일은 함부로 커밋 / 되돌리지 않고 먼저 언급
2. 노션 기록은 **포트폴리오용** : 문제 → 원인 → 해결 → 결과 위주로 간결하게(커밋 해시, "확인 필요", 세션 중 오간 대화, 수치 나열 같은 군더더기는 제외). 코드 조각과 mermaid 다이어그램은 핵심만. 작업 후 **노션 기록** : **날짜별 작업 로그 페이지**에 `### N. 제목` 섹션을 끝에 추가(현재 26.10.14 페이지 8번까지. 2스테이지 초기 구성 작업은 26.10.13 페이지에 일부 누락), 문제 해결은 트러블 슈팅 페이지에 `### N.` 추가(현재 6번까지). 기존 양식(불릿 + 코드블록 + 원작 대비 변경점) 유지
3. 스크립트 인코딩 : **UTF-8 with BOM + CRLF** (`Game/.editorconfig`). 새 파일 작성 후 반드시 변환
4. 원작 씬은 건드리지 않고 **Remake 씬에서 작업** (`RemakeTest.unity` = 1스테이지, `Stage2.unity` = 2스테이지)
5. 씬 구성은 가능한 한 **에디터 도구(`Tools/Deus/...`)로 재현 가능하게** 만들고, 여러 번 실행해도 결과가 같도록(idempotent) 작성
6. 원작에 없던 기능(예: 넉백)은 임의로 추가하지 않음. 원작 수치를 기준으로 하되 밸런스는 사용자 피드백으로 조정

---

## 3. 조작 (PlayerInput 한 곳에서 관리)

| 키 | 동작 |
|---|---|
| 방향키 | 이동 (속도 3.5) |
| Space | 점프 (높이 1.4칸, `jumpHeight`) |
| 마우스 좌클릭 | 공격 / 공중에서는 점프 공격(크리티컬 x2, 체공 중 1회) |
| LeftShift | 구르기 (무적, 공격/점프 중 캔슬 가능, 공중 1회) |
| 1 | 포션 (5초간 50 회복, 쿨 10초, 피격 시 1초 회복 정지) |
| 2 / 3 / 4 | 레벨업 스탯 선택 (체력 / 공격력 / 스태미너) |
| G | 상호작용 (포탈, 퍼즐 불 / 돌 등) |
| F1 | 치트 : 공격력 +50, 체력 +100, 스태미너 +100 (에디터 / 개발 빌드 전용) |

---

## 4. 아키텍처

### 4.1 캐릭터 공통 (`Scripts/`)
- `BaseCharacter` : 체력, 피격, 무적(피격 무적 `hitInvincible` / 상태 무적 `ForceInvincible` 분리), 피격 플래시, `SetTint`(상태 연출 색), 레벨업 보너스(`bonusMaxHp/bonusAttack`), 이벤트 `OnHpChanged / DamageTaken / OnDied`, 근접 판정 `AnimationTrigger()`(OverlapCircle, 대상 중복 제거), 풀 재사용용 `ResetCharacter()`
- `IDamageable.TakeDamage(float)` — 넉백 없음(원작에 없음)
- `CharacterStats`(SO) → `PlayerStats`, `EnemyStats` → `RangedEnemyStats`, `BossStats`
  - Stats 에셋은 공유 원본 → **런타임에 수정 금지**, 보너스는 캐릭터 쪽에 누적
- `CombatEvents.OnEnemyKilled(enemy, exp)` : 적 처치 → 경험치 (적은 누가 받는지 모름)

### 4.2 플레이어
- `PlayerController` : StateMachine 소유, 스태미너(단계별 회복 0.7s 대기 → 1.5배 → 1.5s 후 3배), 바닥 판정(OverlapBox), 구르기 경로 계산 `PlanDodge`, `InputLocked`
- 상태 : `PlayerGroundedState`(Idle/Move 부모 : 낙하 > 구르기 > 점프 > 공격 우선순위) / `PlayerAirState`(Jump/Fall 부모) / `Attack` / `JumpAttack` / `Dodge` / `Hit` / `Dead`
  - `PlayerState.TryDodge()` : 어느 상태에서든 구르기 캔슬 공통 처리
- **구르기 관통 규칙 (사용자가 B안 선택)** : 기본 거리 안에서 플레이어 중심이 적 중심을 넘을 수 있으면 관통(빠져나갈 때까지 거리 연장, 끝까지 무적), 아니면 적 앞에서 정지. 벽 뒤로는 관통 안 함
- 기타 컴포넌트 : `PlayerProgression`(경험치 50 x 1.5^(Lv-1)), `PlayerPotion`, `PlayerRespawn`(낙사/함정 → 체크포인트, 최대체력 20% 감소), `PlayerInteractor`(G키), `PlayerSessionSync`(스테이지 간 성장 유지), `DebugCheats`
- **프리팹** : `Prefabs/Player.prefab`, `Prefabs/PlayerHUD.prefab` (RemakeTest의 오브젝트에서 생성, `Tools/Deus/Build Player Prefabs`)

### 4.3 몬스터
- `Monster`(BaseCharacter) : FSM `Idle → Chase → Attack → (Hit) → Dead`, 공격 중 슈퍼아머, 멈춰 있을 땐 X축 고정(밀리지 않음), `attackStateName`(애니메이터 공격 상태 이름 : Golem `Attack`. Crab / Dragon은 이제 BT 보스라 `Monster`를 쓰지 않음), 애니메이션 이벤트 `Hit()` → `PerformAttack()`
- `RangedMonster` : `PerformAttack`만 투사체 발사로 교체 (마녀)
- `MonsterSpawner` : `UnityEngine.Pool.ObjectPool`, 사망 → 풀 반환 → `respawnDelay` 뒤 재사용 (`Monster.Spawn`이 상태 전부 초기화)
- `Projectile` / `ProjectilePool` : 진행 방향 회전, 대상이 무적이면 통과

### 4.4 보스 (Behavior Tree)
- `BT/BehaviorTree.cs` : 직접 구현한 BT (메모리 Sequence/Selector, RandomSelector, Condition, ActionNode, Cooldown, Wait, Abort)
- `Boss/BossController` : 강인도(누적 피해 80 → 그로기 1.5s, 트리 Abort), 2페이즈(체력 50%), 방어 자세(2초에 3회 피격), 사망 시 슬로모션 + `SpriteShatter` 파편
- `Boss/BossBehaviorTree` : 포효 > 방어+반격 > RandomSelector[연타 / 지면 가시 / 돌진 / 에너지탄](각 패턴 뒤 0.8s 숨 고르기) > 추적
- `Boss/BossActions` : 패턴 노드들. 보스 전용 애니메이터 `Animations/Boss/Boss.controller`(에셋 팩의 미사용 모션 연결)
- **보스 공통 연출** : `Boss/BossTelegraph`(수축 링 + 머리 위 아이콘 + 패턴별 색 : 주황 ▼ 바닥 찍기 / 노랑 ! 잡기 / 보라 ▲ 도약 / 청록 >> 돌진 / 하늘 방패 방어 / 흰 X 근접 / 연두 ▼ 지면 가시 / 분홍 조준점 에너지탄 / 파랑 별 카운터 가능), `Boss/GroundZone`(바닥 공격 장판 : 추적 → Lock → 차오름), `Fx/Fx`(코드 생성 스프라이트 / Burst / Gather / Trail / Ring), `Fx/FxSpriteAnim`
- **카운터 규칙** : 기를 모으는 패턴의 후반 구간(`CounterWindowOpen`)에 플레이어가 점프 공격(`JumpAttackState`)으로 맞히면 패턴이 끊기고 긴 그로기(`CounterStaggerTime`). `BossController.StaggerFor(duration)`는 행동 노드 안에서 그로기를 걸 때 사용(다음 프레임 적용)
- **Crab (2스테이지 보스, 체력 500)** : `CrabBoss` / `CrabBossStats` / `CrabBossBehaviorTree` / `CrabActions` / `Editor/CrabBossBuilder`. 패턴 : 집게 연타, 바닥 찍기(발밑 + 좌우 충격파), 잡기(구르기로만 회피), 기 모아 강타(후반 40%에 점프 공격 카운터 → 3초 그로기, 2페이즈는 충전 0.8배 / 창 0.85배), 도약 내려찍기(장판 추적 → 내려찍기, 2페이즈 2연속), 돌진 / 방어 / 포효(공통 노드 재사용)
- **Dragon (2스테이지 최종 보스, 체력 900)** : `DragonBoss` / `DragonBossStats` / `DragonBossBehaviorTree` / `DragonActions` / `Editor/DragonBossBuilder`(에셋 팩 PNG 프레임으로 모션 클립 생성, 컨트롤러 `Animations/Boss/DragonBoss.controller`). 패턴 : 발톱(공통 근접 노드), 화염 브레스(후반 40% 카운터, 2페이즈 2연속), 화염구 비(장판 + 낙하), 붙잡아 내려찍기(제한 시간 안에 범위 밖으로 못 나가면 잡혀서 들어올려진 뒤 내려찍힘 → 서로 반대로 날아가고 보스 그로기 1.5초 / 플레이어 스태미너 0). 등장 연출 동안은 `CanEngage`로 전투 지연. 보스 체력바는 보스마다 하나(`BossHealthBar.boss`)
- **Hades (3스테이지 최종 보스, 2형태)** : `HadesBoss` / `HadesBossStats` / `HadesBossBehaviorTree` / `HadesActions` / `Editor/HadesBossBuilder`(원작 `Hades.controller`의 Wizard_* / Rades_* 모션을 재사용해 `Animations/Boss/HadesBoss.controller` 생성, `revived` 불 값으로 모션 세트 전환). 1형태(체력 900) 처치 시 `BossController.HasMoreForms` 훅으로 사망 연출 없이 쓰러진 채 대기 → 레버로 `Revive()` → 더 큰 2형태(체력 1200, 공격 34, Phase2 강화)로 부활 → 2형태 처치 시 일반 보스처럼 슬로모션 + 파편. 패턴 : 마법 타격(`HadesMeleeAction`, 시간 기반 판정, 2형태는 모션 번갈아) / 화염탄 비(`HadesBombAction`, 맵 전체 3발·2형태 5발, 첫 발은 플레이어 위치)
- `Boss/BossArenaGate` : 입구 기둥(전투 시작 시 낙하) / 출구 문(처치 후 열림). BT 보스(`boss`) 또는 일반 몬스터(`guardian` + `engageRange`) 둘 다 지원

### 4.5 레벨 / 상호작용 (`Scripts/Level`, `Scripts/Interaction`)
- `FallingPlatform`(밟으면 흔들림 → 낙하 → 복구), `MovingPlatform`(Kinematic + MovePosition 왕복), `Checkpoint`, `WeaponPickup`(공격력 +3, 무기 스프라이트 교체), `BreakableBox`(IDamageable, 부수면 `LevelUpPickup`), `Crusher`(2스테이지 내려찍는 장애물 : 눈 완전히 감음 = 1회, 반만 감음 = 2회 내려찍기, 장애물 구역에서는 구르기 불가), `DodgeLockZone`(보스 입구 기둥 왼쪽에서는 구르기 금지, 기둥 콜라이더 중심 기준), `ColorPuzzle/ColorFire/ColorStone`(2스테이지 퍼즐 : 불 4개(빨강/파랑/초록/노랑) 중 2개를 섞어 돌 3개의 필요한 색(돌 위 힌트 표시)을 맞춤. 노랑 조합(갈색/청록/라임)은 오답), `EncounterTrigger`(카메라 포커스 + 적 등장), `PlayerSession`, `HadesEncounter`(3스테이지 전투 흐름 : 제단 G → 졸개 4마리 → G → 하데스 등장 → 레버 → 부활 → 레버 → 출구 문) / `HadesLever` / `EndingTrigger`(문 너머 트리거 → Ending 씬)
- `IInteractable` + `PlayerInteractor` : 새 상호작용 대상은 인터페이스만 구현하면 됨
- `StagePortal` : G키 → `PlayerSession.Save` → `ScreenFader`로 페이드 후 씬 로드, `requiredDefeated`로 잠금 가능

### 4.6 UI (`Scripts/UI`, 전부 런타임에 코드로 생성)
- `PlayerHUD`(HP/ST/EXP 바 + 레벨업 패널), `LowHealthVignette`(체력 20% 이하 붉은 맥동), `PotionSlotUI`(왼쪽 아래, Radial 쿨타임), `WorldHealthBar`(몬스터 머리 위, 별도 오브젝트로 추적), `BossHealthBar`(화면 하단)
- 캐릭터는 UI를 모르고 UI가 이벤트를 구독하는 구조

---

## 5. 씬과 에디터 도구 (`Tools/Deus/...`)

### 1스테이지 — `Assets/Remake/Scenes/RemakeTest.unity` (원작 first_stage 기반)
흐름 : 사냥 구간(x -71 ~ -11, 스포너 5) → 원래 구간(Golem/마녀) → 보스방(x 28 기둥 ~ x 62 출구문, Reinforced Golem BT 보스) → 검(x 78) → 낭떠러지 점프 구간(x 79~103, 발판 6) → 원작 점프맵(x 104~, 상자 → LV+1) → 포탈(x 156) → Stage2

| 메뉴 | 역할 |
|---|---|
| Build Monster Prefabs From Scene | 씬의 Golem/마녀 → `Golem.prefab`, `Witch.prefab` |
| Build Boss | 보스 애니메이터 + `BossStats` + `Boss.prefab` |
| Build Hunting Field | 왼쪽 60칸 지형 확장(타일 복사 + **CompositeCollider 재계산**), 경계벽, 카메라 한계, 스포너 |
| Build Jump Section | 출구문, 검, 점프 발판, 원작 Trap 발판 교체, 체크포인트, 상자, 포탈, 레거시 Sword_Man 제거 |
| Build Player Prefabs | 플레이어 / HUD 프리팹 저장 후 연결 |
| Generate Player Roll Clip | 파츠 리그(body/Leg/Leg (1))를 회전시키는 구르기 애니메이션 생성 |

### 2스테이지 — `Assets/Remake/Scenes/Stage2.unity` (원작 second_stage 복사본)
- `Stage 2 - Build Sword Climb` : 보스 문 뒤 → 검까지의 길. 원작은 텔레포트로 오르던 지형 → 점프 + 공중 구르기로 오르는 계단 발판 10개(쉬운 도약 틈 1 / 구르기 도약 틈 3 번갈아, x 90~133), 검 발판에서 오른쪽으로 내려가면 지면(x 134~). 체크포인트3은 중간 발판, 퍼즐 이후 몬스터 없음(스포너는 `Spawner_S2_1` 하나)
- `Build Crab Boss` / `Build Dragon Boss` : 각각 Behavior Tree 보스로 변환 (Build Stage 2에 포함)
- `Build Stage 2` : 씬 복사/열기 → Player/HUD 프리팹 배치 + 카메라 대상 연결 + 레거시 Canvas 끔 → Rockhead x7 → `Crusher` → Crab(`CrabBossBuilder`) / Dragon(`DragonBossBuilder`) BT 보스 → 보스방 Trap/Door → 검 → 색 퍼즐(Stone1=흰색, Stone2=자홍, Stone3=주황 / 빨강+파랑=자홍, 파랑+초록=주황, 초록+빨강=흰색 / 노랑 불은 갈색·청록·라임 오답 조합) → key2 = 드래곤 등장 트리거 → 포탈(드래곤 처치 후 `third_stage`) → 체크포인트 5, 스포너 1(`Spawner_S2_1`, x 138)
- 빌드 설정에 RemakeTest, Stage2, Stage3 추가됨

### 3스테이지 — `Assets/Remake/Scenes/Stage3.unity` (원작 third_stage 복사본)
- `Tools/Deus/Build Stage 3` : 씬 복사/열기 → Player/HUD 프리팹 + 카메라 연결 + 레거시 Canvas 끔 → 졸개 4마리(Skeleton x2, Goblin x2 : 레거시 Enemy/EnemyAI → `Monster`, `SkeletonStats`/`GoblinStats`) → `HadesBossBuilder.Convert`(Hades → `HadesBoss`, 보스 체력바 연결) → 제단(원래 `Spawner`, 이름을 `HadesAltar`로)에 `HadesEncounter`, 레버(`HadesLever`, 처음엔 비활성), 출구 문(`Door`)/왼쪽 벽(`Trap`)은 Kinematic + Ground 레이어 → `trigger` 오브젝트에 `EndingTrigger` → 체크포인트 2개
- 졸개 / 하데스 / 레버는 씬에 비활성으로 저장되고 `HadesEncounter`가 순서대로 활성화. 원작 `BGMManager_Hades`는 보스 / 최종 형태 BGM 전환에 재사용
- 원작의 MP 파이어볼 스킬(하데스에게 추가 피해)은 리메이크에 MP 시스템이 없어 이식하지 않음
- 2스테이지 포탈의 `nextSceneName`은 `Stage3`

---

## 6. 반드시 알아야 할 함정 (실제로 겪은 문제)

1. **꺼진(enabled=false) 레거시 스크립트도 물리 메시지(OnTriggerEnter2D)와 애니메이션 이벤트는 계속 받는다** → 리메이크 오브젝트에서는 레거시 컴포넌트를 *제거*해야 함 (Sword_Man, Enemy, EnemyAI, trigger.cs, RockHeadController 등)
2. **Unity 가짜 null** : `GetComponent<T>() ?? AddComponent<T>()`, `?.` 는 동작하지 않음 → `GetOrAdd<T>()` 패턴처럼 `!= null`로 명시 검사
3. **스크립트로 타일을 바꾸면 CompositeCollider2D 외곽선이 갱신되지 않음** → TilemapCollider2D를 껐다 켜고 `GenerateGeometry()`
4. 애니메이터 Any State 전이 + 남은 트리거 → 사망 상태에서 빠져나감 → 사망 시 모든 행동 트리거 `ResetTrigger`
5. 공격 종료 판정은 `IsName(상태) && !IsInTransition && normalizedTime >= 1` (진입 직후 이전 상태를 읽는 문제)
6. 원작 Attack 클립(`External/Low_Swordman/2.Animation/Attack.anim`)은 Loop Time을 끔
7. 플레이어 무기 스프라이트 경로 : `body/Weapon/Weapon-Sword`
8. **검 획득 시 크기** : 획득한 검(`key`) 스프라이트는 기본 검보다 2배 커서(높이 0.64 vs 0.32) 그대로 교체하면 몸보다 큼 → `PlayerSessionSync.EquipWeapon`이 기본 검과 같은 월드 길이(1.28)로 스케일을 맞춤. `WeaponPickup`과 `PlayerSession.Restore` 둘 다 이 함수를 사용(스테이지 이동 후에도 동일)
9. **비활성 상태에서 시작하는 몬스터**를 코드로 즉시 죽이면 `Start` 전이라 StateMachine이 null → 테스트 시 한 프레임 진행 후 피해를 줄 것

### MCP Unity 도구 사용 시 주의
- Unity 창이 비활성이면 **플레이 모드가 거의 진행되지 않음** → `pause` 후 `step` 반복으로 검증
- 플레이 모드 진입이 실패한 상태에서 `set_transform` 하면 **편집 모드 씬이 바뀜** → 반드시 `get_play_mode_status` 확인
- `update_component`로 씬 오브젝트 참조는 연결 불가(에셋 경로만 가능) → 코드에서 자동 탐색하거나 에디터 도구에서 SerializedObject로 설정
- `delete_gameobject`는 권한 분류기에서 막힐 수 있음 → 비활성화 + 이름 변경으로 대체
- **`File/Save Project` 메뉴 실행 시 Unity가 크래시한 적 있음**(텍스처 처리 스레드) → 사용 금지, 씬은 `save_scene` 사용
- 런타임 검증 불가한 입력(키/마우스)은 사용자에게 테스트 요청

---

## 7. 현재 상태와 다음 할 일

### 검증 상태
- 1스테이지 전 구간 : 사용자 테스트 완료 ("다 잘 작동된다")
- 2스테이지 : Crusher / Crab 보스 / 검 가는 길은 사용자 플레이 확인("딱 좋다"). **드래곤 보스**는 사용자 플레이 1회 진행(브레스 / 화염구 비 / 발톱 피해 로그 확인), 붙잡아 내려찍기 범위(반지름 4.6) / 제한 시간(1.5초) / 브레스 입 위치 / 화염구 장판 크기는 추가 피드백 대기. 노랑 조합 퍼즐은 미검증
  - 드래곤 아레나 입구 기둥(보스 이벤트 연동)과 불꽃 파도 패턴은 아직 없음

- 3스테이지 : 코드 / 씬 구성 완료, 스모크 테스트(졸개 → 하데스 등장 → 1형태 처치 → 부활 → 2형태 처치 → 문 상승)까지 통과. **사용자 플레이 테스트 대기**(난이도 수치 / 화염탄 위치 / 레버 위치 / Ending 씬 진입 확인 필요)
- 검 크기 축소(`EquipWeapon`) : 사용자 플레이 확인 대기. 그립 위치(피벗)가 어색하면 `WeaponWorldLength` 또는 무기 Transform 오프셋 조정

### 다음 후보
- 2스테이지 테스트 피드백 반영 (난이도 / 위치)
- 드래곤 피드백 반영(수치 / 연출), 아레나 기둥, 2페이즈 추가 패턴
- 3스테이지 피드백 반영 (하데스 수치 / 연출, Ending 씬 리메이크 연동)
- 세이브(현재 PlayerSession은 메모리에만 유지), 사운드(원작 SFX/BGM 매니저 연동 안 됨)
- 노션 "해야 되는 것들" : 스폰/성장/UI/BT 보스 모두 구현됨 → 사용자와 정리 필요
