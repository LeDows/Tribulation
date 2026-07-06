using Tribulation.Core;
using Tribulation.Config;
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

        private LevelConfig levelConfig = LevelConfig.CreateDefault();
        private bool configured;

        private void Awake()
        {
            if (!configured)
            {
                Configure(GameConfigService.Config.GetSelectedCharacter(), GameConfigService.Config.level);
            }

            Health = MaxHealth;
        }

        private void Start()
        {
            GameManager.Instance.RegisterPlayer(this);
        }

        public void Configure(CharacterConfig character, LevelConfig level)
        {
            character ??= CharacterConfig.CreateDefault();
            levelConfig = level ?? LevelConfig.CreateDefault();
            Level = levelConfig.startLevel;
            Experience = 0;
            ExperienceToNextLevel = levelConfig.firstLevelExperience;
            MoveSpeed = character.moveSpeed;
            MaxHealth = character.maxHealth;
            Health = MaxHealth;
            DamageMultiplier = character.damageMultiplier;
            configured = true;
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
            ExperienceToNextLevel = Mathf.CeilToInt(
                ExperienceToNextLevel * levelConfig.experienceGrowthMultiplier + levelConfig.experienceGrowthFlat);
            MaxHealth += levelConfig.maxHealthPerLevel;
            Health = Mathf.Min(MaxHealth, Health + levelConfig.healOnLevelUp);
            MoveSpeed += levelConfig.moveSpeedPerLevel;
            DamageMultiplier += levelConfig.damageMultiplierPerLevel;
        }
    }
}
