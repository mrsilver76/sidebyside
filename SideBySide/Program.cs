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

using System.Text;
using System.Reflection;
using static SideBySide.Helpers;
using SkiaSharp;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace SideBySide
{
    internal class Program
    {
        // User-defined variables
        public static List<string> inputDirs = [];  // List of input directories to scan for images
        public static string destinationFolder = "";  // Destination folder for processed images
        public static int frameWidth;  // Width of the digital frame
        public static int frameHeight;  // Height of the digital frame
        public static int middleBarWidth;  // Width of the black middle bar between images
        public static bool verboseMode = false;  // Flag for verbose output
        public static bool overwriteExisting = false;  // Flag to overwrite existing images
        public static bool deleteExisting = false;  // Flag to delete existing images before processing
        public static bool randomiseSorting = false;  // Flag to randomise the order of images
        public static bool recursiveSearch = false;  // Flag to search directories recursively
        public static string inputFile = "";  // Input file containing image paths (if any)
        public static bool mirrorMode = false;  // Flag to enable mirror mode, which deletes stale files in the destination folder

        // Internal variables
        public class ImageInfo  // Class to hold information about each image
        {
            public required string FullPath { get; set; }  // Full path to the image file
            public required string FileName { get; set; }  // Name of the image file
            public DateTime CreationDate { get; set; }  // Creation date of the image file
            public int FileWidth { get; set; }  // Width of the image file
            public int FileHeight { get; set; }  // Height of the image file
        }
        public static List<ImageInfo> images = [];  // List of images to be processed
        public static Version version = Assembly.GetExecutingAssembly().GetName().Version!;  // Version of the application
        public static string appDataPath = ""; // Path to the app data folder
        public static List<string> imageFileList = [];  // List of image files found during scanning
        public static HashSet<string> processedFiles = [];  // Set to track processed files

        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            InitialiseLogger();
            ParseArguments(args);

            Console.WriteLine($"SideBySide v{OutputVersion(version)}, Copyright © 2024-{DateTime.Now.Year} Richard Lawrence");
            Console.WriteLine($"Combine two portrait photos into a single landscape image, useful for");
            Console.WriteLine($"digital photo frames that display vertical images awkwardly.");
            Console.WriteLine($"https://github.com/mrsilver76/sidebyside\n");
            Console.WriteLine($"This program comes with ABSOLUTELY NO WARRANTY. This is free software,");
            Console.WriteLine($"and you are welcome to redistribute it under certain conditions; see");
            Console.WriteLine($"the documentation for details.");
            Console.WriteLine();

            Logger("Starting SideBySide...");

            // Log details about the environment and arguments
            LogEnvironmentInfo(args);

            // Clear the image file list before starting
            imageFileList.Clear();

            // Look for files in the specified input directories
            if (inputDirs.Count > 0)
                foreach (string dir in inputDirs)
                    GetImageFilesFromFolder(dir);

            // Look for files in the specified input file list
            if (!string.IsNullOrEmpty(inputFile) && File.Exists(inputFile))
                GetImageFilesFromFileList();

            // If no image files were found, log a warning and exit
            if (imageFileList.Count == 0)
            {
                Logger("No image files found to process. Please check your input directories or file list.");
                System.Environment.Exit(1);
            }

            // Get the information about the images found, selecting the ones that are appropriate for processing
            CollectImageInformation();

            DeleteExistingFiles();
            ProcessFiles();

            if (mirrorMode)
                MirrorCleanup();

            Logger("SideBySide finished.");
            CheckLatestRelease();
            System.Environment.Exit(0);
        }

        /// <summary>
        /// Given a source folder, searches for all JPEG files (both .jpg and .jpeg) and adds them to the imageFileList.
        /// </summary>
        /// <param name="sourceFolder"></param>
        public static void GetImageFilesFromFolder(string sourceFolder)
        {
            SearchOption searchOption = recursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            Logger($"Looking for images in: {sourceFolder}{(recursiveSearch ? " (and sub-directories)" : "")}");

            var files = System.IO.Directory.GetFiles(sourceFolder, "*.*", searchOption)
                .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

            imageFileList.AddRange(files);
        }

        /// <summary>
        /// Loads the image file paths from a specified file list, filtering for JPEG files (.jpg and .jpeg). These
        /// paths are added to the imageFileList for further processing.
        /// </summary>
        /// <param name="fileListPath"></param>

        public static void GetImageFilesFromFileList()
        {
            if (string.IsNullOrEmpty(inputFile))
            {
                Logger("No input file list specified, skipping.", true);
                return;
            }

            if (!File.Exists(inputFile))
            {
                Logger($"File list '{inputFile}' does not exist.");
                return;
            }

            Logger($"Looking for images in: {inputFile}");

            var lines = File.ReadAllLines(inputFile)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .Where(line => line.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               line.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

            foreach (var file in lines)
            {
                if (File.Exists(file))
                    imageFileList.Add(file);
                else
                    Logger($"Warning: '{file}' listed in file list does not exist.", true);
            }
        }

        /// <summary>
        /// Walks through the image file list, collects information about each image and adds only the portrait-oriented images
        /// to the images list.
        /// </summary>
        public static void CollectImageInformation()
        {
            Logger("Examining images and identifying candidates for processing...");

            foreach (string file in imageFileList)
            {
                using var codec = SKCodec.Create(file);
                if (codec == null || codec.Info.Width == 0 || codec.Info.Height == 0)
                {
                    Logger($"Skipping {file} as it is not a valid image file.", true);
                    continue;
                }

                // Skip images that are not portrait-oriented
                if (codec.Info.Width >= codec.Info.Height)
                    continue;

                // Add the image to the list with its dimensions and creation date
                images.Add(new ImageInfo
                {
                    FullPath = file,
                    FileName = Path.GetFileName(file),
                    FileWidth = codec.Info.Width,
                    FileHeight = codec.Info.Height,
                    CreationDate = GetExifDateTaken(file)
                });

                Logger($"Added {file} ({codec.Info.Width}x{codec.Info.Height})", true);
            }

            Logger($"Found {images.Count} portrait images suitable for processing.");
        }

        /// <summary>
        /// Extracts the EXIF date taken from the image, falling back to file creation
        /// or modification date if EXIF data is not available or corrupted.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static DateTime GetExifDateTaken(string path)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(path);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime exifDate))
                    return exifDate;

                Logger($"No DateTimeOriginal EXIF tag found for {path}", true);
            }
            catch (Exception ex)
            {
                Logger($"Failed to read EXIF data from {path}: {ex.Message}", true);
            }

            DateTime created = File.GetCreationTime(path);
            DateTime modified = File.GetLastWriteTime(path);

            if (created < modified)
            {
                Logger($"Falling back to creation date: {created}", true);
                return created;
            }
            else
            {
                Logger($"Falling back to modified date: {modified}", true);
                return modified;
            }
        }

        /// <summary>
        /// Given a string in the format "[width]x[height]" or "[width],[height], extracts the width and height values
        /// and assigns them to the frameWidth and frameHeight variables. Returns true if successful, false otherwise.
        /// </summary>
        /// <param name="dimensions"></param>
        /// <returns></returns>
        public static bool ExtractDimensions(string dimensions)
        {
            var match = Regex.Match(dimensions.Trim().ToLower(), @"^(\d+)[x,](\d+)$");
            if (!match.Success)
                return false;

            if (!int.TryParse(match.Groups[1].Value, out frameWidth) || !int.TryParse(match.Groups[2].Value, out frameHeight))
                return false;

            return true;
        }

        /// <summary>
        /// Deletes existing files in the destination folder if the deleteExisting flag is set to true.
        /// </summary>
        public static void DeleteExistingFiles()
        {
            // Don't delete existing files if the flag is not set
            if (deleteExisting == false)
                return;

            // Delete existing files in the destination folder
            Logger($"Cleaning existing files in: {destinationFolder}");

            if (!System.IO.Directory.Exists(destinationFolder))
            {
                Logger($"Destination folder does not exist: {destinationFolder}", true);
                return;
            }

            // Get all files matching the pattern "sideby-*.jpg" in the destination folder
            string[] existingFiles = System.IO.Directory.GetFiles(destinationFolder, "sideby-*.jpg", SearchOption.TopDirectoryOnly);
            if (existingFiles.Length == 0)
            {
                Logger($"No existing files to delete in: {destinationFolder}", true);
                return;
            }

            // Attempt to delete each existing file
            foreach (string file in existingFiles)
            {
                try
                {
                    File.Delete(file);
                    Logger($"Deleted existing file: {Path.GetFileName(file)}", true);
                }
                catch (Exception ex)
                {
                    Logger($"Failed to delete {file}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sorts the images by creation date and processes them in pairs to generate landscape images.
        /// </summary>
        public static void ProcessFiles()
        {
            if (images.Count == 0)
            {
                Logger("No images found to process.");
                return;
            }

            // Sort the files depending on the user preference
            if (randomiseSorting)
            {
                Logger($"Shuffling files found randomly.");
                images = images.OrderBy(i => Guid.NewGuid()).ToList();
            }
            else
            {
                Logger($"Sorting files found by creation date.");
                images = images.OrderBy(i => i.CreationDate).ToList();
            }

            if (images.Count < 2)
            {
                Logger($"Not enough images to process: need at least 2, found {images.Count}.");
                return;
            }
            Logger($"Generating {frameWidth}x{frameHeight} landscape images from {images.Count} portrait files...");

            int generated = 0, skipped = 0;
            for (int i = 0; i + 1 < images.Count; i += 2)
            {
                ImageInfo image1 = images[i];
                ImageInfo image2 = images[i + 1];

                if (image1 == null || image2 == null)
                {
                    Logger($"Skipping invalid image pair: {image1?.FileName ?? "null"} and {image2?.FileName ?? "null"}", true);
                    continue;
                }

                string filename = GenerateFilename(image1.FileName, image2.FileName);
                string outputPath = Path.Combine(destinationFolder, filename);
                bool exists = File.Exists(outputPath);

                if (exists && !overwriteExisting)
                {
                    Logger($"Skipping existing image: {Path.GetFileName(filename)}");
                    processedFiles.Add(outputPath);
                    skipped++;
                    continue;
                }

                if (exists && overwriteExisting)
                    Logger($"Overwriting existing image: {filename}", true);

                // Generate the side-by-side image
                if (GenerateSideBySideImage(image1, image2, outputPath))
                {
                    generated++;
                    processedFiles.Add(outputPath);
                }
                else
                    skipped++;
            }

            if (images.Count % 2 != 0)
            {
                Logger($"Unpaired image skipped: {images[^1].FileName}");
                skipped++;
            }

            Logger($"Finished generating {Pluralise(generated, "landscape image", "landscape images")} ({skipped} skipped)");
        }

        /// <summary>
        /// Generate a filename for the combined image based on a SHA256 hash of both filenames.
        /// </summary>
        /// <param name="file1"></param>
        /// <param name="file2"></param>
        /// <returns></returns>
        public static string GenerateFilename(string file1, string file2)
        {
            if (string.IsNullOrEmpty(file1) || string.IsNullOrEmpty(file2))
                throw new ArgumentException("Both file names must be provided.");
            if (file1.Equals(file2, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("File names must be different.");
            
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(file1 + "|" + file2));

            // Base64url encoding: URL-safe, compact, no padding
            string base64 = Convert.ToBase64String(hashBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            // Truncate to first 16 chars (~96 bits entropy)
            return $"sideby-{base64[..16]}.jpg";
        }

        /// <summary>
        /// Generates a side-by-side landscape image from two portrait images.
        /// </summary>
        /// <param name="imageA"></param>
        /// <param name="imageB"></param>
        /// <param name="outputPath"></param>
        /// <returns></returns>
        public static bool GenerateSideBySideImage(ImageInfo imageA, ImageInfo imageB, string outputPath)
        {
            if (imageA == null || imageB == null)
            {
                Logger("Invalid image information provided.", true);
                return false;
            }

            Logger($"Generating landscape for {imageA.FileName} and {imageB.FileName} --> {outputPath}", true);

            // Load original images
            using var original1 = SKBitmap.Decode(imageA.FullPath);
            using var original2 = SKBitmap.Decode(imageB.FullPath);

            // Resize images to fit the frame height while maintaining aspect ratio
            using var resized1 = ResizeToHeight(original1, frameHeight);
            using var resized2 = ResizeToHeight(original2, frameHeight);

            // Create a black bitmap which is the same height as the frame and half the width of the frame. This
            // is what we'll place the resized images on to.
            int singleWidth = frameWidth / 2;
            using var padded1 = new SKBitmap(singleWidth, frameHeight);
            using (var canvas = new SKCanvas(padded1))
            {
                canvas.Clear(SKColors.Black);
                int x = (singleWidth - resized1.Width) / 2;
                canvas.DrawBitmap(resized1, x, 0);
            }

            using var padded2 = new SKBitmap(singleWidth, frameHeight);
            using (var canvas = new SKCanvas(padded2))
            {
                canvas.Clear(SKColors.Black);
                int x = (singleWidth - resized2.Width) / 2;
                canvas.DrawBitmap(resized2, x, 0);
            }

            // Create the final output bitmap which is double the width of a single image
            // and draw both padded images onto it side by side.
            using var output = new SKBitmap(frameWidth, frameHeight);
            using (var canvas = new SKCanvas(output))
            {
                canvas.DrawBitmap(padded1, 0, 0);
                canvas.DrawBitmap(padded2, singleWidth, 0);

                // If a middle bar is specified, then draw this over the top of the new composite image. This
                // will cause some clipping of the images, but this avoids complicated calculations and, unless
                // the width is large, it won't impact the image very much.
                if (middleBarWidth > 0)
                {
                    int xOffset = (frameWidth - middleBarWidth) / 2;
                    canvas.DrawRect(new SKRect(xOffset, 0, xOffset + middleBarWidth, frameHeight), new SKPaint { Color = SKColors.Black });
                }
            }

            // Save the output image (at 80% quality) to the specified path
            using var image = SKImage.FromBitmap(output);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
            using var stream = File.OpenWrite(outputPath);
            data.SaveTo(stream);

            Logger($"Landscape image created: {Path.GetFileName(outputPath)}");

            // Set the creation and last modified dates to the earlier of the two images' creation dates, but only
            // if randomiseSorting is false. This ensures that the output image retains a meaningful timestamp.
            if (!randomiseSorting)
            {
                // Use the earlier creation date of the two images
                DateTime creationDate = imageA.CreationDate < imageB.CreationDate ? imageA.CreationDate : imageB.CreationDate;
                Logger($"Setting creation and last modified dates to {creationDate} for {Path.GetFileName(outputPath)}", true);
                File.SetCreationTime(outputPath, creationDate);
                File.SetLastWriteTime(outputPath, creationDate);
            }

            return true;
        }

        /// <summary>
        /// Removes any stale files in the destination folder that were not processed during this run.
        /// </summary>
        private static void MirrorCleanup()
        {
            // If mirror mode is not enabled, do nothing
            if (!mirrorMode)
                return;

            // Walk through the destination folder and delete any files that were not processed
            // in this run.

            var existing = System.IO.Directory.GetFiles(destinationFolder, "sideby-*.jpg", SearchOption.TopDirectoryOnly);
            foreach (var file in existing)
            {
                var name = Path.GetFileName(file);
                if (!processedFiles.Contains(file))
                {
                    Logger($"Mirror mode. Deleting stale file: {name}");
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Logger($"Failed to delete stale file {name}: {ex.Message}", true);
                    }
                }
            }
        }

        /// <summary>
        /// Resizes the image to the target height while maintaining aspect ratio.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="targetHeight"></param>
        private static SKBitmap ResizeToHeight(SKBitmap img, int targetHeight)
        {
            double scale = (double)targetHeight / img.Height;
            int newWidth = (int)(img.Width * scale);

            var resized = new SKBitmap(newWidth, targetHeight);
            using var canvas = new SKCanvas(resized);
            canvas.DrawBitmap(img, new SKRect(0, 0, newWidth, targetHeight));
            return resized;
        }
    }
}
