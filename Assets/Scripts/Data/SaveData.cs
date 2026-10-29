using System;
using System.Collections.Generic;

namespace LuminaFlow
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int unlockedLevel = 1;
        public List<LevelProgress> levels = new List<LevelProgress>();
        public SaveSettings settings = new SaveSettings();

        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (var level in levels) total += level.stars;
                return total;
            }
        }
    }

    [Serializable]
    public sealed class LevelProgress
    {
        public int levelId;
        public int stars;
        public int bestMoves;
    }

    [Serializable]
    public sealed class SaveSettings
    {
        public bool music = true;
        public bool sound = true;
        public bool haptics = true;
        public string skin = "cyan";
    }
}
