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

using SkiaSharp;
using System.Security.Cryptography;
using System.Text;

namespace SideBySide
{
    /// <summary>
    /// Pairs the portrait images and renders each pair as one landscape image.
    /// </summary>
    internal sealed class ImageProcessor
    {
        /// <summary>A "day" starts at this hour rather than midnight, so a late night party does not get split in two.</summary>
        private const int DayStartHour = 6;

        /// <summary>
        /// Sorts the images by creation date and processes them in pairs to generate landscape images.
        /// </summary>
        /// <returns>The number of image pairs that could not be generated. Zero means that nothing failed.</returns>
        public static int ProcessFiles()
        {
            if (Globals.Images.Count == 0)
            {
                Logger.Write("No images found to process.");
                return 0;
            }

            // Sort the files depending on the user preference
            if (Globals.RandomiseSorting)
            {
                Logger.Write("Shuffling files found randomly...");
                Globals.Images = [.. Globals.Images.OrderBy(i => Guid.NewGuid())];
            }
            else
            {
                Logger.Write("Sorting files found by creation date...");
                Globals.Images = [.. Globals.Images.OrderBy(i => i.CreationDate)];
            }

            // If we don't have at least 2 images then there isn't much we can generate

            if (Globals.Images.Count < 2)
            {
                Logger.Write($"Not enough images to process: need at least 2, found {Globals.Images.Count}.");
                return 0;
            }

            // Let's generate those images...
            Logger.Write("Generating landscape images...");

            int generated = 0, skipped = 0, failed = 0;

            // Show a progress bar based on the number of images dealt with. It is not used in verbose mode,
            // as the log line for each pair would collide with it.
            bool showProgress = !Globals.VerboseMode;
            if (showProgress)
            {
                ProgressBar.Total = Globals.Images.Count;
                ProgressBar.Start();
            }

            int i = 0;
            try
            {
                while (i + 1 < Globals.Images.Count)
                {
                    // i is the number of images dealt with so far, as the loop moves on by one or two
                    ProgressBar.Completed = i;

                    // Get the two images to process
                    Globals.ImageInfo image1 = Globals.Images[i];
                    Globals.ImageInfo image2 = Globals.Images[i + 1];

                    // If the two images are identical files, then skip one of them
                    if (ImageMetadataExtractor.FilesAreEqual(image1.FullPath, image2.FullPath))
                    {
                        Logger.Write($"Skipping duplicate image pair: {image1.FileName} and {image2.FileName}");
                        skipped++;
                        i++;  // Skip by one to try the next image.
                        continue;
                    }

                    // If the two images were taken less than ProximitySeconds apart then we're going to make a 
                    // very broad assumption that they are similar and look for a better partner. Previously we didn't
                    // do this and it ended up with the photo frame showing two very similar images side by side, which is
                    // not very interesting. If we find a better partner then we will move it up to be the partner for
                    // the first image and continue with the next pair.

                    int partnerIndex = FindPartnerIndex(i, Globals.ProximitySeconds);
                    if (partnerIndex != i + 1)
                    {
                        Globals.ImageInfo partner = Globals.Images[partnerIndex];
                        double totalSeconds = Math.Abs((image2.CreationDate - image1.CreationDate).TotalSeconds);
                        double partnerSeconds = Math.Abs((partner.CreationDate - image1.CreationDate).TotalSeconds);
                        Logger.Write($"{image1.FileName} and {image2.FileName} are {totalSeconds} secs apart (limit {Globals.ProximitySeconds} secs), using {partner.FileName} ({partnerSeconds} secs apart) as the partner for {image1.FileName} instead", true);
                        Globals.Images.RemoveAt(partnerIndex);
                        Globals.Images.Insert(i + 1, partner);
                        image2 = partner;
                    }

                    // Two different files with the same name would hash to an invalid output name, so
                    // treat them like a duplicate and try the next image
                    if (image1.FileName.Equals(image2.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Write($"Skipping same-named image pair: {image1.FullPath} and {image2.FullPath}");
                        skipped++;
                        i++;  // Skip by one to try the next image.
                        continue;
                    }

                    // Generate a filename for them and then check if the destination exists
                    string filename = GenerateFilename(image1.FileName, image2.FileName);
                    string outputPath = Path.Combine(Globals.DestinationFolder, filename);
                    bool exists = File.Exists(outputPath);

                    // If the user has not requested for existing images to be overwritten then
                    // we should skip past this one
                    if (exists && !Globals.OverwriteExisting)
                    {
                        Logger.Write($"Skipping existing image: {Path.GetFileName(filename)}", true);
                        Globals.ProcessedFiles.Add(outputPath);
                        skipped++;
                        i += 2;  // Move to the next pair.
                        continue;
                    }

                    // If they want it overwritten then let's confirm and continue
                    if (exists && Globals.OverwriteExisting)
                        Logger.Write($"Overwriting existing image: {filename}", true);

                    // Generate the side-by-side image
                    if (GenerateSideBySideImage(image1, image2, outputPath))
                    {
                        generated++;
                        Globals.ProcessedFiles.Add(outputPath);
                    }
                    else
                    {
                        // Something went wrong generating it (the reason has already been logged)
                        skipped++;
                        failed++;
                    }

                    i += 2;  // Move to the next pair.
                }
            }
            finally
            {
                // Clear the bar and restore the cursor, even if something threw
                ProgressBar.Stop();
            }

            // Log if we skipped an image because we didn't have a partner for it. The loop can move on by one image
            // as well as two, so the total count says nothing about this: check where the loop stopped.
            if (i < Globals.Images.Count)
            {
                Logger.Write($"Unpaired image skipped: {Globals.Images[i].FileName}");
                skipped++;
            }

            if (skipped > 0)
                Logger.Write($"Finished generating {GrammarHelper.Pluralise(generated, "landscape image", "landscape images")} ({skipped} skipped)");
            else
                Logger.Write($"Finished generating {GrammarHelper.Pluralise(generated, "landscape image", "landscape images")}.");

            if (failed > 0)
                Logger.Write($"{GrammarHelper.Pluralise(failed, "image pair", "image pairs")} could not be generated. See the messages above for the reasons.");

            return failed;
        }

        /// <summary>
        /// Finds the index of the best partner for the image at the given index. This is the first image on the same
        /// day that was not taken close to it, or the next image if there isn't one.
        /// </summary>
        /// <param name="index">The index of the first image of the pair.</param>
        /// <param name="closeSeconds">Images taken fewer than this many seconds apart count as close together.</param>
        /// <returns>The index of the partner, which is index + 1 if no better partner was found.</returns>
        private static int FindPartnerIndex(int index, int closeSeconds)
        {
            // A value of 0 turns the check off
            if (closeSeconds == 0)
                return index + 1;

            Globals.ImageInfo image1 = Globals.Images[index];
            DateTime day = GetDay(image1.CreationDate);

            for (int j = index + 1; j < Globals.Images.Count; j++)
            {
                Globals.ImageInfo candidate = Globals.Images[j];

                // Stop at the first image from another day (when sorted by date, nothing later can match)
                if (GetDay(candidate.CreationDate) != day)
                    break;

                double seconds = Math.Abs((candidate.CreationDate - image1.CreationDate).TotalSeconds);
                if (seconds < closeSeconds)
                    continue;

                if (ImageMetadataExtractor.FilesAreEqual(image1.FullPath, candidate.FullPath))
                    continue;

                return j;
            }

            return index + 1;
        }

        /// <summary>
        /// Gets the day that a date belongs to, where a day starts at DayStartHour rather than midnight.
        /// </summary>
        /// <param name="date">The date and time to place in a day.</param>
        /// <returns>The date of the day it belongs to, with the time set to midnight.</returns>
        private static DateTime GetDay(DateTime date)
        {
            return date.AddHours(-DayStartHour).Date;
        }

        /// <summary>
        /// Generate a filename for the combined image based on a SHA256 hash of both filenames.
        /// </summary>
        /// <param name="file1">The file name (not the path) of the first image.</param>
        /// <param name="file2">The file name (not the path) of the second image.</param>
        /// <returns>The output file name, for example "sideby-AbCdEfGhIjKlMnOp.jpg".</returns>
        public static string GenerateFilename(string file1, string file2)
        {
            if (string.IsNullOrEmpty(file1) || string.IsNullOrEmpty(file2))
                throw new ArgumentException("Both file names must be provided.", nameof(file1));
            if (file1.Equals(file2, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("File names must be different.", nameof(file2));

            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{file1}|{file2}"));

            // Base64url, so the name is safe in a file name and has no padding
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
        /// <param name="imageA">The image for the left half.</param>
        /// <param name="imageB">The image for the right half.</param>
        /// <param name="outputPath">The full path of the JPEG to write.</param>
        /// <returns>True if the file was written, false if the pair could not be generated.</returns>
        public static bool GenerateSideBySideImage(Globals.ImageInfo imageA, Globals.ImageInfo imageB, string outputPath)
        {
            Logger.Write($"Generating landscape for {imageA.FileName} and {imageB.FileName} => {outputPath}", true);

            // Does not match sideby-*.jpg, so --clean and --mirror never treat it as an output file
            string tempPath = $"{outputPath}.tmp";

            try
            {
                using var output = ComposeBitmap(imageA, imageB);
                if (output == null)
                    return false;

                SaveJpegToFile(output, tempPath, outputPath);

                Logger.Write($"Landscape image created: {Path.GetFileName(outputPath)}", true);
            }
            catch (Exception ex)
            {
                // A bad file, a locked or read-only output, or a full disk only costs us this pair, so carry on with the rest
                Logger.Write($"Failed to generate {Path.GetFileName(outputPath)} from {imageA.FileName} and {imageB.FileName}: {ex.Message}");
                Logger.Write(ex.ToString(), true);

                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Nothing more can be done about a temporary file we cannot delete
                }

                return false;
            }

            // The image is safely written by now, so a failure to set its dates must not make the pair count as failed
            try
            {
                SetOutputDates(imageA, imageB, outputPath);
            }
            catch (Exception ex)
            {
                Logger.Write($"Created {Path.GetFileName(outputPath)} but could not set its dates: {ex.Message}");
                Logger.Write(ex.ToString(), true);
            }

            return true;
        }

        /// <summary>
        /// Loads two portrait images and combines them into one landscape bitmap, with the optional gap drawn over the join.
        /// </summary>
        /// <param name="imageA">The image for the left half.</param>
        /// <param name="imageB">The image for the right half.</param>
        /// <returns>The landscape bitmap, or null if either image could not be loaded.</returns>
        private static SKBitmap? ComposeBitmap(Globals.ImageInfo imageA, Globals.ImageInfo imageB)
        {
            // Load original images
            using var original1 = LoadBitmapWithOrientation(imageA.FullPath);
            using var original2 = LoadBitmapWithOrientation(imageB.FullPath);

            // Handle any failures to load the images
            if (original1 == null)
            {
                Logger.Write($"Failed to load image: {imageA.FileName}", true);
                return null;
            }
            else if (original2 == null)
            {
                Logger.Write($"Failed to load image: {imageB.FileName}", true);
                return null;
            }

            // Resize images to fit the frame height while maintaining aspect ratio
            using var resized1 = ResizeToHeight(original1, Globals.FrameHeight);
            using var resized2 = ResizeToHeight(original2, Globals.FrameHeight);

            int singleWidth = Globals.FrameWidth / 2;
            using var padded1 = PadToHalfWidth(resized1, singleWidth);
            using var padded2 = PadToHalfWidth(resized2, singleWidth);

            // Create the final output bitmap which is double the width of a single image
            // and draw both padded images onto it side by side.
            SKBitmap output = new(Globals.FrameWidth, Globals.FrameHeight);
            using (var canvas = new SKCanvas(output))
            {
                canvas.DrawBitmap(padded1, 0, 0, SKSamplingOptions.Default);
                canvas.DrawBitmap(padded2, singleWidth, 0, SKSamplingOptions.Default);

                // If a middle bar is specified, then draw this over the top of the new composite image. This
                // will cause some clipping of the images, but this avoids complicated calculations and, unless
                // the width is large, it won't impact the image very much.
                if (Globals.MiddleBarWidth > 0)
                {
                    int xOffset = (Globals.FrameWidth - Globals.MiddleBarWidth) / 2;
                    using var paint = new SKPaint { Color = SKColors.Black };
                    canvas.DrawRect(new SKRect(xOffset, 0, xOffset + Globals.MiddleBarWidth, Globals.FrameHeight), paint);
                }
            }

            return output;
        }

        /// <summary>
        /// Centres a resized image on a black bitmap that is the frame height and half the frame width.
        /// </summary>
        /// <param name="resized">The image, already resized to the frame height.</param>
        /// <param name="singleWidth">The width of the black bitmap.</param>
        /// <returns>The new bitmap. The caller disposes it.</returns>
        private static SKBitmap PadToHalfWidth(SKBitmap resized, int singleWidth)
        {
            SKBitmap padded = new(singleWidth, Globals.FrameHeight);
            using var canvas = new SKCanvas(padded);
            canvas.Clear(SKColors.Black);
            int x = (singleWidth - resized.Width) / 2;
            canvas.DrawBitmap(resized, x, 0, SKSamplingOptions.Default);

            return padded;
        }

        /// <summary>
        /// Saves a bitmap as a JPEG (at 80% quality) by writing a temporary file and then renaming it.
        /// </summary>
        /// <param name="bitmap">The bitmap to save.</param>
        /// <param name="tempPath">The temporary file to write first.</param>
        /// <param name="outputPath">The final path of the JPEG.</param>
        private static void SaveJpegToFile(SKBitmap bitmap, string tempPath, string outputPath)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);

            // Write to a temporary name first, so an interrupted run never leaves a truncated sideby-*.jpg
            using (var stream = File.Open(tempPath, FileMode.Create, FileAccess.Write))
                data.SaveTo(stream);

            File.Move(tempPath, outputPath, true);
        }

        /// <summary>
        /// Gives the output the earlier of the two source dates for both created and modified, so it sorts with
        /// the originals. With --shuffle the dates are left alone, to avoid implying an artificial order.
        /// </summary>
        /// <param name="imageA">The first source image.</param>
        /// <param name="imageB">The second source image.</param>
        /// <param name="outputPath">The output file to change.</param>
        private static void SetOutputDates(Globals.ImageInfo imageA, Globals.ImageInfo imageB, string outputPath)
        {
            if (Globals.RandomiseSorting)
                return;

            // Use the earlier creation date of the two images
            DateTime creationDate = imageA.CreationDate;
            if (imageB.CreationDate < creationDate)
                creationDate = imageB.CreationDate;

            Logger.Write($"Setting creation and last modified dates to {creationDate} for {Path.GetFileName(outputPath)}", true);
            File.SetCreationTime(outputPath, creationDate);
            File.SetLastWriteTime(outputPath, creationDate);
        }

        /// <summary>
        /// Loads a bitmap from the specified path and applies orientation based on EXIF data.
        /// </summary>
        /// <param name="path">The image file to load.</param>
        /// <returns>The upright bitmap, or null if the file could not be opened or decoded.</returns>
        private static SKBitmap? LoadBitmapWithOrientation(string path)
        {
            using var codec = SKCodec.Create(path);
            if (codec == null)
            {
                Logger.Write($"Failed to open {path}", true);
                return null;
            }

            SKBitmap? bitmap = SKBitmap.Decode(path);
            if (bitmap == null)
            {
                Logger.Write($"Failed to decode {path}", true);
                return null;
            }

            return codec.EncodedOrigin switch
            {
                SKEncodedOrigin.TopLeft => bitmap,  // No rotation
                SKEncodedOrigin.BottomRight => RotateBitmap(bitmap, 180),  // Rotate 180°
                SKEncodedOrigin.RightTop => RotateBitmap(bitmap, 90),  // Rotate 90° CW
                SKEncodedOrigin.LeftBottom => RotateBitmap(bitmap, 270),  // Rotate 270° CW
                _ => bitmap  // Ignore all other cases
            };
        }

        /// <summary>
        /// Given a bitmap and a rotation in degrees (90, 180, 270), returns a new rotated bitmap.
        /// </summary>
        /// <param name="source">The bitmap to rotate. It is disposed by this method.</param>
        /// <param name="degrees">The clockwise rotation: 90, 180 or 270.</param>
        /// <returns>The rotated bitmap.</returns>
        private static SKBitmap RotateBitmap(SKBitmap source, float degrees)
        {
            // Boolean to indicate if we need to swap width and height
            bool swap = degrees == 90 || degrees == 270;

            // Calculate new dimensions, taking into account if we need to swap width and height
            int newWidth = source.Width;
            int newHeight = source.Height;
            if (swap)
            {
                newWidth = source.Height;
                newHeight = source.Width;
            }

            // Create a new bitmap to hold the rotated image
            SKBitmap rotated = new(newWidth, newHeight);

            using (var canvas = new SKCanvas(rotated))
            {
                canvas.Clear(SKColors.Black);

                // Move the origin to the centre of the target bitmap
                canvas.Translate(newWidth / 2f, newHeight / 2f);

                // Rotate around the centre
                canvas.RotateDegrees(degrees);

                // Draw the source bitmap centred
                canvas.DrawBitmap(source, -source.Width / 2f, -source.Height / 2f, SKSamplingOptions.Default);
            }

            // Free original
            source.Dispose();

            return rotated;
        }

        /// <summary>
        /// Resizes the image to the target height while maintaining aspect ratio.
        /// </summary>
        /// <param name="img">The bitmap to scale.</param>
        /// <param name="targetHeight">The height of the result in pixels.</param>
        /// <returns>A new bitmap of the target height, with the width scaled to match.</returns>
        private static SKBitmap ResizeToHeight(SKBitmap img, int targetHeight)
        {
            double scale = (double)targetHeight / img.Height;
            int newWidth = (int)(img.Width * scale);

            var resized = new SKBitmap(newWidth, targetHeight);
            using var canvas = new SKCanvas(resized);
            canvas.DrawBitmap(img, new SKRect(0, 0, newWidth, targetHeight), SKSamplingOptions.Default);
            return resized;
        }
    }
}
