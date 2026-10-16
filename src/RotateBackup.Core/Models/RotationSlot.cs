namespace RotateBackup.Core.Models
{
    public class RotationSlot
    {
        public int Index { get; set; }
        public string Path { get; set; } = string.Empty;
        public bool Keep { get; set; }
    }
}
