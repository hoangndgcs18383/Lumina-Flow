using System;
using System.Collections.Generic;

namespace LuminaFlow
{
    [Serializable]
    public sealed class LevelCatalog
    {
        public int version = 1;
        public List<LevelData> levels = new List<LevelData>();

        public LevelCatalog DeepClone()
        {
            var clone = new LevelCatalog { version = version, levels = new List<LevelData>() };
            if (levels != null)
                foreach (var level in levels) clone.levels.Add(level == null ? null : level.DeepClone());
            return clone;
        }
    }

    [Serializable]
    public sealed class LevelData
    {
        public int id;
        public string name = "";
        public string subtitle = "";
        public int width;
        public int height;
        public int par;
        public List<PieceData> pieces = new List<PieceData>();

        public LevelData DeepClone()
        {
            var clone = (LevelData)MemberwiseClone();
            clone.pieces = new List<PieceData>();
            if (pieces != null)
                foreach (var piece in pieces) clone.pieces.Add(piece == null ? null : piece.DeepClone());
            return clone;
        }
    }

    [Serializable]
    public sealed class PieceData
    {
        public string id = "";
        public string type = "";
        public int x;
        public int y;
        public int rotation;
        public string color = "";
        public string link = "";
        public bool @fixed;
        public int solution = -1;

        public bool CanRotate { get { return !@fixed && (type == "mirror" || type == "splitter"); } }
        public PieceData DeepClone() { return (PieceData)MemberwiseClone(); }
    }
}
