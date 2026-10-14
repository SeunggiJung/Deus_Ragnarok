using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 3스테이지 졸개(해골 / 고블린) 애니메이션 정리.
// 에셋 팩 컨트롤러를 리메이크 폴더로 복사해서 손봄 (원본 팩은 다른 씬이 쓰므로 그대로 둠)
//  - 걷기 클립이 반복(Loop)이 아니라 한 걸음만 재생되고 멈춘 채 미끄러지던 문제 → 반복으로 변경
// (등장 모션 중 미끄러지는 문제 / 좌우 떨림 / 동시에 움직이는 문제는 Monster 코드에서 처리)
public static class MinionAnimationBuilder
{
    private const string Folder = "Assets/Remake/Animations/Monsters";

    public const string SkeletonSource = "Assets/External/Monsters Creatures Fantasy/Animations/Skeleton/Skeleton.controller";
    public const string GoblinSource = "Assets/External/Monsters Creatures Fantasy/Animations/Goblin/Goblin1.controller";

    public static RuntimeAnimatorController Prepare(string sourcePath, string name)
    {
        Directory.CreateDirectory(Folder);
        string targetPath = $"{Folder}/{name}.controller";

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(targetPath) == null)
        {
            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                Debug.LogError($"컨트롤러 복사 실패 : {sourcePath}");
                return null;
            }
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(targetPath);
        foreach (var child in controller.layers[0].stateMachine.states)
        {
            var clip = child.state.motion as AnimationClip;
            if (clip == null || !clip.name.ToLowerInvariant().Contains("walk")) continue;

            // 걷기 클립은 원본 팩 에셋이므로 복사본을 만들어 반복 설정
            string clipPath = $"{Folder}/{name}_{clip.name}.anim";
            var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (copy == null)
            {
                string sourceClipPath = AssetDatabase.GetAssetPath(clip);
                if (!AssetDatabase.CopyAsset(sourceClipPath, clipPath))
                {
                    Debug.LogWarning($"걷기 클립 복사 실패 : {sourceClipPath}");
                    continue;
                }
                copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            EditorUtility.SetDirty(copy);
            child.state.motion = copy;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }
}
