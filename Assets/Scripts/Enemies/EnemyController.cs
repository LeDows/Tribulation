using Tribulation.Core;
using Tribulation.Config;
using UnityEngine;

namespace Tribulation.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyController : MonoBehaviour
    {
        public float MoveSpeed = 3.1f;
        public float ContactDamage = 8f;
        public float AttackInterval = 0.75f;
        public float AttackRange = 1.35f;

        private float attackCooldown;

        public void Configure(EnemyConfig config)
        {
            MoveSpeed = Random.Range(config.minMoveSpeed, config.maxMoveSpeed) +
                GameManager.Instance.RunTime / Mathf.Max(0.01f, config.speedDifficultySeconds);
            ContactDamage = config.contactDamage;
            AttackInterval = config.attackInterval;
            AttackRange = config.attackRange;
        }

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
            if (toPlayer.sqrMagnitude <= AttackRange * AttackRange && attackCooldown <= 0f)
            {
                player.TakeDamage(ContactDamage);
                attackCooldown = AttackInterval;
            }
        }
    }
}
