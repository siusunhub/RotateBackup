using System;
using System.IO;
using System.Threading;

namespace RotateBackup.Core.Engine
{
    /// <summary>
    /// Self-contained CRC-32 (IEEE 802.3 / zlib polynomial 0xEDB88320) implementation.
    ///
    /// Slice-by-8 table driven, so it runs at multiple GB/s and stays far faster than the
    /// disk it is protecting. No external dependency, which keeps the single file
    /// executables small.
    /// </summary>
    public static class Crc32
    {
        private const uint Polynomial = 0xEDB88320u;

        private static readonly uint[] Table = CreateTable();

        /// <summary>Computes the CRC-32 of a byte buffer.</summary>
        public static uint Compute(byte[] buffer, int offset, int count, uint crc = 0xFFFFFFFFu)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            uint[] table = Table;

            while (count >= 8)
            {
                crc ^= (uint)(buffer[offset]
                              | (buffer[offset + 1] << 8)
                              | (buffer[offset + 2] << 16)
                              | (buffer[offset + 3] << 24));

                crc = table[7 * 256 + (crc & 0xFF)]
                    ^ table[6 * 256 + ((crc >> 8) & 0xFF)]
                    ^ table[5 * 256 + ((crc >> 16) & 0xFF)]
                    ^ table[4 * 256 + ((crc >> 24) & 0xFF)]
                    ^ table[3 * 256 + buffer[offset + 4]]
                    ^ table[2 * 256 + buffer[offset + 5]]
                    ^ table[1 * 256 + buffer[offset + 6]]
                    ^ table[0 * 256 + buffer[offset + 7]];

                offset += 8;
                count -= 8;
            }

            while (count > 0)
            {
                crc = table[(crc ^ buffer[offset]) & 0xFF] ^ (crc >> 8);
                offset++;
                count--;
            }

            return crc;
        }

        /// <summary>Computes the CRC-32 of the given bytes.</summary>
        public static uint Compute(byte[] buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            return Compute(buffer, 0, buffer.Length) ^ 0xFFFFFFFFu;
        }

        /// <summary>
        /// Computes the CRC-32 of a file by reading it from disk.
        /// </summary>
        /// <param name="path">File to read.</param>
        /// <param name="buffer">Reusable read buffer.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="onProgress">Optional progress callback (bytesRead, totalBytes).</param>
        public static uint ComputeFile(
            string path,
            byte[] buffer,
            CancellationToken ct = default,
            Action<long, long>? onProgress = null)
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                buffer.Length,
                FileOptions.SequentialScan);

            long totalBytes = stream.Length;
            long bytesRead = 0;
            uint crc = 0xFFFFFFFFu;

            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ct.ThrowIfCancellationRequested();

                crc = Compute(buffer, 0, read, crc);
                bytesRead += read;

                onProgress?.Invoke(bytesRead, totalBytes);
            }

            return crc ^ 0xFFFFFFFFu;
        }

        /// <summary>Formats a CRC value as a readable 8 digit hex string.</summary>
        public static string ToHex(uint crc)
        {
            return crc.ToString("X8");
        }

        private static uint[] CreateTable()
        {
            var table = new uint[8 * 256];

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ Polynomial : crc >> 1;
                }
                table[i] = crc;
            }

            for (uint i = 0; i < 256; i++)
            {
                uint crc = table[i];
                for (int j = 1; j < 8; j++)
                {
                    crc = table[crc & 0xFF] ^ (crc >> 8);
                    table[j * 256 + i] = crc;
                }
            }

            return table;
        }
    }
}
