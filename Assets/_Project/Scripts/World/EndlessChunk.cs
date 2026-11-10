using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>
    /// A prefab slice for endless mode. Origin is the bottom-left corner. Every chunk must start and end on solid
    /// ground at the same height so any chunk can follow any other.
    /// </summary>
    public class EndlessChunk : MonoBehaviour
    {
        public float width = 24f;
        [Range(1, 5)] public int difficulty = 1;
        public bool isStartChunk;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + new Vector3(width * 0.5f, 7f, 0f), new Vector3(width, 14f, 0f));
        }
    }
}
