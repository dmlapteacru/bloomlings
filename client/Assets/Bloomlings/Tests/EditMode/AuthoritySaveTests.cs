using System;
using System.IO;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Save;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public sealed class AuthoritySaveTests
    {
        private string _folder = null!;
        [SetUp] public void SetUp() => _folder = Path.Combine(Path.GetTempPath(), "authority-save-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown() { if (Directory.Exists(_folder)) Directory.Delete(_folder,true); }
        private SaveService Open() => new SaveService(_folder,new SystemClock());
        [Test] public void SuccessfulActivationRoundTripsTheSameOwnerAndSeal()
        {
            var saves=Open();var save=saves.Load();save.CloudOwnerId="alice";save.ServerEconomyOwnerId="alice";saves.Save();
            Assert.That(Open().Load().ServerEconomyOwnerId,Is.EqualTo("alice"));
            Assert.That(File.ReadAllText(Path.Combine(_folder,SaveService.AuthoritySealName)),Is.EqualTo("alice"));
        }
        [Test] public void OlderBackupCannotSilentlyReopenServerAccountAsLocal()
        {
            var saves=Open();saves.Load();saves.Save();saves.Current.CloudOwnerId="alice";saves.Current.ServerEconomyOwnerId="alice";saves.Save();
            File.WriteAllText(saves.MainPath,"corrupt");Assert.Throws<InvalidDataException>(()=>Open().Load());
            Assert.That(File.Exists(saves.BackupPath),Is.True);
        }
        [Test] public void LosingBothCopiesPreservesSealInsteadOfCreatingLocalMoney()
        {
            var saves=Open();saves.Load();saves.Current.CloudOwnerId="alice";saves.Current.ServerEconomyOwnerId="alice";saves.Save();saves.Save();
            File.WriteAllText(saves.MainPath,"corrupt");File.WriteAllText(saves.BackupPath,"corrupt");
            Assert.Throws<InvalidDataException>(()=>Open().Load());Assert.That(File.ReadAllText(saves.MainPath),Is.EqualTo("corrupt"));
        }
        [Test] public void InterruptedFirstActivationCannotFallBackToPreActivationSave()
        {
            var saves=Open();saves.Load();saves.Save();saves.Current.CloudOwnerId="alice";saves.Current.ServerEconomyOwnerId="alice";
            saves.AfterTempWrite=()=>throw new IOException("power loss");Assert.Throws<IOException>(saves.Save);
            Assert.Throws<InvalidDataException>(()=>Open().Load());
        }
        [Test] public void OwnerMismatchAndModeRemovalAreRejectedBeforeWriting()
        {
            var saves=Open();saves.Load();saves.Current.CloudOwnerId="alice";saves.Current.ServerEconomyOwnerId="alice";saves.Save();
            saves.Current.ServerEconomyOwnerId=null;Assert.Throws<InvalidDataException>(saves.Save);
            Assert.That(Open().Load().ServerEconomyOwnerId,Is.EqualTo("alice"));
            Assert.Throws<Bloomlings.Content.Json.ContentFormatException>(()=>SaveSerializer.Read(SaveSerializer.Write(PlayerSave.CreateNew("guest",DateTime.UtcNow)).Replace("\"localPlayerId\":", "\"serverEconomyOwnerId\":\"bob\",\"localPlayerId\":")));
        }
    }
}
