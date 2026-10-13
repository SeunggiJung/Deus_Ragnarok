using UnityEngine;

// 캐릭터 머리 위 체력바 (월드 공간, SpriteRenderer 2개로 구성).
// 몬스터의 스케일(Golem은 10 x 12)이나 좌우 반전에 영향받지 않도록 자식으로 두지 않고 별도 오브젝트로 따라다님
[RequireComponent(typeof(BaseCharacter))]
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(1.6f, 0.15f);
    [SerializeField] private float heightOffset = 0.3f; // 콜라이더 윗면에서 띄울 높이
    [SerializeField] private Color fillColor = new Color(0.85f, 0.2f, 0.2f);
    [SerializeField] private Color backColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private int sortingOrder = 50;

    private static Sprite whiteSprite;

    private BaseCharacter owner;
    private Collider2D ownerCollider;
    private Transform barRoot;
    private Transform fill;

    private void Awake()
    {
        owner = GetComponent<BaseCharacter>();
        ownerCollider = GetComponent<Collider2D>();
        Build();
    }

    private void Build()
    {
        if (whiteSprite == null)
        {
            // 1x1 흰색 텍스처로 1유닛 크기 스프라이트 생성 (별도 에셋 없이 사용)
            Texture2D tex = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0.5f), tex.width);
        }

        barRoot = new GameObject($"{name} HP Bar").transform;

        CreatePart("Back", backColor, sortingOrder);
        fill = CreatePart("Fill", fillColor, sortingOrder + 1);
    }

    private Transform CreatePart(string partName, Color color, int order)
    {
        var go = new GameObject(partName, typeof(SpriteRenderer));
        go.transform.SetParent(barRoot, false);
        go.transform.localPosition = new Vector3(-size.x * 0.5f, 0f, 0f); // 피벗이 왼쪽이므로 가운데 정렬
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go.transform;
    }

    private void OnEnable()
    {
        owner.OnHpChanged += UpdateBar;
    }

    private void Start()
    {
        UpdateBar(owner.CurrentHp, owner.MaxHp);
    }

    private void OnDisable()
    {
        owner.OnHpChanged -= UpdateBar;
    }

    private void UpdateBar(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        fill.localScale = new Vector3(size.x * ratio, size.y, 1f);

        if (owner.IsDead)
            barRoot.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        float top = ownerCollider != null ? ownerCollider.bounds.max.y : transform.position.y;
        barRoot.position = new Vector3(transform.position.x, top + heightOffset, 0f);
    }

    private void OnDestroy()
    {
        if (barRoot != null)
            Destroy(barRoot.gameObject);
    }
}
