using System.Collections.Generic;

namespace RotateBackup.Core.Models
{
    public class BackupConfig
    {
        public string Version { get; set; } = "2.0";
        public List<BackupSet> BackupSets { get; set; } = new List<BackupSet>();
    }
}
