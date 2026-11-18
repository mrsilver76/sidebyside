# Version history

## 1.3.0 (18 November 2026)
- Added `-p` (`--proximity`) to set how close together in time two photos can be before SideBySide tries not to pair them. The default is now 120 seconds (it was 60) and `0` turns the check off.
- Photos taken close together, including bursts of three or more, are now kept out of the same pair more reliably.
- Pairing now avoids crossing over days where possible, so photos from different days are less likely to end up paired. A day ends at 6am to handle photos that go into the early hours of the morning.
- The default gap between the two images is now 16 pixels (it was 0).
- SideBySide now needs the .NET 10 Runtime, as support for .NET 8 ended on 10th November 2026.
- Updated SkiaSharp from 3.119.0 to 4.156.1 and MetadataExtractor from 2.8.1 to 2.9.3.
- Added a progress bar (with estimated time left) when scanning for portrait images and generating landscape images.
- Improved the robustness of image scanning and image pair creation, so minor failures don't cause the entire run to crash and half-written files aren't left lying around.
- Exit codes are now reported correctly. This enables scripts to understand if the task was successful or not.
- `-v` (`--verbose`) now actually works, previously it did nothing.
- `-r` (`--recursive`) no longer stops at the first folder it cannot read.
- Input folders with capital letters in their paths now work on Linux.
- A multitude of parsing issues with command line options have been resolved.
- The update check can no longer crash or hold up the run. It gives up after 5 seconds when GitHub is down, and a damaged update file is ignored.
- Fixed issues with the saving of logs and settings on Linux.
- Fixed an issue where `--help` and error messages weren't being printed when the output is being piped or redirected.
- Minor output and documentation changes.

## 1.2.0 (17 January 2026)
- Added a time-based check to avoid pairing photos taken within one minute of each other. This reduces the chances of visually similar photos being placed together, while still allowing photos from the same event to pair.
- Improved the output and grammar of content displayed during processing.
- Fixed an issue where the resulting images did not have the "Last Modified" date set correctly.

## 1.1.0 (18 October 2025)
- Added image comparison to prevent identical photos from being paired together.
- Fixed issue where photos rotated via EXIF data were incorrectly detected as landscape.
- Improved usage output: commands are now grouped by category and wrap correctly across terminal widths.
- Logger now includes OS information to help with troubleshooting across platforms.
- Added GitHub version checking with new `-nc` (`--no-check`) option to disable it.
- New header displays all key configuration information.
- Improved logger performance by keeping files open instead of repeatedly opening/closing.
- Split utility functions into static classes for clearer structure.
- Resolved all .NET code analysis warnings to standardize style and tidy the codebase.
- Updated publish Powershell script.

## 1.0.0 (02 August 2025)
- 🏁 Declared as the first stable release.
- Added `--mirror` option.
- Added `--filelist` option.
- A destination folder will now be created if one doesn't previously exist.
- Cleaned up `--help` output.
- Logger now includes OS information to help with troubleshooting across platforms.
- Cleaned up various pieces of code (analyzer suggestions regarding naming, simplifications, and style)

## 0.9.0 (02 June 2025)
- Initial version.