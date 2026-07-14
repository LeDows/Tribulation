using System;
using System.Collections.Generic;
using Tribulation.Core;
using Tribulation.Config;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Tribulation.Enemies
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        public float SpawnInterval = 1.25f;
        public float MinimumSpawnInterval = 0.28f;
        public float PressureRampSeconds = 180f;
        public float SpawnRadius = 16f;
        public int MaxEnemies = 90;
        public int ProjectilePoolSize = 48;
        public int ExperienceOrbPoolSize = 32;
        public int PrewarmBatchSize = 4;

        private float timer;
        private EnemyConfig[] enemyPool = Array.Empty<EnemyConfig>();
        private int prewarmStage;

        public void Configure(SpawnConfig config)
        {
            if (config == null)
            {
                return;
            }

            enemyPool = BuildEnemyPool(config);
            SpawnInterval = config.interval;
            MinimumSpawnInterval = config.minimumInterval;
            PressureRampSeconds = config.pressureRampSeconds;
            SpawnRadius = config.radius;
            MaxEnemies = config.maxEnemies;
            ProjectilePoolSize = config.projectilePoolSize;
            ExperienceOrbPoolSize = config.experienceOrbPoolSize;
            PrewarmBatchSize = config.prewarmBatchSize;
            prewarmStage = 0;
        }

        private void Update()
        {
            var manager = GameManager.Instance;
            if (manager == null || manager.State != GameState.Running || manager.Player == null)
            {
                return;
            }

            PrewarmPools(manager.RunRoot);

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            if (EnemyHealth.ActiveCount < MaxEnemies)
            {
                SpawnEnemy(manager);
            }

            var pressure = Mathf.Clamp01(manager.RunTime / Mathf.Max(0.01f, PressureRampSeconds));
            timer = Mathf.Lerp(SpawnInterval, MinimumSpawnInterval, pressure);
        }

        private void PrewarmPools(Transform parent)
        {
            switch (prewarmStage)
            {
                case 0:
                    if (RuntimePrefabCatalog.PrewarmStep(
                        RuntimePrefabCatalog.Enemy,
                        parent,
                        Mathf.Max(0, MaxEnemies),
                        PrewarmBatchSize))
                    {
                        prewarmStage++;
                    }
                    break;
                case 1:
                    if (RuntimePrefabCatalog.PrewarmStep(
                        RuntimePrefabCatalog.Projectile,
                        parent,
                        Mathf.Max(0, ProjectilePoolSize),
                        PrewarmBatchSize))
                    {
                        prewarmStage++;
                    }
                    break;
                case 2:
                    if (RuntimePrefabCatalog.PrewarmStep(
                        RuntimePrefabCatalog.ExperienceOrb,
                        parent,
                        Mathf.Max(0, ExperienceOrbPoolSize),
                        PrewarmBatchSize))
                    {
                        prewarmStage++;
                    }
                    break;
            }
        }

        private void SpawnEnemy(GameManager manager)
        {
            var enemy = PickEnemy();
            if (enemy == null || manager == null || manager.Player == null)
            {
                return;
            }

            var playerPosition = manager.Player.transform.position;
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var position = playerPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SpawnRadius;
            position.y = 1f;

            var enemyObject = RuntimePrefabCatalog.InstantiatePooled(RuntimePrefabCatalog.Enemy, manager.RunRoot);
            if (enemyObject == null)
            {
                return;
            }

            enemyObject.name = ConfigCenter.Text(enemy.displayNameKey);
            enemyObject.transform.position = position;
            if (enemyObject.TryGetComponent<Renderer>(out var renderer))
            {
                RuntimePrefabCatalog.SetRendererColor(renderer, enemy.color);
            }

            if (enemyObject.TryGetComponent<CapsuleCollider>(out var collider))
            {
                collider.radius = enemy.colliderRadius;
            }

            var health = enemyObject.GetComponent<EnemyHealth>();
            var player = manager.Player;
            var eliteChance = Mathf.Clamp01(enemy.eliteChance + (player != null ? player.RareRewardChanceBonus : 0f));
            var experience = Random.value < eliteChance ? enemy.eliteExperienceValue : enemy.experienceValue;
            health.Configure(enemy.maxHealth, experience);

            var controller = enemyObject.GetComponent<EnemyController>();
            controller.Configure(enemy);
        }

        private static EnemyConfig[] BuildEnemyPool(SpawnConfig config)
        {
            var pool = new List<EnemyConfig>();
            var ids = config.GetEnemyIds();
            foreach (var id in ids)
            {
                var enemy = FindEnemy(id);
                if (enemy == null)
                {
                    Debug.LogWarning($"Spawn pool references unknown enemy id '{id}'.");
                    continue;
                }

                pool.Add(enemy);
            }

            if (pool.Count == 0)
            {
                pool.Add(ConfigCenter.GetEnemy(config.enemyId));
            }

            return pool.ToArray();
        }

        private static EnemyConfig FindEnemy(string id)
        {
            foreach (var enemy in ConfigCenter.Enemies)
            {
                if (enemy != null && string.Equals(enemy.id, id, StringComparison.Ordinal))
                {
                    return enemy;
                }
            }

            return null;
        }

        private EnemyConfig PickEnemy()
        {
            if (enemyPool.Length == 0)
            {
                return ConfigCenter.GetEnemy("hungry_spirit");
            }

            var totalWeight = 0f;
            foreach (var enemy in enemyPool)
            {
                if (enemy != null)
                {
                    totalWeight += Mathf.Max(0f, enemy.spawnWeight);
                }
            }

            if (totalWeight <= 0f)
            {
                return enemyPool[0];
            }

            var roll = Random.value * totalWeight;
            EnemyConfig lastPositive = null;
            foreach (var enemy in enemyPool)
            {
                if (enemy == null || enemy.spawnWeight <= 0f)
                {
                    continue;
                }

                lastPositive = enemy;
                roll -= enemy.spawnWeight;
                if (roll <= 0f)
                {
                    return enemy;
                }
            }

            return lastPositive ?? enemyPool[0];
        }
    }
}
