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
|`SideBySide-x.x.x-win-x64.exe`|✅ For Microsoft Windows 10 and 11 ⬅️ **Most users should choose this**
|`SideBySide-x.x.x-linux-x64.zip`|For Linux on Intel/AMD CPUs|
|`SideBySide-x.x.x-linux-arm64.zip`|For Linux on ARM (e.g. Raspberry Pi)|
|`SideBySide-x.x.x-osx-arm64.zip`|For macOS on Apple Silicon (eg. M1 and newer)|
|`SideBySide-x.x.x-osx-x64.zip`|For macOS on Intel-based Macs (pre-Apple Silicon)|
|Source code (zip)|ZIP archive of the source code|
|Source code (tar.gz)|TAR.GZ archive of the source code|

NEED TO CHANGE INSTRUCTIONS HERE BECAUSE IT IS DIFFERENT FOR WINDOWS AND OTHER USERS.

> [!TIP]
> There is no installer. Just download the appropriate file and run it from the command line.

### Linux/macOS users

* You will need to install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime).
* Don't forget to make the file executable: `chmod +x SideBySide-x.x.x-<your-platform>`

### Platform testing notes

* Tested extensively: Windows 11
* Tested moderately: Linux (ARM)
* Not tested at all: Windows 10, Linux (x64), macOS (x64 & Apple Silicon)

## 🚀 Quick start guide

Here is an example for using SideBySide. It will work on all platforms.

```
SideBySide "C:\Users\Richard\Pictures\Holiday" "C:\Users\Richard\Pictures\Frame" 1080x720 12 -d

SideBySide "~/Pictures/Holiday" "~/Pictures/Frame" 1080x720 12 --delete
```
* Look for portrait images in the `Pictures\Holiday` home folder
* Output landscapes images in the `Pictures\Frame` home folder
* Delete previously generated images in `Pictures\Frame` before starting
* All landscape images to be 1080 x 720
* Use a 12 pixel separator (where necessary)


## 💻 Command line options

```
SideBySide <source dir> <destination dir> <output dimensions> <separator width> [options]
```

### Mandatory arguments

- **`<source dir>`**   
  Directory containing portrait `.jpg` or `.jpeg` images. Landscape or square images will be ignored.

- **`<destination dir>`**   
  Directory where generated landscape images will be saved. Must already exist.

- **`<output dimensions>`**   
  Output image dimensions specified as `[width]x[height]` (e.g. `1080x720`).

>[!TIP]
>For optimal display quality, set the output dimensions to match the native resolution of your digital photo frame. This ensures images are scaled accurately without distortion or unnecessary padding.

- **`<seperator width>`**   
  Minimum width in pixels of the black separator between images. Set to `0` to avoid adding extra separation.

>[!IMPORTANT]
>Black borders on the left and right may appear depending on the aspect ratio of the original images. The separator width defines the _minimum_ number of pixels placed between the two images. If natural spacing already exceeds this value due to image proportions, no additional separator will be added.

### Optional arguments

- **`-v`, `--verbose`**   
  Enables detailed logging output for debugging and progress tracking. This information is always included in the logs.

- **`-o`, `--overwrite`**   
  Overwrite existing output images if they already exist in the destination directory.

- **`-d`, `--delete`**   
  Remove all generated images from the destination folder before processing begins. Only `.jpg` files with names starting with `sideby-` will be deleted.

- **`-r`, `--random`**   
  Randomise the order of input images instead of sorting by date taken/created. When enabled, the output images will have their creation and modification timestamps set to the time of processing rather than preserving the originals.

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
>By default, the tool will not overwrite existing files. If you change visual settings and want updated images, use the `--overwrite` flag to force regeneration.

When images are sorted by date (default), the output file’s creation and modification times are set to the earliest of the two input images. When using `--random`, timestamps are not modified to avoid implying an artificial order.

## 🛟 Questions/problems?

Please raise an issue at https://github.com/mrsilver76/sidebyside/issues.

## 📝 Attribution
- Frame icon created by Freepik - Flaticon (https://www.flaticon.com/free-icons/frame)
- Image manipulation powered by ImageMagick - https://www.imagemagick.org
- Photo: [Coconut Tree Near Body of Water Under Blue Sky](https://www.pexels.com/photo/coconut-tree-near-body-of-water-under-blue-sky-240526/) by Asad Photo Maldives
- Photo: [Person Laying On Sand](https://www.pexels.com/photo/person-laying-on-sand-1770310/) by Rebeca Gonçalves
- Photo: [Brown Wooden Panel](https://www.pexels.com/photo/brown-wooden-panel-347139/) by Tirachard Kumtanom

## 🕰️ Version history

### 0.9.0 (xx May 2025)
- Initial version.
