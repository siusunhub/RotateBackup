using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using RotateBackup.Core.Models;

namespace RotateBackup.Core.Storage
{
    public class JsonBackupRepository
    {
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            PropertyNameCaseInsensitive = true
        };

        public BackupConfig Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return new BackupConfig();
            }

            try
            {
                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var config = JsonSerializer.Deserialize<BackupConfig>(json, _options);
                return config ?? new BackupConfig();
            }
            catch
            {
                return new BackupConfig();
            }
        }

        public void Save(BackupConfig config, string path)
        {
            ArgumentNullException.ThrowIfNull(config);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be empty", nameof(path));
            }

            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(config, _options);
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        }
    }
}
