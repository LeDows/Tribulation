using Tribulation.Core;
using Tribulation.Config;
using Tribulation.Player;
using UnityEngine;

namespace Tribulation.Pickups
{
    public sealed class ExperienceOrb : MonoBehaviour
    {
        public int Value = 1;
        public float MagnetRadius = 5f;
        public float MoveSpeed = 10f;

        public static void Spawn(Vector3 position, int value)
        {
            var config = ConfigCenter.Pickup;
            var parent = GameManager.Instance != null ? GameManager.Instance.RunRoot : null;
            var orbObject = RuntimePrefabCatalog.InstantiatePooled(RuntimePrefabCatalog.ExperienceOrb, parent);
            if (orbObject == null)
            {
                return;
            }

            orbObject.name = ConfigCenter.Text(config.nameKey);
            orbObject.transform.position = position + Vector3.up * 0.3f;
            orbObject.transform.localScale = Vector3.one * config.scale;
            if (orbObject.TryGetComponent<Renderer>(out var renderer))
            {
                RuntimePrefabCatalog.SetRendererColor(renderer, config.color);
            }

            var orb = orbObject.GetComponent<ExperienceOrb>();
            orb.Value = Mathf.Max(0, value);
            orb.MagnetRadius = config.magnetRadius;
            orb.MoveSpeed = config.moveSpeed;
        }

        private void Update()
        {
            if (!GameManager.IsSimulationRunning)
            {
                return;
            }

            var player = GameManager.Instance.Player;
            if (player == null)
            {
                return;
            }

            var toPlayer = player.transform.position - transform.position;
            var magnetRadius = MagnetRadius + player.PickupRadiusBonus;
            if (toPlayer.sqrMagnitude <= magnetRadius * magnetRadius)
            {
                transform.position += toPlayer.normalized * (MoveSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!GameManager.IsSimulationRunning)
            {
                return;
            }

            if (!other.TryGetComponent<PlayerStats>(out var player))
            {
                return;
            }

            player.AddExperience(Value);
            RuntimePrefabCatalog.ReleasePooled(gameObject, RuntimePrefabCatalog.ExperienceOrb);
        }

    }
}
