using System;
using System.Collections.Generic;

namespace LuminaFlow
{
    [Serializable]
    public sealed class LaserSegment
    {
        public float x1, y1, x2, y2;
        public string color;

        public LaserSegment(float x1, float y1, float x2, float y2, string color)
        {
            this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2; this.color = color;
        }
    }

    public sealed class LaserResult
    {
        public readonly List<LaserSegment> Segments = new List<LaserSegment>();
        public readonly HashSet<string> LitTargets = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> LitCrystals = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> ActiveSensors = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> OpenDoors = new HashSet<string>(StringComparer.Ordinal);
        public bool Won;
    }

    /// <summary>Deterministic grid optics. Directions are right, down, left, up; y increases down.</summary>
    public static class LaserEngine
    {
        private static readonly int[] Dx = { 1, 0, -1, 0 };
        private static readonly int[] Dy = { 0, 1, 0, -1 };

        private struct Beam : IEquatable<Beam>
        {
            public int x, y, direction;
            public string color;
            public Beam(int x, int y, int direction, string color)
            { this.x = x; this.y = y; this.direction = direction; this.color = color; }
            public bool Equals(Beam other)
            { return x == other.x && y == other.y && direction == other.direction && color == other.color; }
            public override bool Equals(object other) { return other is Beam && Equals((Beam)other); }
            public override int GetHashCode()
            { unchecked { return (((x * 397) ^ y) * 397 ^ direction) * 397 ^ color.GetHashCode(); } }
        }

        public static LaserResult Evaluate(LevelData level)
        {
            LevelSerializer.Validate(level);
            var cells = new PieceData[level.width, level.height];
            var portals = new Dictionary<string, PieceData>(StringComparer.Ordinal);
            var partners = new Dictionary<string, PieceData>(StringComparer.Ordinal);
            int targets = 0;
            foreach (var piece in level.pieces)
            {
                cells[piece.x, piece.y] = piece;
                if (piece.type == "target") targets++;
                if (piece.type != "portal") continue;
                PieceData first;
                if (portals.TryGetValue(piece.link, out first))
                { partners.Add(first.id, piece); partners.Add(piece.id, first); }
                else portals.Add(piece.link, piece);
            }

            // Reset on every evaluation. Opening doors can expose further sensors, never close a door.
            var links = new HashSet<string>(StringComparer.Ordinal);
            var sensors = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                var result = Trace(level, cells, partners, links);
                sensors.UnionWith(result.ActiveSensors);
                int oldCount = links.Count;
                foreach (var piece in level.pieces)
                    if (piece.type == "sensor" && sensors.Contains(piece.id)) links.Add(piece.link);
                if (links.Count != oldCount) continue;
                result.ActiveSensors.UnionWith(sensors);
                foreach (var piece in level.pieces)
                    if (piece.type == "door" && links.Contains(piece.link)) result.OpenDoors.Add(piece.id);
                result.Won = targets > 0 && result.LitTargets.Count == targets;
                return result;
            }
        }

        private static LaserResult Trace(LevelData level, PieceData[,] cells,
            Dictionary<string, PieceData> partners, HashSet<string> openLinks)
        {
            var result = new LaserResult();
            var queue = new Queue<Beam>();
            var visited = new HashSet<Beam>();
            foreach (var piece in level.pieces)
                if (piece.type == "emitter") queue.Enqueue(new Beam(piece.x, piece.y, piece.rotation, piece.color));

            while (queue.Count > 0)
            {
                Beam beam = queue.Dequeue();
                if (!visited.Add(beam)) continue;
                int x = beam.x, y = beam.y, d = beam.direction;
                // Trace to the next occupied cell or board edge; empty cells need no allocations.
                PieceData hit;
                do
                {
                    x += Dx[d]; y += Dy[d];
                    if (x < 0 || y < 0 || x >= level.width || y >= level.height)
                    {
                        result.Segments.Add(new LaserSegment(beam.x, beam.y,
                            x - Dx[d] * 0.5f, y - Dy[d] * 0.5f, beam.color));
                        goto NextBeam;
                    }
                    hit = cells[x, y];
                } while (hit == null);

                result.Segments.Add(new LaserSegment(beam.x, beam.y, x, y, beam.color));
                switch (hit.type)
                {
                    case "mirror":
                        queue.Enqueue(new Beam(x, y, Reflect(d, hit.rotation), beam.color));
                        break;
                    case "splitter":
                        queue.Enqueue(new Beam(x, y, d, beam.color));
                        queue.Enqueue(new Beam(x, y, Reflect(d, hit.rotation), beam.color));
                        break;
                    case "portal":
                        PieceData exit = partners[hit.id];
                        queue.Enqueue(new Beam(exit.x, exit.y, d, beam.color));
                        break;
                    case "target":
                        if (Matches(hit.color, beam.color)) result.LitTargets.Add(hit.id);
                        break;
                    case "crystal":
                        if (Matches(hit.color, beam.color)) result.LitCrystals.Add(hit.id);
                        queue.Enqueue(new Beam(x, y, d, beam.color));
                        break;
                    case "filter":
                        queue.Enqueue(new Beam(x, y, d, hit.color));
                        break;
                    case "sensor":
                        if (Matches(hit.color, beam.color)) result.ActiveSensors.Add(hit.id);
                        queue.Enqueue(new Beam(x, y, d, beam.color));
                        break;
                    case "door":
                        if (openLinks.Contains(hit.link)) queue.Enqueue(new Beam(x, y, d, beam.color));
                        break;
                    // Walls, emitters, and closed doors absorb incoming beams.
                }
                NextBeam:;
            }
            return result;
        }

        private static int Reflect(int direction, int rotation)
        { return rotation % 2 == 0 ? 3 - direction : direction ^ 1; }
        private static bool Matches(string required, string actual)
        { return string.IsNullOrEmpty(required) || required == actual; }
    }

    public static class StarRating
    {
        public static int Calculate(LevelData level, LaserResult result, int moves)
        {
            if (level == null || result == null) throw new ArgumentNullException();
            if (moves < 0) throw new ArgumentOutOfRangeException("moves");
            if (!result.Won) return 0;
            bool allCrystals = true;
            foreach (var piece in level.pieces)
                if (piece.type == "crystal" && !result.LitCrystals.Contains(piece.id)) allCrystals = false;
            return 1 + (allCrystals ? 1 : 0) + (moves <= level.par ? 1 : 0);
        }
    }
}
