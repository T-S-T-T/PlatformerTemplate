using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Runtime-generated sprites for code-spawned objects (FX, fallback projectiles) so they work with no art.</summary>
    public static class PlaceholderSprites
    {
        static Sprite square;

        /// <summary>A 1x1 unit white square.</summary>
        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    var px = new Color32[16];
                    for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(px);
                    tex.Apply();
                    square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                    square.name = "PlaceholderSquare";
                }
                return square;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { square = null; }
    }
}
