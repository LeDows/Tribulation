using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tribulation.Tests.EditMode
{
    public sealed class EnemySystemTests
    {
        private static readonly Dictionary<string, string> ExpectedBehaviors = new()
        {
            ["boar_spirit"] = "Charger",
            ["wild_fox"] = "RangedKite",
            ["fallen_cultivator"] = "WoundedFlee",
            ["venom_snake"] = "Ambusher"
        };

        [SetUp]
        public void SetUp()
        {
            RequiredType("Tribulation.Config.ConfigCenter")
                .GetMethod("Reload", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, Array.Empty<object>());
        }

        [Test]
        public void QingyunSpawnPoolContainsFourDocumentedEnemyBehaviors()
        {
            var configCenter = RequiredType("Tribulation.Config.ConfigCenter");
            var map = configCenter.GetMethod("GetSelectedMap", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            var spawn = GetField(map, "spawn");
            var ids = (string[])Invoke(spawn, "GetEnemyIds");

            CollectionAssert.AreEqual(ExpectedBehaviors.Keys, ids);

            var enemies = (Array)configCenter.GetProperty("Enemies", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            foreach (var expected in ExpectedBehaviors)
            {
                var enemy = FindById(enemies, expected.Key);
                Assert.NotNull(enemy, "Missing configured enemy: " + expected.Key);
                Assert.AreEqual(expected.Value, GetField(enemy, "behavior").ToString());
                Assert.Greater((float)GetField(enemy, "spawnWeight"), 0f);
                Assert.IsFalse(string.IsNullOrWhiteSpace((string)GetField(enemy, "displayNameKey")));
            }
        }

        [Test]
        public void SpawnPoolParserTrimsDuplicatesAndFallsBackToLegacyEnemyId()
        {
            var spawn = Activator.CreateInstance(RequiredType("Tribulation.Config.SpawnConfig"));
            SetField(spawn, "enemyId", "bone_imp");
            SetField(spawn, "enemyIds", "  ");

            CollectionAssert.AreEqual(new[] { "bone_imp" }, (string[])Invoke(spawn, "GetEnemyIds"));

            SetField(spawn, "enemyIds", " boar_spirit, wild_fox,boar_spirit,, venom_snake ");
            CollectionAssert.AreEqual(
                new[] { "boar_spirit", "wild_fox", "venom_snake" },
                (string[])Invoke(spawn, "GetEnemyIds"));
        }

        [Test]
        public void NewEnemyNamesExistInChineseAndEnglishLocalization()
        {
            var chinese = Resources.Load<TextAsset>("Config/localization.zh-CN");
            var english = Resources.Load<TextAsset>("Config/localization.en-US");

            Assert.NotNull(chinese);
            Assert.NotNull(english);

            foreach (var enemyId in ExpectedBehaviors.Keys)
            {
                var key = $"enemy.{enemyId}.name";
                Assert.That(chinese.text, Does.Contain($"key=\"{key}\""));
                Assert.That(english.text, Does.Contain($"key=\"{key}\""));
            }
        }

        [Test]
        public void DynamicProjectilePoolReusesReleasedInstance()
        {
            var catalog = RequiredType("Tribulation.Core.RuntimePrefabCatalog");
            var projectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
            var parent = new GameObject("PoolTestRoot");

            Assert.NotNull(projectilePrefab);

            try
            {
                var first = (GameObject)catalog
                    .GetMethod("InstantiatePooled", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { projectilePrefab, parent.transform });

                catalog.GetMethod("ReleasePooled", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { first, projectilePrefab });

                var second = (GameObject)catalog
                    .GetMethod("InstantiatePooled", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { projectilePrefab, parent.transform });

                Assert.AreSame(first, second);
            }
            finally
            {
                catalog.GetMethod("ClearPools", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void EnemyPrefabUsesKinematicBodyAndLightweightRendering()
        {
            var enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemy");

            Assert.NotNull(enemyPrefab);

            var body = enemyPrefab.GetComponent<Rigidbody>();
            var renderer = enemyPrefab.GetComponent<Renderer>();
            var healthBarCanvas = enemyPrefab.GetComponentInChildren<Canvas>(true);

            Assert.NotNull(body);
            Assert.IsTrue(body.isKinematic);
            Assert.IsFalse(body.useGravity);
            Assert.AreEqual(
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ,
                body.constraints);
            Assert.NotNull(renderer);
            Assert.AreEqual(0, (int)renderer.shadowCastingMode);
            Assert.AreEqual(2, (int)renderer.motionVectorGenerationMode);
            Assert.NotNull(healthBarCanvas);
            Assert.IsFalse(healthBarCanvas.enabled);
        }

        [Test]
        public void SelectedMapProvidesBoundedPoolPrewarmConfiguration()
        {
            var configCenter = RequiredType("Tribulation.Config.ConfigCenter");
            var map = configCenter.GetMethod("GetSelectedMap", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            var spawn = GetField(map, "spawn");

            Assert.Greater((int)GetField(spawn, "maxEnemies"), 0);
            Assert.Greater((int)GetField(spawn, "projectilePoolSize"), 0);
            Assert.Greater((int)GetField(spawn, "experienceOrbPoolSize"), 0);
            Assert.Greater((int)GetField(spawn, "prewarmBatchSize"), 0);
        }

        private static object FindById(Array items, string id)
        {
            foreach (var item in items)
            {
                if (item != null && string.Equals((string)GetField(item, "id"), id, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private static object GetField(object target, string fieldName)
        {
            return target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance).GetValue(target);
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
