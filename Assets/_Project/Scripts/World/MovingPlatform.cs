using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Platform that oscillates between its start and start + travel. Runs before the player so the player
    /// can read FrameDelta and ride along. Put the collider on the Ground (or OneWay) layer.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MovingPlatform : MonoBehaviour
    {
        public Vector2 travel = new Vector2(4f, 0f);
        public float speed = 2f;
        public float waitTime = 0.3f;

        public Vector2 FrameDelta { get; private set; }

        Vector3 start;
        float t;
        bool returning;
        float wait;

        void Awake()
        {
            start = transform.position;
        }

        void Update()
        {
            FrameDelta = Vector2.zero;
            if (GameManager.Instance.State != GameState.Playing) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (wait > 0f) { wait -= dt; return; }
            float length = travel.magnitude;
            if (length < 0.01f) return;

            t += (speed / length) * dt * (returning ? -1f : 1f);
            if (t >= 1f) { t = 1f; returning = true; wait = waitTime; }
            else if (t <= 0f) { t = 0f; returning = false; wait = waitTime; }

            Vector3 target = start + (Vector3)(travel * t);
            FrameDelta = target - transform.position;
            transform.position = target;
            Physics2D.SyncTransforms();
        }

        void OnDrawGizmosSelected()
        {
            Vector3 a = Application.isPlaying ? start : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(a, a + (Vector3)travel);
        }
    }
}
