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

using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SideBySide
{
    internal sealed class ConsoleOutput
    {
        /// <summary>
        /// Displays the usage information to the user and optional error message.
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        public static void ShowUsage(string errorMessage = "")
        {
            Console.WriteLine($"Usage: {System.AppDomain.CurrentDomain.FriendlyName} [<input_dir>...] -o <output_dir> -d <WxH> [options]\n" +
                                "Combine two portrait photos into a single landscape image, useful for\n" +
                                "digital photo frames that display vertical images awkwardly.\n");


            if (string.IsNullOrEmpty(errorMessage))
                Console.WriteLine($"This is version {VersionHelper.OutputVersion(Globals.ProgramVersion)}, copyright © 2024-{DateTime.Now.Year} Richard Lawrence.\n" +
                                    "Image manipulation powered by SkiaSharp (https://github.com/mono/SkiaSharp)\n" +
                                    "Frame icon created by Freepik - Flaticon (https://www.flaticon.com/free-icons/frame)\n");

            PrintOptionsWithDescriptions();

            Console.WriteLine($"Logs are written to {Path.Combine(Globals.AppDataPath, "Logs")}");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine();
                Console.WriteLine($"Error: {errorMessage}");
                Environment.Exit(-1);
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Outputs the command line options along with their descriptions in a formatted manner.
        /// Each section is clearly separated, and descriptions are wrapped to fit within the console width.
        /// Wrapped lines are indented for readability and sub-options are indented for clarity.
        /// </summary>
        static void PrintOptionsWithDescriptions()
        {
            // Define sections and their options + descriptions
            var sections = new Dictionary<string, (string option, string description)[]>
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
                ("-g, --gap <pixels>", "Minimum number of black pixels between images. Use 0 to remove the separator where possible."),
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

            // Determine max line width (at least 50 chars, or console width - 5)
            int maxLineWidth = Math.Max(Console.WindowWidth, 50) - 5;

            // Find max option length across all sections for consistent column width
            int firstColWidth = 0;
            foreach (var section in sections.Values)
                foreach (var (option, _) in section)
                    firstColWidth = Math.Max(firstColWidth, option.Length);
            firstColWidth += 2; // 2-character gap

            // Print each section followed by the options
            foreach (var (header, options) in sections)
            {
                // Section header
                Console.WriteLine(header + ":");

                // Each option + description
                foreach (var (option, description) in options)
                {
                    // Wrap description
                    var wrapped = WrapText(description, maxLineWidth - firstColWidth - 1); // -1 for extra indent
                    bool firstLine = true;

                    foreach (var line in wrapped)
                    {
                        if (firstLine)
                        {
                            // first line: 1-char indent + option + description
                            Console.WriteLine(" " + option.PadRight(firstColWidth) + line);
                            firstLine = false;
                        }
                        else
                        {
                            // wrapped lines: 1-char indent + firstColWidth spaces + 1 space + text
                            Console.WriteLine(new string(' ', firstColWidth + 2) + line);
                        }
                    }
                }

                Console.WriteLine(); // single newline between sections
            }

        }

        /// <summary>
        /// Displays the header information for the application, including version, copyright, and settings.
        /// </summary>
        /// <param name="args"></param>
        public static void ShowHeader(string[] args)
        {
            Console.WriteLine(new string('-', 70));
            WriteLeftRight(
                $"\x1b[1;33mSideBySide v{VersionHelper.OutputVersion(Globals.ProgramVersion)}\x1b[0m",
                $"Copyright © 2024-{DateTime.Now.Year} Richard Lawrence"
            );
            Console.WriteLine("\x1b[3mCombine two portrait photos into a single landscape image.\x1b[0m");
            WriteLeftRight("GNU GPL v2 or later", "https://github.com/mrsilver76/sidebyside");
            Console.WriteLine(new string('-', 70));

            // Prepare titles + content
            var items = new List<(string Title, string Value)>();

            // Source files
            if (!string.IsNullOrEmpty(Globals.InputFile))
                items.Add(("Source file:", Globals.InputFile));

            // Source directories
            if (Globals.InputDirs.Count == 1)
                items.Add(("Source dir:", Globals.InputDirs[0]));
            else if (Globals.InputDirs.Count > 1)
            {
                items.Add(("Sources dirs:", Globals.InputDirs[0]));
                foreach (var folder in Globals.InputDirs.Skip(1))
                    items.Add(("", folder)); // continuation lines
            }

            // Output size of the image (frame dimensions)
            items.Add(("Output size:", $"{Globals.FrameWidth}x{Globals.FrameHeight}"));

            // Size of the middle black bar
            items.Add(("Middle gap:", $"{Globals.MiddleBarWidth} pixels"));

            // Other flags
            if (CommandLineParser.ParsedFlags.Count > 0)
                items.Add(("Other flags:", string.Join(", ", CommandLineParser.ParsedFlags)));

            // Output directory
            items.Add(("Output dir:", Globals.DestinationFolder));

            // Find longest title length
            int pad = items.Max(i => i.Title.Length) + 2;

            // Print everything
            foreach (var (title, value) in items)
                Console.WriteLine($"{title.PadRight(pad)}{value}");

            Console.WriteLine(new string('-', 70));
            Console.WriteLine();

            // Log environment info
            LogEnvironmentInfo(args);
        }

        /// <summary>
        /// Writes two strings, one aligned to the left and the other to the right, within a specified total width.
        /// </summary>
        /// <remarks>If the combined visible length of the <paramref name="left"/> and <paramref
        /// name="right"/> strings  exceeds the <paramref name="totalWidth"/>, the strings are written directly next to
        /// each other  with a single space in between. ANSI escape sequences (e.g., for text formatting) are ignored 
        /// when calculating the visible length of the strings.</remarks>
        /// <param name="left">The string to be displayed on the left side of the output.</param>
        /// <param name="right">The string to be displayed on the right side of the output.</param>
        /// <param name="totalWidth">The total width of the output, including both strings and any padding between them.  Defaults to 70 if not
        /// specified.</param>
        public static void WriteLeftRight(string left, string right, int totalWidth = 70)
        {
            // Regex to remove ANSI escape sequences
            string ansiRegex = @"\x1B\[[0-9;]*m";

            int visibleLeftLength = Regex.Replace(left, ansiRegex, "").Length;
            int visibleRightLength = Regex.Replace(right, ansiRegex, "").Length;

            if (visibleLeftLength + visibleRightLength >= totalWidth)
                Console.WriteLine(left + " " + right);
            else
                Console.WriteLine(left + new string(' ', totalWidth - visibleLeftLength - visibleRightLength) + right);
        }

        /// <summary>
        /// Output to the logs the environment information, such as .NET version, OS and architecture.
        /// Also includes the parsed command line arguments if any were provided.
        /// </summary>
        /// <param name="args"></param>
        private static void LogEnvironmentInfo(string[] args)
        {
            var dotnet = RuntimeInformation.FrameworkDescription;
            var os = RuntimeInformation.OSDescription.Trim();

            var archName = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

            Logger.Write($"Running {VersionHelper.OutputVersion(Globals.ProgramVersion)} on {dotnet} ({os}, {archName})", true);

            if (args.Length > 0)
                Logger.Write($"Parsed arguments: {string.Join(" ", args)}", true);
        }

        /// <summary>
        /// Checks if there is a later release of the application on GitHub and notifies the user.
        /// </summary>
        public static void CheckLatestRelease()
        {
            // Skip if disabled
            if (Globals.GitHubVersionCheck == false)
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
        /// <param name="text"></param>
        /// <param name="maxWidth"></param>
        /// <returns></returns>
        private static IEnumerable<string> WrapText(string text, int maxWidth)
        {
            var words = text.Split(' ');
            var line = new StringBuilder();

            foreach (var word in words)
            {
                if (line.Length + word.Length + (line.Length > 0 ? 1 : 0) > maxWidth)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                if (line.Length > 0) line.Append(' ');
                line.Append(word);
            }

            if (line.Length > 0) yield return line.ToString();
        }
    }
}
