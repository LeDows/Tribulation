using Tribulation.Core;
using Tribulation.Config;
using UnityEngine;

namespace Tribulation.Enemies
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        public float SpawnInterval = 1.25f;
        public float MinimumSpawnInterval = 0.28f;
        public float PressureRampSeconds = 180f;
        public float SpawnRadius = 16f;
        public int MaxEnemies = 90;

        private float timer;
        private string enemyId = "hungry_spirit";

        public void Configure(SpawnConfig config)
        {
            if (config == null)
            {
                return;
            }

            enemyId = config.enemyId;
            SpawnInterval = config.interval;
            MinimumSpawnInterval = config.minimumInterval;
            PressureRampSeconds = config.pressureRampSeconds;
            SpawnRadius = config.radius;
            MaxEnemies = config.maxEnemies;
        }

        private void Update()
        {
            if (GameManager.Instance.Player == null || GameManager.Instance.IsGameOver)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            var enemyCount = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length;
            if (enemyCount < MaxEnemies)
            {
                SpawnEnemy();
            }

            var pressure = Mathf.Clamp01(GameManager.Instance.RunTime / Mathf.Max(0.01f, PressureRampSeconds));
            timer = Mathf.Lerp(SpawnInterval, MinimumSpawnInterval, pressure);
        }

        private void SpawnEnemy()
        {
            var enemy = GameConfigService.Config.GetEnemy(enemyId);
            var playerPosition = GameManager.Instance.Player.transform.position;
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var position = playerPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SpawnRadius;
            position.y = 1f;

            var enemyObject = RuntimePrefabCatalog.Instantiate(RuntimePrefabCatalog.Enemy, GameManager.Instance.RunRoot);
            enemyObject.name = enemy.displayName;
            enemyObject.transform.position = position;
            if (enemyObject.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = enemy.color;
            }

            if (enemyObject.TryGetComponent<CapsuleCollider>(out var collider))
            {
                collider.radius = enemy.colliderRadius;
            }

            var health = enemyObject.GetComponent<EnemyHealth>();
            var difficulty = 1f + GameManager.Instance.RunTime / Mathf.Max(0.01f, enemy.healthDifficultySeconds);
            var experience = Random.value < enemy.eliteChance ? enemy.eliteExperienceValue : enemy.experienceValue;
            health.Configure(enemy.maxHealth * difficulty, experience);

            var controller = enemyObject.GetComponent<EnemyController>();
            controller.Configure(enemy);
        }
    }
}
