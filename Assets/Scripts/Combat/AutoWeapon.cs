using Tribulation.Enemies;
using Tribulation.Config;
using Tribulation.Core;
using Tribulation.Player;
using UnityEngine;

namespace Tribulation.Combat
{
    [RequireComponent(typeof(PlayerStats))]
    public sealed class AutoWeapon : MonoBehaviour
    {
        public float FireInterval = 0.45f;
        public float Range = 14f;
        public float BaseDamage = 18f;
        public float ProjectileSpeed = 18f;
        public float ProjectileLifetime = 2.2f;
        public float ProjectileScale = 0.28f;
        public string ProjectileName = "Flying Sword";
        public Color ProjectileColor = new(0.8f, 0.95f, 1f, 1f);

        private PlayerStats stats;
        private float cooldown;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            ApplyConfig(GameConfigService.Config.weapon);
        }

        private void ApplyConfig(WeaponConfig config)
        {
            FireInterval = config.fireInterval;
            Range = config.range;
            BaseDamage = config.baseDamage;
            ProjectileSpeed = config.projectileSpeed;
            ProjectileLifetime = config.projectileLifetime;
            ProjectileScale = config.projectileScale;
            ProjectileName = config.projectileName;
            ProjectileColor = config.projectileColor;
        }

        private void Update()
        {
            cooldown -= Time.deltaTime;
            if (cooldown > 0f)
            {
                return;
            }

            var target = FindNearestEnemy();
            if (target == null)
            {
                return;
            }

            FireAt(target.transform.position);
            cooldown = FireInterval;
        }

        private EnemyHealth FindNearestEnemy()
        {
            EnemyHealth nearest = null;
            var nearestDistance = Range * Range;
            var enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

            foreach (var enemy in enemies)
            {
                var distance = (enemy.transform.position - transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private void FireAt(Vector3 targetPosition)
        {
            var projectileObject = RuntimePrefabCatalog.Instantiate(RuntimePrefabCatalog.Projectile, GameManager.Instance.RunRoot);
            projectileObject.name = ProjectileName;
            projectileObject.transform.position = transform.position + Vector3.up * 0.7f;
            projectileObject.transform.localScale = Vector3.one * ProjectileScale;
            if (projectileObject.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = ProjectileColor;
            }

            var projectile = projectileObject.GetComponent<Projectile>();
            projectile.Launch(
                targetPosition - transform.position,
                BaseDamage * stats.DamageMultiplier,
                ProjectileSpeed,
                ProjectileLifetime);
        }
    }
}
