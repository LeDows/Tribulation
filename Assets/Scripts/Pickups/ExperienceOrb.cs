using Tribulation.Core;
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
            var orbObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbObject.name = "Spirit Qi";
            orbObject.transform.position = position + Vector3.up * 0.3f;
            orbObject.transform.localScale = Vector3.one * 0.35f;
            orbObject.GetComponent<Renderer>().material.color = new Color(0.35f, 1f, 0.7f);

            var collider = orbObject.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            var orb = orbObject.AddComponent<ExperienceOrb>();
            orb.Value = value;
        }

        private void Update()
        {
            var player = GameManager.Instance.Player;
            if (player == null)
            {
                return;
            }

            var toPlayer = player.transform.position - transform.position;
            if (toPlayer.sqrMagnitude <= MagnetRadius * MagnetRadius)
            {
                transform.position += toPlayer.normalized * (MoveSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerStats>(out var player))
            {
                return;
            }

            player.AddExperience(Value);
            Destroy(gameObject);
        }
    }
}
