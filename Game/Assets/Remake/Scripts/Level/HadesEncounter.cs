using System.Collections;
using UnityEngine;

// 3스테이지 하데스 전투 흐름 (원작 Sword_Man의 third_stage 처리를 계승)
//  1) 제단(Spawner)에서 G → 해골 / 고블린 4마리 등장
//  2) 전부 처치하고 다시 G → 카메라가 하데스를 비추며 등장 (BGM 전환)
//  3) 하데스 1형태 처치 → 레버 등장. 레버 G → 더 거대한 2형태로 부활
//  4) 2형태 처치 → 레버 G → 출구 문이 열림 → 문 너머 트리거에 닿으면 엔딩
// 제단 오브젝트에 붙이며, 레버(HadesLever)가 이 클래스로 상호작용을 전달
[RequireComponent(typeof(Collider2D))]
public class HadesEncounter : MonoBehaviour, IInteractable
{
    public enum Stage { Ready, MinionsAlive, BossReady, BossFight, FirstFormDown, SecondForm, Cleared, DoorOpen }

    [SerializeField] private Monster[] minions;
    [SerializeField] private HadesBoss hades;
    [SerializeField] private GameObject lever;
    [SerializeField] private Transform door;
    [Tooltip("카메라가 하데스를 비출 때 이동하는 시간 / 비추는 시간")]
    [SerializeField] private float cameraMoveTime = 1.2f;
    [SerializeField] private float holdTime = 2.5f;
    [SerializeField] private float leverDelay = 3.5f;       // 하데스가 쓰러진 뒤 레버가 나타나기까지
    [SerializeField] private float doorRiseHeight = 13f;
    [SerializeField] private float doorRiseTime = 2.5f;

    public Stage Current { get; private set; } = Stage.Ready;

    private bool busy;

    private bool AllMinionsDead
    {
        get
        {
            foreach (var m in minions)
                if (m != null && !m.IsDead) return false;
            return true;
        }
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        // 시작 시 숨겨둘 대상 : 졸개 / 보스 / 레버
        foreach (var m in minions)
            if (m != null) m.gameObject.SetActive(false);
        if (hades != null) hades.gameObject.SetActive(false);
        if (lever != null) lever.SetActive(false);

        // 레거시 문은 중력 Rigidbody였으므로 코드로만 움직이도록 Kinematic으로 전환
        if (door != null)
        {
            var rb = door.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = Vector2.zero;
            }
        }
    }

    private void OnEnable()
    {
        if (hades == null) return;
        hades.OnFirstFormDefeated += HandleFirstFormDefeated;
        hades.OnDefeated += HandleDefeated;
    }

    private void OnDisable()
    {
        if (hades == null) return;
        hades.OnFirstFormDefeated -= HandleFirstFormDefeated;
        hades.OnDefeated -= HandleDefeated;
    }

    #region IInteractable (제단)
    public string InteractionPrompt => Current == Stage.Ready ? "Awaken the Monsters" : "Summon Hades";

    public Vector3 PromptPosition
    {
        get
        {
            var col = GetComponent<Collider2D>();
            return (col != null ? col.bounds.center : transform.position) + Vector3.up * 1.8f;
        }
    }

    public bool CanInteract(PlayerController player)
    {
        if (busy || player.IsDead) return false;
        if (Current == Stage.Ready) return true;
        if (Current == Stage.MinionsAlive && AllMinionsDead)
        {
            Current = Stage.BossReady;
            return true;
        }
        return Current == Stage.BossReady;
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;

        if (Current == Stage.Ready)
        {
            foreach (var m in minions)
                if (m != null) m.gameObject.SetActive(true);
            Current = Stage.MinionsAlive;
            Debug.Log("하데스의 방 : 졸개 등장");
        }
        else
        {
            Current = Stage.BossFight;
            HideAltar();
            StartCoroutine(AppearRoutine(player));
        }
    }

    // 제단은 역할이 끝나면 모습과 상호작용만 없앰 (이 오브젝트는 흐름을 이어 관리하므로 비활성화하지 않음)
    private void HideAltar()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        GetComponent<Collider2D>().enabled = false;
    }
    #endregion

    #region Lever
    public string LeverPrompt => Current == Stage.FirstFormDown ? "Awaken Hades" : "Open the Door";

    public bool CanUseLever(PlayerController player) =>
        !busy && !player.IsDead && (Current == Stage.FirstFormDown || Current == Stage.Cleared);

    public void UseLever(PlayerController player)
    {
        if (!CanUseLever(player)) return;
        if (lever != null) lever.SetActive(false);

        if (Current == Stage.FirstFormDown)
        {
            Current = Stage.SecondForm;
            StartCoroutine(ReviveRoutine(player));
        }
        else
        {
            Current = Stage.DoorOpen;
            StartCoroutine(OpenDoorRoutine());
        }
    }
    #endregion

    #region Boss events
    private void HandleFirstFormDefeated()
    {
        Current = Stage.FirstFormDown;
        StartCoroutine(ShowLeverAfter(leverDelay));
    }

    private void HandleDefeated()
    {
        Current = Stage.Cleared;
        StartCoroutine(ShowLeverAfter(leverDelay));
    }

    private IEnumerator ShowLeverAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        // 대기 중에 흐름이 넘어갔다면(예: 이미 부활) 레버를 보이지 않음
        if (lever != null && (Current == Stage.FirstFormDown || Current == Stage.Cleared))
            lever.SetActive(true);
    }
    #endregion

    #region Sequences
    private IEnumerator AppearRoutine(PlayerController player)
    {
        busy = true;
        PlayBgm(b => b.boss);
        yield return FocusOnHades(player, () => hades.gameObject.SetActive(true));
        busy = false;
    }

    private IEnumerator ReviveRoutine(PlayerController player)
    {
        busy = true;
        PlayBgm(b => b.final);
        yield return FocusOnHades(player, hades.Revive);
        busy = false;
    }

    // 플레이어 입력을 잠그고 카메라를 하데스로 옮겨 연출(onFocused)을 보여준 뒤 다시 플레이어를 따라가게 함
    private IEnumerator FocusOnHades(PlayerController player, System.Action onFocused)
    {
        player.InputLocked = true;
        player.SetVelocity(0f, player.Rb.velocity.y);

        Camera cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<camera>() : null;
        if (follow != null) follow.isFocusing = true;

        if (cam != null && hades != null)
        {
            Vector3 start = cam.transform.position;
            Vector3 target = new Vector3(hades.transform.position.x, hades.transform.position.y, start.z);
            float elapsed = 0f;
            while (elapsed < cameraMoveTime)
            {
                elapsed += Time.deltaTime;
                cam.transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / cameraMoveTime));
                yield return null;
            }
        }

        onFocused?.Invoke();
        yield return new WaitForSeconds(holdTime);

        if (follow != null) follow.isFocusing = false;
        player.InputLocked = false;
    }

    private IEnumerator OpenDoorRoutine()
    {
        if (door == null) yield break;
        Vector3 start = door.position;
        Vector3 end = start + Vector3.up * doorRiseHeight;
        float elapsed = 0f;
        while (elapsed < doorRiseTime)
        {
            elapsed += Time.deltaTime;
            door.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / doorRiseTime));
            yield return null;
        }
        door.position = end;
        Debug.Log("하데스의 방 : 출구 문 열림");
    }

    // 원작 BGMManager_Hades 재사용 (보스 / 최종 형태 음악)
    private static void PlayBgm(System.Func<BGMManager_Hades, AudioClip> select)
    {
        BGMManager_Hades bgm = BGMManager_Hades.Instance;
        if (bgm == null) return;
        AudioClip clip = select(bgm);
        if (clip != null) bgm.PlaySound(clip);
    }
    #endregion
}
