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
    /// Entry point
    /// </summary>
    internal sealed class Program
    {
        /// <summary>
        /// Main entry point for the application. Returns 0 on success, or 1 if the run failed or
        /// any image pair could not be generated, so that batch files and scheduled tasks can tell.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The exit code: 0 for success, 1 for any failure.</returns>
        static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch (Exception)
            {
                // Ignore it and use the default encoding instead
            }

            // Initialise the logger. If this fails there is no log to write to, so the message can only go to the console.
            string logFolder = Path.Combine(Globals.AppDataPath, "Logs");

            try
            {
                Logger.Initialise(logFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Cannot open the log file in '{logFolder}': {ex.Message}");
                return 1;
            }

            try
            {
                // Parse the command line arguments
                CommandLineParser.ParseArguments(args);

                // Show the header information
                ConsoleOutput.ShowHeader();

                // Collect all image files from the source directories and/or file list
                if (!ImageFileCollector.GetAllImageFiles())
                    return 1;

                // Get the information about the images found, selecting the ones that are appropriate for processing
                ImageMetadataExtractor.CollectImageInformation();

                // Delete existing output files if the clean option is specified
                OutputFileCleaner.DeleteExistingFiles();

                // Process the images, combining them into side-by-side output files
                int failedPairs = ImageProcessor.ProcessFiles();

                // Delete any previously generated files that weren't created in this run if the mirror option is specified
                OutputFileCleaner.MirrorCleanup();

                Logger.Write("SideBySide finished.");

                // Check for latest release
                ConsoleOutput.CheckLatestRelease();

                // Some pairs failed (each one has already been logged), so let scripts know
                if (failedPairs > 0)
                    return 1;

                return 0;
            }
            catch (Exception ex)
            {
                // Any failure not handled where it happens ends the run here. The console gets the
                // message, and the log file gets the full exception for troubleshooting.
                Logger.Write($"Unexpected error: {ex.Message} (details are in the log: {Logger.LogFilePath})");
                Logger.Write(ex.ToString(), true);
                return 1;
            }
        }
    }
}
