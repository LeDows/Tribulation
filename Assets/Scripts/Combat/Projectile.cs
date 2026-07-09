using System.Collections.Generic;
using Tribulation.Enemies;
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
        private readonly HashSet<EnemyHealth> hitEnemies = new();

        public void Launch(Vector3 travelDirection, float hitDamage, float travelSpeed, float duration, int maxHits = 1)
        {
            direction = travelDirection.normalized;
            SetDamage(hitDamage);
            speed = travelSpeed;
            lifeTime = duration;
            remainingHits = Mathf.Max(1, maxHits);
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
            if (!other.TryGetComponent<EnemyHealth>(out var enemy) || hitEnemies.Contains(enemy))
            {
                return;
            }

            hitEnemies.Add(enemy);
            enemy.TakeDamage(damage);
            remainingHits--;
            if (remainingHits <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
