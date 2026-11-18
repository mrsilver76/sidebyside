/*
 * SideBySide - Combine two portrait photos into a single landscape image,
 * useful for digital photo frames that display vertical images awkwardly.
 * Copyright (C) 2024 Richard Lawrence
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
    /// Handles the collection of image files from specified directories and file lists.
    /// </summary>
    internal sealed class ImageFileCollector
    {
        /// <summary>
        /// Collects all image files from the specified input directories and input file list,
        /// then populates Globals.ImageFileList with the results.
        /// </summary>
        /// <returns>True if at least one image file was found, otherwise false.</returns>
        public static bool GetAllImageFiles()
        {
            Globals.ImageFileList.Clear();

            // Look for files in the specified input directories
            foreach (string dir in Globals.InputDirs)
                GetImageFilesFromFolder(dir);

            // Look for files in the specified input file list. If the user specifies both input directories
            // and a file list, we combine the results from both sources.
            if (!string.IsNullOrEmpty(Globals.InputFile) && File.Exists(Globals.InputFile))
                GetImageFilesFromFileList();

            // If no image files were found, log a warning and exit
            if (Globals.ImageFileList.Count == 0)
            {
                Logger.Write("No image files found to process. Please check your input directories or file list.");
                return false;
            }

            Logger.Write($"Found {GrammarHelper.Pluralise(Globals.ImageFileList.Count, "image file", "image files")} to process.");
            return true;
        }

        /// <summary>
        /// Given a source folder, searches for all JPEG files (both .jpg and .jpeg) and adds them to Globals.ImageFileList.
        /// </summary>
        /// <param name="sourceFolder">The folder to search.</param>
        private static void GetImageFilesFromFolder(string sourceFolder)
        {
            // IgnoreInaccessible is on by default, so unreadable sub-folders are skipped instead of throwing
            var options = new EnumerationOptions { RecurseSubdirectories = Globals.RecursiveSearch };

            string recursiveNote = "";
            if (Globals.RecursiveSearch)
                recursiveNote = " (and sub-directories)";

            Logger.Write($"Looking for images in: {sourceFolder}{recursiveNote}");

            var files = Directory.EnumerateFiles(sourceFolder, "*.*", options)
                .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

            Globals.ImageFileList.AddRange(files);
        }

        /// <summary>
        /// Loads the image file paths from a specified file list, filtering for JPEG files (.jpg and .jpeg). These
        /// paths are added to Globals.ImageFileList for further processing.
        /// </summary>
        private static void GetImageFilesFromFileList()
        {
            Logger.Write($"Looking for images in file: {Globals.InputFile}");

            // Read all lines from the file, trim whitespace, and filter for .jpg/.jpeg files
            var lines = File.ReadAllLines(Globals.InputFile)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .Where(line => line.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               line.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

            // Add valid files to the global list, logging a warning for any that don't exist
            foreach (var file in lines)
            {
                if (File.Exists(file))
                    Globals.ImageFileList.Add(file);
                else
                    Logger.Write($"Warning: '{file}' listed in file list does not exist.", true);
            }
        }
    }
}
