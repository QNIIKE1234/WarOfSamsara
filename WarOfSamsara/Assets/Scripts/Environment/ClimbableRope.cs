using UnityEngine;

namespace MapleMMO.Environment
{
    /// <summary>
    /// Represents a ladder or rope that players can grab, climb up/down, and jump off from.
    /// Attach this to a GameObject with a 2D BoxCollider (IsTrigger = true).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ClimbableRope : MonoBehaviour
    {
        [Header("Rope Settings")]
        [Tooltip("The X position the player snaps to when climbing this rope/ladder.")]
        [SerializeField] private bool autoCalculateSnapX = true;
        [SerializeField] private float customSnapX;

        private BoxCollider2D _collider;

        public float SnapX => autoCalculateSnapX ? transform.position.x : customSnapX;
        public float TopY => _collider.bounds.max.y;
        public float BottomY => _collider.bounds.min.y;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
            if (_collider == null) return;

            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(_collider.bounds.center, _collider.bounds.size);
            
            // Draw climb axis line
            Gizmos.color = Color.yellow;
            float x = SnapX;
            Gizmos.DrawLine(new Vector3(x, _collider.bounds.min.y, 0), new Vector3(x, _collider.bounds.max.y, 0));
        }
    }
}
