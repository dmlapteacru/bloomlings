using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary><c>backend/remote-config/defaults.json</c> mirrors every client key with its default and range (T126).</summary>
    public class RemoteConfigDefaultsTests
    {
        [Test]
        public void BackendDefaults_MirrorTheClientKeys()
        {
            string path = Path.Combine(DevContent.RepositoryContentFolder, "..", "backend", "remote-config", "defaults.json");
            var keys = (JObject)JObject.Parse(File.ReadAllText(path))["keys"]!;
            var expected = new HashSet<string>();
            foreach (IntKey key in RemoteConfigKeys.AllInts)
            {
                expected.Add(key.Name);
                Assert.That((int)keys[key.Name]!["default"]!, Is.EqualTo(key.Default), key.Name);
                Assert.That((int)keys[key.Name]!["min"]!, Is.EqualTo(key.Min), key.Name);
                Assert.That((int)keys[key.Name]!["max"]!, Is.EqualTo(key.Max), key.Name);
            }

            foreach (BoolKey key in RemoteConfigKeys.AllBools)
            {
                expected.Add(key.Name);
                Assert.That((bool)keys[key.Name]!["default"]!, Is.EqualTo(key.Default), key.Name);
            }

            foreach (StringKey key in RemoteConfigKeys.AllStrings)
            {
                expected.Add(key.Name);
                Assert.That((string)keys[key.Name]!["default"]!, Is.EqualTo(key.Default), key.Name);
            }

            var actual = new HashSet<string>();
            foreach (JProperty property in keys.Properties())
            {
                actual.Add(property.Name);
            }

            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}
