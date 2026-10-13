using System.Collections;
using UnityEngine;

// 플레이어가 지나가면 카메라가 지정한 지점을 비추고, 숨겨둔 적(드래곤 등)을 등장시키는 연출
// (원작 : hidden 태그(key2)에 닿으면 카메라가 드래곤 쪽으로 이동 → 드래곤 활성화)
[RequireComponent(typeof(Collider2D))]
public class EncounterTrigger : MonoBehaviour
{
    [SerializeField] private GameObject[] activateOnTrigger;  // 시작 시 숨겼다가 등장시킬 오브젝트
    [SerializeField] private Transform focusPoint;
    [SerializeField] private float moveTime = 1.2f;           // 카메라 이동 시간
    [SerializeField] private float holdTime = 1.5f;           // 등장 후 비추는 시간
    [SerializeField] private bool hideSelfOnTrigger = true;

    private bool triggered;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        foreach (var go in activateOnTrigger)
            if (go != null) go.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead) return;

        triggered = true;
        StartCoroutine(Play(player));
    }

    private IEnumerator Play(PlayerController player)
    {
        if (hideSelfOnTrigger)
        {
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        }

        player.InputLocked = true;
        player.SetVelocity(0f, player.Rb.velocity.y);

        // 레거시 카메라 스크립트의 추적을 잠시 멈추고 직접 이동
        Camera cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<camera>() : null;
        if (follow != null) follow.isFocusing = true;

        if (cam != null && focusPoint != null)
        {
            Vector3 start = cam.transform.position;
            Vector3 target = new Vector3(focusPoint.position.x, focusPoint.position.y, start.z);
            float elapsed = 0f;
            while (elapsed < moveTime)
            {
                elapsed += Time.deltaTime;
                cam.transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / moveTime));
                yield return null;
            }
        }

        foreach (var go in activateOnTrigger)
            if (go != null) go.SetActive(true);

        yield return new WaitForSeconds(holdTime);

        if (follow != null) follow.isFocusing = false; // 다시 플레이어 추적 (부드럽게 돌아옴)
        player.InputLocked = false;
    }
}
