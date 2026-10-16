using System;
using System.Collections.Generic;

namespace RotateBackup.Core.Models
{
    public class BackupSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Command { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public bool Disabled { get; set; }
        public bool IsDirectory { get; set; } = true;
        public string SourcePath { get; set; } = string.Empty;
        public bool KeepSource { get; set; }
        public bool KeepFirstDir { get; set; }
        public string PreCommand { get; set; } = string.Empty;
        public string PostCommand { get; set; } = string.Empty;
        public List<RotationSlot> Slots { get; set; } = new List<RotationSlot>();
    }
}
