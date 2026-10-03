using System.Collections;
using UnityEngine;

namespace WarOfSamsara.Environment
{
    /// <summary>
    /// One-way platform allowing players to jump up through and drop down with Down + Jump.
    /// Works with PlatformEffector2D or directly handles player drop-through ignore collision.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class OneWayPlatform : MonoBehaviour
    {
        private Collider2D _platformCollider;

        private void Awake()
        {
            _platformCollider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// Temporarily ignores collision with the player collider so the player falls through.
        /// </summary>
        public void DropThrough(Collider2D playerCollider, float ignoreDuration = 0.35f)
        {
            StartCoroutine(DropThroughRoutine(playerCollider, ignoreDuration));
        }

        private IEnumerator DropThroughRoutine(Collider2D playerCollider, float duration)
        {
            if (playerCollider == null || _platformCollider == null) yield break;

            Physics2D.IgnoreCollision(playerCollider, _platformCollider, true);
            yield return new WaitForSeconds(duration);
            
            if (playerCollider != null && _platformCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, _platformCollider, false);
            }
        }
    }
}
