# SideBySide

<p><img src="https://img.shields.io/badge/Windows-supported-0078D6?logo=windows&logoColor=white" alt="Windows"> <img src="https://img.shields.io/badge/Linux-supported-FCC624?logo=linux&logoColor=black" alt="Linux"> <img src="https://img.shields.io/badge/macOS-supported-000000?logo=apple&logoColor=white" alt="macOS"> <img src="https://img.shields.io/badge/.NET-C%23-512BD4?logo=dotnet&logoColor=white" alt=".NET/C#"> 
<img src="https://img.shields.io/github/license/mrsilver76/sidebyside?logo=gnu&logoColor=white" alt="GPL License"></p>

_A cross-platform command-line tool (Windows, Linux, macOS) for combining two portrait photos into a single landscape image. This is useful for digital photo frames that display vertical images awkwardly._

## 🖼️ Framing the problem

Many digital photo frames are designed with landscape orientation in mind.

When they encounter portrait images, they often resort to awkward workarounds - adding black bars to the sides (_pillarboxing_) or cropping the top and bottom to fill the frame. Both approaches compromise the photo: either by shrinking it down to an underwhelming size or by cutting out important parts of the image. For users who care about presentation, this default behaviour is ugly.

![screenshot](https://github.com/mrsilver76/sidebyside/blob/main/frames.jpg?raw=true)

SideBySide works around this problem by combining two portrait images into a single landscape composition. This ensures each photo is displayed without cropping or black bars. The combined image is also resized to match your frame’s resolution, reducing storage use without affecting visible quality.

>[!TIP]
>- Got a messy photo collection? [GroupMachine](https://github.com/mrsilver76/groupmachine) can automatically sort photos and videos into albums.<br/>
>- Manage your photos with Picasa? [StarCasa](https://github.com/mrsilver76/starcasa) outputs a list of your favorite portraits ready for SideBySide.

## 🧰 Features
* 💻 Runs on Windows, Linux (x64 & ARM) and macOS (Intel & Apple Silicon).
* 📂 Accepts multiple source folders in a single run.
* 📄 Supports file lists for precise control over included images.
* 🔎 Recursively scans subfolders when enabled.
* 🖼️ Combines two portrait `.jpg` images into one seamless landscape `.jpg`.
* 🧠 Detects and avoids pairing visually identical photos.
* ➖ Adds an optional divider to enhance visual separation between images.
* 🎯 Automatically centers each photo within a black background.
* 📐 Resizes output to optimally fit digital photo frame displays.
* 🔐 Creates uniquely named files to prevent duplicates and maintain consistency.
* 🔀 Supports sorting photos by date taken/created or random order.
* 🪞 Mirrors output folder by removing stale files no longer in source.
* 🕰️ Preserves original timestamps (when sorted).

## 📦 Download

Get the latest version from https://github.com/mrsilver76/sidebyside/releases.

Each release includes the following files (`x.x.x` denotes the version number):

|Filename|Description|
|:--------|:-----------|
|`SideBySide-x.x.x-win-x64.zip`|✅ For Microsoft Windows 10 and 11 ⬅️ **Most users should choose this**
|`SideBySide-x.x.x-linux-x64.zip`|For Linux on Intel/AMD CPUs|
|`SideBySide-x.x.x-linux-arm64.zip`|For Linux on ARM (e.g. Raspberry Pi)|
|`SideBySide-x.x.x-osx-arm64.zip`|For macOS on Apple Silicon (eg. M1 and newer)|
|`SideBySide-x.x.x-osx-x64.zip`|For macOS on Intel-based Macs (pre-Apple Silicon)|
|Source code (zip)|ZIP archive of the source code|
|Source code (tar.gz)|TAR.GZ archive of the source code|

### Windows users
- Download the `.zip` file for Windows (see the table above) and extract it by right-clicking and selecting "Extract All.."
- Ensure that the extracted `libSkiaSharp.dll` remains in the same directory as the binary - this is required for the native SkiaSharp bindings to work.  
- Open a Command Prompt in that folder and run the program with your desired arguments.

### macOS users

- Download the appropriate binary for your platform (see table above).
- Ensure that the extracted `libSkiaSharp.dylib` file remains in the same folder as the binary - this is required for the native SkiaSharp bindings to work.
- Install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime). Slightly more technical information can be found [here](https://learn.microsoft.com/en-gb/dotnet/core/install/macos).
- ⚠️ Do not install the SDK, ASP.NET Core Runtime, or Desktop Runtime.
- Make the downloaded file executable: `chmod +x SideBySide-x.x.x-<your-platform>`
- Open a terminal in that folder and run the program with your desired arguments.
- If you get `zsh: killed` when running the executable then:
  - Apply an ad-hoc code signature:
    - `codesign --force --deep --sign - SideBySide-x.x.x-<your-platform>`
    - `codesign --force --deep --sign - libSkiaSharp.dylib`
  - Remove the quarantine attribute:
    - `xattr -d com.apple.quarantine SideBySide-x.x.x-<your-platform>`
    - `xattr -d com.apple.quarantine libSkiaSharp.dylib`

### Linux users

- Download the appropriate binary for your platform (see table above).
- Ensure that the extracted `libSkiaSharp.so` file remains in the same folder as the binary - this is required for the native SkiaSharp bindings to work.
- Install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime). Slightly more technical pages can be found [here](https://learn.microsoft.com/en-gb/dotnet/core/install/linux).
- ⚠️ Do not install the SDK, ASP.NET Core Runtime, or Desktop Runtime.
- Make the downloaded file executable: `chmod +x SideBySide-x.x.x-<your-platform>`
- Open a terminal in that folder and run the program with your desired arguments.

### Platform testing notes

* Tested extensively: Windows 11
* Tested moderately: Linux (ARM)
* Not tested at all: Windows 10, Linux (x64), macOS (x64 & Apple Silicon)

## 🚀 Quick start guide

Here are some examples for using SideBySide. They will work on all platforms.

```
SideBySide "C:\Users\Richard\Pictures\Holiday" -o "C:\Users\Richard\Pictures\Frame" -d 1080x720 -c

SideBySide "~/Pictures/Holiday" --output "~/Pictures/Frame" --dimensions 1080x720 --clean
```
* Look for portrait images in the `Pictures\Holiday` home folder
* Output landscapes images in the `Pictures\Frame` home folder
* Delete previously generated images in `Pictures\Frame` before starting
* All landscape images to be 1080 x 720
* Use no pixel separator (where possible)

```
SideBySide "C:\Users\Richard\Pictures\Holiday\Europe" "C:\Users\Richard\Pictures\Holiday\America" -o "C:\Users\Richard\Pictures\Frame" -d 1080x720 -g 10 -r -w

SideBySide "~/Pictures/Holiday/Europe" "~/Pictures/Holiday/America" --ouput "~/Pictures/Frame" --dimensions 1080x720 -gap 10 --recursive --write
```
* Look for portrait images in the `Pictures\Holiday\Europe` and `Pictures\Holiday\America` home folders (and all sub-folders)
* Output landscapes images in the `Pictures\Frame` home folder
* All landscape images to be 1080 x 720
* Use a pixel separator of no less than `10` pixels
* Overwrites any previously generated images in `Pictures\Frame`

## 💻 Command line options

SideBySide is a command-line tool. Run it from a terminal or command prompt, supplying all options and arguments directly on the command line. Logs with detailed information are also written and you can find the log file location using `-h` (`--help`).

```
Usage: SideBySide [<input_dir>...] -o <output_dir> -d <WxH> [options]
```

### Mandatory arguments

- **`<input_dir>...`**   
  One or more directories containing portrait `.jpg` or `.jpeg` images. Landscape or square images will be ignored.

>[!NOTE]
>At least one input directory or the `-f` (`--filelist`) option is required.

- **`-o <output_dir>`, `--output <output_dir>`**   
  Directory where generated landscape images will be saved. Must already exist.

- **`-d <WxH>`, `--dimensions <WxH>`**   
  Output image dimensions specified as `[width]x[height]` or `[width],[height]` (e.g. `1080x720` or `1080,720`).

>[!TIP]
>For optimal display quality, set the output dimensions to match the native resolution of your digital photo frame. This ensures images are scaled accurately without distortion or unnecessary padding.

### Input control

- **`-r`, `--recursive`**   
  Recursively scan all subdirectories beneath each source folder. Only files with supported extensions will be considered.

- **`-f <file>`, `--filelist <file>`**   
  Use a plain text file (`<file>`) to explicitly define which images to process. Each line must contain the full path to a .jpg or .jpeg image. Empty lines and invalid paths will be silently ignored, meaning that messy or autogenerated lists can be used.

  This option is ideal when:
  - You want full control over exactly which images are processed, regardless of folder structure.
  - You're using a third-party tool like [StarCasa](https://github.com/mrsilver76/starcasa) to generate a `txt` file of starred photos in [Google's Picasa](https://picasa.en.softonic.com/) or similar apps.
  
>[!NOTE]
>Using `-f` (`--filelist`) does not require you to provide a folder for processing. However if you specify both a file list and input folders, all valid images from both sources will be used.

### Output control

- **`-g <pixels>`, `--gap <pixels>`**   
  Minimum width in pixels of the black separator between images. If not provided or set to `0` then extra separation will be avoided where possible.

>[!IMPORTANT]
>Black borders on the left and right may appear depending on the aspect ratio of the original images. The separator width defines the _minimum_ number of pixels placed between the two images. If natural spacing already exceeds this value due to image proportions, no additional separator will be added.

- **`-w`, `--write`**   
  Overwrite existing output images if they already exist in the destination directory.

- **`-c`, `--clean`**   
  Clean/remove all generated images from the destination folder before processing begins. Only `.jpg` files with names starting with `sideby-` will be deleted.

- **`-m`, `--mirror`**   
  Enable mirror mode for the output directory. Any previously generated files (matching `sideby-*.jpg`) in the destination folder that are not recreated or deliberately skipped in the current run will be deleted. This ensures the output folder is an accurate mirror of the current inputs - no outdated or stale composite images will be left behind.

  This is particularly useful when you regularly update or rotate source content, you use file lists (`-f` or `--filelist`) for precise input control and/or you want a clean output folder without accumulating unused files.

>[!NOTE]
>Mirror mode only affects files generated by this tool (`sideby-*.jpg`). It will not touch other files in the output directory.

### Miscellaneous

- **`-s`, `--shuffle`**   
  Shuffle/randomise the order of input images instead of sorting by date taken/created. When enabled, the output images will have their creation and modification timestamps set to the time of processing rather than preserving the originals.

- **`-nc`, `--no-check`**  
  Disables GitHub version checks for GroupMachine.

>[!NOTE]
>Version checks occur at most once every 7 days. SideBySide connects only to [this URL](https://api.github.com/repos/mrsilver76/sidebyside/releases/latest) to retrieve version information. No data about you or your photo library is shared with the author or GitHub - you can verify this yourself by reviewing `GitHubVersionChecker.cs`

 - **`-v`, `--verbose`**   
  Enables detailed logging output for debugging and progress tracking. This information is always included in the logs.

- **`/?`, `-h`, `--help`**  
  Displays the full help text with all available options, credits and the location of the log files.

## 🧾 Output file naming and timestamp behavour

Each generated file is uniquely named using a short SHA256 hash derived from the full paths of the two source images.

For example:
```
sideby-Z9dcF2gqex7o7RGn.jpg
sideby-gBX41a-GkfcaaBA4.jpg
sideby-QgVzwyOYxBrFTEjb.jpg
```
The filename is deterministic: given the same pair of input files, the output name will always be the same - regardless of options like separator width or output resolution. This ensures consistency across runs and avoids unnecessary duplication.

>[!NOTE]
>By default, the tool will not overwrite existing files. If you change visual settings and want updated images, use the `--write` flag to force regeneration.

When images are sorted by date (default), the output file’s creation and modification times are set to the earliest of the two input images. When using `--shuffle`, timestamps are not modified to avoid implying an artificial order.

## 🛟 Questions/problems?

Please raise an issue at https://github.com/mrsilver76/sidebyside/issues.

## 💡 Future development: open but unplanned

SideBySide currently meets the needs it was designed for, and no major new features are planned at this time. However, the project remains open to community suggestions and improvements. If you have ideas or see ways to enhance the tool, please feel free to submit a [feature request](https://github.com/mrsilver76/sidebyside/issues).

## 📝 Attribution
- Frame icon created by Freepik - Flaticon (https://www.flaticon.com/free-icons/frame)
- Image manipulation powered by [SkiaSharp](https://github.com/mono/SkiaSharp), a .NET wrapper around Google's Skia 2D graphics library.
- Photo: [Coconut Tree Near Body of Water Under Blue Sky](https://www.pexels.com/photo/coconut-tree-near-body-of-water-under-blue-sky-240526/) by Asad Photo Maldives.
- Photo: [Person Laying On Sand](https://www.pexels.com/photo/person-laying-on-sand-1770310/) by Rebeca Gonçalves.
- Photo: [Brown Wooden Panel](https://www.pexels.com/photo/brown-wooden-panel-347139/) by Tirachard Kumtanom.

## 🕰️ Version history

### 1.1.0 (18 October 2025)
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

### 1.0.0 (02 August 2025)
- 🏁 Declared as the first stable release.
- Added `--mirror` option.
- Added `--filelist` option.
- A destination folder will now be created if one doesn't previously exist.
- Cleaned up `--help` output.
- Logger now includes OS information to help with troubleshooting across platforms.
- Cleaned up various pieces of code (analyzer suggestions regarding naming, simplifications, and style)

### 0.9.0 (02 June 2025)
- Initial version.
