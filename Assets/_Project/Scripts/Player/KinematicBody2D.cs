using System.Collections.Generic;
using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// Box-cast character mover (no dynamic forces). Used by the player and by walking/flying enemies.
    /// Move() resolves horizontal then vertical movement against solid and one-way layers and reports contacts.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class KinematicBody2D : MonoBehaviour
    {
        public LayerMask solidMask;
        public LayerMask oneWayMask;
        [Tooltip("Gap kept between the body and surfaces.")]
        public float skin = 0.02f;

        public bool Below { get; private set; }
        public bool Above { get; private set; }
        public bool Left { get; private set; }
        public bool Right { get; private set; }
        public Collider2D Ground { get; private set; }

        BoxCollider2D box;
        readonly List<RaycastHit2D> results = new List<RaycastHit2D>(8);

        BoxCollider2D Box
        {
            get
            {
                if (box == null) box = GetComponent<BoxCollider2D>();
                return box;
            }
        }

        void Reset()
        {
            solidMask = LayerMask.GetMask("Ground");
            oneWayMask = LayerMask.GetMask("OneWay");
        }

        static Vector2 Abs(Vector3 v) { return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y)); }

        public Vector2 Center
        {
            get { return (Vector2)transform.position + Vector2.Scale(Box.offset, Abs(transform.lossyScale)); }
        }

        public Vector2 Size
        {
            get { return Vector2.Scale(Box.size, Abs(transform.lossyScale)); }
        }

        bool IsOneWay(Collider2D c)
        {
            return ((oneWayMask.value >> c.gameObject.layer) & 1) != 0;
        }

        bool Cast(Vector2 origin, Vector2 size, Vector2 dir, float dist, bool allowOneWay, out RaycastHit2D best)
        {
            int mask = solidMask.value | (allowOneWay ? oneWayMask.value : 0);
            var filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(mask);

            results.Clear();
            Physics2D.BoxCast(origin, size, 0f, dir, filter, results, dist);

            best = default(RaycastHit2D);
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < results.Count; i++)
            {
                RaycastHit2D h = results[i];
                if (h.collider == null || h.distance >= bestDist) continue;
                if (IsOneWay(h.collider))
                {
                    // One-way platforms only block a downward move that starts fully above them.
                    if (dir.y >= 0f || h.distance <= 0.0001f || h.normal.y < 0.5f) continue;
                }
                best = h;
                bestDist = h.distance;
                found = true;
            }
            return found;
        }

        /// <summary>Moves by delta, stopping at obstacles. Updates Below/Above/Left/Right/Ground.</summary>
        public void Move(Vector2 delta, bool ignoreOneWay = false)
        {
            Below = Above = Left = Right = false;
            Ground = null;

            Vector2 pos = transform.position;
            Vector2 off = Center - pos;
            Vector2 size = Size;

            // Horizontal
            if (Mathf.Abs(delta.x) > 1e-6f)
            {
                float dir = Mathf.Sign(delta.x);
                Vector2 hSize = new Vector2(size.x, Mathf.Max(0.05f, size.y - 4f * skin));
                RaycastHit2D hit;
                if (Cast(pos + off, hSize, new Vector2(dir, 0f), Mathf.Abs(delta.x) + skin, false, out hit))
                {
                    pos.x += dir * Mathf.Max(0f, hit.distance - skin);
                    if (dir > 0f) Right = true; else Left = true;
                }
                else pos.x += delta.x;
            }

            // Vertical
            if (Mathf.Abs(delta.y) > 1e-6f)
            {
                float dir = Mathf.Sign(delta.y);
                Vector2 vSize = new Vector2(Mathf.Max(0.05f, size.x - 4f * skin), size.y);
                bool oneWay = dir < 0f && !ignoreOneWay;
                RaycastHit2D hit;
                if (Cast(pos + off, vSize, new Vector2(0f, dir), Mathf.Abs(delta.y) + skin, oneWay, out hit))
                {
                    pos.y += dir * Mathf.Max(0f, hit.distance - skin);
                    if (dir < 0f) { Below = true; Ground = hit.collider; }
                    else Above = true;
                }
                else pos.y += delta.y;
            }

            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }

        /// <summary>Checks for a surface within dist in a cardinal direction without moving.</summary>
        public bool Probe(Vector2 dir, float dist, bool allowOneWay, out Collider2D hitCollider)
        {
            Vector2 size = Size;
            Vector2 s = Mathf.Abs(dir.x) > 0f
                ? new Vector2(size.x, Mathf.Max(0.05f, size.y - 4f * skin))
                : new Vector2(Mathf.Max(0.05f, size.x - 4f * skin), size.y);
            RaycastHit2D hit;
            bool found = Cast(Center, s, dir, dist + skin, allowOneWay && dir.y < 0f, out hit);
            hitCollider = found ? hit.collider : null;
            return found;
        }

        /// <summary>If moving up by dy would hit a ceiling corner, nudge sideways to slip past it.</summary>
        public bool TryCornerCorrection(float dy, float maxShift)
        {
            if (dy <= 0f || maxShift <= 0f) return false;
            Vector2 c = Center;
            Vector2 s = Size - new Vector2(2f * skin, 2f * skin);
            int mask = solidMask.value;
            if (Physics2D.OverlapBox(c + new Vector2(0f, dy + skin), s, 0f, mask) == null) return false;

            for (float d = 0.05f; d <= maxShift + 0.001f; d += 0.05f)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 shift = new Vector2(side * d, 0f);
                    if (Physics2D.OverlapBox(c + shift, s, 0f, mask) != null) continue;
                    if (Physics2D.OverlapBox(c + shift + new Vector2(0f, dy + skin), s, 0f, mask) != null) continue;
                    transform.position += (Vector3)shift;
                    return true;
                }
            }
            return false;
        }

        /// <summary>True if there is ground just ahead of the feet (for ledge detection).</summary>
        public bool GroundAhead(int dir, float depth = 0.6f)
        {
            Vector2 size = Size;
            Vector2 origin = Center + new Vector2(dir * (size.x * 0.5f + 0.1f), -size.y * 0.5f + 0.05f);
            int mask = solidMask.value | oneWayMask.value;
            return Physics2D.Raycast(origin, Vector2.down, depth, mask).collider != null;
        }
    }
}
