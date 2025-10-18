/*
 * SideBySide - Combine two portrait photos into a single landscape image,
 * useful for digital photo frames that display vertical images awkwardly.
 * Copyright (C) 2024-2025 Richard Lawrence
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
        /// <summary>The list of parsed flags for reporting purposes.</summary>
        public static List<string> ParsedFlags = [];

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

                if (arg == "/?" || arg == "--help" || arg == "-h")
                    ConsoleOutput.ShowUsage();
                else if (arg == "-v" || arg == "--verbose")
                {
                    Globals.VerboseMode = true;
                    ParsedFlags.Add("Verbose");
                }
                else if (arg == "-w" || arg == "--write")
                {
                    Globals.OverwriteExisting = true;
                    ParsedFlags.Add("Overwrite");
                }
                else if (arg == "-c" || arg == "--clean")
                {
                    Globals.DeleteExisting = true;
                    ParsedFlags.Add("Clean");
                }
                else if (arg == "-s" || arg == "--shuffle")
                {
                    Globals.RandomiseSorting = true;
                    ParsedFlags.Add("Shuffle");
                }
                else if (arg == "-r" || arg == "--recursive")
                {
                    Globals.RecursiveSearch = true;
                    ParsedFlags.Add("Recursive");
                }
                else if (arg == "-g" || arg == "--gap")
                {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int gap) && gap >= 0)
                    {
                        Globals.MiddleBarWidth = gap;
                        i++; // Skip the next argument as it's the value
                    }
                    else
                        ConsoleOutput.ShowUsage("Invalid or missing value for --gap.");
                }
                else if (arg == "-o" || arg == "--output")
                {
                    if (i + 1 < args.Length)
                    {
                        Globals.DestinationFolder = args[i + 1];
                        i++; // Skip the next argument as it's the value
                    }
                    else
                        ConsoleOutput.ShowUsage("Missing or invalid output directory.");
                }
                else if (arg == "-d" || arg == "--dimensions")
                {
                    if (i + 1 < args.Length && !ImageMetadataExtractor.ExtractDimensions(args[i + 1]))
                        ConsoleOutput.ShowUsage("Invalid or missing value for --dimensions.");
                    i++; // Skip the next argument as it's the value
                }
                else if (arg == "-f" || arg == "--filelist")
                {
                    if (i + 1 >= args.Length || !File.Exists(args[i+1]))
                        ConsoleOutput.ShowUsage("Missing or invalid file list.");
                    Globals.InputFile = args[i+1];
                    i++; // Skip the next argument as it's the value
                }
                else if (arg == "-m" || arg == "--mirror")
                {
                    Globals.MirrorMode = true;
                    ParsedFlags.Add("Mirror");
                }
                else if (arg == "-nc" || arg == "--no-check")
                {
                    Globals.GitHubVersionCheck = false;
                    ParsedFlags.Add("NoCheck");
                }

                else if (arg.StartsWith('-'))
                    ConsoleOutput.ShowUsage($"Unknown option: {arg}");

                else
                    Globals.InputDirs.Add(arg);
            }

            // Now validate the arguments
            ValidateArguments();
        }

        /// <summary>
        /// Validates the parsed command line arguments and displays usage information along with
        /// an error message if any are invalid.
        /// </summary>
        private static void ValidateArguments()
        {
            // Final checks
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

            if (Globals.MiddleBarWidth > Globals.FrameWidth)
                ConsoleOutput.ShowUsage($"Middle bar width ({Globals.MiddleBarWidth}) cannot exceed frame width ({Globals.FrameWidth}).");

            // Check if all input directories exist
            foreach (var dir in Globals.InputDirs)
                if (!Directory.Exists(dir))
                    ConsoleOutput.ShowUsage($"Input directory '{dir}' does not exist.");

            // Make sure that source and destination folders are not the same
            if (Globals.InputDirs.Any(dir => string.Equals(dir, Globals.DestinationFolder, StringComparison.OrdinalIgnoreCase)))
                ConsoleOutput.ShowUsage($"Source and destination folders cannot be the same.");

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
