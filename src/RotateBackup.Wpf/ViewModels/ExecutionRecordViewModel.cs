using System;
using System.Windows.Media;
using RotateBackup.Core.Models;

namespace RotateBackup.Wpf.ViewModels
{
    /// <summary>
    /// Display wrapper for a single execution log entry (one backup execution).
    /// </summary>
    public class ExecutionRecordViewModel
    {
        private static readonly Brush SuccessBrush = CreateBrush("#10B981");
        private static readonly Brush FailureBrush = CreateBrush("#EF4444");
        private static readonly Brush RunningBrush = CreateBrush("#0284C7");

        public ExecutionRecordViewModel(ExecutionRecord record)
        {
            Record = record ?? throw new ArgumentNullException(nameof(record));
        }

        public ExecutionRecord Record { get; }

        public string RunId => Record.RunId;

        public string Command => Record.Command;

        public string Source => Record.Source;

        public int ProcessId => Record.ProcessId;

        public string StartedAtDisplay => Record.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");

        public string EndedAtDisplay => Record.EndedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";

        public string DurationDisplay => Record.DurationSeconds.HasValue
            ? $"{Record.DurationSeconds.Value:F2}s"
            : "—";

        public string ResultDisplay => Record.Outcome switch
        {
            ExecutionOutcome.Success => "SUCCESS",
            ExecutionOutcome.Failed => "FAILED",
            _ => "RUNNING"
        };

        public Brush ResultBrush => Record.Outcome switch
        {
            ExecutionOutcome.Success => SuccessBrush,
            ExecutionOutcome.Failed => FailureBrush,
            _ => RunningBrush
        };

        public string Message => Record.Message;

        public string FilePath => Record.FilePath;

        private static Brush CreateBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
    }
}
