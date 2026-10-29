using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LuminaFlow
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Object.FindObjectOfType<GameManager>() != null) return;
            Build();
        }

        public static GameManager Build()
        {
            var root = new GameObject("Lumina Flow");
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Neon Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
                cameraObject.transform.SetParent(root.transform);
            }
            camera.orthographic = true;
            camera.orthographicSize = 8;
            camera.transform.SetPositionAndRotation(new Vector3(0, 0, -20), Quaternion.identity);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = NeonPalette.Background;
            camera.allowHDR = true;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            cameraData.volumeLayerMask = ~0;
            var profile = Resources.Load<VolumeProfile>("Lumina/Bloom");
            if (profile != null)
            {
                var volume = root.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = profile;
            }
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(root.transform);
            root.AddComponent<AudioManager>();
            var game = root.AddComponent<GameManager>();
            var board = new GameObject("Procedural Board").AddComponent<BoardRenderer>();
            board.transform.SetParent(root.transform);
            var ui = new GameObject("Responsive UI").AddComponent<UIManager>();
            ui.transform.SetParent(root.transform);
            ui.Initialize(game);
            board.Initialize(game, camera, ui.BoardZone);
            return game;
        }
    }
}
