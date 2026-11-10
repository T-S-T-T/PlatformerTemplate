using UnityEngine;

namespace PlatformerTemplate
{
    /// <summary>Marks where the player starts in a scene.</summary>
    public class PlayerSpawn : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.9f, 1.45f, 0f));
        }
    }
}
