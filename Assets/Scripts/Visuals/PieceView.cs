using UnityEngine;
#if LUMINA_DOTWEEN
using DG.Tweening;
#endif

namespace LuminaFlow
{
    public sealed class PieceView : MonoBehaviour
    {
        private PieceData piece;
        private Transform rotor;
        private SpriteRenderer glow, center;
        private LineRenderer ring;
        private Color tint;
        private bool lit;
        private int shownRotation;
        public void Initialize(PieceData data)
        {
            piece = data;
            tint = NeonPalette.Beam(data.color);
            rotor = new GameObject("Optical element").transform;
            rotor.SetParent(transform, false);
            string type = (data.type ?? "wall").ToLowerInvariant();
            glow = ProceduralArt.Sprite(transform, "Aura", ProceduralArt.Glow, Vector2.zero, Vector2.one * 1.4f, NeonPalette.Alpha(tint, .1f), 1);
            switch (type)
            {
                case "mirror": case "splitter":
                    ProceduralArt.Ring(transform, .29f, NeonPalette.Alpha(type == "splitter" ? NeonPalette.Pink : NeonPalette.Cyan, .3f), .018f);
                    var mirrorColor = type == "splitter" ? NeonPalette.Pink : NeonPalette.Text;
                    ProceduralArt.Line(rotor, "Mirror glow", new[] { new Vector3(-.25f, -.25f), new Vector3(.25f, .25f) }, NeonPalette.Alpha(mirrorColor, .4f), .16f, 5, false, true);
                    ProceduralArt.Line(rotor, "Reflective face", new[] { new Vector3(-.24f, -.24f), new Vector3(.24f, .24f) }, NeonPalette.HDR(mirrorColor, 1.5f), .055f, 7);
                    ProceduralArt.Sprite(rotor, "Pivot", ProceduralArt.Disc, Vector2.zero, Vector2.one * .1f, mirrorColor, 8);
                    if (type == "splitter") ProceduralArt.Sprite(rotor, "Prism", ProceduralArt.Diamond, Vector2.zero, Vector2.one * .22f, NeonPalette.Pink, 8);
                    break;
                case "emitter":
                    ProceduralArt.Ring(transform, .25f, tint, .035f);
                    center = ProceduralArt.Sprite(transform, "Energy source", ProceduralArt.Disc, Vector2.zero, Vector2.one * .27f, NeonPalette.HDR(tint, 2), 6);
                    ProceduralArt.Line(rotor, "Direction", new[] { new Vector3(.15f, .13f), new Vector3(.34f, 0), new Vector3(.15f, -.13f) }, tint, .05f, 7);
                    break;
                case "target": case "core":
                    ring = ProceduralArt.Ring(transform, .3f, tint, .045f);
                    ProceduralArt.Ring(transform, .21f, NeonPalette.Alpha(tint, .4f), .018f);
                    center = ProceduralArt.Sprite(transform, "Target core", ProceduralArt.Disc, Vector2.zero, Vector2.one * .22f, NeonPalette.Alpha(tint, .3f), 6);
                    break;
                case "crystal":
                    center = ProceduralArt.Sprite(transform, "Crystal", ProceduralArt.Diamond, Vector2.zero, new Vector2(.43f, .63f), NeonPalette.Alpha(tint, .6f), 6);
                    ProceduralArt.Line(transform, "Crystal facet", new[] { new Vector3(0, -.22f), new Vector3(.1f, 0), new Vector3(0, .22f) }, Color.white, .017f, 7);
                    break;
                case "portal":
                    ring = ProceduralArt.Ring(transform, .3f, tint, .05f);
                    ProceduralArt.Ring(transform, .19f, NeonPalette.Alpha(tint, .5f), .025f);
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = (i * 120 + 30) * Mathf.Deg2Rad;
                        ProceduralArt.Sprite(transform, "Portal mote", ProceduralArt.Disc, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .3f, Vector2.one * .08f, Color.white, 7);
                    }
                    break;
                case "sensor":
                    ring = ProceduralArt.Ring(transform, .25f, tint, .03f);
                    center = ProceduralArt.Sprite(transform, "Sensor", ProceduralArt.Diamond, Vector2.zero, Vector2.one * .26f, NeonPalette.Alpha(tint, .4f), 6);
                    break;
                case "door":
                    center = ProceduralArt.Sprite(transform, "Door", ProceduralArt.Tile, Vector2.zero, new Vector2(.7f, .48f), NeonPalette.Alpha(tint, .65f), 5);
                    for (int i = -1; i <= 1; i++) ProceduralArt.Sprite(transform, "Door slot", ProceduralArt.Tile, new Vector2(i * .18f, 0), new Vector2(.035f, .3f), NeonPalette.Background, 6);
                    break;
                case "filter": case "colorfilter":
                    center = ProceduralArt.Sprite(rotor, "Filter glass", ProceduralArt.Diamond, Vector2.zero, new Vector2(.42f, .72f), NeonPalette.Alpha(tint, .8f), 5);
                    ProceduralArt.Line(rotor, "Glass edge", new[] { new Vector3(0, -.29f), new Vector3(.15f, 0), new Vector3(0, .29f) }, tint, .03f, 6);
                    break;
                default:
                    glow.enabled = false;
                    ProceduralArt.Sprite(transform, "Wall", ProceduralArt.Tile, Vector2.zero, Vector2.one * .72f, NeonPalette.Hex("29364F"), 4);
                    ProceduralArt.Line(transform, "Wall seam", new[] { new Vector3(-.18f, -.18f), new Vector3(.18f, .18f) }, NeonPalette.Hex("46536A"), .035f, 5);
                    break;
            }
            if (data.@fixed && (type == "mirror" || type == "splitter"))
                ProceduralArt.Sprite(transform, "Fixed rivet", ProceduralArt.Tile, new Vector2(.3f, -.3f), Vector2.one * .08f, NeonPalette.Muted, 8);
            shownRotation = data.rotation;
            rotor.localRotation = Quaternion.Euler(0, 0, -shownRotation * 90);
        }
        public void Refresh(LaserResult result)
        {
            lit = result.LitTargets.Contains(piece.id) || result.LitCrystals.Contains(piece.id) || result.ActiveSensors.Contains(piece.id) || result.OpenDoors.Contains(piece.id);
            if (center != null) center.color = NeonPalette.Alpha(NeonPalette.HDR(tint, lit ? 2 : 1), lit ? 1 : .5f);
            if (ring != null) ring.startColor = ring.endColor = NeonPalette.HDR(tint, lit ? 1.8f : 1);
            if (string.Equals(piece.type, "door", System.StringComparison.OrdinalIgnoreCase) && center != null)
                center.color = NeonPalette.Alpha(tint, lit ? .13f : .65f);
#if LUMINA_DOTWEEN
            if (shownRotation != piece.rotation) { rotor.DOKill(); rotor.DOLocalRotate(new Vector3(0, 0, -piece.rotation * 90), .16f).SetEase(Ease.OutCubic); }
#endif
            shownRotation = piece.rotation;
        }
        private void Update()
        {
#if !LUMINA_DOTWEEN
            if (rotor != null) rotor.localRotation = Quaternion.Slerp(rotor.localRotation, Quaternion.Euler(0, 0, -shownRotation * 90), 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
#endif
            if (glow != null && glow.enabled)
            {
                var color = glow.color;
                color.a = (lit ? .65f : .15f) * (.9f + Mathf.Sin(Time.unscaledTime * 2 + transform.position.x) * .1f);
                glow.color = color;
            }
        }
    }
}
