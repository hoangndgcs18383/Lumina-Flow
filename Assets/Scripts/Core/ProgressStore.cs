using System;
using UnityEngine;

namespace LuminaFlow
{
    /// <summary>Adapts catalog order to the persistence service's stable numeric level IDs.</summary>
    public static class ProgressStore
    {
        private static SaveManager saves;
        private static LevelCatalog catalog;
        public static int LastLevel
        {
            get => PlayerPrefs.GetInt("LuminaFlow.LastLevel", 0);
            set { PlayerPrefs.SetInt("LuminaFlow.LastLevel", value); PlayerPrefs.Save(); }
        }
        public static bool SoundEnabled => saves == null || saves.Data.settings.sound;
        public static bool MusicEnabled => saves == null || saves.Data.settings.music;
        public static bool HapticsEnabled => saves == null || saves.Data.settings.haptics;
        public static void Initialize(LevelCatalog levels)
        {
            catalog = levels;
            saves = new SaveManager(Application.persistentDataPath);
            saves.Load();
        }
        public static bool IsUnlocked(int index)
        {
            if (catalog == null || index < 0 || index >= catalog.levels.Count) return false;
            return index == 0 || (saves != null && saves.GetStars(catalog.levels[index - 1].id) > 0);
        }
        public static int Stars(int id) => saves == null ? 0 : saves.GetStars(id);
        public static string Skin => saves == null ? "cyan" : saves.Data.settings.skin;
        public static bool SelectSkin(string id)
        {
            if (saves == null || !LaserSkins.IsUnlocked(id, TotalStars)) return false;
            saves.Data.settings.skin = id;
            Flush();
            return true;
        }
        public static int TotalStars => saves == null ? 0 : saves.Data.TotalStars;
        public static void Complete(int index, LevelData level, LaserResult result, int moves)
        {
            if (saves == null) return;
            int nextId = index + 1 < catalog.levels.Count ? catalog.levels[index + 1].id : 0;
            try { saves.RecordResult(level, result, moves, nextId); }
            catch (Exception exception) { Debug.LogWarning("Không lưu được tiến trình: " + exception.Message); }
        }
        public static void ToggleSound()
        {
            if (saves == null) return;
            saves.Data.settings.sound = !saves.Data.settings.sound;
            Flush();
        }
        public static void ToggleMusic()
        {
            if (saves == null) return;
            saves.Data.settings.music = !saves.Data.settings.music;
            Flush();
            AudioManager.Instance?.RefreshSettings();
        }
        public static void ToggleHaptics()
        {
            if (saves == null) return;
            saves.Data.settings.haptics = !saves.Data.settings.haptics;
            Flush();
        }
        public static void Flush()
        {
            if (saves == null) return;
            try { saves.Save(); }
            catch (Exception exception) { Debug.LogWarning("Không lưu được cài đặt: " + exception.Message); }
        }
    }
}
