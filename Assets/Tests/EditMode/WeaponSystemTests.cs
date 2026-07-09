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
            var weapon = playerObject.AddComponent(RequiredType("Tribulation.Combat.AutoWeapon"));

            Assert.AreEqual(1, GetProperty(weapon, "EquippedCount"));
            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "fireball"));
            Assert.IsTrue((bool)Invoke(weapon, "EquipWeapon", "thunder_call"));
            Assert.IsFalse((bool)Invoke(weapon, "EquipWeapon", "poison_needle"));
            Assert.AreEqual(3, GetProperty(weapon, "EquippedCount"));

            Assert.IsTrue((bool)Invoke(weapon, "EnhanceWeapon", "iron_sword"));
            Assert.IsTrue((bool)Invoke(weapon, "EnhanceWeapon", "iron_sword"));
            Assert.IsTrue((bool)Invoke(weapon, "EnhanceWeapon", "iron_sword"));
            Assert.IsFalse((bool)Invoke(weapon, "EnhanceWeapon", "iron_sword"));
        }

        private static object GetWeaponCatalog()
        {
            return RequiredType("Tribulation.Config.ConfigCenter")
                .GetProperty("Weapon", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);
        }

        private static object GetField(object target, string fieldName)
        {
            return target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
        }

        private static object GetProperty(object target, string propertyName)
        {
            return target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
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
