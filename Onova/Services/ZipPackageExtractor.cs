﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Onova.Internal;

namespace Onova.Services
{
    /// <summary>
    /// Extracts packages as ZIP archives.
    /// </summary>
    public class ZipPackageExtractor : IPackageExtractor
    {
        /// <inheritdoc />
        public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath,
            IProgress<double> progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            sourceFilePath.GuardNotNull(nameof(sourceFilePath));
            destDirPath.GuardNotNull(nameof(destDirPath));

            // Read the zip
            using (var stream = File.OpenRead(sourceFilePath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                // For progress reporting
                var totalBytes = archive.Entries.Sum(e => e.Length);
                var totalBytesCopied = 0L;

                // Loop through all entries
                foreach (var entry in archive.Entries)
                {
                    var entryDestFilePath = Path.Combine(destDirPath, entry.FullName);
                    var entryDestDirPath = Path.GetDirectoryName(entryDestFilePath);

                    // Create directory
                    Directory.CreateDirectory(entryDestDirPath);

                    // Extract entry
                    using (var input = entry.Open())
                    using (var output = File.Create(entryDestFilePath))
                    {
                        int bytesCopied;
                        do
                        {
                            // Copy
                            bytesCopied = await input.CopyChunkToAsync(output, cancellationToken)
                                .ConfigureAwait(false);

                            // Report progress
                            totalBytesCopied += bytesCopied;
                            progress?.Report(1.0 * totalBytesCopied / totalBytes);
                        } while (bytesCopied > 0);
                    }
                }
            }
        }
    }
}