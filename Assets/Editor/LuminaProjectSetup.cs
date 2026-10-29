using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LuminaFlow.Editor
{
    /// <summary>Safe to rerun. Never replaces an existing scene, renderer, pipeline, or bloom profile.</summary>
    [InitializeOnLoad]
    public static class LuminaProjectSetup
    {
        private const string Generated = "Assets/Scripts/Visuals/Generated";
        private const string MainScene = "Assets/Scenes/Main.unity";
        static LuminaProjectSetup() { EditorApplication.delayCall += ConfigureWhenReady; }
        private static void ConfigureWhenReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ConfigureWhenReady;
                return;
            }
            try { Configure(); }
            catch (Exception exception) { Debug.LogError("Lumina Flow setup: " + exception); }
        }
        [MenuItem("Lumina Flow/Configure project and neon rendering")]
        public static void Configure()
        {
            bool firstSetup = !File.Exists(Generated + "/LuminaURP.asset");
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory("Assets/Scripts/Visuals/Resources/Lumina");
            AssetDatabase.Refresh();
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Generated + "/LuminaRenderer.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "Lumina Unlit Renderer";
                AssetDatabase.CreateAsset(renderer, Generated + "/LuminaRenderer.asset");
            }
            ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);
            if (renderer.postProcessData != null) ResourceReloader.ReloadAllNullIn(renderer.postProcessData, UniversalRenderPipelineAsset.packagePath);
            EditorUtility.SetDirty(renderer);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Generated + "/LuminaURP.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "Lumina URP 14";
                pipeline.supportsHDR = true;
                pipeline.msaaSampleCount = 1;
                pipeline.renderScale = 1;
                AssetDatabase.CreateAsset(pipeline, Generated + "/LuminaURP.asset");
            }
            if (GraphicsSettings.defaultRenderPipeline == null) GraphicsSettings.defaultRenderPipeline = pipeline;
            // A new project has no per-quality override. Preserve deliberate user overrides.
            if (QualitySettings.renderPipeline == null) QualitySettings.renderPipeline = pipeline;
            const string bloomPath = "Assets/Scripts/Visuals/Resources/Lumina/Bloom.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(bloomPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "Lumina Bloom";
                AssetDatabase.CreateAsset(profile, bloomPath);
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(.85f);
                bloom.intensity.Override(.75f);
                bloom.scatter.Override(.65f);
                bloom.tint.Override(new Color(.75f, .9f, 1));
                AssetDatabase.AddObjectToAsset(bloom, profile);
                EditorUtility.SetDirty(profile);
            }
            if (!File.Exists(MainScene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                var marker = new GameObject("Lumina Flow - automatic runtime bootstrap");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(marker, scene);
                EditorSceneManager.SaveScene(scene, MainScene);
                EditorSceneManager.CloseScene(scene, true);
            }
            if (!EditorBuildSettings.scenes.Any(scene => scene.path == MainScene && scene.enabled))
            {
                var scenes = EditorBuildSettings.scenes.Where(scene => scene.path != MainScene).ToList();
                scenes.Insert(0, new EditorBuildSettingsScene(MainScene, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            if (firstSetup)
            {
                PlayerSettings.companyName = "Lumina Studio";
                PlayerSettings.productName = "Lumina Flow";
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                PlayerSettings.allowedAutorotateToPortrait = true;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = false;
                PlayerSettings.allowedAutorotateToLandscapeRight = false;
                PlayerSettings.defaultScreenWidth = 540;
                PlayerSettings.defaultScreenHeight = 960;
                PlayerSettings.runInBackground = false;
                PlayerSettings.colorSpace = ColorSpace.Linear;
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "studio.lumina.flow");
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "studio.lumina.flow");
                SetLegacyInput();
            }
            AssetDatabase.SaveAssets();
        }
        private static void SetLegacyInput()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0) return;
            var settings = new SerializedObject(assets[0]);
            var activeInput = settings.FindProperty("activeInputHandler");
            if (activeInput == null || activeInput.intValue == 0) return;
            activeInput.intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Lumina Flow/Open playable main scene")]
        public static void OpenMainScene()
        {
            Configure();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(MainScene);
        }
    }
    public sealed class LuminaBuildPreparation : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;
        public void OnPreprocessBuild(BuildReport report) => LuminaProjectSetup.Configure();
    }
}
