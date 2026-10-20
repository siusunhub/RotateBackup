using System;
using System.Text;

namespace RotateBackup.Core.Validation
{
    /// <summary>
    /// Validation rules for the command name of a backup set.
    ///
    /// The command name is used in three places, so it has to stay simple and safe:
    ///   * as an argument of the CLI runner  ->  "RotateBackup.exe &lt;command&gt;"
    ///   * in the Windows Task Scheduler command line
    ///   * as a field of the pipe delimited execution log
    ///
    /// Only English letters, digits, underscore and hyphen are accepted. Spaces, non ASCII
    /// characters and shell/path characters are rejected because they would break the CLI
    /// argument and the log format.
    /// </summary>
    public static class CommandNameValidator
    {
        /// <summary>Maximum length of a command name.</summary>
        public const int MaxLength = 64;

        /// <summary>
        /// Characters that may be used in a command name.
        /// </summary>
        public const string AllowedCharacters = "English letters, numbers, '_' and '-'";

        /// <summary>Validates a command name and returns the reason when it is invalid.</summary>
        public static bool IsValid(string? command, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(command))
            {
                error = "Command name is required.";
                return false;
            }

            if (command.Length > MaxLength)
            {
                error = $"Command name must be {MaxLength} characters or less.";
                return false;
            }

            foreach (char c in command)
            {
                if (!IsAllowed(c))
                {
                    error = $"Command name may only contain {AllowedCharacters} (no spaces or special characters).";
                    return false;
                }
            }

            return true;
        }

        /// <summary>Convenience helper that returns null when the command name is valid.</summary>
        public static string? GetError(string? command)
        {
            return IsValid(command, out string error) ? null : error;
        }

        /// <summary>Checks whether a single character may be part of a command name.</summary>
        public static bool IsAllowed(char c)
        {
            return (c >= 'a' && c <= 'z')
                || (c >= 'A' && c <= 'Z')
                || (c >= '0' && c <= '9')
                || c == '_'
                || c == '-';
        }

        /// <summary>
        /// Removes the characters that would break the pipe delimited log format.
        /// </summary>
        public static string SanitizeForLog(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '|':
                        builder.Append('/');
                        break;
                    case '\r':
                    case '\n':
                        // Line breaks would split the entry into two lines - collapse them.
                        if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                        {
                            builder.Append(' ');
                        }
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
