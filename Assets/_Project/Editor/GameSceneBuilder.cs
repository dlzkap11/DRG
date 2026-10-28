using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DRG.EditorTools
{
    // Generates the Lobby, Game, and Result scenes (spec §18, DEC-010) and their shared PanelSettings,
    // so scenes are reproducible instead of hand-edited. Build settings order: Lobby, Game, Result.
    // Menu: DRG > Build Scenes
    // Command line: Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod DRG.EditorTools.GameSceneBuilder.Build
    public static class GameSceneBuilder
    {
        public const string ScenesFolder = "Assets/_Project/Scenes";
        public const string LobbyScenePath = ScenesFolder + "/" + SceneNames.Lobby + ".unity";
        public const string ScenePath = ScenesFolder + "/" + SceneNames.Game + ".unity";
        public const string ResultScenePath = ScenesFolder + "/" + SceneNames.Result + ".unity";
        public const string LobbyUxmlPath = "Assets/_Project/UI/LobbyScreen.uxml";
        public const string UxmlPath = "Assets/_Project/UI/BattleScreen.uxml";
        public const string ResultUxmlPath = "Assets/_Project/UI/ResultScreen.uxml";
        public const string PanelSettingsPath = "Assets/_Project/UI/DRGPanelSettings.asset";
        private const string ThemePath = "Assets/_Project/UI/DRGRuntimeTheme.tss";

        [MenuItem("DRG/Build Scenes")]
        public static void Build()
        {
            CreateOrUpdatePanelSettings();
            AssetDatabase.SaveAssets();
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            BuildScene(LobbyScenePath, "LobbyScreen", LobbyUxmlPath, typeof(LobbyScreen));
            BuildScene(ScenePath, "BattleScreen", UxmlPath, typeof(BattleScreen));
            BuildScene(ResultScenePath, "ResultScreen", ResultUxmlPath, typeof(ResultScreen));

            SetBuildSceneOrder(new[] { LobbyScenePath, ScenePath, ResultScenePath });
            AssetDatabase.SaveAssets();
            Debug.Log("DRG scenes built: " + LobbyScenePath + ", " + ScenePath + ", " + ResultScenePath);
        }

        private static void BuildScene(string scenePath, string screenName, string uxmlPath, Type screenComponent)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load assets after NewScene: opening a new single scene can unload assets loaded earlier,
            // which left later scenes with an empty PanelSettings reference.
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (panelSettings == null || uxml == null)
            {
                throw new InvalidOperationException("Missing PanelSettings or UXML for " + scenePath);
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 26, 32, 255);

            GameObject screenObject = new GameObject(screenName);
            UIDocument document = screenObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = uxml;
            screenObject.AddComponent(screenComponent);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateOrUpdatePanelSettings()
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
                throw new InvalidOperationException("Missing theme at " + ThemePath);
            }

            panelSettings.themeStyleSheet = theme;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1280, 720);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            EditorUtility.SetDirty(panelSettings);
        }

        // Puts the given scenes first, in order, and keeps any other registered scenes after them.
        private static void SetBuildSceneOrder(string[] scenePaths)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            for (int i = 0; i < scenePaths.Length; i++)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePaths[i], true));
            }

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (Array.IndexOf(scenePaths, existing[i].path) < 0)
                {
                    scenes.Add(existing[i]);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
