using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace GameFoundation.Saves
{
    /// <summary>Files synchronized by Steam Auto-Cloud before launch and after exit.</summary>
    internal static class SaveFileStorage
    {
        internal static string DirectoryPath => Path.Combine(Application.persistentDataPath, "Saves");
        private static string SlotPath(int slot)
        {
            if (slot < 1 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(DirectoryPath, "slot-" + slot + ".json");
        }
        internal static bool Exists(int slot) => File.Exists(SlotPath(slot));
        internal static string ReadBackup(int slot) => File.ReadAllText(SlotPath(slot) + ".bak", Encoding.UTF8);
        internal static string Read(int slot) => File.ReadAllText(SlotPath(slot), Encoding.UTF8);
        internal static void Write(int slot, string json)
        {
            string path = SlotPath(slot), temporary = path + ".tmp";
            Directory.CreateDirectory(DirectoryPath);
            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                // A same-directory atomic replacement keeps a crash from truncating a valid save.
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
