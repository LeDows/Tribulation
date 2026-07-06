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

        public void Launch(Vector3 travelDirection, float hitDamage, float travelSpeed, float duration)
        {
            direction = travelDirection.normalized;
            damage = hitDamage;
            speed = travelSpeed;
            lifeTime = duration;
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
            if (!other.TryGetComponent<EnemyHealth>(out var enemy))
            {
                return;
            }

            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
