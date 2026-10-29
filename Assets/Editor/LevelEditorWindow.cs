using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LuminaFlow.Editor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const string CatalogPath = "Assets/Resources/Levels/catalog.json";
        private static readonly string[] Tools = { "Select", "mirror", "splitter", "emitter", "target", "crystal", "portal", "wall", "filter", "sensor", "door", "Erase" };
        private static readonly string[] Colors = { "cyan", "pink", "amber", "" };
        private LevelCatalog catalog = new LevelCatalog();
        private int selectedIndex, tool = 1, colorIndex;
        private PieceData selectedPiece;
        private Vector2 listScroll, mainScroll, codeScroll;
        private string code = "", feedback = "";
        private bool dirty, evaluate = true, solutionPreview;
        private LaserResult preview;
        private string validation = "";
        private LevelData Level => catalog.levels.Count == 0 ? null : catalog.levels[Mathf.Clamp(selectedIndex, 0, catalog.levels.Count - 1)];

        [MenuItem("Lumina Flow/Level editor")]
        public static void Open() => GetWindow<LevelEditorWindow>("Lumina • Levels");
        private void OnEnable() { minSize = new Vector2(860, 680); LoadCatalog(); }
        private void LoadCatalog()
        {
            try
            {
                catalog = File.Exists(CatalogPath) ? LevelSerializer.DeserializeCatalog(File.ReadAllText(CatalogPath)) : new LevelCatalog();
                selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, catalog.levels.Count - 1));
                selectedPiece = null;
                dirty = false; evaluate = true; feedback = "Catalog loaded.";
            }
            catch (Exception exception) { feedback = exception.Message; }
        }
        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("LUMINA FLOW" + (dirty ? " • unsaved" : ""), EditorStyles.boldLabel, GUILayout.Width(190));
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
                {
                    if (!dirty || EditorUtility.DisplayDialog("Reload catalog?", "Discard unsaved editor changes?", "Reload", "Cancel")) LoadCatalog();
                }
                if (GUILayout.Button("Save catalog", EditorStyles.toolbarButton)) SaveCatalog();
                if (GUILayout.Button("New level", EditorStyles.toolbarButton)) NewLevel();
                if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton) && Level != null)
                {
                    var clone = Level.DeepClone(); clone.id = NextId(); clone.name += " copy";
                    catalog.levels.Add(clone); selectedIndex = catalog.levels.Count - 1; selectedPiece = null; MarkChanged();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open Main", EditorStyles.toolbarButton)) LuminaProjectSetup.OpenMainScene();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLevelList();
                mainScroll = EditorGUILayout.BeginScrollView(mainScroll);
                if (Level == null) EditorGUILayout.HelpBox("Create a level or load Resources/Levels/catalog.json.", MessageType.Info);
                else DrawLevel();
                EditorGUILayout.EndScrollView();
            }
            if (!string.IsNullOrEmpty(feedback)) EditorGUILayout.HelpBox(feedback, MessageType.None);
        }
        private void DrawLevelList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(175)))
            {
                listScroll = EditorGUILayout.BeginScrollView(listScroll);
                for (int i = 0; i < catalog.levels.Count; i++)
                {
                    var level = catalog.levels[i];
                    var previous = GUI.backgroundColor;
                    if (i == selectedIndex) GUI.backgroundColor = new Color(.4f, .9f, 1);
                    if (GUILayout.Button(level.id.ToString("00") + "  " + level.name, GUILayout.Height(36)))
                    { selectedIndex = i; selectedPiece = null; evaluate = true; }
                    GUI.backgroundColor = previous;
                }
                EditorGUILayout.EndScrollView();
                if (GUILayout.Button("Remove selected") && Level != null && EditorUtility.DisplayDialog("Remove level?", Level.name + " will be removed from this editor session. Save to apply.", "Remove", "Cancel"))
                { catalog.levels.RemoveAt(selectedIndex); selectedIndex = Mathf.Max(0, selectedIndex - 1); selectedPiece = null; MarkChanged(); }
            }
        }
        private void DrawLevel()
        {
            var level = Level;
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                level.id = EditorGUILayout.IntField("ID", level.id, GUILayout.MinWidth(120));
                level.name = EditorGUILayout.TextField("Name", level.name);
            }
            level.subtitle = EditorGUILayout.TextField("Subtitle", level.subtitle);
            using (new EditorGUILayout.HorizontalScope())
            {
                level.width = Mathf.Clamp(EditorGUILayout.IntField("Width", level.width), 2, 16);
                level.height = Mathf.Clamp(EditorGUILayout.IntField("Height", level.height), 2, 16);
                level.par = Mathf.Max(0, EditorGUILayout.IntField("Par", level.par));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                tool = EditorGUILayout.Popup("Brush", tool, Tools);
                colorIndex = EditorGUILayout.Popup("Color", colorIndex, Colors);
            }
            solutionPreview = EditorGUILayout.ToggleLeft("Preview solution metadata (does not change authored rotations)", solutionPreview);
            if (EditorGUI.EndChangeCheck()) MarkChanged();
            EditorGUILayout.LabelField("Left click: select / paint. Right click: rotate. Shift-click: erase. Y increases down.", EditorStyles.miniLabel);
            if (evaluate) RefreshPreview();
            float cell = Mathf.Clamp((position.width - 230) / Mathf.Max(1, level.width), 25, 58);
            var gridRect = GUILayoutUtility.GetRect(level.width * cell, level.height * cell, GUILayout.ExpandWidth(false));
            DrawGrid(gridRect, cell);
            EditorGUILayout.HelpBox(validation, preview == null ? MessageType.Warning : MessageType.Info);
            DrawPieceInspector();
            EditorGUILayout.Space(8);
            DrawShareTools();
        }
        private void DrawGrid(Rect rect, float cell)
        {
            var level = Level;
            for (int y = 0; y < level.height; y++) for (int x = 0; x < level.width; x++)
                EditorGUI.DrawRect(new Rect(rect.x + x * cell + 1, rect.y + y * cell + 1, cell - 2, cell - 2), (x + y) % 2 == 0 ? NeonPalette.Panel : NeonPalette.Hex("172740"));
            if (preview != null)
            {
                Handles.BeginGUI();
                foreach (var segment in preview.Segments)
                {
                    Handles.color = NeonPalette.Beam(segment.color);
                    Handles.DrawAAPolyLine(3, new Vector3(rect.x + (segment.x1 + .5f) * cell, rect.y + (segment.y1 + .5f) * cell), new Vector3(rect.x + (segment.x2 + .5f) * cell, rect.y + (segment.y2 + .5f) * cell));
                }
                Handles.EndGUI();
            }
            foreach (var piece in level.pieces)
            {
                var pieceRect = new Rect(rect.x + piece.x * cell, rect.y + piece.y * cell, cell, cell);
                var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(cell * .4f) };
                style.normal.textColor = NeonPalette.Beam(piece.color);
                GUI.Label(pieceRect, Symbol(piece), style);
                if (piece == selectedPiece)
                {
                    Handles.BeginGUI(); Handles.color = Color.white;
                    Handles.DrawWireCube(pieceRect.center, pieceRect.size - Vector2.one * 5); Handles.EndGUI();
                }
            }
            var e = Event.current;
            if (e.type != EventType.MouseDown || !rect.Contains(e.mousePosition) || (e.button != 0 && e.button != 1)) return;
            int gx = Mathf.FloorToInt((e.mousePosition.x - rect.x) / cell), gy = Mathf.FloorToInt((e.mousePosition.y - rect.y) / cell);
            var existing = level.pieces.Find(piece => piece.x == gx && piece.y == gy);
            if (e.shift || Tools[tool] == "Erase") { if (existing != null) { level.pieces.Remove(existing); selectedPiece = null; MarkChanged(); } }
            else if (e.button == 1) { if (existing != null) { existing.rotation = (existing.rotation + 1) % 4; selectedPiece = existing; MarkChanged(); } }
            else if (Tools[tool] == "Select") selectedPiece = existing;
            else
            {
                if (existing != null) level.pieces.Remove(existing);
                string type = Tools[tool];
                selectedPiece = new PieceData { id = type + "-" + Guid.NewGuid().ToString("N").Substring(0, 6), type = type, x = gx, y = gy, color = Colors[colorIndex], @fixed = type != "mirror" && type != "splitter", solution = -1 };
                level.pieces.Add(selectedPiece); MarkChanged();
            }
            e.Use(); Repaint();
        }
        private static string Symbol(PieceData piece)
        {
            switch (piece.type)
            {
                case "mirror": return (piece.rotation & 1) == 0 ? "/" : "\\";
                case "splitter": return (piece.rotation & 1) == 0 ? "/+" : "\\+";
                case "emitter": return new[] { "→", "↓", "←", "↑" }[piece.rotation & 3];
                case "target": return "◎";
                case "crystal": return "◆";
                case "portal": return "◉";
                case "wall": return "■";
                case "filter": return "F";
                case "sensor": return "S";
                case "door": return "D";
                default: return "?";
            }
        }
        private void DrawPieceInspector()
        {
            if (selectedPiece == null) return;
            EditorGUILayout.LabelField("Selected piece", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedPiece.id = EditorGUILayout.TextField("Unique ID", selectedPiece.id);
            using (new EditorGUILayout.HorizontalScope())
            {
                selectedPiece.rotation = EditorGUILayout.IntSlider("Rotation (0=right)", selectedPiece.rotation, 0, 3);
                selectedPiece.solution = EditorGUILayout.IntSlider("Solution (-1=none)", selectedPiece.solution, -1, 3);
            }
            selectedPiece.color = EditorGUILayout.TextField("Color", selectedPiece.color);
            selectedPiece.link = EditorGUILayout.TextField("Portal / sensor-door link", selectedPiece.link);
            selectedPiece.@fixed = EditorGUILayout.Toggle("Fixed", selectedPiece.@fixed);
            if (EditorGUI.EndChangeCheck()) MarkChanged();
        }
        private void DrawShareTools()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy share code")) Run(() => { code = LevelSerializer.Serialize(Level); EditorGUIUtility.systemCopyBuffer = code; feedback = "Base64 UTF-8 LevelData JSON copied."; });
                if (GUILayout.Button("Copy JSON")) Run(() => { code = LevelSerializer.ToJson(Level); EditorGUIUtility.systemCopyBuffer = code; feedback = "Level JSON copied."; });
                if (GUILayout.Button("Import JSON / share")) ImportCode();
                if (GUILayout.Button("Export JSON file")) Run(() =>
                {
                    string path = EditorUtility.SaveFilePanel("Export level", "", "level-" + Level.id + ".json", "json");
                    if (!string.IsNullOrEmpty(path)) File.WriteAllText(path, LevelSerializer.ToJson(Level));
                });
                if (GUILayout.Button("Import file")) Run(() =>
                {
                    string path = EditorUtility.OpenFilePanel("Import level JSON or share text", "", "");
                    if (!string.IsNullOrEmpty(path)) { code = File.ReadAllText(path); ImportCode(); }
                });
            }
            codeScroll = EditorGUILayout.BeginScrollView(codeScroll, GUILayout.Height(90));
            code = EditorGUILayout.TextArea(code, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        private void ImportCode() => Run(() =>
        {
            var imported = code.TrimStart().StartsWith("{") ? LevelSerializer.FromJson(code) : LevelSerializer.Deserialize(code);
            if (catalog.levels.Any(level => level.id == imported.id)) imported.id = NextId();
            catalog.levels.Add(imported); selectedIndex = catalog.levels.Count - 1; selectedPiece = null; MarkChanged();
            feedback = "Imported as level " + imported.id + ". Save catalog to persist.";
        });
        private void SaveCatalog() => Run(() =>
        {
            var ids = new HashSet<int>();
            foreach (var level in catalog.levels)
            {
                LevelSerializer.Validate(level);
                if (!ids.Add(level.id)) throw new FormatException("Duplicate level ID " + level.id + ".");
            }
            string json = LevelSerializer.SerializeCatalog(catalog);
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
            File.WriteAllText(CatalogPath, json);
            AssetDatabase.ImportAsset(CatalogPath);
            dirty = false; feedback = "Catalog saved and validated: " + catalog.levels.Count + " levels.";
        });
        private void NewLevel()
        {
            var level = new LevelData { id = NextId(), name = "New light", subtitle = "Guide the beam", width = 7, height = 7, par = 1 };
            level.pieces.Add(new PieceData { id = "source", type = "emitter", x = 0, y = 3, color = "cyan", @fixed = true });
            level.pieces.Add(new PieceData { id = "mirror", type = "mirror", x = 3, y = 3, rotation = 1, solution = 0, color = "cyan" });
            level.pieces.Add(new PieceData { id = "core", type = "target", x = 3, y = 0, color = "cyan", @fixed = true });
            catalog.levels.Add(level); selectedIndex = catalog.levels.Count - 1; selectedPiece = null; MarkChanged();
        }
        private int NextId() => catalog.levels.Count == 0 ? 1 : catalog.levels.Max(level => level.id) + 1;
        private void MarkChanged() { dirty = true; evaluate = true; }
        private void RefreshPreview()
        {
            evaluate = false;
            try
            {
                LevelSerializer.Validate(Level);
                var copy = Level.DeepClone();
                if (solutionPreview) foreach (var piece in copy.pieces) if (piece.CanRotate && piece.solution >= 0) piece.rotation = piece.solution;
                preview = LaserEngine.Evaluate(copy);
                validation = "Valid • " + preview.Segments.Count + " beam segments • " + preview.LitTargets.Count + " lit targets" + (preview.Won ? " • SOLVED" : "");
            }
            catch (Exception exception) { preview = null; validation = exception.Message; }
        }
        private void Run(Action action) { try { action(); } catch (Exception exception) { feedback = exception.Message; Debug.LogWarning("Lumina level editor: " + exception.Message); } }
    }
}
