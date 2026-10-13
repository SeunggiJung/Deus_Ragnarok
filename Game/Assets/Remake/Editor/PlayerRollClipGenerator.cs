using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 플레이어 구르기 애니메이션 생성기.
// 플레이어는 body / Leg / Leg (1) 파츠로 나뉜 리그라서, 각 파츠를 몸 중심(pivot) 기준으로
// 함께 360도 회전시키고 중간에 몸을 웅크리게(tuck) 해서 구르는 모습을 만든다.
// 메뉴 : Tools/Deus/Generate Player Roll Clip  (수치를 바꾸면 다시 실행해서 재생성)
public static class PlayerRollClipGenerator
{
    private const string ClipPath = "Assets/Remake/Animations/Player/Roll.anim";
    private const string ControllerPath = "Assets/Remake/Animations/Player/Player.controller";
    private const string StatsPath = "Assets/Remake/Data/PlayerStats.asset";
    private const string PlayerName = "sword_man";

    private static readonly string[] PartPaths = { "body", "Leg", "Leg (1)" };
    private static readonly Vector2 Pivot = new Vector2(0f, 0.25f); // 플레이어 로컬 기준 회전 중심 (몸통 중앙)
    private const float TuckAmount = 0.3f;  // 회전 중간에 파츠를 중심 쪽으로 당기는 비율
    private const int KeyCount = 13;

    [MenuItem("Tools/Deus/Generate Player Roll Clip")]
    public static void Generate()
    {
        GameObject player = GameObject.Find(PlayerName);
        if (player == null)
        {
            Debug.LogError($"씬에서 {PlayerName}을 찾을 수 없습니다. RemakeTest 씬을 열고 실행하세요.");
            return;
        }

        PlayerStats stats = AssetDatabase.LoadAssetAtPath<PlayerStats>(StatsPath);
        float duration = stats != null ? stats.dodgeDuration : 0.35f;

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        bool isNew = clip == null;
        if (isNew) clip = new AnimationClip();
        clip.ClearCurves();
        clip.frameRate = 60f;

        foreach (string path in PartPaths)
        {
            Transform part = player.transform.Find(path);
            if (part == null)
            {
                Debug.LogError($"{PlayerName}/{path} 파츠를 찾을 수 없습니다.");
                return;
            }
            AddPartCurves(clip, path, part.localPosition, part.localEulerAngles, duration);
        }

        // 구르기는 한 번만 재생
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew) AssetDatabase.CreateAsset(clip, ClipPath);
        EditorUtility.SetDirty(clip);

        HookUpController(clip);
        AssetDatabase.SaveAssets();
        Debug.Log($"구르기 애니메이션 생성 완료 : {ClipPath} (길이 {duration}초)");
    }

    private static void AddPartCurves(AnimationClip clip, string path, Vector3 basePos, Vector3 baseEuler, float duration)
    {
        var posX = new AnimationCurve();
        var posY = new AnimationCurve();
        var posZ = new AnimationCurve();
        var rotZ = new AnimationCurve();

        Vector2 offset = (Vector2)basePos - Pivot;

        for (int i = 0; i < KeyCount; i++)
        {
            float t = i / (float)(KeyCount - 1);
            // localScale.x = 1(왼쪽을 봄) 기준으로 앞(왼쪽)으로 구르려면 반시계(+) 회전.
            // 오른쪽을 볼 때는 localScale.x = -1로 뒤집히므로 자동으로 오른쪽으로 구름
            float angle = 360f * t;
            float tuck = 1f - TuckAmount * Mathf.Sin(Mathf.PI * t);

            Vector2 rotated = (Vector2)(Quaternion.Euler(0f, 0f, angle) * (offset * tuck));
            Vector2 pos = Pivot + rotated;

            posX.AddKey(new Keyframe(t * duration, pos.x));
            posY.AddKey(new Keyframe(t * duration, pos.y));
            posZ.AddKey(new Keyframe(t * duration, basePos.z));
            rotZ.AddKey(new Keyframe(t * duration, baseEuler.z + angle));
        }

        // 회전이 키 사이에서 튀지 않도록 선형 보간
        SetLinear(rotZ);

        clip.SetCurve(path, typeof(Transform), "localPosition.x", posX);
        clip.SetCurve(path, typeof(Transform), "localPosition.y", posY);
        clip.SetCurve(path, typeof(Transform), "localPosition.z", posZ);
        // localEulerAnglesRaw를 써야 0 → 360도가 한 바퀴로 유지됨 (쿼터니언 보간 시 0도로 인식)
        EditorCurveBinding rotBinding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z");
        AnimationUtility.SetEditorCurve(clip, rotBinding, rotZ);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"), AnimationCurve.Constant(0f, duration, baseEuler.x));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.y"), AnimationCurve.Constant(0f, duration, baseEuler.y));
    }

    private static void SetLinear(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
    }

    // Roll 상태를 만들고, Dodge 파라미터로 들어가는 Any State 전이의 목적지를 Roll로 교체
    private static void HookUpController(AnimationClip clip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"{ControllerPath}를 찾을 수 없습니다.");
            return;
        }

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState roll = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Roll");
        if (roll == null)
            roll = sm.AddState("Roll", new Vector3(530f, 530f, 0f));
        roll.motion = clip;
        roll.writeDefaultValues = true;

        foreach (AnimatorStateTransition t in sm.anyStateTransitions)
        {
            if (t.conditions.Any(c => c.parameter == "Dodge"))
                t.destinationState = roll;
        }

        EditorUtility.SetDirty(controller);
    }
}
