using UnityEngine;
using UnityEngine.InputSystem;

namespace PlatformerTemplate
{
    /// <summary>
    /// F3 toggles a debug panel: player state, velocity, timers, FPS. Also draws the player's collision box
    /// (Gizmos must be enabled in the Game view). Editor and development builds only.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        bool show;
        GUIStyle style;
        float fps;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) show = !show;
            if (!show) return;

            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) fps = Mathf.Lerp(fps, 1f / dt, 0.1f);

            var p = PlayerController.Instance;
            if (p != null)
            {
                Vector2 c = p.Body.Center;
                Vector2 s = p.Body.Size;
                Color col = p.Grounded ? Color.green : Color.yellow;
                Vector3 a = new Vector3(c.x - s.x * 0.5f, c.y - s.y * 0.5f, 0f);
                Vector3 b = new Vector3(c.x + s.x * 0.5f, c.y - s.y * 0.5f, 0f);
                Vector3 d = new Vector3(c.x + s.x * 0.5f, c.y + s.y * 0.5f, 0f);
                Vector3 e = new Vector3(c.x - s.x * 0.5f, c.y + s.y * 0.5f, 0f);
                Debug.DrawLine(a, b, col); Debug.DrawLine(b, d, col);
                Debug.DrawLine(d, e, col); Debug.DrawLine(e, a, col);
            }
        }

        void OnGUI()
        {
            if (!show) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                style.normal.textColor = Color.white;
            }

            var gm = GameManager.Instance;
            var p = PlayerController.Instance;
            string text = "FPS " + fps.ToString("0") + "   State " + gm.State + "   Mode " + (gm.Mode != null ? gm.Mode.GetType().Name : "none");
            if (p != null)
            {
                text += "\nPlayer " + p.State + "   grounded " + p.Grounded + "   facing " + p.Facing
                     + "\nvelocity " + p.Velocity.x.ToString("0.0") + ", " + p.Velocity.y.ToString("0.0")
                     + "\ncoyote " + Mathf.Max(0f, p.CoyoteTimer).ToString("0.00") + "   buffer " + Mathf.Max(0f, p.BufferTimer).ToString("0.00")
                     + "\nair jumps " + p.ExtraJumpsLeft + "   dashes " + p.DashesLeft + "   shots " + p.RangedEnergy.ToString("0.0")
                     + "\nhealth " + p.Health.Current + "/" + p.Health.maxHealth + "   abilities " + p.Abilities.Current;
            }

            GUI.Box(new Rect(8, 8, 520, p != null ? 140 : 40), GUIContent.none);
            GUI.Label(new Rect(16, 12, 510, 140), text, style);
        }
    }
}
