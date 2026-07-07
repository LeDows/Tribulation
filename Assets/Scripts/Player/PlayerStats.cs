using System.Globalization;
using Tribulation.Config;
using Tribulation.Core;
using UnityEngine;

namespace Tribulation.Player
{
    public sealed class PlayerStats : MonoBehaviour
    {
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public int ExperienceToNextLevel { get; private set; } = 6;
        public int UnspentAttributePoints { get; private set; }
        public string RealmDisplayName { get; private set; } = string.Empty;
        public string RealmLayerDisplay { get; private set; } = string.Empty;
        public float MoveSpeed { get; private set; } = 7f;
        public float MaxHealth { get; private set; } = 100f;
        public float Health { get; private set; }
        public float DamageMultiplier { get; private set; } = 1f;
        public float CritChance { get; private set; }
        public float DamageReduction { get; private set; }
        public float ExperienceGainMultiplier { get; private set; } = 1f;
        public float PickupRadiusBonus { get; private set; }
        public float RareRewardChanceBonus { get; private set; }

        public int SpiritPower => allocatedSpiritPower + realmSpiritPowerBonus;
        public int DivineSense => allocatedDivineSense + realmDivineSenseBonus;
        public int Root => allocatedRoot;
        public int Insight => allocatedInsight;
        public int Fortune => allocatedFortune;
        public int Agility => allocatedAgility;
        public int Will => allocatedWill;

        private LevelConfig levelConfig = LevelConfig.CreateDefault();
        private CultivationConfig cultivationConfig = CultivationConfig.CreateDefault();
        private float baseMoveSpeed = 7f;
        private float baseMaxHealth = 100f;
        private float baseDamageMultiplier = 1f;
        private float bonusMoveSpeed;
        private float bonusMaxHealth;
        private float bonusDamageMultiplier;
        private float experienceRemainder;
        private int allocatedSpiritPower;
        private int allocatedDivineSense;
        private int allocatedRoot;
        private int allocatedInsight;
        private int allocatedFortune;
        private int allocatedAgility;
        private int allocatedWill;
        private int realmSpiritPowerBonus;
        private int realmDivineSenseBonus;
        private float realmMaxHealthBonus;
        private bool configured;

        private void Awake()
        {
            if (!configured)
            {
                Configure(ConfigCenter.GetSelectedCharacter(), ConfigCenter.Level, ConfigCenter.Cultivation);
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPlayer(this);
            }
        }

        public void Configure(CharacterConfig character, LevelConfig level, CultivationConfig cultivation = null)
        {
            character ??= CharacterConfig.CreateDefault();
            levelConfig = level ?? LevelConfig.CreateDefault();
            cultivationConfig = cultivation ?? CultivationConfig.CreateDefault();

            Level = Mathf.Clamp(levelConfig.startLevel, 0, Mathf.Max(0, cultivationConfig.maxLevel));
            Experience = 0;
            ExperienceToNextLevel = Mathf.Max(1, levelConfig.firstLevelExperience);
            UnspentAttributePoints = 0;
            experienceRemainder = 0f;

            baseMoveSpeed = character.moveSpeed;
            baseMaxHealth = character.maxHealth;
            baseDamageMultiplier = character.damageMultiplier;
            bonusMoveSpeed = 0f;
            bonusMaxHealth = 0f;
            bonusDamageMultiplier = 0f;

            allocatedSpiritPower = 0;
            allocatedDivineSense = 0;
            allocatedRoot = 0;
            allocatedInsight = 0;
            allocatedFortune = 0;
            allocatedAgility = 0;
            allocatedWill = 0;

            RefreshRealmState();
            RecalculateDerivedStats(true);
            configured = true;
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0 || Level >= cultivationConfig.maxLevel)
            {
                return;
            }

            var scaledExperience = amount * ExperienceGainMultiplier + experienceRemainder;
            var gainedExperience = Mathf.FloorToInt(scaledExperience);
            experienceRemainder = scaledExperience - gainedExperience;
            if (gainedExperience <= 0)
            {
                return;
            }

            Experience += gainedExperience;

            while (Level < cultivationConfig.maxLevel && Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                LevelUp();
            }

            if (Level >= cultivationConfig.maxLevel)
            {
                Experience = Mathf.Min(Experience, Mathf.Max(0, ExperienceToNextLevel - 1));
            }
        }

        public void TakeDamage(float amount)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                return;
            }

            var reducedAmount = Mathf.Max(0f, amount) * (1f - DamageReduction);
            Health = Mathf.Max(0f, Health - reducedAmount);
            if (Health <= 0f && GameManager.Instance != null)
            {
                GameManager.Instance.EndRun();
            }
        }

        public bool AddAttributePoint(CultivationAttribute attribute)
        {
            if (UnspentAttributePoints <= 0)
            {
                return false;
            }

            switch (attribute)
            {
                case CultivationAttribute.SpiritPower:
                    allocatedSpiritPower++;
                    break;
                case CultivationAttribute.DivineSense:
                    allocatedDivineSense++;
                    break;
                case CultivationAttribute.Root:
                    allocatedRoot++;
                    break;
                case CultivationAttribute.Insight:
                    allocatedInsight++;
                    break;
                case CultivationAttribute.Fortune:
                    allocatedFortune++;
                    break;
                case CultivationAttribute.Agility:
                    allocatedAgility++;
                    break;
                case CultivationAttribute.Will:
                    allocatedWill++;
                    break;
                default:
                    return false;
            }

            UnspentAttributePoints--;
            RecalculateDerivedStats(false);
            return true;
        }

        public int GetAttributePoints(CultivationAttribute attribute)
        {
            return attribute switch
            {
                CultivationAttribute.SpiritPower => SpiritPower,
                CultivationAttribute.DivineSense => DivineSense,
                CultivationAttribute.Root => Root,
                CultivationAttribute.Insight => Insight,
                CultivationAttribute.Fortune => Fortune,
                CultivationAttribute.Agility => Agility,
                CultivationAttribute.Will => Will,
                _ => 0
            };
        }

        public void IncreaseMaxHealth(float amount, bool healByAmount)
        {
            var oldMaxHealth = MaxHealth;
            bonusMaxHealth += amount;
            RecalculateDerivedStats(false);

            if (healByAmount)
            {
                Heal(MaxHealth - oldMaxHealth);
            }
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + Mathf.Max(0f, amount));
        }

        public void AddMoveSpeed(float amount)
        {
            bonusMoveSpeed += amount;
            RecalculateDerivedStats(false);
        }

        public void AddDamageMultiplier(float amount)
        {
            bonusDamageMultiplier += amount;
            RecalculateDerivedStats(false);
        }

        private void LevelUp()
        {
            Level = Mathf.Min(cultivationConfig.maxLevel, Level + 1);
            ExperienceToNextLevel = Mathf.Max(
                1,
                Mathf.CeilToInt(ExperienceToNextLevel * levelConfig.experienceGrowthMultiplier + levelConfig.experienceGrowthFlat));

            RefreshRealmState();
            if (ShouldGrantAttributePoints())
            {
                UnspentAttributePoints += RollAttributePoints();
            }

            RecalculateDerivedStats(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RequestLevelUpSelection();
            }
        }

        private int RollAttributePoints()
        {
            var minimum = Mathf.Max(0, cultivationConfig.minAttributePointsPerLevel);
            var maximum = Mathf.Max(minimum, cultivationConfig.maxAttributePointsPerLevel);
            var points = Random.Range(minimum, maximum + 1);
            var extraPointChance = Mathf.Clamp01(Insight * cultivationConfig.insightExtraPointChancePerPoint);

            while (points < maximum && Random.value < extraPointChance)
            {
                points++;
            }

            return points;
        }

        private bool ShouldGrantAttributePoints()
        {
            var firstLevel = Mathf.Max(1, cultivationConfig.firstAttributePointLevel);
            var interval = Mathf.Max(1, cultivationConfig.attributePointLevelInterval);
            return Level >= firstLevel && (Level - firstLevel) % interval == 0;
        }

        private void RefreshRealmState()
        {
            if (Level <= 0)
            {
                RealmDisplayName = ConfigCenter.Text(cultivationConfig.initialRealmNameKey);
                RealmLayerDisplay = RealmDisplayName;
                realmSpiritPowerBonus = 0;
                realmDivineSenseBonus = 0;
                realmMaxHealthBonus = 0f;
                return;
            }

            var realm = cultivationConfig.GetRealmForLevel(Level);
            RealmDisplayName = ConfigCenter.Text(realm.displayNameKey);

            var levelOffset = Mathf.Max(0, Level - realm.startLevel);
            var layer = levelOffset / 10 + 1;
            var minor = Mathf.Abs(Level % 10);
            RealmLayerDisplay = ConfigCenter.Text(
                "cultivation.realm.layer_format",
                RealmDisplayName,
                GetLayerText(layer),
                minor.ToString(CultureInfo.InvariantCulture));

            var levelsInRealm = Mathf.Clamp(Level - realm.startLevel + 1, 0, realm.endLevel - realm.startLevel + 1);
            var completedTenLevelSteps = levelsInRealm / 10;
            realmSpiritPowerBonus = completedTenLevelSteps * realm.spiritPowerPerTen;
            realmDivineSenseBonus = completedTenLevelSteps * realm.divineSensePerTen;
            realmMaxHealthBonus = completedTenLevelSteps * realm.maxHealthPerTen;
        }

        private string GetLayerText(int layer)
        {
            var key = "cultivation.layer." + layer.ToString(CultureInfo.InvariantCulture);
            var localized = ConfigCenter.Text(key);
            return localized == key ? layer.ToString(CultureInfo.InvariantCulture) : localized;
        }

        private void RecalculateDerivedStats(bool healToFull)
        {
            var spiritPowerEffect = cultivationConfig.GetEffect(CultivationAttribute.SpiritPower);
            var divineSenseEffect = cultivationConfig.GetEffect(CultivationAttribute.DivineSense);
            var rootEffect = cultivationConfig.GetEffect(CultivationAttribute.Root);
            var insightEffect = cultivationConfig.GetEffect(CultivationAttribute.Insight);
            var fortuneEffect = cultivationConfig.GetEffect(CultivationAttribute.Fortune);
            var agilityEffect = cultivationConfig.GetEffect(CultivationAttribute.Agility);
            var willEffect = cultivationConfig.GetEffect(CultivationAttribute.Will);

            MaxHealth = Mathf.Max(1f, baseMaxHealth + bonusMaxHealth + realmMaxHealthBonus + Root * rootEffect.maxHealth);
            MoveSpeed = Mathf.Max(0f, baseMoveSpeed + bonusMoveSpeed + Agility * agilityEffect.moveSpeed);
            DamageMultiplier = Mathf.Max(0f, baseDamageMultiplier + bonusDamageMultiplier + SpiritPower * spiritPowerEffect.damageMultiplier);
            CritChance = Mathf.Clamp01(DivineSense * divineSenseEffect.critChance);
            PickupRadiusBonus = Mathf.Max(0f, DivineSense * divineSenseEffect.pickupRadius);
            ExperienceGainMultiplier = Mathf.Max(0f, 1f + Insight * insightEffect.experienceGain);
            RareRewardChanceBonus = Mathf.Max(0f, Fortune * fortuneEffect.rareRewardChance);
            DamageReduction = Mathf.Clamp(Will * willEffect.damageReduction, 0f, cultivationConfig.maxDamageReduction);

            Health = healToFull ? MaxHealth : Mathf.Min(Health, MaxHealth);
        }
    }
}
