using System.Collections.Generic;
using Tribulation.Core;
using Tribulation.Pickups;
using UnityEngine;

namespace Tribulation.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        private static readonly List<EnemyHealth> ActiveEnemyList = new();

        public float MaxHealth = 35f;
        public int ExperienceValue = 1;

        private float health;
        private float damageOverTimePerSecond;
        private float damageOverTimeRemaining;
        private EnemyHealthBar healthBar;

        public float Health => health;
        public float HealthFraction => MaxHealth > 0f ? Mathf.Clamp01(health / MaxHealth) : 0f;
        public float DamageOverTimePerSecond => damageOverTimePerSecond;
        public float DamageOverTimeRemaining => damageOverTimeRemaining;
        public int SpawnVersion { get; private set; }
        public static IReadOnlyList<EnemyHealth> ActiveEnemies => ActiveEnemyList;
        public static int ActiveCount => ActiveEnemyList.Count;

        private void Awake()
        {
            health = MaxHealth;
            healthBar = GetComponentInChildren<EnemyHealthBar>(true);
            healthBar?.ResetDisplay();
        }

        private void OnEnable()
        {
            if (!ActiveEnemyList.Contains(this))
            {
                ActiveEnemyList.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveEnemyList.Remove(this);
        }

        public void Configure(float maxHealth, int experienceValue)
        {
            MaxHealth = maxHealth;
            health = MaxHealth;
            ExperienceValue = experienceValue;
            damageOverTimePerSecond = 0f;
            damageOverTimeRemaining = 0f;
            SpawnVersion++;
            healthBar?.ResetDisplay();
        }

        private void Update()
        {
            if (!GameManager.IsSimulationRunning)
            {
                return;
            }

            TickDamageOverTime(Time.deltaTime);
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || health <= 0f)
            {
                return;
            }

            health = Mathf.Max(0f, health - amount);
            healthBar?.SetHealthFraction(HealthFraction);
            if (health > 0f)
            {
                return;
            }

            GameManager.Instance?.RegisterKill();
            ExperienceOrb.Spawn(transform.position, ExperienceValue);
            RuntimePrefabCatalog.ReleasePooled(gameObject, RuntimePrefabCatalog.Enemy);
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveEnemies()
        {
            ActiveEnemyList.Clear();
        }
    }
}
