using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tribulation.Tests.EditMode
{
    public sealed class WeaponSystemTests
    {
        private readonly List<GameObject> createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            RequiredType("Tribulation.Config.ConfigCenter")
                .GetMethod("Reload", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, Array.Empty<object>());
        }

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
        public void WeaponCatalogLoadsGddWeaponFamilies()
        {
            var catalog = GetWeaponCatalog();
            var weapons = (Array)GetField(catalog, "weapons");

            Assert.AreEqual(3, GetField(catalog, "maxEquipped"));
            Assert.AreEqual(42, weapons.Length);
            Assert.NotNull(Invoke(catalog, "FindWeapon", "zhuxian_sword"));

            var schools = new HashSet<object>();
            foreach (var weapon in weapons)
            {
                schools.Add(GetField(weapon, "school"));
            }

            Assert.AreEqual(7, schools.Count);
        }

        [Test]
        public void ForgeRecipeCanCreateZhuxianSword()
        {
            var catalog = GetWeaponCatalog();
            var args = new object[] { new[] { "seven_star_sword", "zhuxian_sword_array" }, null };

            Assert.IsTrue((bool)Invoke(catalog, "TryGetForgeResult", args));
            Assert.AreEqual("zhuxian_sword", GetField(args[1], "id"));
        }

        [Test]
        public void AutoWeaponRespectsThreeWeaponLimitAndEnhancementCap()
        {
            var playerObject = new GameObject("WeaponSystemTestPlayer");
            createdObjects.Add(playerObject);

            playerObject.AddComponent(RequiredType("Tribulation.Player.PlayerStats"));
            var weapon = AddConfiguredAutoWeapon(playerObject);

            Assert.AreEqual(1, GetProperty(weapon, "EquippedCount"));
            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "fireball"));
            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "thunder_call"));
            Assert.IsFalse((bool)Invoke(weapon, "EquipWeapon", "poison_needle"));
            Assert.AreEqual(3, GetProperty(weapon, "EquippedCount"));

            Assert.IsTrue((bool)Invoke(weapon, "EnhanceWeapon", "iron_sword", 1));
            Assert.IsTrue((bool)Invoke(weapon, "EnhanceWeapon", "iron_sword", 1));
            Assert.IsTrue((bool)Invoke(weapon, "EnhanceWeapon", "iron_sword", 1));
            Assert.IsFalse((bool)Invoke(weapon, "EnhanceWeapon", "iron_sword", 1));
        }

        [Test]
        public void GlobalEnhancementStrengthensEveryEligibleEquippedWeapon()
        {
            var playerObject = new GameObject("GlobalWeaponEnhancementTestPlayer");
            createdObjects.Add(playerObject);

            playerObject.AddComponent(RequiredType("Tribulation.Player.PlayerStats"));
            var weapon = AddConfiguredAutoWeapon(playerObject);
            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "fireball"));
            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "thunder_call"));

            Assert.IsTrue((bool)InvokeStaticNonPublic(
                RequiredType("Tribulation.Core.GameManager"),
                "ApplyWeaponEnhancement",
                weapon,
                GetUpgrade("temper_weapon")));
            AssertEnhancementLevels(weapon, 1, 1, 1);

            Assert.IsTrue((bool)Invoke(weapon, "EnhanceWeapon", "iron_sword", 2));
            Assert.IsTrue((bool)Invoke(weapon, "EnhanceAllWeapons", 1));
            AssertEnhancementLevels(weapon, 3, 2, 2);

            Assert.IsTrue((bool)Invoke(weapon, "EnhanceWeapon", "fireball", 1));
            Assert.IsTrue((bool)Invoke(weapon, "EnhanceWeapon", "thunder_call", 1));
            Assert.IsFalse((bool)Invoke(weapon, "CanApplyUpgrade", GetUpgrade("temper_weapon")));
            Assert.IsFalse((bool)Invoke(weapon, "EnhanceAllWeapons", 1));
            AssertEnhancementLevels(weapon, 3, 3, 3);
        }

        [Test]
        public void RepeatedSpecificWeaponOptionEnhancesOnlyThatWeapon()
        {
            var playerObject = new GameObject("SpecificWeaponEnhancementTestPlayer");
            createdObjects.Add(playerObject);

            playerObject.AddComponent(RequiredType("Tribulation.Player.PlayerStats"));
            var weapon = AddConfiguredAutoWeapon(playerObject);

            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "fireball"));
            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "fireball"));
            Assert.AreEqual(2, GetProperty(weapon, "EquippedCount"));
            AssertEnhancementLevels(weapon, 0, 1);
        }

        [Test]
        public void FullWeaponSlotsRejectRandomWeaponUpgrade()
        {
            var playerObject = new GameObject("FullWeaponSlotsTestPlayer");
            createdObjects.Add(playerObject);

            playerObject.AddComponent(RequiredType("Tribulation.Player.PlayerStats"));
            var weapon = AddConfiguredAutoWeapon(playerObject);
            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "fireball"));
            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "thunder_call"));

            Assert.IsFalse((bool)Invoke(weapon, "CanApplyUpgrade", GetUpgrade("find_weapon")));
            Assert.IsFalse((bool)Invoke(weapon, "EquipRandomWeapon"));
            AssertEnhancementLevels(weapon, 0, 0, 0);
        }

        [Test]
        public void GlobalFireRateUpgradeBecomesUnavailableAtCap()
        {
            var playerObject = new GameObject("WeaponFireRateCapTestPlayer");
            createdObjects.Add(playerObject);

            playerObject.AddComponent(RequiredType("Tribulation.Player.PlayerStats"));
            var weapon = AddConfiguredAutoWeapon(playerObject);
            var option = GetUpgrade("quickened_blade");
            var applications = 0;

            while ((bool)Invoke(weapon, "CanApplyUpgrade", option) && applications < 100)
            {
                Assert.IsTrue((bool)Invoke(weapon, "ReduceFireIntervalPercent", (float)GetField(option, "value")));
                applications++;
            }

            Assert.Greater(applications, 0);
            Assert.Less(applications, 100);
            Assert.IsFalse((bool)Invoke(weapon, "CanApplyUpgrade", option));
            Assert.IsFalse((bool)Invoke(weapon, "ReduceFireIntervalPercent", (float)GetField(option, "value")));
        }

        [Test]
        public void ProjectileCountUpgradeAppliesThroughGameManagerAndFormatsAsWholeNumber()
        {
            var option = GetUpgrade("multishot_art");
            Assert.AreEqual("projectile_count", GetField(option, "effectType"));
            Assert.AreEqual(1f, (float)GetField(option, "value"), 0.001f);
            Assert.AreEqual(
                1,
                InvokeStaticNonPublic(
                    RequiredType("Tribulation.UI.GameUiController"),
                    "GetUpgradeDisplayValue",
                    option));

            var playerObject = new GameObject("ProjectileCountUpgradeTestPlayer");
            createdObjects.Add(playerObject);
            var stats = playerObject.AddComponent(RequiredType("Tribulation.Player.PlayerStats"));
            var weapon = AddConfiguredAutoWeapon(playerObject);
            Assert.IsTrue((bool)Invoke(weapon, "CanApplyUpgrade", option));

            var managerObject = new GameObject("ProjectileCountUpgradeTestManager");
            createdObjects.Add(managerObject);
            var manager = managerObject.AddComponent(RequiredType("Tribulation.Core.GameManager"));
            Invoke(manager, "RegisterPlayer", stats);
            InvokeNonPublic(manager, "ApplyUpgradeOption", option);

            Assert.AreEqual(1, GetNonPublicField(weapon, "projectileCountBonus"));
        }

        [Test]
        public void ProjectileCountCalculationAddsUpgradeAndSetBonuses()
        {
            var weaponType = RequiredType("Tribulation.Combat.AutoWeapon");

            Assert.AreEqual(2, InvokeStaticNonPublic(weaponType, "CalculateProjectileCount", 1, 1, 0));
            Assert.AreEqual(5, InvokeStaticNonPublic(weaponType, "CalculateProjectileCount", 3, 1, 1));
            Assert.AreEqual(7, InvokeStaticNonPublic(weaponType, "CalculateProjectileCount", 7, 0, 0));
            Assert.AreEqual(1, InvokeStaticNonPublic(weaponType, "CalculateProjectileCount", 1, -1, -1));
        }

        [Test]
        public void ProjectileCountBonusOnlySupportsProjectileAttackPatterns()
        {
            var weaponType = RequiredType("Tribulation.Combat.AutoWeapon");
            var attackPatternType = RequiredType("Tribulation.Config.WeaponAttackPattern");

            Assert.IsTrue((bool)InvokeStaticNonPublic(
                weaponType,
                "SupportsProjectileCount",
                Enum.Parse(attackPatternType, "Projectile")));
            Assert.IsTrue((bool)InvokeStaticNonPublic(
                weaponType,
                "SupportsProjectileCount",
                Enum.Parse(attackPatternType, "BurstProjectiles")));

            foreach (var unsupportedPattern in new[] { "Cone", "AreaAtTarget", "Lightning", "Aura" })
            {
                Assert.IsFalse((bool)InvokeStaticNonPublic(
                    weaponType,
                    "SupportsProjectileCount",
                    Enum.Parse(attackPatternType, unsupportedPattern)),
                    unsupportedPattern + " should not receive a projectile-count bonus.");
            }
        }

        [Test]
        public void ProjectileSetDescriptionsMatchImplementedExtraProjectileBonus()
        {
            var chinese = Resources.Load<TextAsset>("Config/localization.zh-CN");
            var english = Resources.Load<TextAsset>("Config/localization.en-US");

            Assert.NotNull(chinese);
            Assert.NotNull(english);
            Assert.That(chinese.text, Does.Contain("剑系投射武器额外发射 1 个弹体"));
            Assert.That(chinese.text, Does.Contain("火系投射武器额外发射 1 个弹体"));
            Assert.That(english.text, Does.Contain("Sword projectile weapons fire 1 extra projectile"));
            Assert.That(english.text, Does.Contain("Fire projectile weapons fire 1 extra projectile"));
        }

        [Test]
        public void GlobalEnhancementDescriptionUsesConfiguredLevel()
        {
            var option = GetUpgrade("temper_weapon");
            var displayValue = InvokeStaticNonPublic(
                RequiredType("Tribulation.UI.GameUiController"),
                "GetUpgradeDisplayValue",
                option);

            Assert.AreEqual(1, displayValue);

            var description = (string)RequiredType("Tribulation.Config.ConfigCenter")
                .GetMethod("Text", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new[] { GetField(option, "descriptionKey"), new[] { displayValue } });
            StringAssert.Contains("+1", description);
            StringAssert.DoesNotContain("{0}", description);

            var chinese = Resources.Load<TextAsset>("Config/localization.zh-CN");
            var english = Resources.Load<TextAsset>("Config/localization.en-US");
            Assert.NotNull(chinese);
            Assert.NotNull(english);
            Assert.That(chinese.text, Does.Contain("将所有已装备武器强化 +{0}。"));
            Assert.That(english.text, Does.Contain("Enhance all equipped weapons by +{0}."));
        }

        [Test]
        public void EveryUpgradeDescriptionResolvesItsDisplayPlaceholder()
        {
            var uiType = RequiredType("Tribulation.UI.GameUiController");
            var textMethod = RequiredType("Tribulation.Config.ConfigCenter")
                .GetMethod("Text", BindingFlags.Public | BindingFlags.Static);

            foreach (var option in GetUpgrades())
            {
                var displayValue = InvokeStaticNonPublic(uiType, "GetUpgradeDisplayValue", option);
                var description = (string)textMethod.Invoke(
                    null,
                    new[] { GetField(option, "descriptionKey"), new[] { displayValue } });

                Assert.That(
                    description,
                    Does.Not.Match(@"\{\d+\}"),
                    "Upgrade description kept an unresolved placeholder: " + GetField(option, "id"));
            }
        }

        [Test]
        public void BaseWeaponConfigsLoadDistinctAttackMechanics()
        {
            var catalog = GetWeaponCatalog();
            var fireball = Invoke(catalog, "FindWeapon", "fireball");
            var thunderCall = Invoke(catalog, "FindWeapon", "thunder_call");
            var poisonNeedle = Invoke(catalog, "FindWeapon", "poison_needle");

            Assert.IsTrue((bool)GetField(fireball, "areaOnImpact"));
            Assert.AreEqual(2f, (float)GetField(fireball, "hitRadius"), 0.001f);
            Assert.AreEqual(1, GetField(thunderCall, "chainCount"));
            Assert.AreEqual(5.4f, (float)GetField(poisonNeedle, "dotDamagePerSecond"), 0.001f);
            Assert.AreEqual(3f, (float)GetField(poisonNeedle, "dotDuration"), 0.001f);
        }

        [Test]
        public void LightningTargetCountAddsConfiguredChainsAndSetBonus()
        {
            var weaponType = RequiredType("Tribulation.Combat.AutoWeapon");

            Assert.AreEqual(2, InvokeStaticNonPublic(weaponType, "CalculateLightningTargetCount", 1, 1, 0));
            Assert.AreEqual(5, InvokeStaticNonPublic(weaponType, "CalculateLightningTargetCount", 1, 3, 1));
            Assert.AreEqual(5, InvokeStaticNonPublic(weaponType, "CalculateLightningTargetCount", 5, 0, 0));
        }

        [Test]
        public void EnemyDamageOverTimeTicksForConfiguredDuration()
        {
            var enemyObject = new GameObject("WeaponSystemDotTarget");
            createdObjects.Add(enemyObject);
            var enemy = enemyObject.AddComponent(RequiredType("Tribulation.Enemies.EnemyHealth"));

            Invoke(enemy, "Configure", 100f, 0);
            Invoke(enemy, "ApplyDamageOverTime", 10f, 2f);
            InvokeNonPublic(enemy, "TickDamageOverTime", 0.5f);

            Assert.AreEqual(95f, (float)GetProperty(enemy, "Health"), 0.001f);
            Assert.AreEqual(1.5f, (float)GetProperty(enemy, "DamageOverTimeRemaining"), 0.001f);

            InvokeNonPublic(enemy, "TickDamageOverTime", 2f);

            Assert.AreEqual(80f, (float)GetProperty(enemy, "Health"), 0.001f);
            Assert.AreEqual(0f, (float)GetProperty(enemy, "DamageOverTimeRemaining"), 0.001f);
            Assert.AreEqual(0f, (float)GetProperty(enemy, "DamageOverTimePerSecond"), 0.001f);
        }

        [Test]
        public void ProjectileKeepsLegacyLaunchAndExposesPlayerLaunch()
        {
            var projectileType = RequiredType("Tribulation.Combat.Projectile");
            var launchParameters = new[] { typeof(Vector3), typeof(float), typeof(float), typeof(float), typeof(int) };
            var playerLaunchParameters = new[] { typeof(Vector3), typeof(float), typeof(float), typeof(float) };

            Assert.NotNull(projectileType.GetMethod(
                "Launch",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                launchParameters,
                null));
            Assert.NotNull(projectileType.GetMethod(
                "LaunchAgainstPlayer",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                playerLaunchParameters,
                null));
        }

        private static object GetWeaponCatalog()
        {
            return RequiredType("Tribulation.Config.ConfigCenter")
                .GetProperty("Weapon", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);
        }

        private static Component AddConfiguredAutoWeapon(GameObject playerObject)
        {
            var weapon = playerObject.AddComponent(RequiredType("Tribulation.Combat.AutoWeapon"));
            Invoke(weapon, "ApplyConfig", GetWeaponCatalog());
            return weapon;
        }

        private static object GetUpgrade(string id)
        {
            foreach (var upgrade in GetUpgrades())
            {
                if ((string)GetField(upgrade, "id") == id)
                {
                    return upgrade;
                }
            }

            Assert.Fail("Expected upgrade config to be available: " + id);
            return null;
        }

        private static Array GetUpgrades()
        {
            return (Array)RequiredType("Tribulation.Config.ConfigCenter")
                .GetProperty("Upgrades", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);
        }

        private static void AssertEnhancementLevels(object weapon, params int[] expectedLevels)
        {
            var views = (Array)Invoke(weapon, "GetEquippedWeaponViews");
            Assert.AreEqual(expectedLevels.Length, views.Length);
            for (var i = 0; i < expectedLevels.Length; i++)
            {
                Assert.AreEqual(expectedLevels[i], GetField(views.GetValue(i), "EnhancementLevel"));
            }
        }

        private static object GetField(object target, string fieldName)
        {
            return target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
        }

        private static object GetProperty(object target, string propertyName)
        {
            return target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
        }

        private static object GetNonPublicField(object target, string fieldName)
        {
            return target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            return target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance).Invoke(target, args);
        }

        private static object InvokeNonPublic(object target, string methodName, params object[] args)
        {
            return target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
        }

        private static object InvokeStaticNonPublic(Type targetType, string methodName, params object[] args)
        {
            return targetType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
        }

        private static Type RequiredType(string typeName)
        {
            var type = Type.GetType(typeName + ", Assembly-CSharp");
            Assert.NotNull(type, "Expected runtime type to be available: " + typeName);
            return type;
        }
    }
}
