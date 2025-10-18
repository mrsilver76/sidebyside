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

using SkiaSharp;
using System.Text;
using System.Security.Cryptography;

namespace SideBySide
{
    internal sealed class ImageProcessor
    {
        /// <summary>
        /// Sorts the images by creation date and processes them in pairs to generate landscape images.
        /// </summary>
        public static void ProcessFiles()
        {
            if (Globals.Images.Count == 0)
            {
                Logger.Write("No images found to process.");
                return;
            }

            // Sort the files depending on the user preference
            if (Globals.RandomiseSorting)
            {
                Logger.Write($"Shuffling files found randomly...");
                Globals.Images = [.. Globals.Images.OrderBy(i => Guid.NewGuid())];
            }
            else
            {
                Logger.Write($"Sorting files found by creation date...");
                Globals.Images = [.. Globals.Images.OrderBy(i => i.CreationDate)];
            }

            // If we don't have at least 2 images then there isn't much we can generate

            if (Globals.Images.Count < 2)
            {
                Logger.Write($"Not enough images to process: need at least 2, found {Globals.Images.Count}.");
                return;
            }

            // Let's generate those images...
            Logger.Write($"Generating landscape images...");

            int generated = 0, skipped = 0;

            int i = 0;
            while (i + 1 < Globals.Images.Count)
            {
                Globals.ImageInfo image1 = Globals.Images[i];
                Globals.ImageInfo image2 = Globals.Images[i + 1];

                if (image1 == null || image2 == null)
                {
                    Logger.Write($"Skipping invalid image pair: {image1?.FileName ?? "null"} and {image2?.FileName ?? "null"}", true);
                    i++; // move forward to avoid infinite loop
                    continue;
                }

                if (ImageMetadataExtractor.FilesAreEqual(image1.FullPath, image2.FullPath))
                {
                    Logger.Write($"Skipping duplicate image pair: {image1.FileName} and {image2.FileName}", true);
                    skipped++;
                    i++; // skip by one to try next image
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
                    Logger.Write($"Skipping existing image: {Path.GetFileName(filename)}");
                    Globals.ProcessedFiles.Add(outputPath);
                    skipped++;
                    i += 2; // move to the next pair
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
                    // Something went wrong generating it
                    skipped++;
                }
                i += 2; // move to the next pair
            }

            // Log if we skipped an image because we didn't have a partner for it
            if (Globals.Images.Count % 2 != 0)
            {
                Logger.Write($"Unpaired image skipped: {Globals.Images[^1].FileName}");
                skipped++;
            }

            Logger.Write($"Finished generating {GrammarHelper.Pluralise(generated, "landscape image", "landscape images")} ({skipped} skipped)");
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
        public static bool GenerateSideBySideImage(Globals.ImageInfo imageA, Globals.ImageInfo imageB, string outputPath)
        {
            if (imageA == null || imageB == null)
            {
                Logger.Write("Invalid image information provided.", true);
                return false;
            }

            Logger.Write($"Generating landscape for {imageA.FileName} and {imageB.FileName} => {outputPath}", true);

            // Load original images
            using var original1 = LoadBitmapWithOrientation(imageA.FullPath);
            using var original2 = LoadBitmapWithOrientation(imageB.FullPath);

            // Handle any failures to load the images
            if (original1 == null)
            {
                Logger.Write($"Failed to load image: {imageA.FileName}", true);
                return false;
            }
            else if (original2 == null)
            {
                Logger.Write($"Failed to load image: {imageB.FileName}", true);
                return false;
            }

            // Resize images to fit the frame height while maintaining aspect ratio
            using var resized1 = ResizeToHeight(original1, Globals.FrameHeight);
            using var resized2 = ResizeToHeight(original2, Globals.FrameHeight);

            // Create a black bitmap which is the same height as the frame and half the width of the frame. This
            // is what we'll place the resized images on to.
            int singleWidth = Globals.FrameWidth / 2;
            using var padded1 = new SKBitmap(singleWidth, Globals.FrameHeight);
            using (var canvas = new SKCanvas(padded1))
            {
                canvas.Clear(SKColors.Black);
                int x = (singleWidth - resized1.Width) / 2;
                canvas.DrawBitmap(resized1, x, 0);
            }

            using var padded2 = new SKBitmap(singleWidth, Globals.FrameHeight);
            using (var canvas = new SKCanvas(padded2))
            {
                canvas.Clear(SKColors.Black);
                int x = (singleWidth - resized2.Width) / 2;
                canvas.DrawBitmap(resized2, x, 0);
            }

            // Create the final output bitmap which is double the width of a single image
            // and draw both padded images onto it side by side.
            using var output = new SKBitmap(Globals.FrameWidth, Globals.FrameHeight);
            using (var canvas = new SKCanvas(output))
            {
                canvas.DrawBitmap(padded1, 0, 0);
                canvas.DrawBitmap(padded2, singleWidth, 0);

                // If a middle bar is specified, then draw this over the top of the new composite image. This
                // will cause some clipping of the images, but this avoids complicated calculations and, unless
                // the width is large, it won't impact the image very much.
                if (Globals.MiddleBarWidth > 0)
                {
                    int xOffset = (Globals.FrameWidth - Globals.MiddleBarWidth) / 2;
                    canvas.DrawRect(new SKRect(xOffset, 0, xOffset + Globals.MiddleBarWidth, Globals.FrameHeight), new SKPaint { Color = SKColors.Black });
                }
            }

            // Save the output image (at 80% quality) to the specified path
            using var image = SKImage.FromBitmap(output);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
            using var stream = File.OpenWrite(outputPath);
            data.SaveTo(stream);

            Logger.Write($"Landscape image created: {Path.GetFileName(outputPath)}");

            // Set the creation and last modified dates to the earlier of the two images' creation dates, but only
            // if randomiseSorting is false. This ensures that the output image retains a meaningful timestamp.
            if (!Globals.RandomiseSorting)
            {
                // Use the earlier creation date of the two images
                DateTime creationDate = imageA.CreationDate < imageB.CreationDate ? imageA.CreationDate : imageB.CreationDate;
                Logger.Write($"Setting creation and last modified dates to {creationDate} for {Path.GetFileName(outputPath)}", true);
                File.SetCreationTime(outputPath, creationDate);
                File.SetLastWriteTime(outputPath, creationDate);
            }

            return true;
        }

        /// <summary>
        /// Loads a bitmap from the specified path and applies orientation based on EXIF data.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static SKBitmap? LoadBitmapWithOrientation(string path)
        {
            using var codec = SKCodec.Create(path);
            if (codec == null)
            {
                Logger.Write($"Failed to open {path}", true);
                return null;
            }

            SKBitmap bitmap = SKBitmap.Decode(path);

            return codec.EncodedOrigin switch
            {
                SKEncodedOrigin.TopLeft => bitmap,  // No rotation
                SKEncodedOrigin.BottomRight => RotateBitmap(bitmap, 180),  // Rotate 180°
                SKEncodedOrigin.RightTop => RotateBitmap(bitmap, 90),  // Rotate 90° CW
                SKEncodedOrigin.LeftBottom => RotateBitmap(bitmap, 270), // Rotate 270° CW
                _ => bitmap  // Ignore all other cases
            };
        }

        /// <summary>
        /// Given a bitmap and a rotation in degrees (90, 180, 270), returns a new rotated bitmap.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="degrees"></param>
        /// <returns></returns>
        private static SKBitmap RotateBitmap(SKBitmap source, float degrees)
        {
            // Boolean to indicate if we need to swap width and height
            bool swap = degrees == 90 || degrees == 270;

            // Calculate new dimensions, taking into account if we need to swap width and height
            int newWidth = swap ? source.Height : source.Width;
            int newHeight = swap ? source.Width : source.Height;

            // Create a new bitmap to hold the rotated image
            SKBitmap rotated = new(newWidth, newHeight);

            using (var canvas = new SKCanvas(rotated))
            {
                canvas.Clear(SKColors.Black);

                // Move the origin to the center of the target bitmap
                canvas.Translate(newWidth / 2f, newHeight / 2f);

                // Rotate around center
                canvas.RotateDegrees(degrees);

                // Draw the source bitmap centered
                canvas.DrawBitmap(source, -source.Width / 2f, -source.Height / 2f);
            }

            // Free original
            source.Dispose();

            return rotated;
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
