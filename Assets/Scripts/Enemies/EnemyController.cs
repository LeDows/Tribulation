using Tribulation.Core;
using UnityEngine;

namespace Tribulation.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyController : MonoBehaviour
    {
        public float MoveSpeed = 3.1f;
        public float ContactDamage = 8f;
        public float AttackInterval = 0.75f;

        private float attackCooldown;

        private void Update()
        {
            var player = GameManager.Instance.Player;
            if (player == null || GameManager.Instance.IsGameOver)
            {
                return;
            }

            var toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.01f)
            {
                transform.position += toPlayer.normalized * (MoveSpeed * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            }

            attackCooldown -= Time.deltaTime;
            if (toPlayer.sqrMagnitude <= 1.35f * 1.35f && attackCooldown <= 0f)
            {
                player.TakeDamage(ContactDamage);
                attackCooldown = AttackInterval;
            }
        }
    }
}
