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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace SideBySide
{
    /// <summary>
    /// Writes the usage text, the run header and the update notice to the console.
    /// </summary>
    internal sealed class ConsoleOutput
    {
        /// <summary>
        /// Displays the usage information to the user and optional error message. This method never returns.
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        [DoesNotReturn]
        public static void ShowUsage(string errorMessage = "")
        {
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [<input_dir>...] -o <output_dir> -d <WxH> [options]\n" +
                              $"Combine two portrait photos into a single landscape image, useful for digital photo frames that display vertical images awkwardly.\n");

            if (string.IsNullOrEmpty(errorMessage))
                Console.WriteLine($"This is version {VersionHelper.OutputVersion(Globals.ProgramVersion)}, copyright © 2024-{DateTime.Now.Year} Richard Lawrence.\n" +
                                  $"Image manipulation powered by SkiaSharp (https://github.com/mono/SkiaSharp)\n" +
                                  $"Frame icon created by Freepik - Flaticon (https://www.flaticon.com/free-icons/frame)\n");

            PrintOptionsWithDescriptions();

            Console.WriteLine($"Logs are written to {Path.Combine(Globals.AppDataPath, "Logs")}");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine();
                Console.WriteLine($"Error: {errorMessage}");
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }

        /// <summary>
        /// Prints each section of command line options with its descriptions. Descriptions are wrapped to the
        /// console width, with wrapped lines and sub-options indented.
        /// </summary>
        private static void PrintOptionsWithDescriptions()
        {
            // Define sections and their options + descriptions
            Dictionary<string, (string Option, string Description)[]> sections = new()
            {
                ["Mandatory"] =
                [
                    ("<input_dir>", "One or more source directories containing portrait JPEG images. Landscape or square images are ignored. At least one input directory or the --filelist option is required."),
                    ("-o, --output <dir>", "Destination directory where combined images will be saved. Will be created if it doesn't exist."),
                    ("-d, --dimensions <WxH>", "Output resolution, e.g. 800x600 (match your photo frame).")
                ],
                ["Input control"] =
                [
                    ("-r, --recursive", "Recursively search input directories for images."),
                    ("-f, --filelist <file>", "Load a list of image paths (absolute, one per line). Input directories are optional with this option.")
                ],
                ["Output control"] =
                [
                    ("-g, --gap <pixels>", "Minimum number of black pixels between images. Default 16. Use 0 to remove the separator where possible."),
                    ("-p, --proximity <seconds>", "Try to avoid pairing photos taken fewer than this many seconds apart. If no other photo from the same day is available they are paired anyway. Default 120. Use 0 to turn off."),
                    ("-w, --write", "Overwrite existing output files."),
                    ("-c, --clean", "Delete previously generated 'sideby-' JPGs before starting."),
                    ("-m, --mirror", "Delete any previously generated files not created in this run.")
                ],
                ["Miscellaneous"] =
                [
                    ("-s, --shuffle", "Shuffle images randomly instead of sorting by date taken. Timestamps will not be set on output files."),
                    ("-nc, --no-check", "Don't check GitHub for later versions."),
                    ("-v, --verbose", "Display detailed logging to the console."),
                    ("-h, --help", "Display this help message and exit.")
                ]
            };

            // Determine max line width (at least 50 chars, or console width - 5).
            // Console.WindowWidth throws when output is redirected, so use 80 columns then.
            int consoleWidth = 80;
            if (!Console.IsOutputRedirected)
                consoleWidth = Console.WindowWidth;

            int maxLineWidth = Math.Max(consoleWidth, 50) - 5;

            // Find max option length across all sections for consistent column width
            int firstColWidth = 0;

            foreach (var section in sections.Values)
                foreach (var (option, _) in section)
                    firstColWidth = Math.Max(firstColWidth, option.Length);

            firstColWidth += 2;  // 2-character gap

            // Print each section followed by the options
            foreach (var (header, options) in sections)
            {
                // Section header
                Console.WriteLine($"{header}:");

                // Each option + description
                foreach (var (option, description) in options)
                {
                    // Wrap description
                    var wrapped = WrapText(description, maxLineWidth - firstColWidth - 1);  // -1 for extra indent
                    bool firstLine = true;

                    foreach (var line in wrapped)
                    {
                        if (firstLine)
                        {
                            // First line: 1-character indent, then the option, then the description.
                            Console.WriteLine($" {option.PadRight(firstColWidth)}{line}");
                            firstLine = false;
                        }
                        else
                        {
                            // Wrapped lines: 1-character indent, then firstColWidth spaces, 1 space and the text.
                            Console.WriteLine(new string(' ', firstColWidth + 2) + line);
                        }
                    }
                }

                Console.WriteLine();  // Blank line between sections.
            }

        }

        /// <summary>
        /// Displays the header information for the application, including version, copyright, and settings.
        /// </summary>
        public static void ShowHeader()
        {
            string line = new('─', 70);

            Console.WriteLine(line);
            Console.WriteLine($"SideBySide {VersionHelper.OutputVersion(Globals.ProgramVersion)}");
            Console.WriteLine($"Copyright © 2024-{DateTime.Now.Year} Richard Lawrence");
            Console.WriteLine("https://github.com/mrsilver76/sidebyside");
            Console.WriteLine();

            // Prepare titles + content
            List<(string Title, string Value)> items = [];

            // Source files
            if (!string.IsNullOrEmpty(Globals.InputFile))
                items.Add(("Source file:", Globals.InputFile));

            // Source directories
            if (Globals.InputDirs.Count == 1)
                items.Add(("Source dir:", Globals.InputDirs[0]));
            else if (Globals.InputDirs.Count > 1)
            {
                items.Add(("Sources:", Globals.InputDirs[0]));
                foreach (var folder in Globals.InputDirs.Skip(1))
                    items.Add(("", folder));  // continuation lines
            }

            // Output size of the image (frame dimensions)
            items.Add(("Output size:", $"{Globals.FrameWidth}x{Globals.FrameHeight}"));

            // Size of the middle black bar
            items.Add(("Middle gap:", $"{Globals.MiddleBarWidth} pixels"));

            // How close together in time photos can be before we try not to pair them
            if (Globals.ProximitySeconds == 0)
                items.Add(("Proximity:", "Off"));
            else
                items.Add(("Proximity:", GrammarHelper.DescribeSeconds(Globals.ProximitySeconds)));

            // Only the options that change the run are shown
            if (Globals.RandomiseSorting)
                items.Add(("Order:", "Shuffled"));

            List<string> existing = [];
            if (Globals.OverwriteExisting)
                existing.Add("Overwrite");
            if (Globals.DeleteExisting)
                existing.Add("Clean");
            if (Globals.MirrorMode)
                existing.Add("Mirror");
            if (existing.Count > 0)
                items.Add(("Existing files:", string.Join(", ", existing)));

            if (Globals.RecursiveSearch)
                items.Add(("Recursive:", "Yes"));

            // Output directory
            items.Add(("Output dir:", Globals.DestinationFolder));

            // Find longest title length
            int pad = items.Max(i => i.Title.Length) + 2;

            // Print everything
            foreach (var (title, value) in items)
                Console.WriteLine($"{title.PadRight(pad)}{value}");

            Console.WriteLine(line);
            Console.WriteLine();

            // Log environment info
            LogEnvironmentInfo();
        }

        /// <summary>
        /// Output to the logs the environment information, such as .NET version, OS and architecture.
        /// Also includes the full command line.
        /// </summary>
        private static void LogEnvironmentInfo()
        {
            string dotnet = RuntimeInformation.FrameworkDescription;
            string os = RuntimeInformation.OSDescription.Trim();

            string archName = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

            Logger.Write($"Running {VersionHelper.OutputVersion(Globals.ProgramVersion)} on {dotnet} ({os}, {archName})", true);

            Logger.Write($"Command line: {Environment.CommandLine}", true);
        }

        /// <summary>
        /// Checks if there is a later release of the application on GitHub and notifies the user.
        /// </summary>
        public static void CheckLatestRelease()
        {
            // Skip if disabled
            if (!Globals.GitHubVersionCheck)
                return;

            string gitRepo = "mrsilver76/sidebyside";
            var result = GitHubVersionChecker.CheckLatestRelease(Globals.ProgramVersion, gitRepo, Path.Combine(Globals.AppDataPath, "versionCheck.ini"));

            if (result.UpdateAvailable)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"  ℹ️ A new version ({VersionHelper.OutputVersion(result.LatestVersion)}) is available!");
                Console.ResetColor();
                Console.WriteLine($" You are using {VersionHelper.OutputVersion(Globals.ProgramVersion)}");
                Console.WriteLine($"     Get it from https://www.github.com/{gitRepo}/");
            }
        }

        /// <summary>
        /// Given a block of text and a maximum line width, yields lines of text wrapped at word boundaries.
        /// </summary>
        /// <param name="text">The text to wrap.</param>
        /// <param name="maxWidth">The longest line allowed, in characters.</param>
        /// <returns>The wrapped lines, without line breaks.</returns>
        private static IEnumerable<string> WrapText(string text, int maxWidth)
        {
            var words = text.Split(' ');
            var line = new StringBuilder();

            foreach (var word in words)
            {
                int separatorWidth = 0;
                if (line.Length > 0)
                    separatorWidth = 1;

                if (line.Length + word.Length + separatorWidth > maxWidth)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                if (line.Length > 0)
                    line.Append(' ');

                line.Append(word);
            }

            if (line.Length > 0)
                yield return line.ToString();
        }
    }
}
