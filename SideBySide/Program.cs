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

namespace SideBySide
{
    internal sealed class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Initialise the logger
            Logger.Initialise(Path.Combine(Globals.AppDataPath, "Logs"));

            // Parse the command line arguments
            CommandLineParser.ParseArguments(args);

            // Show the header information
            ConsoleOutput.ShowHeader(args);

            // Collect all image files from the source directories and/or file list
            ImageFileCollector.GetAllImageFiles();

            // Get the information about the images found, selecting the ones that are appropriate for processing
            ImageMetadataExtractor.CollectImageInformation();

            // Delete existing output files if the clean option is specified
            OutputFileCleaner.DeleteExistingFiles();

            // Process the images, combining them into side-by-side output files
            ImageProcessor.ProcessFiles();

            // Delete any previously generated files that weren't created in this run if the mirror option is specified
            OutputFileCleaner.MirrorCleanup();

            Logger.Write("SideBySide finished.");

            // Check for latest release
            ConsoleOutput.CheckLatestRelease();

            // Exit successfully
            System.Environment.Exit(0);
        }
    }
}
