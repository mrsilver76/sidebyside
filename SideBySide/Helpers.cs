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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;
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

            // Loop through all arguments
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();

                if (arg == "/?" || arg == "--help" || arg == "-h")
                {
                    DisplayUsage();
                }

                if (arg.StartsWith('-'))
                {
                    if (arg == "-v" || arg == "--verbose")
                        verboseMode = true;
                    else if (arg == "-o" || arg == "--overwrite")
                        overwriteExisting = true;
                    else if (arg == "-d" || arg == "--delete")
                        deleteExisting = true;
                    else if (arg == "-r" || arg == "--random")
                        randomiseSorting = true;
                    else
                        DisplayUsage($"Unknown option: {arg}");
                }
                else
                {
                    if (string.IsNullOrEmpty(sourceFolder))
                    {
                        if (System.IO.Directory.Exists(arg))
                            sourceFolder = arg;
                        else
                            DisplayUsage($"Source folder '{arg}' does not exist.");
                    }
                    else if (string.IsNullOrEmpty(destinationFolder))
                    {
                        if (System.IO.Directory.Exists(arg))
                            destinationFolder = arg;
                        else
                            DisplayUsage($"Destination folder '{arg}' does not exist.");
                    }
                    else if (frameHeight == 0 && frameWidth == 0)
                    {
                        if (ExtractDimensions(arg) == false)
                            DisplayUsage("Photo frame dimensions are not in the format: [width]x[height]");
                    }
                    else 
                    {
                        if (int.TryParse(arg, out int result))
                        {
                            if (result < 0)
                                DisplayUsage("Minimum frame gap cannot be a negative number.");
                            else
                                middleBarWidth = result;
                        }
                    }
                }
            }
       

            // Sanity checks here

            if (string.IsNullOrEmpty(sourceFolder))
                DisplayUsage("Missing source folder.");
            if (string.IsNullOrEmpty(destinationFolder))
                DisplayUsage("Missing destination folder.");
            if (string.Compare(sourceFolder, destinationFolder, StringComparison.OrdinalIgnoreCase) == 0)
                DisplayUsage("Source and destination folders cannot be the same.");

            // Now verify dimensions
            if (frameWidth == 0 && frameHeight == 0)
                DisplayUsage("Missing photo frame dimensions. Use the format: [width]x[height]");
            if (frameWidth == 0 || frameHeight == 0)
                DisplayUsage($"Invalid photo frame dimensions ({frameWidth}x{frameHeight})");

            if (frameWidth <= frameHeight)
                DisplayUsage($"Photo frame width ({frameWidth}) must be greater than height ({frameHeight}).");

            // Verify the middle black bar width
            if (middleBarWidth > frameWidth)
                DisplayUsage($"Middle black bar width ({middleBarWidth}) cannot be greater than photo frame width ({frameWidth}).");
        }

        /// <summary>
        /// Displays the usage information to the user and optional error message.
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        public static void DisplayUsage(string errorMessage = "")
        {
            Console.WriteLine($"Usage: {System.AppDomain.CurrentDomain.FriendlyName} <source dir> <destination dir> <output dimensions> <separator width> [options]\n" +
                                "Combine two portrait photos into a single landscape image, useful for\n" +
                                "digital photo frames that display vertical images awkwardly.\n");


            if (string.IsNullOrEmpty(errorMessage))
                Console.WriteLine($"This is version {OutputVersion(version)}, copyright © 2024-{DateTime.Now.Year} Richard Lawrence.\n" +
                                    "Image manipulation powered by ImageMagick - https://www.imagemagick.org\n" +
                                    "Frame icon created by Freepik - Flaticon (https://www.flaticon.com/free-icons/frame)\n");

            Console.WriteLine("Arguments:\n" +
                                "  <source dir>           Directory containing portrait '.jpg' or '.jpeg' images.\n" +
                                "                         Landscape or square images will be ignored.\n" +
                                "  <destination dir>      Directory where combined landscape images will be saved.\n" +
                                "                         Must already exist.\n" +
                                "  <output dimensions>    Output resolution in the format [width]x[height]\n" +
                                "                         Example: 800x600 (match your photo frame's resolution)\n" +
                                "  <separator width>      Minimum number of black pixels between the two images.\n" +
                                "                         Use 0 to remove the separator only when images fill\n" +
                                "                         the full width exactly. Black bars may still appear.\n" +
                                "\n" +
                                "Options:\n" +
                                "  -v, --verbose          Display detailed logging to the console.\n" +
                                "  -o, --overwrite        Regenerate output files even if they already exist.\n" +
                                "  -d, --delete           Remove any previously generated images before starting.\n" +
                                "                         Only files with the prefix 'sideby-' will be deleted.\n" +
                                "  -r, --random           Shuffle images randomly instead of sorting by date taken.\n" +
                                "                         Timestamps will not be set on output files in this mode.\n" +
                                "\n" +
                               $"Logs are written to {Path.Combine(appDataPath, "Logs")}\n");

            if (!string.IsNullOrEmpty(errorMessage))
            {
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
            {
                result += $"-pre{netVersion.Build}";
            }

            return result;
        }
    }
}
