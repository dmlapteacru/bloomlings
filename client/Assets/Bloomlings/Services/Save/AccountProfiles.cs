using System;
using System.IO;
using System.Text;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Content.Packs;

namespace Bloomlings.Client.Services.Save
{
    /// <summary>
    /// Local profile repository. Each owner has a different SaveService and directory, so delayed callbacks to A
    /// cannot write B's file. The caller must stop/recreate profile-dependent services when selecting an account.
    /// Selection is explicit; changing the platform account does not call this API automatically.
    /// </summary>
    public sealed class AccountProfiles
    {
        private readonly string _root;
        private readonly IClock _clock;
        public SaveService Current { get; private set; }
        public long SessionVersion { get; private set; }
        public event Action? Changing;
        public event Action? Changed;

        public AccountProfiles(string root, IClock clock)
        {
            _root = root;
            _clock = clock;
            Directory.CreateDirectory(root);
            string? owner = ReadActiveOwner();
            if (owner != null) Current = OpenOwner(owner);
            else
            {
                Current = new SaveService(root, clock);
                Current.Load(); // original installation save remains readable; no destructive reset
                if (Current.Source == SaveSource.New && (File.Exists(Current.MainPath) || File.Exists(Current.BackupPath)))
                    throw new InvalidDataException("Existing local save is unreadable; refusing to create a new guest over it.");
                if (Current.Current.CloudOwnerId != null)
                {
                    owner = Current.Current.CloudOwnerId;
                    SaveService target = OpenOwner(owner, create: false);
                    if (target.Source == SaveSource.New)
                    {
                        target.Current.Assign(Current.Current);
                        target.Save();
                    }
                    Current = target;
                    WriteActiveOwner(owner);
                }
            }
        }

        /// <summary>
        /// Only after authentication has linked this guest to a NEW, empty backend account.
        /// Bind the original file first. A crash during migration can retry A, but never copy the guest to B.
        /// </summary>
        public void ClaimGuest(string authenticatedOwner)
        {
            ValidateOwner(authenticatedOwner);
            string? oldOwner = Current.Current.CloudOwnerId;
            if (oldOwner != null && oldOwner != authenticatedOwner)
                throw new InvalidOperationException("This guest was already claimed by another account.");
            SaveService target = OpenOwner(authenticatedOwner, create: false);
            if (target.Source != SaveSource.New && target.Current.LocalPlayerId != Current.Current.LocalPlayerId)
                throw new InvalidOperationException("Existing account must be restored explicitly, not merged with a guest.");
            Changing?.Invoke();
            Current.Current.CloudOwnerId = authenticatedOwner;
            Current.Save();
            target.Current.Assign(Current.Current);
            target.Save();
            WriteActiveOwner(authenticatedOwner);
            Current = target;
            SessionVersion++;
            Changed?.Invoke();
        }

        /// <summary>Call only following explicit account selection AND successful authentication as that owner.</summary>
        public void SelectAuthenticated(string authenticatedOwner)
        {
            ValidateOwner(authenticatedOwner);
            if (Current.Current.CloudOwnerId == authenticatedOwner) return;
            Changing?.Invoke();
            Current.Save(); // offline changes remain in A's directory; they never become B's queue
            SaveService target = OpenOwner(authenticatedOwner);
            WriteActiveOwner(authenticatedOwner);
            Current = target;
            SessionVersion++;
            Changed?.Invoke();
        }

        /// <summary>
        /// Explicit restoration, e.g. on a new device. Archive the local branch first. This discards that branch
        /// from active play; it does not combine its wallet/items with the remote snapshot.
        /// The caller must verify the cloud response's authenticated owner/session before calling.
        /// </summary>
        public void RestoreConfirmed(string authenticatedOwner, PlayerSave remote)
        {
            if (authenticatedOwner != Current.Current.CloudOwnerId || remote.CloudOwnerId != authenticatedOwner)
                throw new InvalidOperationException("Restore owner mismatch.");
            Changing?.Invoke();
            Current.Save();
            string archive = Path.Combine(Path.GetDirectoryName(Current.MainPath)!, "conflicts", Guid.NewGuid().ToString("N"));
            var backup = new SaveService(archive, _clock);
            backup.Current.Assign(Current.Current);
            backup.Save();
            CloudSnapshot.Restore(Current.Current, remote);
            Current.Save();
            SessionVersion++;
            Changed?.Invoke();
        }

        private SaveService OpenOwner(string owner, bool create = true)
        {
            ValidateOwner(owner);
            string key = PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(owner));
            var save = new SaveService(Path.Combine(_root, "accounts", key), _clock);
            save.Load();
            if (save.Source == SaveSource.New && (File.Exists(save.MainPath) || File.Exists(save.BackupPath)))
                throw new InvalidDataException("Both account save copies are unreadable; refusing to replace them with an empty profile.");
            if (save.Source != SaveSource.New && save.Current.CloudOwnerId != owner)
                throw new InvalidDataException("Account directory contains a different owner.");
            save.Current.CloudOwnerId = owner;
            if (create && save.Source == SaveSource.New) save.Save();
            return save;
        }

        private string? ReadActiveOwner()
        {
            string path = Path.Combine(_root, "active-account.txt");
            if (!File.Exists(path))
            {
                if (File.Exists(path + ".bak")) path += ".bak";
                else return null;
            }
            string owner = File.ReadAllText(path, Encoding.UTF8);
            ValidateOwner(owner); // invalid selector must not silently open/create an unrelated guest
            return owner;
        }

        private void WriteActiveOwner(string owner)
        {
            string path = Path.Combine(_root, "active-account.txt");
            string tmp = path + ".tmp";
            byte[] data = Encoding.UTF8.GetBytes(owner);
            using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(data, 0, data.Length);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(tmp, path, path + ".bak");
            else File.Move(tmp, path);
        }

        private static void ValidateOwner(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner) || owner.Length > 256 || owner.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
                throw new ArgumentException("Invalid account owner.", nameof(owner));
        }
    }
}
