using Tribulation.Enemies;
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

        private PlayerStats stats;
        private float cooldown;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
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
            var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "Flying Sword";
            projectileObject.transform.position = transform.position + Vector3.up * 0.7f;
            projectileObject.transform.localScale = Vector3.one * 0.28f;
            projectileObject.GetComponent<Renderer>().material.color = new Color(0.8f, 0.95f, 1f);

            var collider = projectileObject.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            var body = projectileObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var projectile = projectileObject.AddComponent<Projectile>();
            projectile.Launch(targetPosition - transform.position, BaseDamage * stats.DamageMultiplier, ProjectileSpeed);
        }
    }
}
