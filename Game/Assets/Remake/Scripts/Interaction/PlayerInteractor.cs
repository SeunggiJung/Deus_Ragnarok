using System.Collections.Generic;
using UnityEngine;

// 플레이어 주변의 상호작용 대상을 추적하고, 가장 가까운 대상 위에 "[G] ..." 안내를 띄우며 G키 입력을 전달
[RequireComponent(typeof(PlayerController))]
[DefaultExecutionOrder(100)] // PlayerController.Update에서 입력을 읽은 뒤 실행되도록
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private string keyLabel = "G";

    private PlayerController player;
    private readonly List<IInteractable> nearby = new List<IInteractable>();
    private IInteractable current;
    private TextMesh prompt;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        prompt = CreatePrompt();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable target = other.GetComponentInParent<IInteractable>();
        if (target != null && !nearby.Contains(target))
            nearby.Add(target);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IInteractable target = other.GetComponentInParent<IInteractable>();
        if (target != null)
            nearby.Remove(target);
    }

    private void Update()
    {
        current = FindClosest();
        UpdatePrompt();

        if (current != null && player.Input.InteractPressed)
            current.Interact(player);
    }

    private IInteractable FindClosest()
    {
        IInteractable best = null;
        float bestDistance = float.MaxValue;
        for (int i = nearby.Count - 1; i >= 0; i--)
        {
            var target = nearby[i];
            if (target == null || (target is Object o && o == null))
            {
                nearby.RemoveAt(i); // 파괴된 대상 정리
                continue;
            }
            if (!target.CanInteract(player)) continue;

            float d = ((Vector2)(target.PromptPosition - transform.position)).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = target;
            }
        }
        return best;
    }

    private void UpdatePrompt()
    {
        bool show = current != null && !player.IsDead && !player.InputLocked;
        prompt.gameObject.SetActive(show);
        if (!show) return;

        prompt.text = $"[{keyLabel}] {current.InteractionPrompt}";
        // 위아래로 살짝 움직여 눈에 띄게
        prompt.transform.position = current.PromptPosition + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.08f;
    }

    // 월드 공간 안내 문구 (플레이어의 좌우 반전에 영향받지 않도록 자식이 아닌 별도 오브젝트)
    private static TextMesh CreatePrompt()
    {
        var text = new GameObject("Interaction Prompt").AddComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var renderer = text.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = text.font.material;
        renderer.sortingOrder = 60;
        text.fontSize = 48;
        text.characterSize = 0.1f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.gameObject.SetActive(false);
        return text;
    }

    private void OnDestroy()
    {
        if (prompt != null) Destroy(prompt.gameObject);
    }
}
