# SideBySide
_A cross-platform command-line tool (Windows, Linux, macOS) for combining two portrait photos into a single landscape image. This is useful for digital photo frames that display vertical images awkwardly._

## 🖼️ Framing the problem

Many digital photo frames are designed with landscape orientation in mind.

When they encounter portrait images, they often resort to awkward workarounds - adding black bars to the sides (_pillarboxing_) or cropping the top and bottom to fill the frame. Both approaches compromise the photo: either by shrinking it down to an underwhelming size or by cutting out important parts of the image. For users who care about presentation, this default behaviour can feel unsatisfying and unpolished.

![screenshot](https://github.com/mrsilver76/sidebyside/blob/main/frames.jpg?raw=true)

SideBySide works around this problem by combining two portrait images into a single landscape composition. The pairing is automatic, the layout balanced and the final resolution tailored to your display. Because the output matches your frame’s aspect ratio exactly, each image appears larger and sharper - making better use of the available screen.

The result is a photo frame that feels curated - not cluttered or compromised.

## 🧰 Features
* 💻 Runs on Windows, Linux (x64 & ARM) and macOS (Intel & Apple Silicon).
* 📂 Accepts multiple source folders in a single run.
* 🔎 Recursively scans subfolders when enabled.
* 🖼️ Combines two portrait `.jpg` images into one seamless landscape `.jpg`.
* ➖ Adds an optional divider to enhance visual separation between images.
* 🎯 Automatically centers each photo within a black background.
* 📐 Resizes output to optimally fit digital photo frame displays.
* 🔐 Creates uniquely named files to prevent duplicates and maintain consistency.
* 🔀 Supports sorting photos by date taken/created or random order.
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
- Ensure that the extracted `.dll` remains in the same directory as the binary - this is required for the native SkiaSharp bindings to work.  
- Open a Command Prompt in that folder and run the program with your desired arguments.
 
### Linux and macOS users
- Download the `.zip` file for your architecture (see the table above) and extract it.
- Install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime) if it's not already available.
- Make the binary executable: `chmod +x SideBySide-x.x.x-<your-platform>`
- Ensure that the extracted `.so` (Linux) or `.dylib` (macOS) file remains in the same folder as the binary - this is required for the native SkiaSharp bindings to work.
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

```
Usage: SideBySide [<input_dir>...] -o <output_dir> -d <WxH> [options]
```

### Mandatory arguments

- **`<input_dir>...`**   
  One or more directories containing portrait `.jpg` or `.jpeg` images. Landscape or square images will be ignored.

- **`-o <output_dir>`, `--output <output_dir>`**   
  Directory where generated landscape images will be saved. Must already exist.

- **`-d <WxH>`, `--dimensions <WxH>`**   
  Output image dimensions specified as `[width]x[height]` or `[width],[height]` (e.g. `1080x720` or `1080,720`).

>[!TIP]
>For optimal display quality, set the output dimensions to match the native resolution of your digital photo frame. This ensures images are scaled accurately without distortion or unnecessary padding.

### Optional arguments

- **`-g <pixels>`, `--gap <pixels>`**   
  Minimum width in pixels of the black separator between images. If not provided or set to `0` then extra separation will be avoided where possible.

>[!IMPORTANT]
>Black borders on the left and right may appear depending on the aspect ratio of the original images. The separator width defines the _minimum_ number of pixels placed between the two images. If natural spacing already exceeds this value due to image proportions, no additional separator will be added.

- **`-s`, `--shuffle`**   
  Shuffle/randomise the order of input images instead of sorting by date taken/created. When enabled, the output images will have their creation and modification timestamps set to the time of processing rather than preserving the originals.

- **`-r`, `--recursive`**   
  Recursively scan all subdirectories beneath each source folder. Only files with supported extensions will be considered.

- **`-w`, `--write`**   
  Overwrite existing output images if they already exist in the destination directory.

- **`-c`, `--clean`**   
  Clean/remove all generated images from the destination folder before processing begins. Only `.jpg` files with names starting with `sideby-` will be deleted.
  
- **`-v`, `--verbose`**   
  Enables detailed logging output for debugging and progress tracking. This information is always included in the logs.

- **`/?`, `-h`, `--help`**  
  Displays the full help text with all available options, credits and the location of the log files.

## 🧾 Output file naming and timestamp behavour

Each generated file is uniquely named using a short SHA256 hash derived from the full paths of the two source images.

For example:
```
sideby-Z9dcF2gqex7o7RGn.jpg
```
The filename is deterministic: given the same pair of input files, the output name will always be the same - regardless of options like separator width or output resolution. This ensures consistency across runs and avoids unnecessary duplication.

>[!NOTE]
>By default, the tool will not overwrite existing files. If you change visual settings and want updated images, use the `--write` flag to force regeneration.

When images are sorted by date (default), the output file’s creation and modification times are set to the earliest of the two input images. When using `--shuffle`, timestamps are not modified to avoid implying an artificial order.

## 🛟 Questions/problems?

Please raise an issue at https://github.com/mrsilver76/sidebyside/issues.

## 📝 Attribution
- Frame icon created by Freepik - Flaticon (https://www.flaticon.com/free-icons/frame)
- Image manipulation powered by [SkiaSharp](https://github.com/mono/SkiaSharp), a .NET wrapper around Google's Skia 2D graphics library.
- Photo: [Coconut Tree Near Body of Water Under Blue Sky](https://www.pexels.com/photo/coconut-tree-near-body-of-water-under-blue-sky-240526/) by Asad Photo Maldives.
- Photo: [Person Laying On Sand](https://www.pexels.com/photo/person-laying-on-sand-1770310/) by Rebeca Gonçalves.
- Photo: [Brown Wooden Panel](https://www.pexels.com/photo/brown-wooden-panel-347139/) by Tirachard Kumtanom.

## 🕰️ Version history

### 0.9.0 (02 June 2025)
- Initial version.
