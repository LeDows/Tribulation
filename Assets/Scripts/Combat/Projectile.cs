using System;
using System.Collections.Generic;
using Tribulation.Core;
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
        private Action<EnemyHealth, float> enemyHitHandler;
        private ProjectileTarget target = ProjectileTarget.Enemy;
        private readonly Dictionary<EnemyHealth, int> hitEnemyVersions = new();
        private bool launched;

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
            Action<EnemyHealth, float> onEnemyHit)
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
            hitEnemyVersions.Clear();
            launched = true;
        }

        public void SetDamage(float hitDamage)
        {
            damage = hitDamage;
        }

        private void Update()
        {
            if (!launched || !GameManager.IsSimulationRunning)
            {
                return;
            }

            transform.position += direction * (speed * Time.deltaTime);
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                Despawn();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!launched || !GameManager.IsSimulationRunning)
            {
                return;
            }

            if (target == ProjectileTarget.Player)
            {
                if (other.TryGetComponent<PlayerStats>(out var player))
                {
                    player.TakeDamage(damage);
                    Despawn();
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
                Despawn();
                return;
            }

            remainingHits--;
            if (remainingHits <= 0)
            {
                Despawn();
            }
        }

        private bool HitEnemy(EnemyHealth enemy)
        {
            if (enemy == null ||
                hitEnemyVersions.TryGetValue(enemy, out var hitVersion) && hitVersion == enemy.SpawnVersion)
            {
                return false;
            }

            hitEnemyVersions[enemy] = enemy.SpawnVersion;

            if (enemyHitHandler != null)
            {
                enemyHitHandler(enemy, damage);
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
            var enemies = EnemyHealth.ActiveEnemies;
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy != null && (enemy.transform.position - center).sqrMagnitude <= radiusSqr)
                {
                    HitEnemy(enemy);
                }
            }
        }

        private void Despawn()
        {
            if (!launched)
            {
                return;
            }

            launched = false;
            enemyHitHandler = null;
            RuntimePrefabCatalog.ReleasePooled(gameObject, RuntimePrefabCatalog.Projectile);
        }

        private void OnDisable()
        {
            launched = false;
            enemyHitHandler = null;
            hitEnemyVersions.Clear();
        }

        private enum ProjectileTarget
        {
            Enemy,
            Player
        }
    }
}
