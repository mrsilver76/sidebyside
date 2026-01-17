/*
 * SideBySide - Combine two portrait photos into a single landscape image,
 * useful for digital photo frames that display vertical images awkwardly.
 * Copyright (C) 2024-2026 Richard Lawrence
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, see
 * <https://www.gnu.org/licenses/>.
 */

namespace SideBySide
{
    /// <summary>
    /// Handles deletion of existing output files and stale files in mirror mode.
    /// </summary>
    internal sealed class OutputFileCleaner
    {
        /// <summary>
        /// Deletes existing files in the destination folder if the deleteExisting flag is set to true.
        /// </summary>
        public static void DeleteExistingFiles()
        {
            // Don't delete existing files if the flag is not set
            if (Globals.DeleteExisting == false)
                return;

            // Delete existing files in the destination folder
            Logger.Write($"Cleaning existing files in: {Globals.DestinationFolder}");

            if (!System.IO.Directory.Exists(Globals.DestinationFolder))
            {
                Logger.Write($"Destination folder does not exist: {Globals.DestinationFolder}", true);
                return;
            }

            // Get all files matching the pattern "sideby-*.jpg" in the destination folder
            string[] existingFiles = System.IO.Directory.GetFiles(Globals.DestinationFolder, "sideby-*.jpg", SearchOption.TopDirectoryOnly);
            if (existingFiles.Length == 0)
            {
                Logger.Write($"No existing files to delete in: {Globals.DestinationFolder}", true);
                return;
            }

            // Attempt to delete each existing file
            foreach (string file in existingFiles)
            {
                try
                {
                    File.Delete(file);
                    Logger.Write($"Deleted existing file: {Path.GetFileName(file)}", true);
                }
                catch (Exception ex)
                {
                    Logger.Write($"Failed to delete {file}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Removes any stale files in the destination folder that were not processed during this run.
        /// </summary>
        public static void MirrorCleanup()
        {
            // If mirror mode is not enabled, do nothing
            if (!Globals.MirrorMode)
                return;

            int deleted = 0;

            // Walk through the destination folder and delete any files that were not processed
            // in this run.
            var existing = System.IO.Directory.GetFiles(Globals.DestinationFolder, "sideby-*.jpg", SearchOption.TopDirectoryOnly);
            foreach (var file in existing)
            {
                var name = Path.GetFileName(file);
                if (!Globals.ProcessedFiles.Contains(file))
                {
                    Logger.Write($"Mirror mode: deleting stale file: {name}", true);
                    try
                    {
                        File.Delete(file);
                        deleted++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Write($"Failed to delete stale file {name}: {ex.Message}", true);
                    }
                }
            }
            if (deleted > 0)
                Logger.Write($"Mirror mode: deleted {GrammarHelper.Pluralise(deleted, "stale file", "stale files")} from destination folder.");
        }
    }
}
