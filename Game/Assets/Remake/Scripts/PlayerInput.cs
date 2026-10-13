using UnityEngine;

// 플레이어 입력을 한 곳에서 읽어 상태 클래스들에 제공.
// 키 변경, 모바일 버튼, New Input System 전환 시 이 클래스만 수정하면 됨
[System.Serializable]
public class PlayerInput
{
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode dodgeKey = KeyCode.LeftShift;
    [SerializeField] private int attackMouseButton = 0; // 0 : 좌클릭

    public float MoveX { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool DodgePressed { get; private set; }

    // 프레임마다 한 번만 호출 (PlayerController.Update)
    public void Read()
    {
        MoveX = Input.GetAxisRaw("Horizontal");
        JumpPressed = Input.GetKeyDown(jumpKey);
        AttackPressed = Input.GetMouseButtonDown(attackMouseButton);
        DodgePressed = Input.GetKeyDown(dodgeKey);
    }

    // 사망 등 입력을 막아야 할 때
    public void Clear()
    {
        MoveX = 0f;
        JumpPressed = AttackPressed = DodgePressed = false;
    }
}
