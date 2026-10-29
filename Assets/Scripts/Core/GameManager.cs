using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuminaFlow
{
    /// <summary>The presentation's single source of truth; the laser simulation stays in LaserEngine.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public LevelCatalog Catalog { get; private set; }
        public LevelData CurrentLevel { get; private set; }
        public LaserResult Result { get; private set; }
        public int CurrentIndex { get; private set; }
        public bool IsCampaign { get; private set; } = true;
        public LevelData ExportLevel => resetSource == null ? null : resetSource.DeepClone();
        private LevelData resetSource;
        public int Moves => history.Count;
        public bool IsWon => Result != null && Result.Won;
        public int EarnedStars { get; private set; }
        public string LoadError { get; private set; }
        public bool InputBlocked { get; set; }
        public event Action LevelLoaded;
        public event Action Changed;
        public event Action Won;
        public event Action<string> Notification;
        private readonly Stack<RotationMove> history = new Stack<RotationMove>();
        private struct RotationMove { public PieceData piece; public int rotation; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private void Start()
        {
            try
            {
                var source = Resources.Load<TextAsset>("Levels/catalog");
                if (source == null) throw new InvalidOperationException("Không tìm thấy Resources/Levels/catalog.json.");
                Catalog = LevelSerializer.DeserializeCatalog(source.text);
                if (Catalog == null || Catalog.levels == null || Catalog.levels.Count == 0)
                    throw new InvalidOperationException("Danh sách màn chơi đang trống.");
                ProgressStore.Initialize(Catalog);
                int last = Mathf.Clamp(ProgressStore.LastLevel, 0, Catalog.levels.Count - 1);
                LoadLevel(IsUnlocked(last) ? last : 0);
            }
            catch (Exception exception)
            {
                LoadError = exception.Message;
                Debug.LogError("Lumina Flow: " + exception);
                Changed?.Invoke();
            }
        }

        public bool IsUnlocked(int index) => Catalog != null && index >= 0 && index < Catalog.levels.Count && ProgressStore.IsUnlocked(index);
        public void LoadLevel(int index)
        {
            if (!IsUnlocked(index)) { Notify("Hoàn thành màn trước để mở khóa."); return; }
            BeginLevel(Catalog.levels[index], true, index);
        }

        public void LoadSharedLevel(LevelData level)
        {
            LevelSerializer.Validate(level);
            BeginLevel(level, false, -1);
        }
        private void BeginLevel(LevelData source, bool campaign, int index)
        {
            IsCampaign = campaign;
            CurrentIndex = index;
            resetSource = source.DeepClone();
            CurrentLevel = resetSource.DeepClone();
            history.Clear();
            EarnedStars = 0;
            InputBlocked = false;
            if (campaign) ProgressStore.LastLevel = index;
            Result = LaserEngine.Evaluate(CurrentLevel);
            LevelLoaded?.Invoke();
            Changed?.Invoke();
            CheckWin();
        }

        public static bool CanRotate(PieceData piece) => piece != null && !piece.@fixed &&
            (string.Equals(piece.type, "mirror", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(piece.type, "splitter", StringComparison.OrdinalIgnoreCase));

        public void Rotate(PieceData piece)
        {
            if (InputBlocked || IsWon || !CanRotate(piece)) return;
            history.Push(new RotationMove { piece = piece, rotation = piece.rotation });
            piece.rotation = (piece.rotation + 1) % 4;
            AudioManager.Instance?.PlayTurn(Moves);
            HapticManager.Tap();
            Evaluate();
        }

        public void Undo()
        {
            if (history.Count == 0 || InputBlocked) return;
            var move = history.Pop();
            move.piece.rotation = move.rotation;
            EarnedStars = 0;
            AudioManager.Instance?.PlayTap();
            Evaluate();
        }
        public void ResetLevel()
        {
            if (resetSource == null) return;
            if (IsCampaign) LoadLevel(CurrentIndex);
            else LoadSharedLevel(resetSource);
        }
        public void RefreshPresentation() => Changed?.Invoke();
        public void NextLevel()
        {
            if (Catalog == null || !IsCampaign) return;
            if (CurrentIndex + 1 < Catalog.levels.Count) LoadLevel(CurrentIndex + 1);
            else Notify("Bạn đã thắp sáng toàn bộ hành trình!");
        }
        public void Hint()
        {
            if (CurrentLevel == null || IsWon || InputBlocked) return;
            foreach (var piece in CurrentLevel.pieces)
            {
                if (CanRotate(piece) && piece.solution >= 0 && (piece.rotation & 1) != (piece.solution & 1))
                {
                    Rotate(piece);
                    Notify("Đã xoay một khối theo lời giải. Gợi ý tính một lượt.");
                    return;
                }
            }
            Notify("Các khối đã đúng hướng, hoặc màn này chưa có gợi ý.");
        }
        public void Notify(string message) => Notification?.Invoke(message);
        private void Evaluate()
        {
            Result = LaserEngine.Evaluate(CurrentLevel);
            Changed?.Invoke();
            CheckWin();
        }
        private void CheckWin()
        {
            if (!IsWon) return;
            int crystalCount = 0;
            foreach (var piece in CurrentLevel.pieces)
                if (string.Equals(piece.type, "crystal", StringComparison.OrdinalIgnoreCase)) crystalCount++;
            EarnedStars = 1 + (Result.LitCrystals.Count >= crystalCount ? 1 : 0) + (Moves <= CurrentLevel.par ? 1 : 0);
            if (IsCampaign) ProgressStore.Complete(CurrentIndex, CurrentLevel, Result, Moves);
            AudioManager.Instance?.PlayWin();
            HapticManager.Success();
            Won?.Invoke();
        }
        private void OnApplicationPause(bool paused) { if (paused) ProgressStore.Flush(); }
        private void OnApplicationQuit() => ProgressStore.Flush();
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
