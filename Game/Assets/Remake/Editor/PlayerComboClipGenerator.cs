using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 플레이어 3단 콤보 애니메이션 생성기.
// 플레이어는 파츠 리그(body / Leg / body/Head / body/Weapon)라서 각 파츠의 회전 / 위치를 키프레임으로 만든다.
//   Attack1 : 사선 휘두르기 (빠름)   Attack2 : 반대 방향 올려 베기   Attack3 : 찌르기 (몸을 던지는 마무리)
// 기준 자세는 Player.prefab의 현재 값이고, 각 키는 거기서의 변화량(delta)이라 프리팹 자세가 바뀌어도 재생성하면 된다.
// 무기 회전 : + 방향이 앞으로 휘두르는 쪽 (원작 내려치기가 -96 → +86). 위치 : 로컬 -x가 앞
// 메뉴 : Tools/Deus/Generate Player Combo Clips  (수치를 바꾸면 다시 실행해서 재생성)
public static class PlayerComboClipGenerator
{
    private const string PrefabPath = "Assets/Remake/Prefabs/Player.prefab";
    private const string ClipFolder = "Assets/Remake/Animations/Player";
    private const string ControllerPath = "Assets/Remake/Animations/Player/Player.controller";

    private const string Body = "body";
    private const string Leg = "Leg";
    private const string Head = "body/Head";
    private const string Weapon = "body/Weapon";

    // 한 시점의 자세 : 무기는 절대 각도(weaponZ)와 위치 변화량, 나머지는 변화량
    private struct Key
    {
        public float t;
        public float weaponZ;
        public Vector2 weaponPos;
        public float bodyZ;
        public Vector2 bodyPos;
        public float headZ;
        public Vector2 legPos;

        public Key(float t, float weaponZ, Vector2 weaponPos, float bodyZ, Vector2 bodyPos, float headZ, Vector2 legPos)
        {
            this.t = t; this.weaponZ = weaponZ; this.weaponPos = weaponPos;
            this.bodyZ = bodyZ; this.bodyPos = bodyPos; this.headZ = headZ; this.legPos = legPos;
        }
    }

    private class ClipDef
    {
        public string name;
        public float hitTime;
        public Key[] keys;
    }

    private static Vector2 V(float x, float y) => new Vector2(x, y);

    private static ClipDef[] Definitions(float restWeaponZ)
    {
        float r = restWeaponZ;
        return new[]
        {
            // 1타 : 뒤로 살짝 젖혔다가 앞으로 사선 베기 (0.30초, 판정 0.12초)
            new ClipDef { name = "Attack1", hitTime = 0.12f, keys = new[]
            {
                new Key(0.00f, r,    V(0f, 0f),        0f,   V(0f, 0f),       0f,  V(0f, 0f)),
                new Key(0.05f, -30f, V(0.10f, 0.45f),  -9f,  V(0.03f, 0f),    -6f, V(0.02f, 0f)),
                new Key(0.12f, 95f,  V(-0.30f, -0.10f), 14f, V(-0.06f, 0f),   10f, V(-0.04f, 0f)),
                new Key(0.20f, 105f, V(-0.32f, -0.12f), 12f, V(-0.06f, 0f),    8f, V(-0.04f, 0f)),
                new Key(0.30f, r,    V(0f, 0f),        0f,   V(0f, 0f),       0f,  V(0f, 0f)),
            }},
            // 2타 : 아래 앞에서 반대로 올려 베기 (0.30초, 판정 0.09초)
            new ClipDef { name = "Attack2", hitTime = 0.09f, keys = new[]
            {
                new Key(0.00f, r,    V(0f, 0f),         0f,  V(0f, 0f),       0f,  V(0f, 0f)),
                new Key(0.04f, 135f, V(-0.12f, -0.40f), 7f,  V(-0.02f, -0.02f), 6f, V(-0.03f, 0f)),
                new Key(0.09f, 80f,  V(-0.35f, 0.05f),  -2f, V(-0.07f, 0f),   2f,  V(-0.05f, 0f)),
                new Key(0.15f, -35f, V(0.02f, 0.60f),   -12f, V(-0.04f, 0.02f), -8f, V(-0.03f, 0f)),
                new Key(0.30f, r,    V(0f, 0f),         0f,  V(0f, 0f),       0f,  V(0f, 0f)),
            }},
            // 3타 : 칼을 뒤로 당겼다가 앞으로 찌르며 몸을 던짐 (0.40초, 판정 0.15초)
            new ClipDef { name = "Attack3", hitTime = 0.15f, keys = new[]
            {
                new Key(0.00f, r,   V(0f, 0f),           0f,  V(0f, 0f),        0f,  V(0f, 0f)),
                new Key(0.09f, 88f, V(0.28f, -0.05f),    -10f, V(0.08f, 0f),    -5f, V(0.05f, 0f)),
                new Key(0.15f, 88f, V(-0.60f, -0.02f),   9f,  V(-0.20f, -0.02f), 8f,  V(-0.12f, 0f)),
                new Key(0.27f, 88f, V(-0.58f, -0.02f),   8f,  V(-0.19f, -0.02f), 7f,  V(-0.11f, 0f)),
                new Key(0.40f, r,   V(0f, 0f),           0f,  V(0f, 0f),        0f,  V(0f, 0f)),
            }},
        };
    }

    [MenuItem("Tools/Deus/Generate Player Combo Clips")]
    public static void Generate()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform body = root.transform.Find(Body);
            Transform leg = root.transform.Find(Leg);
            Transform head = root.transform.Find(Head);
            Transform weapon = root.transform.Find(Weapon);
            if (body == null || leg == null || head == null || weapon == null)
            {
                Debug.LogError("Player.prefab에서 body / Leg / body/Head / body/Weapon 파츠를 찾을 수 없습니다.");
                return;
            }

            Pose bodyBase = new Pose(body), legBase = new Pose(leg), headBase = new Pose(head), weaponBase = new Pose(weapon);
            Debug.Log($"[콤보] 기준 자세 : 무기 z {weaponBase.euler.z:F1}, 무기 위치 {weaponBase.pos}, 머리 위치 {headBase.pos}, 몸 위치 {bodyBase.pos}");

            foreach (ClipDef def in Definitions(weaponBase.euler.z))
            {
                string path = $"{ClipFolder}/{def.name}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                bool isNew = clip == null;
                if (isNew) clip = new AnimationClip();
                clip.ClearCurves();
                clip.frameRate = 60f;

                AddPart(clip, Body, bodyBase, def.keys, k => k.bodyPos, k => bodyBase.euler.z + k.bodyZ);
                AddPart(clip, Leg, legBase, def.keys, k => k.legPos, k => legBase.euler.z);
                AddPart(clip, Head, headBase, def.keys, k => Vector2.zero, k => headBase.euler.z + k.headZ);
                AddPart(clip, Weapon, weaponBase, def.keys, k => k.weaponPos, k => k.weaponZ);

                // 판정 이벤트 : 플레이어 BaseCharacter.AnimationTrigger (원작 Attack 클립과 같은 함수)
                AnimationUtility.SetAnimationEvents(clip, new[]
                {
                    new UnityEngine.AnimationEvent { time = def.hitTime, functionName = "AnimationTrigger" }
                });

                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                if (isNew) AssetDatabase.CreateAsset(clip, path);
                EditorUtility.SetDirty(clip);

                HookUpController(def.name, clip);
                Debug.Log($"콤보 애니메이션 생성 : {path} (길이 {def.keys.Last().t:F2}초, 판정 {def.hitTime:F2}초)");
            }
            AssetDatabase.SaveAssets();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private struct Pose
    {
        public Vector3 pos;
        public Vector3 euler;
        public Pose(Transform t) { pos = t.localPosition; euler = t.localEulerAngles; }
    }

    private static void AddPart(AnimationClip clip, string path, Pose basePose, Key[] keys,
        System.Func<Key, Vector2> posDelta, System.Func<Key, float> rotZ)
    {
        var px = new AnimationCurve();
        var py = new AnimationCurve();
        var pz = new AnimationCurve();
        var rz = new AnimationCurve();
        foreach (Key k in keys)
        {
            Vector2 d = posDelta(k);
            px.AddKey(k.t, basePose.pos.x + d.x);
            py.AddKey(k.t, basePose.pos.y + d.y);
            pz.AddKey(k.t, basePose.pos.z);
            rz.AddKey(k.t, rotZ(k));
        }
        float end = keys.Last().t;

        // 키 사이에서 휘청이지 않도록 선형 보간 (빠른 베기 느낌)
        foreach (AnimationCurve c in new[] { px, py, rz })
            for (int i = 0; i < c.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.Linear);
            }

        clip.SetCurve(path, typeof(Transform), "localPosition.x", px);
        clip.SetCurve(path, typeof(Transform), "localPosition.y", py);
        clip.SetCurve(path, typeof(Transform), "localPosition.z", pz);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"), rz);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"), AnimationCurve.Constant(0f, end, basePose.euler.x));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.y"), AnimationCurve.Constant(0f, end, basePose.euler.y));
    }

    // AttackN bool 파라미터 + 상태 + Any State 전이 (조건 : AttackN) 추가. 여러 번 실행해도 중복 생성하지 않음
    private static void HookUpController(string name, AnimationClip clip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"{ControllerPath}를 찾을 수 없습니다.");
            return;
        }

        if (!controller.parameters.Any(p => p.name == name))
            controller.AddParameter(name, AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == name);
        if (state == null)
        {
            int index = name[name.Length - 1] - '1';
            state = sm.AddState(name, new Vector3(780f, 290f + index * 120f, 0f));
        }
        state.motion = clip;
        state.speed = 1f;
        state.writeDefaultValues = true;

        AnimatorStateTransition transition = sm.anyStateTransitions.FirstOrDefault(t => t.conditions.Any(c => c.parameter == name));
        if (transition == null)
        {
            transition = sm.AddAnyStateTransition(state);
            transition.AddCondition(AnimatorConditionMode.If, 0f, name);
        }
        transition.destinationState = state;
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;

        EditorUtility.SetDirty(controller);
    }
}
