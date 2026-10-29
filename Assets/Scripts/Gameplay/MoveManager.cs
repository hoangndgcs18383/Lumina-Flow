using System;
using System.Collections.Generic;

namespace LuminaFlow
{
    /// <summary>Owns a mutable play copy; catalog data and the reset snapshot are never edited.</summary>
    public sealed class MoveManager
    {
        private struct Move { public string id; public int before; }
        private readonly LevelData initial;
        private readonly Stack<Move> history = new Stack<Move>();
        public LevelData Level { get; private set; }
        public LaserResult Result { get; private set; }
        public int MoveCount { get { return history.Count; } }
        public bool CanUndo { get { return history.Count > 0; } }
        public event Action Changed;

        public MoveManager(LevelData level)
        {
            LevelSerializer.Validate(level);
            initial = level.DeepClone();
            Reset();
        }

        public bool Rotate(string id, int quarterTurns = 1)
        {
            var piece = Level.pieces.Find(p => p.id == id);
            int turns = ((quarterTurns % 4) + 4) % 4;
            if (piece == null || !piece.CanRotate || turns == 0) return false;
            history.Push(new Move { id = id, before = piece.rotation });
            piece.rotation = (piece.rotation + turns) % 4;
            Refresh();
            return true;
        }

        public bool Undo()
        {
            if (!CanUndo) return false;
            Move move = history.Pop();
            Level.pieces.Find(p => p.id == move.id).rotation = move.before;
            Refresh();
            return true;
        }

        public void Reset()
        {
            Level = initial.DeepClone();
            history.Clear();
            Refresh();
        }

        private void Refresh()
        {
            Result = LaserEngine.Evaluate(Level);
            var changed = Changed;
            if (changed != null) changed();
        }
    }
}
