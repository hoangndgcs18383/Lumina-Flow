using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LuminaFlow
{
    /// <summary>Encrypted local progress, authenticated before deserialization, with recoverable writes.</summary>
    public sealed class SaveManager
    {
        // Shipping clients contain this key. Encryption handles local privacy/corruption, not trusted scores.
        private const string DefaultSecret = "LuminaFlow/local-progress/v1/client-held-key";
        private const int MaxSaveBytes = 1024 * 1024;
        private readonly string secret;
        public string FilePath { get; private set; }
        public string BackupPath { get { return FilePath + ".bak"; } }
        public SaveData Data { get; private set; }
        public bool RecoveredFromBackup { get; private set; }
        public bool RecoveredFromTemporary { get; private set; }
        public bool HadCorruptSave { get; private set; }

        public SaveManager(string directory, string secret = null)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A persistent directory is required.", "directory");
            FilePath = Path.Combine(directory, "lumina-progress.dat");
            this.secret = secret ?? DefaultSecret;
            if (this.secret.Length == 0) throw new ArgumentException("A save key is required.", "secret");
            Data = new SaveData();
        }

        public SaveData Load()
        {
            RecoveredFromBackup = RecoveredFromTemporary = HadCorruptSave = false;
            SaveData loaded;
            if (TryRead(FilePath, out loaded)) { Data = loaded; return Data; }
            HadCorruptSave = File.Exists(FilePath);
            if (TryRead(BackupPath, out loaded))
            { Data = loaded; RecoveredFromBackup = true; Save(); return Data; }
            if (TryRead(FilePath + ".tmp", out loaded))
            { Data = loaded; RecoveredFromTemporary = true; Save(); return Data; }
            HadCorruptSave |= File.Exists(BackupPath) || File.Exists(FilePath + ".tmp");
            Data = new SaveData();
            return Data;
        }

        public void Save()
        {
            ValidateSave(Data);
            byte[] bytes = CryptoUtility.Encrypt(JsonCodec.Utf8.GetBytes(JsonCodec.Write(Data)), secret);
            if (bytes.Length > MaxSaveBytes) throw new InvalidOperationException("Progress data exceeds the save limit.");
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temporary = FilePath + ".tmp";
            // Complete and flush the new file before changing either known-good copy.
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            SaveData previous;
            bool validPrimary = TryRead(FilePath, out previous);
            if (File.Exists(FilePath))
            {
                try { File.Replace(temporary, FilePath, validPrimary ? BackupPath : null); }
                catch (PlatformNotSupportedException) { RecoverableReplace(temporary, validPrimary); }
                catch (NotSupportedException) { RecoverableReplace(temporary, validPrimary); }
            }
            else File.Move(temporary, FilePath);
            // Seed a backup on the first save; preserve good backups when repairing a bad primary.
            if (!TryRead(BackupPath, out previous)) File.Copy(FilePath, BackupPath, true);
        }

        public int GetStars(int id)
        { var progress = Data.levels.Find(p => p.levelId == id); return progress == null ? 0 : progress.stars; }
        public bool IsUnlocked(int id) { return id >= 1 && id <= Data.unlockedLevel; }

        public int RecordResult(LevelData level, LaserResult result, int moves, int nextLevelId = 0)
        {
            LevelSerializer.Validate(level);
            int stars = StarRating.Calculate(level, result, moves);
            if (stars == 0) return 0;
            var progress = Data.levels.Find(p => p.levelId == level.id);
            if (progress == null)
            { progress = new LevelProgress { levelId = level.id, stars = stars, bestMoves = moves }; Data.levels.Add(progress); }
            else
            { progress.stars = Math.Max(progress.stars, stars); progress.bestMoves = Math.Min(progress.bestMoves, moves); }
            int next = nextLevelId > 0 ? nextLevelId : level.id == int.MaxValue ? level.id : level.id + 1;
            Data.unlockedLevel = Math.Max(Data.unlockedLevel, next);
            Save();
            return stars;
        }

        private void RecoverableReplace(string temporary, bool validPrimary)
        {
            // Platforms lacking File.Replace still retain a backup across the rename gap.
            if (validPrimary) File.Copy(FilePath, BackupPath, true);
            File.Delete(FilePath);
            File.Move(temporary, FilePath);
        }

        private bool TryRead(string path, out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > MaxSaveBytes) return false;
                byte[] plain = CryptoUtility.Decrypt(File.ReadAllBytes(path), secret);
                object tree = JsonCodec.Read(JsonCodec.Utf8.GetString(plain));
                var map = tree as Dictionary<string, object>;
                if (map == null || !map.ContainsKey("version") || !map.ContainsKey("unlockedLevel") || !map.ContainsKey("levels") || !map.ContainsKey("settings"))
                    return false;
                data = JsonCodec.ConvertTo<SaveData>(tree);
                ValidateSave(data);
                return true;
            }
            catch (IOException) { return false; }
            catch (CryptographicException) { return false; }
            catch (FormatException) { return false; }
            catch (DecoderFallbackException) { return false; }
        }

        private static void ValidateSave(SaveData data)
        {
            if (data == null || data.version != 1 || data.unlockedLevel < 1 || data.levels == null || data.levels.Count > 10000 || data.settings == null)
                throw new FormatException("Invalid progress data.");
            var ids = new HashSet<int>();
            foreach (var level in data.levels)
                if (level == null || level.levelId < 1 || !ids.Add(level.levelId) || level.stars < 0 || level.stars > 3 || level.bestMoves < 0)
                    throw new FormatException("Invalid level progress.");
            if (string.IsNullOrWhiteSpace(data.settings.skin) || data.settings.skin.Length > 64)
                throw new FormatException("Invalid skin setting.");
        }
    }
}
