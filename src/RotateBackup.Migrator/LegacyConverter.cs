using System.Collections.Generic;
using RotateBackup.Core.Models;

namespace RotateBackup.Migrator
{
    public static class LegacyConverter
    {
        public static BackupSet ToModern(LegacyBackupSetting legacy)
        {
            var modern = new BackupSet
            {
                Id = legacy._id?.AsString ?? System.Guid.NewGuid().ToString("N"),
                Command = legacy.Command ?? string.Empty,
                Remark = legacy.Remark ?? string.Empty,
                Disabled = legacy.disable,
                IsDirectory = legacy.isDirectory,
                SourcePath = legacy.mainPath ?? string.Empty,
                KeepSource = legacy.keepMain,
                KeepFirstDir = legacy.keepFirstDir,
                PreCommand = legacy.preCommand ?? string.Empty,
                PostCommand = legacy.postCommand ?? string.Empty,
                Slots = new List<RotationSlot>()
            };

            var paths = new (string path, bool keep)[]
            {
                (legacy.Path1, legacy.keepPath1),
                (legacy.Path2, legacy.keepPath2),
                (legacy.Path3, legacy.keepPath3),
                (legacy.Path4, legacy.keepPath4),
                (legacy.Path5, legacy.keepPath5),
                (legacy.Path6, legacy.keepPath6),
                (legacy.Path7, legacy.keepPath7),
                (legacy.Path8, legacy.keepPath8),
                (legacy.Path9, legacy.keepPath9),
                (legacy.Path10, legacy.keepPath10),
                (legacy.Path11, legacy.keepPath11),
                (legacy.Path12, legacy.keepPath12),
                (legacy.Path13, legacy.keepPath13),
                (legacy.Path14, legacy.keepPath14)
            };

            int count = legacy.maxPath > 0 ? legacy.maxPath : 14;
            if (count > paths.Length) count = paths.Length;

            for (int i = 0; i < count; i++)
            {
                var (path, keep) = paths[i];
                modern.Slots.Add(new RotationSlot
                {
                    Index = i + 1,
                    Path = path ?? string.Empty,
                    Keep = keep
                });
            }

            return modern;
        }
    }
}
