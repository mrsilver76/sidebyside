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

/*
 * ProgressBar.cs
 * An extremely simple console progress bar that displays progress, percentage complete,
 * and estimated time remaining. To use, set ProgressBar.Total to the total number of items
 * you will be processing, then call ProgressBar.Start() before starting, and ProgressBar.Stop()
 * when done. Call ProgressBar.Add (thread-safe) or set ProgressBar.Completed as you process each item.
 */

namespace SideBySide
{
    /// <summary>
    /// Shows a progress bar in the console, with the percentage complete and an estimated time left. It is not
    /// shown when the console output is redirected. To use it, set Total, call Start, add to Completed (or call Add) as work is done, then call Stop.
    /// It is safe to use from several threads.
    /// </summary>
    internal static class ProgressBar
    {
        /// <summary>Timer that redraws the bar every second, or null when the bar is not running.</summary>
        private static Timer? _timer;

        /// <summary>Stops Start, Stop and the redraw from running at the same time.</summary>
        private static readonly Lock _lock = new();

        /// <summary>UTC time the bar was started, used to estimate the time left. Null before Start.</summary>
        private static DateTime? _startTime;

        /// <summary>Text at the start of the line, before the bar.</summary>
        private const string LinePrefix = "   Status: ";

        /// <summary>The most columns the text after the bar can need. Used for ensuring a long ETA doesn't wrap.</summary>
        private const int RightTextWidth = 34;

        /// <summary>Smallest bar drawn, used when the terminal is very narrow.</summary>
        private const int MinBarWidth = 10;

        /// <summary>Line width used when the terminal width cannot be read.</summary>
        private const int FallbackLineWidth = 70;

        /// <summary>Backing field for Total.</summary>
        private static long _total;

        /// <summary>Backing field for Completed.</summary>
        private static long _completed;

        /// <summary>The total number of items to process.</summary>
        public static long Total
        {
            get => Interlocked.Read(ref _total);
            set => Interlocked.Exchange(ref _total, value);
        }

        /// <summary>The number of items that have been processed. Use Add when several threads update it.</summary>
        public static long Completed
        {
            get => Interlocked.Read(ref _completed);
            set => Interlocked.Exchange(ref _completed, value);
        }

        /// <summary>Safe way to add the amount done to the total completed, when in a parallel loop.</summary>
        /// <param name="amount">The amount to add.</param>
        public static void Add(long amount) => Interlocked.Add(ref _completed, amount);

        /// <summary>
        /// Resets the completed count, records the start time and starts the timer that redraws the bar every second.
        /// Does nothing if it is already started. If the console output is redirected, the count and start time
        /// are reset but the bar is never drawn.
        /// </summary>
        public static void Start()
        {
            lock (_lock)
            {
                if (_timer != null)
                    return;

                Completed = 0;
                _startTime = DateTime.UtcNow;

                if (Console.IsOutputRedirected)
                    return;

                Console.CursorVisible = false;
                _timer = new Timer(Update, null, 0, 1000);
            }
        }

        /// <summary>
        /// Stops the timer, clears the progress bar from the console and shows the cursor again.
        /// Does nothing if the bar is not running.
        /// </summary>
        public static void Stop()
        {
            lock (_lock)
            {
                if (_timer == null)
                    return;

                _timer.Dispose();
                _timer = null;
                ClearLine();
                Console.CursorVisible = true;
            }
        }

        /// <summary>
        /// Clears the bar from the console so a message can be written on the line, if the bar is running.
        /// The bar is drawn again at the next redraw. Does nothing if the bar is not running.
        /// </summary>
        public static void Pause()
        {
            lock (_lock)
            {
                if (_timer != null)
                    ClearLine();
            }
        }

        /// <summary>
        /// Redraws the progress bar in the console, and stops it once everything is complete.
        /// </summary>
        /// <param name="_">Not used. It is the state argument that the timer callback requires.</param>
        private static void Update(object? _)
        {
            lock (_lock)
            {
                long total = Total;
                long completed = Completed;

                // Don't draw the bar if we don't know how many items there are, or if we haven't started yet.
                if (total == 0 || _startTime == null)
                    return;

                // Calculate the percentage complete, clamping to 0 and 1 in case Completed > Total. This
                // can happen if another file is added to the folder whilst we're processing, or if a file
                // gets bigger while processing (e.g. a copy operation).
                double pct = Math.Clamp((double)completed / total, 0, 1);

                // Fill the terminal, leaving room for the worst-case percentage and ETA
                int lineWidth = GetLineWidth();
                int barWidth = lineWidth - LinePrefix.Length - 2 - RightTextWidth;
                if (barWidth < MinBarWidth)
                    barWidth = MinBarWidth;

                int filled = (int)(pct * barWidth);
                int empty = barWidth - filled;

                string bar = "[" + new string('#', filled) + new string('.', empty) + "]";

                string eta = "";
                if (completed >= 5 && pct >= 0.01)  // Only show ETA with enough data
                    eta = CalculateEta(completed, total, _startTime.Value);

                string line = $"{LinePrefix}{bar} {pct * 100:F0}% complete {eta}";
                if (line.Length < lineWidth)
                    line = line.PadRight(lineWidth);  // pad to prevent leftover chars

                Console.Write("\r" + line);
                Console.Out.Flush();

                if (completed >= total)
                    Stop();
            }
        }

        /// <summary>
        /// Estimates the time left from the progress so far.
        /// </summary>
        /// <param name="completed">The amount processed so far.</param>
        /// <param name="total">The total amount to process.</param>
        /// <param name="startTime">When processing started (UTC).</param>
        /// <returns>The time left in brackets, for example "(15 mins left)", rounded to a coarse step.</returns>
        private static string CalculateEta(long completed, long total, DateTime startTime)
        {
            TimeSpan elapsed = DateTime.UtcNow - startTime;
            double remainingItems = Math.Max(0, total - completed);
            double secondsPerItem = elapsed.TotalSeconds / completed;
            double remainingSeconds = remainingItems * secondsPerItem;

            TimeSpan remaining = TimeSpan.FromSeconds(remainingSeconds);

            // Rounding rules
            if (remaining.TotalSeconds < 60)
                return $"({Math.Ceiling(remaining.TotalSeconds / 5) * 5} secs left)";
            if (remaining.TotalMinutes < 5)
                return $"({Math.Ceiling(remaining.TotalMinutes)} mins left)";
            if (remaining.TotalMinutes < 60)
                return $"({Math.Ceiling(remaining.TotalMinutes / 5) * 5} mins left)";
            return $"({Math.Ceiling(remaining.TotalMinutes / 15) * 15} mins left)";
        }

        /// <summary>
        /// Clears the current console line by overwriting it with spaces and returning the cursor to the start of the line.
        /// </summary>
        private static void ClearLine()
        {
            int width = GetLineWidth();

            // Don't do anything with really narrow terminals.
            if (width < 1)
                return;

            Console.Write($"\r{new string(' ', width)}\r");
        }

        /// <summary>
        /// Gets the number of columns the bar line may use. This is one less than the terminal width, because
        /// writing into the last column can make some terminals wrap.
        /// </summary>
        /// <returns>The usable line width, or FallbackLineWidth if the terminal width cannot be read.</returns>
        private static int GetLineWidth()
        {
            try
            {
                return Console.BufferWidth - 1;
            }
            catch (Exception)
            {
                return FallbackLineWidth;
            }
        }
    }
}
