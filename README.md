# Dowe LanCaster 🚀

## Badges

[![GitHub License](https://img.shields.io/github/license/mrjohndowe/Dowe_LanCaster?style=flat-square)](LICENSE.txt)

## Description

Dowe LanCaster is a versatile Windows-to-Roku application designed for seamless local network (LAN) media casting. It supports a wide range of functionalities, including casting local files, streaming video from public web links, live desktop screen casting, and even remote control of your Roku device.

This application aims to provide a cloud-free media streaming experience, ensuring your content remains within your private network. It leverages powerful tools like FFmpeg and yt-dlp for robust media processing and URL resolution.

## Table of Contents

- [Project Title & Badges](#project-title--badges)
- [Description](#description)
- [Table of Contents](#table-of-contents)
- [Features](#features) ✨
- [Tech Stack](#tech-stack) 💻
- [Installation](#installation) 🛠️
- [Usage](#usage) ▶️
- [How to Use](#how-to-use) 💡
- [Project Structure](#project-structure) 📂
- [Contributing](#contributing) 🤝
- [License](#license) 📜
- [Important Links](#important-links) 🔗
- [Footer](#footer) 

## Features ✨

- **Folder Casting:** Cast entire folders as playlists to your Roku. Supports reordering, shuffling, repeating, and recursive scanning of subfolders. It can also automatically skip failed items and handle mixed media formats.
- **Link Casting:** Stream video content from public webpages or direct media URLs. The application uses `yt-dlp` to extract video sources from webpages and can pass necessary headers like `Referer` and `User-Agent` to FFmpeg.
- **Live Casting (Screen Casting):** Share your Windows desktop, a specific monitor, or an application window in real-time to your Roku device. Includes optional system audio streaming.
- **Local File Casting:** Play local media files directly on your Roku. For unsupported formats, Dowe LanCaster utilizes FFmpeg for on-the-fly transcoding to a Roku-friendly HLS stream.
- **Roku Discovery & Remote Control:** Automatically discover Roku devices on your network via SSDP or manually add them by IP address. The application includes a built-in Roku remote interface with directional pad, OK button, playback controls, and a keyboard for text input. It can also launch installed Roku applications.
- **Dark Mode Support:** The Windows application offers a fully functional dark mode, with customizable styling for various UI elements.
- **Voice Control:** Utilize Windows speech recognition to issue commands to your Roku device (e.g., `Home`, `Up`, `Play`, `Volume Up`).
- **Embedded TeraBox Browser:** Access and cast content directly from an embedded browser for TeraBox accounts.
- **System Audio Loopback:** Capture and stream your PC's system audio along with video content.
- **Roku ECP Ports:** Utilizes standard Roku External Control Protocol (ECP) ports (e.g., 8060).

## Tech Stack 💻

- **Primary Language:** C#
- **Frameworks:** .NET (for Windows application)
- **Core Technologies:** FFmpeg, yt-dlp, NAudio, System.Speech
- **Website:** PHP, CSS, JavaScript
- **Roku Application:** BrightScript

## Installation 🛠️

To build and run Dowe LanCaster, you'll need:

1.  **.NET 8 SDK:** Ensure you have the .NET 8 SDK installed on your Windows machine.
2.  **FFmpeg & FFprobe:** These executables need to be placed in the `tools/ffmpeg/` directory or available in your system's PATH. The project does not include FFmpeg binaries.
3.  **yt-dlp:** The `yt-dlp.exe` executable should be placed in the `tools/yt-dlp/` directory or downloaded via the `SETUP-DEPENDENCIES.cmd` script.

**Building the Windows Application:**

1.  **Close Existing Instance:** Ensure no Dowe LanCaster process is running before building.
    ```powershell
    Get-Process DoweLanCaster -ErrorAction SilentlyContinue | Stop-Process -Force
    ```
2.  **Restore Dependencies:** Navigate to the project's root directory and run:
    ```powershell
    dotnet restore
    ```
3.  **Clean and Build:**
    ```powershell
    dotnet clean
    dotnet build
    ```

**Installing the Roku Receiver:**

The Roku channel package is provided separately. You will need to sideload it onto your Roku device using Roku's developer mode.

## Usage ▶️

Dowe LanCaster provides multiple ways to cast media to your Roku device:

1.  **Folder Casting:** Select a folder containing media files. Dowe LanCaster will create a playlist that can be streamed to your Roku.
2.  **Link Casting:** Paste a URL to a public video or a webpage containing media. The application will attempt to extract the stream.
3.  **Live Casting:** Choose to broadcast your entire screen, a specific monitor, or a single application window. This is ideal for presentations or sharing anything on your desktop.
4.  **Local File Casting:** Directly select and play individual media files from your computer.
5.  **Roku Remote:** Use the integrated remote control to navigate your Roku, launch apps, and control playback.
6.  **Voice Control:** Speak commands like `Home`, `Back`, `Play`, or `Volume Up` to control your Roku.

## How to Use 💡

1.  **Launch Dowe LanCaster:** Run the compiled Windows application.
2.  **Discover or Add Roku:** The application will attempt to discover Roku devices on your network. If your Roku is not found, you can add it manually by its IP address.
3.  **Select Casting Method:** Choose one of the casting options (Folder, Link, Live, File).
4.  **Configure and Play:** Follow the on-screen prompts for your selected method. For example, with Folder Casting, select the desired folder. For Live Casting, choose your screen or application.
5.  **Control:** Use the built-in Roku remote or voice commands for playback control.

### Example: Casting a Folder

1.  Navigate to the 'Folder Cast' tab.
2.  Click 'Browse' to select the folder containing your media files.
3.  Click 'Add Folder to Playlist'.
4.  Select your Roku device from the list.
5.  Click 'Play Folder' to start streaming the playlist.

### Example: Live Casting Your Desktop

1.  Go to the 'Live Cast' tab.
2.  Select 'Desktop' as the capture source.
3.  Optionally, enable 'Capture System Audio'.
4.  Choose your Roku device.
5.  Click 'Start Live Cast'.

## Project Structure 📂

- **`docs/`**: Contains various Markdown and Text files detailing specific features, phases, and documentation.
- **`installer/`**: Includes files related to creating the Windows installer.
- **`scripts/`**: PowerShell scripts for various setup and packaging tasks.
- **`site/`**: Frontend files for the project's website, including PHP, CSS, and JavaScript.
- **`src/DoweLanCaster.Roku/`**: Contains the source code for the Roku BrightScript application.
- **`src/DoweLanCaster.Windows/`**: Houses the core Windows application code (C#), including UI elements, services, and models.
- **`tools/`**: Directory for external tools like FFmpeg and yt-dlp, with READMEs explaining their purpose and expected placement.
- **`tests/`**: Contains unit and integration tests for the application.
- **`CHANGELOG.md`**: Records changes and version history.
- **`LICENSE.txt`**: Contains the project's license information.
- **`README.md`**: The main README file for the project.
- **`DoweLanCaster.sln`**: Visual Studio solution file for the C# project.

## Installation Requirements

- **Operating System:** Windows 10 or 11 (for the casting application).
- **Roku Device:** A Roku device on the same local network as the Windows PC.
- **.NET Runtime:** .NET 8 Runtime is required for the Windows application.
- **External Dependencies:** FFmpeg and yt-dlp executables must be placed in their respective `tools/` subdirectories or accessible via the system's PATH.

## Contributing 🤝

Contributions are welcome! Please follow these guidelines:

1.  **Fork the repository.**
2.  **Create a new branch** for your feature or bug fix (`git checkout -b feature/AmazingFeature`).
3.  **Make your changes** and commit them (`git commit -m 'Add some AmazingFeature'`).
4.  **Push to the branch** (`git push origin feature/AmazingFeature`).
5.  **Open a Pull Request.**

Please ensure your code adheres to the existing style and includes tests where appropriate.

## License 📜

This project is licensed under the [MIT License](LICENSE.txt). See the `LICENSE.txt` file for more details.

## Important Links 🔗

- **GitHub Repository:** [https://github.com/mrjohndowe/Dowe_LanCaster](https://github.com/mrjohndowe/Dowe_LanCaster)
- **Releases Page:** [https://github.com/mrjohndowe/Dowe_LanCaster/releases](https://github.com/mrjohndowe/Dowe_LanCaster/releases)

## Footer 

---

<p align="center">
  <a href="https://github.com/mrjohndowe/Dowe_LanCaster/fork" target="_blank">
    <img src="https://img.shields.io/github/forks/mrjohndowe/Dowe_LanCaster?style=social" alt="GitHub forks">
  </a>
  <a href="https://github.com/mrjohndowe/Dowe_LanCaster/stargazers" target="_blank">
    <img src="https://img.shields.io/github/stars/mrjohndowe/Dowe_LanCaster?style=social" alt="GitHub stars">
  </a>
  <a href="https://github.com/mrjohndowe/Dowe_LanCaster/issues" target="_blank">
    <img src="https://img.shields.io/github/issues/mrjohndowe/Dowe_LanCaster?style=social" alt="GitHub issues">
  </a>
</p>

Made with ❤️ by **mrjohndowe**

[![Dowe LanCaster](https://img.shields.io/badge/Dowe_LanCaster-v0.9.0-blue)](https://github.com/mrjohndowe/Dowe_LanCaster)


---
**<p align="center">Generated by [ReadmeCodeGen](https://www.readmecodegen.com/)</p>**