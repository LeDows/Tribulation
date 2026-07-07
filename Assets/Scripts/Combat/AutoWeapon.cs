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

        public float CurrentDamage => BaseDamage * (stats != null ? stats.DamageMultiplier : 1f);

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            ApplyConfig(ConfigCenter.Weapon);
        }

        private void ApplyConfig(WeaponConfig config)
        {
            FireInterval = config.fireInterval;
            Range = config.range;
            BaseDamage = config.baseDamage;
            ProjectileSpeed = config.projectileSpeed;
            ProjectileLifetime = config.projectileLifetime;
            ProjectileScale = config.projectileScale;
            ProjectileName = ConfigCenter.Text(config.nameKey);
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
            var damage = CurrentDamage;
            if (stats != null && Random.value < stats.CritChance)
            {
                damage *= 2f;
            }

            projectile.Launch(
                targetPosition - transform.position,
                damage,
                ProjectileSpeed,
                ProjectileLifetime);
        }

        public void AddBaseDamage(float amount)
        {
            BaseDamage += amount;
        }

        public void ReduceFireIntervalPercent(float percent)
        {
            FireInterval *= Mathf.Clamp01(1f - percent);
            FireInterval = Mathf.Max(0.08f, FireInterval);
        }

        public void AddRange(float amount)
        {
            Range += amount;
        }

        public void AddProjectileSpeed(float amount)
        {
            ProjectileSpeed += amount;
        }

        public void AddProjectileScale(float amount)
        {
            ProjectileScale = Mathf.Max(0.05f, ProjectileScale + amount);
        }
    }
}
