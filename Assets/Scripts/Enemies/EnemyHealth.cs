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

        public float Health => health;
        public float HealthFraction => MaxHealth > 0f ? Mathf.Clamp01(health / MaxHealth) : 0f;

        private void Awake()
        {
            health = MaxHealth;
        }

        public void Configure(float maxHealth, int experienceValue)
        {
            MaxHealth = maxHealth;
            health = MaxHealth;
            ExperienceValue = experienceValue;
        }

        public void TakeDamage(float amount)
        {
            health = Mathf.Max(0f, health - amount);
            if (health > 0f)
            {
                return;
            }

            GameManager.Instance.RegisterKill();
            ExperienceOrb.Spawn(transform.position, ExperienceValue);
            Destroy(gameObject);
        }
    }
}
