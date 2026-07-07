using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tribulation.Tests.EditMode
{
    public sealed class CultivationSystemTests
    {
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void RealmDisplayUsesInitialRealmForLevelZero()
        {
            var stats = CreateStats(0);

            Assert.AreEqual(0, Get<int>(stats, "Level"));
            Assert.AreEqual("引气入体", Get<string>(stats, "RealmDisplayName"));
            Assert.AreEqual("引气入体", Get<string>(stats, "RealmLayerDisplay"));
        }

        [Test]
        public void DefaultLevelConfigStartsAtInitialRealm()
        {
            var stats = CreateStats(CreateDefault("Tribulation.Config.LevelConfig"), CreateDefault("Tribulation.Config.CultivationConfig"));

            Assert.AreEqual(0, Get<int>(stats, "Level"));
            Assert.AreEqual("\u5f15\u6c14\u5165\u4f53", Get<string>(stats, "RealmLayerDisplay"));
        }

        [Test]
        public void RealmDisplayMatchesGddExampleAtLevel456()
        {
            var stats = CreateStats(456);

            Assert.AreEqual("化神", Get<string>(stats, "RealmDisplayName"));
            Assert.AreEqual("化神六层(6)", Get<string>(stats, "RealmLayerDisplay"));
        }

        [Test]
        public void LevelDoesNotAdvancePastConfiguredCap()
        {
            var level = CreateDefault("Tribulation.Config.LevelConfig");
            SetField(level, "startLevel", 998);
            SetField(level, "firstLevelExperience", 1);
            SetField(level, "experienceGrowthMultiplier", 1f);
            SetField(level, "experienceGrowthFlat", 0);

            var stats = CreateStats(level, CreateDefault("Tribulation.Config.CultivationConfig"));
            Invoke(stats, "AddExperience", 50);

            Assert.AreEqual(999, Get<int>(stats, "Level"));
            Assert.Less(Get<int>(stats, "Experience"), Get<int>(stats, "ExperienceToNextLevel"));
        }

        [Test]
        public void AttributePointsAreGrantedEveryTenLevelsStartingAtLevelOne()
        {
            var cultivation = CreateDefault("Tribulation.Config.CultivationConfig");
            SetField(cultivation, "firstAttributePointLevel", 1);
            SetField(cultivation, "attributePointLevelInterval", 10);
            SetField(cultivation, "minAttributePointsPerLevel", 7);
            SetField(cultivation, "maxAttributePointsPerLevel", 7);

            var level = CreateDefault("Tribulation.Config.LevelConfig");
            SetField(level, "startLevel", 0);
            SetField(level, "firstLevelExperience", 1);
            SetField(level, "experienceGrowthMultiplier", 1f);
            SetField(level, "experienceGrowthFlat", 0);

            var stats = CreateStats(level, cultivation);
            Invoke(stats, "AddExperience", 2);

            Assert.AreEqual(2, Get<int>(stats, "Level"));
            Assert.AreEqual(7, Get<int>(stats, "UnspentAttributePoints"));

            Invoke(stats, "AddExperience", 9);

            Assert.AreEqual(11, Get<int>(stats, "Level"));
            Assert.AreEqual(14, Get<int>(stats, "UnspentAttributePoints"));
        }

        [Test]
        public void RealmBonusesAccumulateEveryTenLevelsWithinCurrentRealm()
        {
            var stats = CreateStats(456);

            Assert.AreEqual(75, Get<int>(stats, "SpiritPower"));
            Assert.AreEqual(40, Get<int>(stats, "DivineSense"));
            Assert.AreEqual(2100f, Get<float>(stats, "MaxHealth"));
        }

        [Test]
        public void AttributePointsUpdateDerivedStats()
        {
            var cultivation = CreateDefault("Tribulation.Config.CultivationConfig");
            SetField(cultivation, "minAttributePointsPerLevel", 7);
            SetField(cultivation, "maxAttributePointsPerLevel", 7);

            var level = CreateDefault("Tribulation.Config.LevelConfig");
            SetField(level, "startLevel", 0);
            SetField(level, "firstLevelExperience", 1);

            var stats = CreateStats(level, cultivation);
            Invoke(stats, "AddExperience", 1);

            Assert.IsTrue(AddAttributePoint(stats, "SpiritPower"));
            Assert.IsTrue(AddAttributePoint(stats, "DivineSense"));
            Assert.IsTrue(AddAttributePoint(stats, "Root"));
            Assert.IsTrue(AddAttributePoint(stats, "Insight"));
            Assert.IsTrue(AddAttributePoint(stats, "Fortune"));
            Assert.IsTrue(AddAttributePoint(stats, "Agility"));
            Assert.IsTrue(AddAttributePoint(stats, "Will"));

            Assert.AreEqual(1.01f, Get<float>(stats, "DamageMultiplier"), 0.0001f);
            Assert.AreEqual(0.005f, Get<float>(stats, "CritChance"), 0.0001f);
            Assert.AreEqual(0.2f, Get<float>(stats, "PickupRadiusBonus"), 0.0001f);
            Assert.AreEqual(110f, Get<float>(stats, "MaxHealth"), 0.0001f);
            Assert.AreEqual(1.01f, Get<float>(stats, "ExperienceGainMultiplier"), 0.0001f);
            Assert.AreEqual(0.01f, Get<float>(stats, "RareRewardChanceBonus"), 0.0001f);
            Assert.AreEqual(7.05f, Get<float>(stats, "MoveSpeed"), 0.0001f);
            Assert.AreEqual(0.005f, Get<float>(stats, "DamageReduction"), 0.0001f);
        }

        [Test]
        public void DamageReductionIsCappedAtConfiguredMaximum()
        {
            var cultivation = CreateDefault("Tribulation.Config.CultivationConfig");
            SetField(cultivation, "minAttributePointsPerLevel", 200);
            SetField(cultivation, "maxAttributePointsPerLevel", 200);

            var level = CreateDefault("Tribulation.Config.LevelConfig");
            SetField(level, "startLevel", 0);
            SetField(level, "firstLevelExperience", 1);

            var stats = CreateStats(level, cultivation);
            Invoke(stats, "AddExperience", 1);

            for (var i = 0; i < 200; i++)
            {
                AddAttributePoint(stats, "Will");
            }

            Assert.AreEqual(0.8f, Get<float>(stats, "DamageReduction"), 0.0001f);
        }

        private object CreateStats(int startLevel)
        {
            var level = CreateDefault("Tribulation.Config.LevelConfig");
            SetField(level, "startLevel", startLevel);
            return CreateStats(level, CreateDefault("Tribulation.Config.CultivationConfig"));
        }

        private object CreateStats(object level, object cultivation)
        {
            var gameObject = new GameObject("PlayerStatsTest");
            createdObjects.Add(gameObject);

            var statsType = RequiredType("Tribulation.Player.PlayerStats");
            var character = CreateDefault("Tribulation.Config.CharacterConfig");
            var stats = gameObject.AddComponent(statsType);
            Invoke(stats, "Configure", character, level, cultivation);
            return stats;
        }

        private static bool AddAttributePoint(object stats, string attributeName)
        {
            var attributeType = RequiredType("Tribulation.Config.CultivationAttribute");
            var attribute = Enum.Parse(attributeType, attributeName);
            return (bool)Invoke(stats, "AddAttributePoint", attribute);
        }

        private static object CreateDefault(string typeName)
        {
            var type = RequiredType(typeName);
            return type.GetMethod("CreateDefault", BindingFlags.Public | BindingFlags.Static).Invoke(null, Array.Empty<object>());
        }

        private static T Get<T>(object target, string propertyName)
        {
            return (T)target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance).SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            return target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance).Invoke(target, args);
        }

        private static Type RequiredType(string typeName)
        {
            var type = Type.GetType(typeName + ", Assembly-CSharp");
            Assert.NotNull(type, "Expected runtime type to be available: " + typeName);
            return type;
        }
    }
}
