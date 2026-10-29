using UnityEngine;

namespace LuminaFlow
{
    /// <summary>All art is generated in memory. No textures or prefabs are required.</summary>
    public static class ProceduralArt
    {
        private static Sprite disc, tile, glow, diamond;
        private static Material material, beamMaterial;
        public static Sprite Disc => disc != null ? disc : disc = MakeSprite(64, (x, y) => Mathf.Clamp01((.48f - new Vector2(x, y).magnitude) * 64));
        public static Sprite Glow => glow != null ? glow : glow = MakeSprite(96, (x, y) => Mathf.Pow(Mathf.Clamp01(1 - new Vector2(x, y).magnitude * 2), 2.5f));
        public static Sprite Tile => tile != null ? tile : tile = MakeSprite(64, (x, y) =>
        {
            var q = new Vector2(Mathf.Abs(x), Mathf.Abs(y)) - new Vector2(.36f, .36f);
            var distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - .12f;
            return Mathf.Clamp01(-distance * 64);
        });
        public static Sprite Diamond => diamond != null ? diamond : diamond = MakeSprite(64, (x, y) => Mathf.Clamp01((.48f - Mathf.Abs(x) - Mathf.Abs(y)) * 64));
        public static Material Material => material != null ? material : material = CreateMaterial();
        public static Material BeamMaterial
        {
            get
            {
                if (beamMaterial != null) return beamMaterial;
                beamMaterial = CreateMaterial();
                var texture = new Texture2D(4, 64, TextureFormat.RGBA32, false) { name = "Procedural soft laser", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                for (int y = 0; y < 64; y++) for (int x = 0; x < 4; x++)
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs((y + .5f) / 32 - 1)), 1.5f)));
                texture.Apply();
                beamMaterial.mainTexture = texture;
                return beamMaterial;
            }
        }
        private static Material CreateMaterial()
        {
            var shader = Resources.Load<Shader>("Lumina/NeonUnlit");
            if (shader == null) shader = Shader.Find("LuminaFlow/NeonUnlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return new Material(shader) { name = "Procedural neon unlit", hideFlags = HideFlags.HideAndDontSave };
        }
        private static Sprite MakeSprite(int size, System.Func<float, float, float> alpha)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha((x + .5f) / size - .5f, (y + .5f) / size - .5f)));
            texture.Apply();
            var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
        public static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 scale, Color color, int order = 4)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = Material;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }
        public static LineRenderer Line(Transform parent, string name, Vector3[] points, Color color, float width, int order = 4, bool loop = false, bool soft = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = soft ? BeamMaterial : Material;
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.numCapVertices = soft ? 0 : 6;
            line.numCornerVertices = 4;
            line.loop = loop;
            line.sortingOrder = order;
            return line;
        }
        public static LineRenderer Ring(Transform parent, float radius, Color color, float width = .035f, int order = 4)
        {
            var points = new Vector3[40];
            for (int i = 0; i < points.Length; i++) { float a = i * Mathf.PI * 2 / points.Length; points[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius; }
            return Line(parent, "Light ring", points, color, width, order, true);
        }
    }
}
