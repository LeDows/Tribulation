using System;
using System.Collections.Generic;
using Tribulation.Enemies;
using Tribulation.Player;
using UnityEngine;

namespace Tribulation.Combat
{
    public sealed class Projectile : MonoBehaviour
    {
        private Vector3 direction;
        private float damage;
        private float speed;
        private float lifeTime = 2.2f;
        private int remainingHits = 1;
        private float enemyImpactRadius;
        private Action<EnemyHealth> enemyHitHandler;
        private ProjectileTarget target = ProjectileTarget.Enemy;
        private readonly HashSet<EnemyHealth> hitEnemies = new();

        public void Launch(Vector3 travelDirection, float hitDamage, float travelSpeed, float duration, int maxHits = 1)
        {
            Launch(travelDirection, hitDamage, travelSpeed, duration, maxHits, 0f, null);
        }

        public void Launch(
            Vector3 travelDirection,
            float hitDamage,
            float travelSpeed,
            float duration,
            int maxHits,
            float impactRadius,
            Action<EnemyHealth> onEnemyHit)
        {
            ConfigureMotion(travelDirection, hitDamage, travelSpeed, duration);
            target = ProjectileTarget.Enemy;
            remainingHits = Mathf.Max(1, maxHits);
            enemyImpactRadius = Mathf.Max(0f, impactRadius);
            enemyHitHandler = onEnemyHit;
        }

        public void LaunchAgainstPlayer(Vector3 travelDirection, float hitDamage, float travelSpeed, float duration)
        {
            ConfigureMotion(travelDirection, hitDamage, travelSpeed, duration);
            target = ProjectileTarget.Player;
            remainingHits = 1;
            enemyImpactRadius = 0f;
            enemyHitHandler = null;
        }

        private void ConfigureMotion(Vector3 travelDirection, float hitDamage, float travelSpeed, float duration)
        {
            direction = travelDirection.normalized;
            SetDamage(hitDamage);
            speed = travelSpeed;
            lifeTime = duration;
            hitEnemies.Clear();
        }

        public void SetDamage(float hitDamage)
        {
            damage = hitDamage;
        }

        private void Update()
        {
            transform.position += direction * (speed * Time.deltaTime);
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (target == ProjectileTarget.Player)
            {
                if (other.TryGetComponent<PlayerStats>(out var player))
                {
                    player.TakeDamage(damage);
                    Destroy(gameObject);
                }

                return;
            }

            if (!other.TryGetComponent<EnemyHealth>(out var enemy) || !HitEnemy(enemy))
            {
                return;
            }

            if (enemyImpactRadius > 0f)
            {
                DamageEnemiesInRadius(transform.position, enemyImpactRadius);
                Destroy(gameObject);
                return;
            }

            remainingHits--;
            if (remainingHits <= 0)
            {
                Destroy(gameObject);
            }
        }

        private bool HitEnemy(EnemyHealth enemy)
        {
            if (enemy == null || !hitEnemies.Add(enemy))
            {
                return false;
            }

            if (enemyHitHandler != null)
            {
                enemyHitHandler(enemy);
            }
            else
            {
                enemy.TakeDamage(damage);
            }

            return true;
        }

        private void DamageEnemiesInRadius(Vector3 center, float radius)
        {
            var radiusSqr = radius * radius;
            var enemies = UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null && (enemy.transform.position - center).sqrMagnitude <= radiusSqr)
                {
                    HitEnemy(enemy);
                }
            }
        }

        private enum ProjectileTarget
        {
            Enemy,
            Player
        }
    }
}
