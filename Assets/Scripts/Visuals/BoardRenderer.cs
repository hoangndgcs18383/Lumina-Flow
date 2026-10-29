using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LuminaFlow
{
    public sealed class BoardRenderer : MonoBehaviour
    {
        private GameManager game;
        private Camera boardCamera;
        private RectTransform boardZone;
        private Transform cells, lasers;
        private readonly List<PieceView> views = new List<PieceView>();
        private readonly Vector3[] corners = new Vector3[4];
        public void Initialize(GameManager manager, Camera camera, RectTransform zone)
        {
            game = manager;
            boardCamera = camera;
            boardZone = zone;
            game.LevelLoaded += Build;
            game.Changed += Refresh;
        }
        private Vector3 Cell(float x, float y) => new Vector3(x - (game.CurrentLevel.width - 1) * .5f, (game.CurrentLevel.height - 1) * .5f - y, 0);
        private void Build()
        {
            if (cells != null) { cells.gameObject.SetActive(false); Destroy(cells.gameObject); }
            cells = new GameObject("Grid").transform;
            cells.SetParent(transform, false);
            views.Clear();
            var level = game.CurrentLevel;
            for (int y = 0; y < level.height; y++) for (int x = 0; x < level.width; x++)
            {
                var color = (x + y) % 2 == 0 ? NeonPalette.Hex("101C33") : NeonPalette.Hex("111F37");
                ProceduralArt.Sprite(cells, "Cell " + x + "," + y, ProceduralArt.Tile, Cell(x, y), Vector2.one * .93f, color, 0);
                ProceduralArt.Sprite(cells, "Grid point", ProceduralArt.Disc, Cell(x, y) + new Vector3(-.43f, .43f), Vector2.one * .028f, NeonPalette.Hex("31405C"), 1);
            }
            float left = -level.width * .5f - .08f, right = -left;
            float top = level.height * .5f + .08f, bottom = -top;
            ProceduralArt.Line(cells, "Board frame", new[] { new Vector3(left, bottom), new Vector3(left, top), new Vector3(right, top), new Vector3(right, bottom) }, NeonPalette.Hex("263A55"), .018f, 1, true);
            foreach (var piece in level.pieces)
            {
                var view = new GameObject(piece.type + " • " + piece.id).AddComponent<PieceView>();
                view.transform.SetParent(cells, false);
                view.transform.localPosition = Cell(piece.x, piece.y);
                view.Initialize(piece);
                views.Add(view);
            }
            FitCamera();
        }
        private void Refresh()
        {
            if (game.Result == null) return;
            if (lasers != null) { lasers.gameObject.SetActive(false); Destroy(lasers.gameObject); }
            lasers = new GameObject("Laser paths").transform;
            lasers.SetParent(transform, false);
            foreach (var segment in game.Result.Segments)
            {
                var a = Cell(segment.x1, segment.y1);
                var b = Cell(segment.x2, segment.y2);
                var points = new[] { a, b };
                var tint = NeonPalette.Beam(segment.color);
                ProceduralArt.Line(lasers, "Laser halo", points, NeonPalette.Alpha(LaserSkins.Halo(tint), .32f), .28f, 2, false, true);
                ProceduralArt.Line(lasers, "Laser glow", points, NeonPalette.HDR(NeonPalette.Alpha(tint, .75f), 2), .11f, 3, false, true);
                ProceduralArt.Line(lasers, "Laser core", points, NeonPalette.HDR(Color.Lerp(tint, Color.white, .7f), 2), .027f, 4);
            }
            foreach (var view in views) view.Refresh(game.Result);
        }
        private void LateUpdate()
        {
            if (game == null || game.CurrentLevel == null) return;
            FitCamera();
            if (game.InputBlocked || game.IsWon) return;
            Vector2 position;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))) return;
                position = touch.position;
            }
            else
            {
                if (!Input.GetMouseButtonDown(0) || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
                position = Input.mousePosition;
            }
            if (!RectTransformUtility.RectangleContainsScreenPoint(boardZone, position)) return;
            var world = boardCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, -boardCamera.transform.position.z));
            int x = Mathf.RoundToInt(world.x + (game.CurrentLevel.width - 1) * .5f);
            int y = Mathf.RoundToInt((game.CurrentLevel.height - 1) * .5f - world.y);
            foreach (var piece in game.CurrentLevel.pieces)
                if (piece.x == x && piece.y == y) { game.Rotate(piece); return; }
        }
        private void FitCamera()
        {
            if (boardZone == null || game.CurrentLevel == null || Screen.height == 0) return;
            boardZone.GetWorldCorners(corners);
            float width = Mathf.Max(1, corners[2].x - corners[0].x);
            float height = Mathf.Max(1, corners[2].y - corners[0].y);
            float scale = Mathf.Max((game.CurrentLevel.width + .6f) / width, (game.CurrentLevel.height + .6f) / height);
            boardCamera.orthographicSize = Screen.height * scale * .5f;
            var center = (corners[0] + corners[2]) * .5f;
            boardCamera.transform.position = new Vector3((Screen.width * .5f - center.x) * scale, (Screen.height * .5f - center.y) * scale, -20);
        }
        private void OnDestroy()
        {
            if (game == null) return;
            game.LevelLoaded -= Build;
            game.Changed -= Refresh;
        }
    }
}
