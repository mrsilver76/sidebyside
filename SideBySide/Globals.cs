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

using System.Reflection;

namespace SideBySide
{
    /// <summary>
    /// All global variables used by the application.
    /// </summary>
    public static class Globals
    {
        // Variables that can be set by the user via command line arguments
        #region User-defined variables

        /// <summary>List of input directories to scan for images</summary>
        public static List<string> InputDirs { get; set; } = [];

        /// <summary>Destination folder for processed images</summary>
        public static string DestinationFolder { get; set; } = "";

        /// <summary>Width of the digital frame</summary>
        public static int FrameWidth { get; set; }

        /// <summary>Height of the digital frame</summary>
        public static int FrameHeight { get; set; }

        /// <summary>Width of the black middle bar between images</summary>
        public static int MiddleBarWidth { get; set; }

        /// <summary>Flag for verbose output</summary>
        public static bool VerboseMode { get; set; }

        /// <summary>Flag to overwrite existing images</summary>
        public static bool OverwriteExisting { get; set; }

        /// <summary>Flag to delete existing images before processing</summary>
        public static bool DeleteExisting { get; set; }

        /// <summary>Flag to randomise the order of images</summary>
        public static bool RandomiseSorting { get; set; }

        /// <summary>Flag to search directories recursively</summary>
        public static bool RecursiveSearch { get; set; }

        /// <summary>Input file containing image paths (if any)</summary>
        public static string InputFile { get; set; } = "";

        /// <summary>Flag to enable mirror mode, which deletes stale files in the destination folder</summary>
        public static bool MirrorMode { get; set; }

        /// <summary>Flag to check if there is a later version on GitHub</summary>
        public static bool GitHubVersionCheck { get; set; } = true;

        #endregion

        // Variables used internally by the application
        #region Internal variables

        /// <summary>Class to hold information about each image</summary>
        public class ImageInfo
        {
            /// <summary>Full path to the image file</summary>
            public required string FullPath { get; set; }

            /// <summary>Name of the image file</summary>
            public required string FileName { get; set; }

            /// <summary>Creation date of the image file</summary>
            public DateTime CreationDate { get; set; }

            /// <summary>Width of the image file</summary>
            public int FileWidth { get; set; }

            /// <summary>Height of the image file</summary>
            public int FileHeight { get; set; }
        }

        /// <summary>List of images to be processed</summary>
        public static List<ImageInfo> Images { get; set; } = [];

        /// <summary>Version of the application</summary>
        public static Version ProgramVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version!;

        /// <summary>Path to the app data folder</summary>
        public static string AppDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SideBySide");

        /// <summary>List of image files found during scanning</summary>
        public static List<string> ImageFileList { get; set; } = [];

        /// <summary>Set to track processed files</summary>
        public static HashSet<string> ProcessedFiles { get; set; } = [];

        #endregion
    }
}
