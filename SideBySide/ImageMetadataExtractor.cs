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

using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using SkiaSharp;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SideBySide
{
    internal sealed partial class ImageMetadataExtractor
    {
        /// <summary>Regexp for identifying dimensions of a photo</summary>
        [GeneratedRegex(@"^(\d+)[x,](\d+)$")]
        private static partial Regex DimensionRegex();

        /// <summary>Dictionary to cache MD5 hashes of files to avoid redundant calculations.</summary>
        private static readonly Dictionary<string, byte[]> _shaCache = [];
        /// <summary>Gets the MD5 hash key for the specified file path and length, using a combination of the file path and length</summary>
        private static string GetCacheKey(string path, long length) =>
            $"{path}|{length}";

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

                Logger.Write($"No DateTimeOriginal EXIF tag found for {path}", true);
            }
            catch (Exception ex)
            {
                Logger.Write($"Failed to read EXIF data from {path}: {ex.Message}", true);
            }

            DateTime created = File.GetCreationTime(path);
            DateTime modified = File.GetLastWriteTime(path);

            if (created < modified)
            {
                Logger.Write($"Falling back to creation date: {created}", true);
                return created;
            }
            else
            {
                Logger.Write($"Falling back to modified date: {modified}", true);
                return modified;
            }
        }

        /// <summary>
        /// Walks through the image file list, collects information about each image and adds only the portrait-oriented images
        /// to the images list.
        /// </summary>
        public static void CollectImageInformation()
        {
            Logger.Write("Examining images and identifying candidates for processing...");

            foreach (string file in Globals.ImageFileList)
            {
                using var codec = SKCodec.Create(file);
                if (codec == null)
                {
                    Logger.Write($"Skipping {file} as it could not be opened.", true);
                    continue;
                }

                if (codec.Info.Width == 0 || codec.Info.Height == 0)
                {
                    Logger.Write($"Skipping {file} as it is not a valid image file.", true);
                    continue;
                }

                // Set width and height variables
                int width = codec.Info.Width;
                int height = codec.Info.Height;

                // Correct for EXIF orientation (swap width/height if rotated 90° or 270°)
                switch (codec.EncodedOrigin)
                {
                    case SKEncodedOrigin.RightTop:   // 90° CW
                    case SKEncodedOrigin.LeftBottom: // 270° CW
                        (width, height) = (height, width);
                        break;
                }

                // Skip images that are not portrait-oriented
                if (width >= height)
                    continue;

                // Add the image to the list with its dimensions and creation date
                Globals.Images.Add(new Globals.ImageInfo
                {
                    FullPath = file,
                    FileName = Path.GetFileName(file),
                    FileWidth = codec.Info.Width,
                    FileHeight = codec.Info.Height,
                    CreationDate = GetExifDateTaken(file)
                });

                Logger.Write($"Added {file} ({codec.Info.Width}x{codec.Info.Height})", true);
            }

            Logger.Write($"Found {Globals.Images.Count} suitable portrait images.");
        }

        /// <summary>
        /// Given a string in the format "[width]x[height]" or "[width],[height], extracts the width and height values
        /// and assigns them to the frameWidth and frameHeight variables. Returns true if successful, false otherwise.
        /// </summary>
        /// <param name="dimensions"></param>
        /// <returns></returns>
        public static bool ExtractDimensions(string dimensions)
        {
            var match = DimensionRegex().Match(dimensions.Trim().ToLower(CultureInfo.CurrentCulture));
            if (!match.Success)
                return false;

            if (!int.TryParse(match.Groups[1].Value, out int width) || !int.TryParse(match.Groups[2].Value, out int height))
                return false;

            // Width and height values are valid, so set them
            Globals.FrameWidth = width;
            Globals.FrameHeight = height;
            return true;
        }

        /// <summary>
        /// Given two file paths, compares the files to determine if they are identical. Uses
        /// file size and SHA256 hash for comparison.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static bool FilesAreEqual(string path1, string path2)
        {
            try
            {
                // Quick checks first — fast and cheap
                var info1 = new FileInfo(path1);
                var info2 = new FileInfo(path2);

                if (!info1.Exists || !info2.Exists)
                    return false;

                if (info1.Length != info2.Length)
                    return false;

                // Compare hashes for final confirmation
                byte[] hash1 = GetSHAHash(info1);
                byte[] hash2 = GetSHAHash(info2);

                // Compare byte arrays
                for (int i = 0; i < hash1.Length; i++)
                    if (hash1[i] != hash2[i])
                        return false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Write($"Error comparing {path1} and {path2}: {ex.Message}", true);
                return false;
            }
        }

        /// <summary>
        /// Given a FileInfo object, computes and returns the SHA256 hash of the file's contents. If
        /// the hash has already been computed for this file (based on its path and length), the cached
        /// value is returned instead to improve performance.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static byte[] GetSHAHash(FileInfo info)
        {
            string key = GetCacheKey(info.FullName, info.Length);

            if (_shaCache.TryGetValue(key, out var cached))
                return cached;
            using var sha = System.Security.Cryptography.SHA256.Create();
            using var stream = info.OpenRead();
            byte[] hash = sha.ComputeHash(stream);

            _shaCache[key] = hash;
            return hash;
        }
    }
}
