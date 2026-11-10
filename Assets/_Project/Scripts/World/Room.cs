using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// The unit of level design: a trigger volume that confines the camera while the player is inside.
    /// Level mode uses one big room per level, metroidvania links many, endless uses none (free camera).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Room : MonoBehaviour
    {
        public string roomId;

        public static Room Current { get; private set; }

        BoxCollider2D box;

        public Bounds WorldBounds
        {
            get
            {
                if (box == null) box = GetComponent<BoxCollider2D>();
                Vector3 s = transform.lossyScale;
                Vector3 center = transform.TransformPoint(box.offset);
                center.z = 0f;
                return new Bounds(center, new Vector3(box.size.x * Mathf.Abs(s.x), box.size.y * Mathf.Abs(s.y), 100f));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Current = null; }

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (Current == this) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            Vector3 p = player.transform.position;
            p.z = 0f;
            if (!WorldBounds.Contains(p)) return;
            Enter();
        }

        void Enter()
        {
            Current = this;
            var mode = GameManager.Instance.Mode;
            if (mode != null && mode.PersistProgress && !string.IsNullOrEmpty(roomId)
                && !SaveSystem.Current.visitedRooms.Contains(roomId))
            {
                SaveSystem.Current.visitedRooms.Add(roomId);
            }
            if (GameEvents.RoomEntered != null) GameEvents.RoomEntered(this);
        }

        void OnDrawGizmos()
        {
            var b = GetComponent<BoxCollider2D>();
            if (b == null) return;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
            Bounds wb = WorldBounds;
            Gizmos.DrawWireCube(wb.center, new Vector3(wb.size.x, wb.size.y, 0f));
        }
    }
}
