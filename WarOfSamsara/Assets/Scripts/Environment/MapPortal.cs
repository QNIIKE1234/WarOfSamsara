using System;
using UnityEngine;

namespace WarOfSamsara.Environment
{
    /// <summary>
    /// MapleStory-style Portal.
    /// When player stands within portal trigger and presses 'Up', triggers map change event.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MapPortal : MonoBehaviour
    {
        [Header("Portal Settings")]
        [Tooltip("Unique ID of this portal within the current map.")]
        [SerializeField] private string portalId = "portal_0";

        [Tooltip("The ID of the target map to warp to.")]
        [SerializeField] private int targetMapId = 100000000;

        [Tooltip("The target portal name in the destination map.")]
        [SerializeField] private string targetPortalId = "portal_in";

        [Tooltip("Spawn offset when player arrives at this portal.")]
        [SerializeField] private Vector2 spawnOffset = Vector2.zero;

        public string PortalId => portalId;
        public int TargetMapId => targetMapId;
        public string TargetPortalId => targetPortalId;
        public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;

        public static event Action<MapPortal> OnPlayerEnterPortal;

        private bool _playerInside;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInside = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInside = false;
            }
        }

        /// <summary>
        /// Called by player controller when pressing the Up key while in portal range.
        /// </summary>
        public bool TryActivatePortal()
        {
            if (!_playerInside) return false;

            Debug.Log($"[MapPortal] Activating portal '{portalId}' -> Target Map: {targetMapId} ({targetPortalId})");
            OnPlayerEnterPortal?.Invoke(this);
            return true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1.0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.75f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(SpawnPosition, 0.2f);
        }
    }
}
