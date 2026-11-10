using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Self-contained 2D follow camera (no Cinemachine needed): dead zone, look-ahead, look-down,
    /// vertical damping that favours landing, room confinement, screen shake and optional pixel snapping.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float orthographicSize = 7f;

        [Header("Follow")]
        public float smoothTimeX = 0.12f;
        public float smoothTimeY = 0.18f;
        public Vector2 deadZone = new Vector2(1.5f, 1.8f);
        public float lookAhead = 2f;
        public float lookAheadSmoothing = 0.25f;
        public float lookDownOffset = 3f;

        [Header("Pixel snapping")]
        public bool pixelSnap = false;
        public float pixelsPerUnit = 16f;

        Camera cam;
        Vector2 smoothPos;
        Vector2 smoothVel;
        float lookX, lookXVel;
        Bounds bounds;
        bool hasBounds;
        float shakeIntensity, shakeDuration, shakeTime;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            smoothPos = transform.position;
        }

        void OnEnable()
        {
            GameEvents.RoomEntered += OnRoomEntered;
            GameEvents.ShakeRequested += OnShake;
        }

        void OnDisable()
        {
            GameEvents.RoomEntered -= OnRoomEntered;
            GameEvents.ShakeRequested -= OnShake;
        }

        void Start()
        {
            if (Room.Current != null) { bounds = Room.Current.WorldBounds; hasBounds = true; }
            SnapToTarget();
        }

        void OnRoomEntered(Room room)
        {
            bounds = room.WorldBounds;
            hasBounds = true;
        }

        void OnShake(float intensity, float duration)
        {
            if (GameSettings.ScreenShake <= 0f) return;
            if (intensity >= shakeIntensity * (shakeTime / Mathf.Max(0.01f, shakeDuration)))
            {
                shakeIntensity = intensity;
                shakeDuration = duration;
                shakeTime = duration;
            }
        }

        Vector2 Clamp(Vector2 p)
        {
            if (!hasBounds) return p;
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = bounds.min.x + halfW, maxX = bounds.max.x - halfW;
            float minY = bounds.min.y + halfH, maxY = bounds.max.y - halfH;
            p.x = minX > maxX ? bounds.center.x : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? bounds.center.y : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }

        /// <summary>Jumps the camera straight to the player (spawn, respawn, room teleports).</summary>
        public void SnapToTarget()
        {
            if (target == null && PlayerController.Instance != null) target = PlayerController.Instance.transform;
            if (target == null) return;
            smoothPos = Clamp(target.position);
            smoothVel = Vector2.zero;
            lookX = 0f;
            Apply(Vector2.zero);
        }

        void LateUpdate()
        {
            if (target == null)
            {
                if (PlayerController.Instance == null) return;
                target = PlayerController.Instance.transform;
                SnapToTarget();
            }

            var pc = PlayerController.Instance;
            float dt = Time.deltaTime;
            if (dt <= 0f) { Apply(Vector2.zero); return; }

            // Look-ahead (keeps its last value while standing still)
            if (pc != null && Mathf.Abs(pc.Velocity.x) > 0.5f)
            {
                float desired = pc.Facing * lookAhead;
                lookX = Mathf.SmoothDamp(lookX, desired, ref lookXVel, lookAheadSmoothing);
            }

            float lookY = 0f;
            if (pc != null && pc.Grounded && pc.LookInput.y < -0.6f && Mathf.Abs(pc.LookInput.x) < 0.3f) lookY = -lookDownOffset;

            Vector2 focus = (Vector2)target.position + new Vector2(lookX, lookY);
            Vector2 desiredPos = smoothPos;

            float dx = focus.x - smoothPos.x;
            if (Mathf.Abs(dx) > deadZone.x * 0.5f) desiredPos.x = focus.x - Mathf.Sign(dx) * deadZone.x * 0.5f;
            float dy = focus.y - smoothPos.y;
            if (Mathf.Abs(dy) > deadZone.y * 0.5f) desiredPos.y = focus.y - Mathf.Sign(dy) * deadZone.y * 0.5f;
            if (lookY < 0f) desiredPos.y = focus.y;

            desiredPos = Clamp(desiredPos);

            // Slow vertical follow while rising (avoids bobbing); fast when falling.
            float smoothY = smoothTimeY;
            if (pc != null)
            {
                if (!pc.Grounded && pc.Velocity.y > 0.5f) smoothY *= 1.6f;
                else if (pc.Velocity.y < -10f) smoothY *= 0.4f;
            }

            smoothPos.x = Mathf.SmoothDamp(smoothPos.x, desiredPos.x, ref smoothVel.x, smoothTimeX);
            smoothPos.y = Mathf.SmoothDamp(smoothPos.y, desiredPos.y, ref smoothVel.y, smoothY);

            // Shake
            Vector2 shake = Vector2.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= dt;
                float k = Mathf.Clamp01(shakeTime / Mathf.Max(0.01f, shakeDuration));
                shake = Random.insideUnitCircle * shakeIntensity * k * GameSettings.ScreenShake;
            }
            else shakeIntensity = 0f;

            Apply(shake);
        }

        void Apply(Vector2 shake)
        {
            Vector2 p = smoothPos + shake;
            if (pixelSnap && pixelsPerUnit > 0f)
            {
                p.x = Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit;
                p.y = Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit;
            }
            transform.position = new Vector3(p.x, p.y, -10f);
        }
    }
}
