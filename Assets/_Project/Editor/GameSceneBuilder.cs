using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DRG.EditorTools
{
    // Generates the M2 GameScene and its PanelSettings so the scene is reproducible instead of hand-edited.
    // Menu: DRG > Build Game Scene
    // Command line: Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod DRG.EditorTools.GameSceneBuilder.Build
    public static class GameSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/GameScene.unity";
        public const string UxmlPath = "Assets/_Project/UI/BattleScreen.uxml";
        public const string PanelSettingsPath = "Assets/_Project/UI/DRGPanelSettings.asset";
        private const string ThemePath = "Assets/_Project/UI/DRGRuntimeTheme.tss";

        [MenuItem("DRG/Build Game Scene")]
        public static void Build()
        {
            PanelSettings panelSettings = CreateOrUpdatePanelSettings();
            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (uxml == null)
            {
                throw new System.InvalidOperationException("Missing UXML at " + UxmlPath);
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 26, 32, 255);

            GameObject screenObject = new GameObject("BattleScreen");
            UIDocument document = screenObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = uxml;
            screenObject.AddComponent<BattleScreen>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettingsFirst(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("DRG GameScene built: " + ScenePath);
        }

        private static PanelSettings CreateOrUpdatePanelSettings()
        {
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                throw new System.InvalidOperationException("Missing theme at " + ThemePath);
            }

            panelSettings.themeStyleSheet = theme;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1280, 720);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            EditorUtility.SetDirty(panelSettings);
            return panelSettings;
        }

        private static void AddSceneToBuildSettingsFirst(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path != scenePath)
                {
                    scenes.Add(existing[i]);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
