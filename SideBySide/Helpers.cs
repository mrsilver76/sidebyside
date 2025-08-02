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

using IniParser;
using IniParser.Model;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static SideBySide.Program;

namespace SideBySide
{
    public static class Helpers
    {
        /// <summary>
        /// Parses command line arguments.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void ParseArguments(string[] args)
        {
            if (args.Length == 0)
                DisplayUsage();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg == "/?" || arg == "--help" || arg == "-h")
                    DisplayUsage();

                if (arg.StartsWith('-'))
                {
                    switch (arg.ToLowerInvariant())
                    {
                        case "-v":
                        case "--verbose":
                            verboseMode = true;
                            break;

                        case "-w":
                        case "--write":
                            overwriteExisting = true;
                            break;

                        case "-c":
                        case "--clean":
                            deleteExisting = true;
                            break;

                        case "-s":
                        case "--shuffle":
                            randomiseSorting = true;
                            break;
                        case "-r":
                        case "--recursive":
                            recursiveSearch = true;
                            break;
                        case "-g":
                        case "--gap":
                            if (i + 1 >= args.Length || !int.TryParse(args[++i], out middleBarWidth) || middleBarWidth < 0)
                                DisplayUsage("Invalid or missing value for --gap.");
                            break;

                        case "-o":
                        case "--output":
                            if (i + 1 >= args.Length)
                                DisplayUsage("Missing or invalid output directory.");
                            destinationFolder = args[++i];
                            break;

                        case "-d":
                        case "--dimensions":
                            if (i + 1 >= args.Length || !ExtractDimensions(args[++i]))
                                DisplayUsage("Invalid or missing value for --dimensions.");
                            break;
                        case "-f":
                        case "--filelist":
                            if (i + 1 >= args.Length || !File.Exists(args[++i]))
                                DisplayUsage("Missing or invalid file list.");
                            inputFile = args[i];
                            break;
                        case "-m":
                        case "--mirror":
                            mirrorMode = true; 
                            break;
                        default:
                            DisplayUsage($"Unknown option: {arg}");
                            break;
                    }
                }
                else
                {
                    if (!Directory.Exists(arg))
                        DisplayUsage($"Input directory '{arg}' does not exist.");
                    inputDirs.Add(arg);
                }
            }

            // Final checks
            if (inputDirs.Count == 0 && string.IsNullOrEmpty(inputFile))
                DisplayUsage("At least one input directory or --filelist must be specified.");

            if (!string.IsNullOrEmpty(inputFile) && !File.Exists(inputFile))
                DisplayUsage($"File list '{inputFile}' does not exist.");

            if (string.IsNullOrEmpty(destinationFolder))
                DisplayUsage("Missing destination folder. Use --output <dir>");

            if (frameWidth == 0 || frameHeight == 0)
                DisplayUsage("Missing or invalid photo frame dimensions. Use --dimensions <WxH>");

            if (frameWidth <= frameHeight)
                DisplayUsage($"Photo frame width ({frameWidth}) must be greater than height ({frameHeight}).");

            if (middleBarWidth > frameWidth)
                DisplayUsage($"Middle bar width ({middleBarWidth}) cannot exceed frame width ({frameWidth}).");

            // Check if all input directories exist
            foreach (var dir in inputDirs)
                if (!Directory.Exists(dir))
                    DisplayUsage($"Input directory '{dir}' does not exist.");

            // Make sure that source and destination folders are not the same
            if (inputDirs.Any(dir => string.Equals(dir, destinationFolder, StringComparison.OrdinalIgnoreCase)))
                DisplayUsage($"Source and destination folders cannot be the same.");

            // If the destination folder does not exist, create it
            if (destinationFolder != null && !Directory.Exists(destinationFolder))
            {
                try
                {
                    Directory.CreateDirectory(destinationFolder);
                }
                catch (Exception ex)
                {
                    DisplayUsage($"Failed to create output directory '{destinationFolder}': {ex.Message}");
                }
            }

        }

        /// <summary>
        /// Displays the usage information to the user and optional error message.
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        public static void DisplayUsage(string errorMessage = "")
        {
            Console.WriteLine($"Usage: {System.AppDomain.CurrentDomain.FriendlyName} [<input_dir>...] -o <output_dir> -d <WxH> [options]\n" +
                                "Combine two portrait photos into a single landscape image, useful for\n" +
                                "digital photo frames that display vertical images awkwardly.\n");


            if (string.IsNullOrEmpty(errorMessage))
                Console.WriteLine($"This is version {OutputVersion(version)}, copyright © 2024-{DateTime.Now.Year} Richard Lawrence.\n" +
                                    "Image manipulation powered by SkiaSharp (https://github.com/mono/SkiaSharp)\n" +
                                    "Frame icon created by Freepik - Flaticon (https://www.flaticon.com/free-icons/frame)\n");

            Console.WriteLine("Mandatory:\n" +
                                "  <input_dir>             One or more source directories containing portrait\n" +
                                "                          JPEG images. Landscape or square images will be\n" +
                                "                          ignored. At least one input directory or the\n" +
                                "                          --filelist option is required.\n" +
                                "  -o, --output <dir>      Destination directory where combined images will be\n" +
                                "                          saved. Will be created if it doesn't exist.\n" +
                                "  -d, --dimensions <WxH>  Output resolution in the format [width]x[height].\n" +
                                "                          Example: 800x600 (match your photo frame resolution)\n" +
                                "\n" +
                                "Optional:\n" +
                                "  -g, --gap <pixels>      Minimum number of black pixels between the two\n" +
                                "                          images. Use 0 to remove the separator where possible.\n" +
                                "  -s, --shuffle           Shuffle images randomly instead of sorting by date\n" +
                                "                          taken. Timestamps will not be set on output files.\n" +
                                "  -r, --recursive         Recursively search input directories for images.\n" +
                                "  -w, --write             Overwrite existing output files.\n" +
                                "  -c, --clean             Delete previously generated images before starting.\n" +
                                "                          Only .jpg files with the prefix 'sideby-' will be\n" +
                                "                          deleted.\n" +
                                "  -f, --filelist <file>   Load a list of image file paths from a text file.\n" +
                                "                          One per line, absolute paths only. Input\n" +
                                "                          directories are optional with this option.\n" +
                                "  -m, --mirror            Delete any previously generated files that weren't\n" +
                                "                          created as part of this run.\n" +
                                "  -v, --verbose           Display detailed logging to the console.\n" +
                                "  -h, --help              Display this help message and exit.\n" +
                                "\n" +
                               $"Logs are written to {Path.Combine(appDataPath, "Logs")}");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine();
                Console.WriteLine($"Error: {errorMessage}");
                Environment.Exit(-1);
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Defines the location for logs and deletes any old log files
        /// </summary>
        public static void InitialiseLogger()
        {
            // Set the path for the application data folder
            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SideBySide");

            // Set the log folder path to be inside the application data folder
            string logFolderPath = Path.Combine(appDataPath, "Logs");

            // Create the folder if it doesn't exist
            Directory.CreateDirectory(logFolderPath);

            // Delete log files older than 14 days
            var logFiles = Directory.GetFiles(logFolderPath, "*.log");
            foreach (var file in logFiles)
            {
                DateTime lastModified = File.GetLastWriteTime(file);
                if ((DateTime.Now - lastModified).TotalDays > 14)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Logger($"Error deleting log file {file}: {ex.Message}", true);
                    }
                }
            }
        }

        /// <summary>
        /// Writes a message to the log file for debugging.
        /// </summary>
        /// <param name="message">Message to output</param>
        /// <param name="verbose">Verbose output, only for the logs</param>

        public static void Logger(string message, bool verbose = false)
        {
            // Define the path and filename for this log
            string logFile = DateTime.Now.ToString("yyyy-MM-dd");
            logFile = Path.Combine(appDataPath, "Logs", $"log-{logFile}.log");

            // Define the timestamp
            string tsTime = DateTime.Now.ToString("HH:mm:ss");
            string tsDate = DateTime.Now.ToString("yyyy-MM-dd");

            // Write to file
            File.AppendAllText(logFile, $"[{tsDate} {tsTime}] {message}{Environment.NewLine}");

            // If verbose mode is enabled, also write to console
            if (verbose == false || (verbose == true && verboseMode))
                Console.WriteLine($"[{tsTime}] {message}");
        }

        /// <summary>
        /// Output to the logs the environment information, such as .NET version, OS and architecture.
        /// Also includes the parsed command line arguments if any were provided.
        /// </summary>
        /// <param name="args"></param>
        public static void LogEnvironmentInfo(string[] args)
        {
            var dotnet = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();

            var archName = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

            Logger($"Running {OutputVersion(version)} on {dotnet} ({os}, {archName})", true);

            if (args.Length > 0)
                Logger($"Parsed arguments: {string.Join(" ", args)}", true);
        }

        /// <summary>
        /// Checks if there is a later release of the application on GitHub and notifies the user.
        /// </summary>
        public static void CheckLatestRelease()
        {
            string gitHubRepo = "mrsilver76/sidebyside";
            string iniPath = Path.Combine(appDataPath, "versionCheck.ini");

            var parser = new FileIniDataParser();
            IniData ini = File.Exists(iniPath) ? parser.ReadFile(iniPath) : new IniData();

            if (NeedsCheck(ini, out Version? cachedVersion))
            {
                var latest = TryFetchLatestVersion(gitHubRepo);
                if (latest != null)
                {
                    ini["Version"]["LatestReleaseChecked"] = latest.Value.Timestamp;

                    if (!string.IsNullOrEmpty(latest.Value.Version))
                    {
                        ini["Version"]["LatestReleaseVersion"] = latest.Value.Version;
                        cachedVersion = ParseSemanticVersion(latest.Value.Version);
                    }

                    parser.WriteFile(iniPath, ini); // Always write if we got any response at all
                }
            }

            if (cachedVersion != null && cachedVersion > version)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($" ℹ️ A new version ({OutputVersion(cachedVersion)}) is available!");
                Console.ResetColor();
                Console.WriteLine($" You are using {OutputVersion(version)}");
                Console.WriteLine($"    Get it from https://www.github.com/{gitHubRepo}/");
            }
        }

        /// <summary>
        /// Takes a semantic version string in the format "major.minor.revision" and returns a Version object in
        /// the format "major.minor.0.revision"
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns></returns>
        public static Version? ParseSemanticVersion(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return null;

            var parts = versionString.Split('.');
            if (parts.Length != 3)
                return null;

            if (int.TryParse(parts[0], out int major) &&
                int.TryParse(parts[1], out int minor) &&
                int.TryParse(parts[2], out int revision))
            {
                try
                {
                    return new Version(major, minor, 0, revision);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Compares the last checked date and version in the INI file to determine if a check is needed.
        /// </summary>
        /// <param name="ini"></param>
        /// <param name="cachedVersion"></param>
        /// <returns></returns>
        private static bool NeedsCheck(IniData ini, out Version? cachedVersion)
        {
            string dateStr = ini["Version"]["LatestReleaseChecked"];
            string versionStr = ini["Version"]["LatestReleaseVersion"];

            bool hasTimestamp = DateTime.TryParse(dateStr, out DateTime lastChecked);
            bool isExpired = !hasTimestamp || (DateTime.UtcNow - lastChecked.ToUniversalTime()).TotalDays >= 7;

            cachedVersion = ParseSemanticVersion(versionStr);

            return isExpired;
        }

        /// <summary>
        /// Fetches the latest version from the GitHub repo by looking at the releases/latest page.
        /// </summary>
        /// <param name="repo">The name of the repo</param>
        /// <returns>Version and today's date and time</returns>
        private static (string? Version, string Timestamp)? TryFetchLatestVersion(string repo)
        {
            string url = $"https://api.github.com/repos/{repo}/releases/latest";
            using var client = new HttpClient();

            string ua = repo.Replace('/', '.') + "/" + OutputVersion(version);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);

            try
            {
                var response = client.GetAsync(url).GetAwaiter().GetResult();
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                if (!response.IsSuccessStatusCode)
                {
                    // Received response, but it's a client or server error (e.g., 404, 500)
                    return (null, timestamp);  // Still update "last checked"
                }

                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var match = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                if (!match.Success)
                {
                    return (null, timestamp);  // Response body not as expected
                }

                string version = match.Groups[1].Value.TrimStart('v', 'V');
                return (version, timestamp);
            }
            catch
            {
                // This means we truly couldn't reach GitHub at all
                return null;
            }
        }

        /// <summary>
        /// Pluralises a string based on the number provided.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="singular"></param>
        /// <param name="plural"></param>
        /// <returns></returns>
        public static string Pluralise(int number, string singular, string plural)
        {
            return number == 1 ? $"{number} {singular}" : $"{number:N0} {plural}";
        }

        /// <summary>
        /// Given a .NET Version object, outputs the version in a semantic version format.
        /// If the build number is greater than 0, it appends `-preX` to the version string.
        /// </summary>
        /// <returns></returns>
        public static string OutputVersion(Version? netVersion)
        {
            if (netVersion == null)
                return "0.0.0";

            // Use major.minor.revision from version, defaulting patch to 0 if missing
            int major = netVersion.Major;
            int minor = netVersion.Minor;
            int revision = netVersion.Revision >= 0 ? netVersion.Revision : 0;

            // Build the base semantic version string
            string result = $"{major}.{minor}.{revision}";

            // Append `-preX` if build is greater than 0
            if (netVersion.Build > 0)
                result += $"-pre{netVersion.Build}";

            return result;
        }
    }
}
