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

using System.Globalization;

namespace SideBySide
{
    /// <summary>
    /// Handles parsing of command line arguments.
    /// </summary>
    internal sealed class CommandLineParser
    {
        /// <summary>Largest value accepted for --proximity = 24 hours (same as look-ahead window)</summary>
        private const int MaxProximitySeconds = 86400;

        /// <summary>Command line options that must be followed by a value</summary>
        private static readonly HashSet<string> OptionsWithValue =
        [
            "-g", "--gap",
            "-p", "--proximity",
            "-o", "--output",
            "-d", "--dimensions",
            "-f", "--filelist"
        ];

        /// <summary>
        /// Parses command line arguments.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void ParseArguments(string[] args)
        {
            if (args.Length == 0)
                ConsoleOutput.ShowUsage();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower(CultureInfo.InvariantCulture);

                // Every option that takes a value must have one following it

                if (OptionsWithValue.Contains(arg) && i + 1 >= args.Length)
                    ConsoleOutput.ShowUsage($"Option '{arg}' requires a value.");

                if (arg == "/?" || arg == "--help" || arg == "-h")
                    ConsoleOutput.ShowUsage();
                else if (arg == "-v" || arg == "--verbose")
                    Globals.VerboseMode = true;
                else if (arg == "-w" || arg == "--write")
                    Globals.OverwriteExisting = true;
                else if (arg == "-c" || arg == "--clean")
                    Globals.DeleteExisting = true;
                else if (arg == "-s" || arg == "--shuffle")
                    Globals.RandomiseSorting = true;
                else if (arg == "-r" || arg == "--recursive")
                    Globals.RecursiveSearch = true;
                else if (arg == "-g" || arg == "--gap")
                {
                    if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int gap))
                    {
                        Globals.MiddleBarWidth = gap;
                        i++;  // Skip the next argument as it's the value
                    }
                    else
                        ConsoleOutput.ShowUsage("Invalid or missing value for --gap.");
                }
                else if (arg == "-p" || arg == "--proximity")
                {
                    if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int proximity))
                    {
                        Globals.ProximitySeconds = proximity;
                        i++;  // Skip the next argument as it's the value
                    }
                    else
                        ConsoleOutput.ShowUsage("Invalid or missing value for --proximity.");
                }
                else if (arg == "-o" || arg == "--output")
                {
                    // A value that looks like another option means the directory was left out
                    if (args[i + 1].StartsWith('-'))
                        ConsoleOutput.ShowUsage("Missing or invalid output directory.");
                    Globals.DestinationFolder = args[i + 1];
                    i++;  // Skip the next argument as it's the value
                }
                else if (arg == "-d" || arg == "--dimensions")
                {
                    if (args[i + 1].StartsWith('-') || !ImageMetadataExtractor.ExtractDimensions(args[i + 1]))
                        ConsoleOutput.ShowUsage($"Invalid or missing value for {arg}.");
                    i++;  // Skip the next argument as it's the value
                }
                else if (arg == "-f" || arg == "--filelist")
                {
                    if (args[i + 1].StartsWith('-') || !File.Exists(args[i + 1]))
                        ConsoleOutput.ShowUsage("Missing or invalid file list.");
                    Globals.InputFile = args[i + 1];
                    i++;  // Skip the next argument as it's the value
                }
                else if (arg == "-m" || arg == "--mirror")
                    Globals.MirrorMode = true;
                else if (arg == "-nc" || arg == "--no-check")
                    Globals.GitHubVersionCheck = false;
                else if (arg.StartsWith('-'))
                    ConsoleOutput.ShowUsage($"Unknown option: {arg}");
                else
                    Globals.InputDirs.Add(args[i]);
            }

            // Now validate the arguments
            ValidateArguments();
        }

        /// <summary>
        /// Returns the full path with any trailing directory separator removed, so that different
        /// spellings of the same folder (relative, trailing slash) compare equal. The path need not exist.
        /// </summary>
        /// <param name="path">The path to normalise.</param>
        /// <returns>The full path without a trailing directory separator.</returns>
        internal static string NormalisePath(string path)
        {
            string full = Path.GetFullPath(path);
            string root = Path.GetPathRoot(full) ?? "";

            // "C:\" or "/" must keep their trailing separator
            if (full.Length <= root.Length)
                return full;

            return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// True if two paths refer to the same folder. Case is ignored on Windows and macOS,
        /// whose default file systems are case-insensitive, and respected on Linux.
        /// </summary>
        /// <param name="a">The first path.</param>
        /// <param name="b">The second path.</param>
        /// <returns>True if both paths refer to the same folder.</returns>
        internal static bool IsSamePath(string a, string b)
        {
            StringComparison comparison = StringComparison.Ordinal;

            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
                comparison = StringComparison.OrdinalIgnoreCase;

            return string.Equals(NormalisePath(a), NormalisePath(b), comparison);
        }

        /// <summary>
        /// Validates the parsed command line arguments and displays usage information along with
        /// an error message if any are invalid.
        /// </summary>
        private static void ValidateArguments()
        {
            // Check that values supplied are valid and sane
            if (Globals.InputDirs.Count == 0 && string.IsNullOrEmpty(Globals.InputFile))
                ConsoleOutput.ShowUsage("At least one input directory or --filelist must be specified.");

            if (!string.IsNullOrEmpty(Globals.InputFile) && !File.Exists(Globals.InputFile))
                ConsoleOutput.ShowUsage($"File list '{Globals.InputFile}' does not exist.");

            if (string.IsNullOrEmpty(Globals.DestinationFolder))
                ConsoleOutput.ShowUsage("Missing destination folder. Use --output <dir>");

            if (Globals.FrameWidth == 0 || Globals.FrameHeight == 0)
                ConsoleOutput.ShowUsage("Missing or invalid photo frame dimensions. Use --dimensions <WxH>");

            if (Globals.FrameWidth <= Globals.FrameHeight)
                ConsoleOutput.ShowUsage($"Photo frame width ({Globals.FrameWidth}) must be greater than height ({Globals.FrameHeight}).");

            if (Globals.MiddleBarWidth < 0)
                ConsoleOutput.ShowUsage($"Invalid value for --gap ({Globals.MiddleBarWidth}). Use 0 or more.");

            if (Globals.ProximitySeconds < 0 || Globals.ProximitySeconds > MaxProximitySeconds)
                ConsoleOutput.ShowUsage($"Invalid value for --proximity ({Globals.ProximitySeconds}). Use 0 to {MaxProximitySeconds}.");

            if (Globals.MiddleBarWidth > Globals.FrameWidth)
                ConsoleOutput.ShowUsage($"Middle bar width ({Globals.MiddleBarWidth}) cannot exceed frame width ({Globals.FrameWidth}).");

            // Check if all input directories exist
            foreach (var dir in Globals.InputDirs)
                if (!Directory.Exists(dir))
                    ConsoleOutput.ShowUsage($"Input directory '{dir}' does not exist.");

            // Make sure that source and destination folders are not the same
            if (Globals.InputDirs.Any(dir => IsSamePath(dir, Globals.DestinationFolder)))
                ConsoleOutput.ShowUsage("Source and destination folders cannot be the same.");

            // If the destination folder does not exist, create it
            if (Globals.DestinationFolder != null && !Directory.Exists(Globals.DestinationFolder))
            {
                try
                {
                    Directory.CreateDirectory(Globals.DestinationFolder);
                }
                catch (Exception ex)
                {
                    ConsoleOutput.ShowUsage($"Failed to create output directory '{Globals.DestinationFolder}': {ex.Message}");
                }
            }
        }
    }
}
