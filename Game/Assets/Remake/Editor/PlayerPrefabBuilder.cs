using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// RemakeTest 씬의 플레이어(sword_man)와 HUD(PlayerHUD)를 프리팹으로 저장하고 씬의 오브젝트를 그 프리팹에 연결.
// 다른 스테이지는 이 프리팹을 배치해 같은 조작 / UI를 사용 (한 곳만 수정하면 모든 스테이지에 반영)
// 메뉴 : Tools/Deus/Build Player Prefabs  (RemakeTest 씬에서 실행)
public static class PlayerPrefabBuilder
{
    public const string PlayerPrefabPath = "Assets/Remake/Prefabs/Player.prefab";
    public const string HudPrefabPath = "Assets/Remake/Prefabs/PlayerHUD.prefab";

    [MenuItem("Tools/Deus/Build Player Prefabs")]
    public static void Build()
    {
        GameObject player = GameObject.Find("sword_man");
        GameObject hud = GameObject.Find("PlayerHUD");
        if (player == null || player.GetComponent<PlayerController>() == null || hud == null)
        {
            Debug.LogError("RemakeTest 씬에서 실행하세요 (sword_man + PlayerController, PlayerHUD 필요).");
            return;
        }

        // 스테이지 이동 시 성장 이어받기
        if (player.GetComponent<PlayerSessionSync>() == null)
            player.AddComponent<PlayerSessionSync>();

        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPrefabPath, InteractionMode.AutomatedAction);
        PrefabUtility.SaveAsPrefabAssetAndConnect(hud, HudPrefabPath, InteractionMode.AutomatedAction);

        EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log($"플레이어 프리팹 생성 : {PlayerPrefabPath}, {HudPrefabPath}");
    }
}
