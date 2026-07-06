using Tribulation.Core;
using UnityEngine;

namespace Tribulation.Player
{
    public sealed class PlayerStats : MonoBehaviour
    {
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNextLevel { get; private set; } = 6;
        public float MoveSpeed { get; private set; } = 7f;
        public float MaxHealth { get; private set; } = 100f;
        public float Health { get; private set; }
        public float DamageMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            Health = MaxHealth;
        }

        private void Start()
        {
            GameManager.Instance.RegisterPlayer(this);
        }

        public void AddExperience(int amount)
        {
            Experience += amount;

            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                LevelUp();
            }
        }

        public void TakeDamage(float amount)
        {
            if (GameManager.Instance.IsGameOver)
            {
                return;
            }

            Health = Mathf.Max(0f, Health - amount);
            if (Health <= 0f)
            {
                GameManager.Instance.EndRun();
            }
        }

        private void LevelUp()
        {
            Level++;
            ExperienceToNextLevel = Mathf.CeilToInt(ExperienceToNextLevel * 1.24f + 2f);
            MaxHealth += 12f;
            Health = Mathf.Min(MaxHealth, Health + 28f);
            MoveSpeed += 0.12f;
            DamageMultiplier += 0.08f;
        }
    }
}
