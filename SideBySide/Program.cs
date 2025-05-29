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
using System.Diagnostics;
using System.Reflection;
using static SideBySide.Helpers;
using static System.Net.Mime.MediaTypeNames;
using ImageMagick;
using System.Security.Cryptography;

namespace SideBySide
{
    internal class Program
    {
        // User-defined constants
        public static string sourceFolder = "";  // Source folder containing images
        public static string destinationFolder = "";  // Destination folder for processed images
        public static int frameWidth;  // Width of the digital frame
        public static int frameHeight;  // Height of the digital frame
        public static int middleBarWidth;  // Width of the black middle bar between images
        public static bool verboseMode = false;  // Flag for verbose output
        public static bool overwriteExisting = false;  // Flag to overwrite existing images
        public static bool deleteExisting = false;  // Flag to delete existing images before processing
        public static bool randomiseSorting = false;  // Flag to randomise the order of images

        // Internal constants
        public class ImageInfo  // Class to hold information about each image
        {
            public required string FullPath { get; set; }  // Full path to the image file
            public required string FileName { get; set; }  // Name of the image file
            public DateTime CreationDate { get; set; }  // Creation date of the image file
            public int FileWidth { get; set; }  // Width of the image file
            public int FileHeight { get; set; }  // Height of the image file
        }
        public static List<ImageInfo> images = new List<ImageInfo>();  // List of images to be processed
        public static Version version = Assembly.GetExecutingAssembly().GetName().Version!;  // Version of the application
        public static string appDataPath = ""; // Path to the app data folder

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

            Logger($"Parsed arguments: {string.Join(" ", args)}", true);

            ScanFiles();
            DeleteExistingFiles();
            ProcessFiles();

            Logger("SideBySide finished.");
            CheckLatestRelease();
            System.Environment.Exit(0);
        }

        /// <summary>
        /// Scans the source folder for image files, extracts their dimensions and EXIF data, and adds them to the images list.
        /// </summary>
        public static void ScanFiles()
        {
            Logger($"Looking for images in: {sourceFolder}");

            string[] imageFiles = System.IO.Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories)
                                           .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                        s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                           .ToArray();

            // For each image file found, work out the width and height
            foreach (string file in imageFiles)
            {
                using var info = new MagickImage();
                info.Ping(file);  // Faster way to get image info without loading the full image

                // Skip files that are not valid images
                if (info.Width == 0 || info.Height == 0)
                {
                    Logger($"Skipping {file} as it is not a valid image file.", true);
                    continue;
                }

                // Skip non-portrait images
                if (info.Width >= info.Height)
                    continue;

                images.Add(new ImageInfo
                {
                    FullPath = file,
                    FileName = Path.GetFileName(file),
                    FileWidth = (int)info.Width,
                    FileHeight = (int)info.Height,
                    CreationDate = GetExifDateTaken(info, file)
                });
                Logger($"Added {file} ({info.Width}x{info.Height})", true);
            }
        }

        /// <summary>
        /// Extracts the EXIF date taken from the image, falling back to file creation
        /// or modification date if EXIF data is not available or corrupted.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private static DateTime GetExifDateTaken(MagickImage image, string path)
        {
            var profile = image.GetExifProfile();
            if (profile != null)
            {
                var value = profile.GetValue(ExifTag.DateTimeOriginal);
                if (value == null)
                    Logger($"No EXIF DateTimeOriginal tag found for {path}", true);
                else
                {
                    if (DateTime.TryParseExact(value.Value, "yyyy:MM:dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var parsed))
                        return parsed;
                    else
                        Logger($"Unable to parse DateTimeOriginal tag of '{value.Value}' for {path}", true);
                }
            }
            else
                Logger($"No EXIF profile found for {path}", true);

            // Fallback: use the earlier of created or modified
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
        /// Given a string in the format "[width]x[height]", extracts the width and height values and assigns them to the frameWidth
        /// and frameHeight variables. Returns true if successful, false otherwise.
        /// </summary>
        /// <param name="dimensions"></param>
        /// <returns></returns>
        public static bool ExtractDimensions(string dimensions)
        {
            string[] parts = (dimensions.ToLower()).Split('x');
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out frameWidth) || !int.TryParse(parts[1], out frameHeight))
                return false;

            return true;
        }

        public static void DeleteExistingFiles()
        {
            if (deleteExisting == false)
                return;

            // Delete existing files in the destination folder
            Logger($"Deleting existing files in: {destinationFolder}");

            if (!Directory.Exists(destinationFolder))
            {
                Logger($"Destination folder does not exist: {destinationFolder}", true);
                return;
            }

            string[] existingFiles = Directory.GetFiles(destinationFolder, "sideby-*.jpg", SearchOption.TopDirectoryOnly);
            if (existingFiles.Length == 0)
            {
                Logger($"No existing files to delete in: {destinationFolder}", true);
                return;
            }
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
            Logger($"Generating landscape images from {images.Count} portrait files...");

            int generated = 0;
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
                    continue;
                }

                if (exists && overwriteExisting)
                    Logger($"Overwriting existing image: {filename}", true);

                if (GenerateSideBySideImage(image1, image2, outputPath))
                    generated++;
            }

            if (images.Count % 2 != 0)
                Logger($"Unpaired image skipped: {images[^1].FileName}");

            Logger($"Finished generating {Pluralise(generated, "landscape image", "landscape images")}.");
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

            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(file1 + "|" + file2));

            // Base64url encoding: URL-safe, compact, no padding
            string base64 = Convert.ToBase64String(hashBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            // Truncate to first 16 chars (~96 bits entropy)
            return $"sideby-{base64[..16]}.jpg";
        }

        public static bool GenerateSideBySideImage(ImageInfo imageA, ImageInfo imageB, string outputPath)
        {
            if (imageA == null || imageB == null)
            {
                Logger("Invalid image information provided.", true);
                return false;
            }

            Logger($"Generating landscape for {imageA.FileName} and {imageB.FileName} --> {outputPath}", true);

            using var image1 = new MagickImage(imageA.FullPath);
            using var image2 = new MagickImage(imageB.FullPath);
            int singleWidth = frameWidth / 2;

            // Resize each image to target height preserving aspect
            ResizeToHeight(image1, frameHeight);
            ResizeToHeight(image2, frameHeight);

            // Extend each image to the target width, centering them and filling with black
            image1.Extent(new MagickGeometry((uint)singleWidth, (uint)frameHeight), Gravity.Center, MagickColors.Black);
            image2.Extent(new MagickGeometry((uint)singleWidth, (uint)frameHeight), Gravity.Center, MagickColors.Black);

            // Append image1 and image2 side by side (no separator yet)
            using var collection = new MagickImageCollection { image1, image2 };
            using var result = collection.AppendHorizontally();
            if (result == null)
            {
                Logger("Failed to create combined image!");
                return false;
            }

            // Composite a black rectangle over the middle (if required)
            if (middleBarWidth > 0)
            {
                int xOffset = ((int)result.Width - middleBarWidth) / 2;
                using var separator = new MagickImage(MagickColors.Black, (uint)middleBarWidth, (uint)frameHeight);
                result.Composite(separator, xOffset, 0, CompositeOperator.Over);
            }

            // Save result
            result.Quality = 90; // Set JPEG quality to 90% as memories should be high quality
            result.Write(outputPath);
            Logger($"Landscape image created: {Path.GetFileName(outputPath)}");

            // Set the creation date and last modified date to the earliest of the two images.
            // However if the images were randomised then this date won't make much sense.
            if (!randomiseSorting)
            {
                DateTime creationDate = imageA.CreationDate < imageB.CreationDate ? imageA.CreationDate : imageB.CreationDate;
                Logger($"Setting creation and last modified dates to {creationDate} for {Path.GetFileName(outputPath)}", true);
                File.SetCreationTime(outputPath, creationDate);
                File.SetLastWriteTime(outputPath, creationDate);
            }
            // Possible future improvement:
            //   set the EXIF DateTimeOriginal tag to the same value as the creation date?

            return true;
        }

        /// <summary>
        /// Resizes the image to the target height while maintaining aspect ratio.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="targetHeight"></param>
        private static void ResizeToHeight(MagickImage img, int targetHeight)
        {
            double scale = (double)targetHeight / img.Height;
            int newWidth = (int)(img.Width * scale);

            img.Resize((uint)newWidth, (uint)targetHeight);
        }

#if false
        /// <summary>
        /// Generates a SHA256 hash of the input string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GenerateSHA256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder hashString = new StringBuilder();
                foreach (byte b in hash)
                {
                    hashString.Append(b.ToString("x2"));
                }
                return hashString.ToString();
            }
        }
#endif
    }
}
