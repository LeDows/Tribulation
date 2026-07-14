using Tribulation.Core;
using Tribulation.Pickups;
using UnityEngine;

namespace Tribulation.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        public float MaxHealth = 35f;
        public int ExperienceValue = 1;

        private float health;
        private float damageOverTimePerSecond;
        private float damageOverTimeRemaining;

        public float Health => health;
        public float HealthFraction => MaxHealth > 0f ? Mathf.Clamp01(health / MaxHealth) : 0f;
        public float DamageOverTimePerSecond => damageOverTimePerSecond;
        public float DamageOverTimeRemaining => damageOverTimeRemaining;

        private void Awake()
        {
            health = MaxHealth;
        }

        public void Configure(float maxHealth, int experienceValue)
        {
            MaxHealth = maxHealth;
            health = MaxHealth;
            ExperienceValue = experienceValue;
            damageOverTimePerSecond = 0f;
            damageOverTimeRemaining = 0f;
        }

        private void Update()
        {
            TickDamageOverTime(Time.deltaTime);
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || health <= 0f)
            {
                return;
            }

            health = Mathf.Max(0f, health - amount);
            if (health > 0f)
            {
                return;
            }

            GameManager.Instance.RegisterKill();
            ExperienceOrb.Spawn(transform.position, ExperienceValue);
            Destroy(gameObject);
        }

        public void ApplyDamageOverTime(float damagePerSecond, float duration)
        {
            if (damagePerSecond <= 0f || duration <= 0f || health <= 0f)
            {
                return;
            }

            damageOverTimePerSecond = damagePerSecond;
            damageOverTimeRemaining = duration;
        }

        private void TickDamageOverTime(float deltaTime)
        {
            if (deltaTime <= 0f || damageOverTimePerSecond <= 0f || damageOverTimeRemaining <= 0f || health <= 0f)
            {
                return;
            }

            var activeTime = Mathf.Min(deltaTime, damageOverTimeRemaining);
            damageOverTimeRemaining = Mathf.Max(0f, damageOverTimeRemaining - activeTime);
            TakeDamage(damageOverTimePerSecond * activeTime);

            if (damageOverTimeRemaining <= 0f)
            {
                damageOverTimePerSecond = 0f;
            }
        }
    }
}
