using Tribulation.Core;
using UnityEngine;

namespace Tribulation.Enemies
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        public float SpawnInterval = 1.25f;
        public float SpawnRadius = 16f;
        public int MaxEnemies = 90;

        private float timer;

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

            var pressure = Mathf.Clamp01(GameManager.Instance.RunTime / 180f);
            timer = Mathf.Lerp(SpawnInterval, 0.28f, pressure);
        }

        private void SpawnEnemy()
        {
            var playerPosition = GameManager.Instance.Player.transform.position;
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var position = playerPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SpawnRadius;
            position.y = 1f;

            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "Hungry Spirit";
            enemyObject.transform.position = position;
            enemyObject.GetComponent<Renderer>().material.color = new Color(0.75f, 0.16f, 0.22f);

            var collider = enemyObject.GetComponent<CapsuleCollider>();
            collider.radius = 0.45f;

            var health = enemyObject.AddComponent<EnemyHealth>();
            var difficulty = 1f + GameManager.Instance.RunTime / 90f;
            health.Configure(30f * difficulty, Random.value < 0.15f ? 2 : 1);

            var controller = enemyObject.AddComponent<EnemyController>();
            controller.MoveSpeed = Random.Range(2.6f, 3.8f) + GameManager.Instance.RunTime / 240f;
        }
    }
}
