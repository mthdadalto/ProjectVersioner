# ProjectVersioner

ProjectVersioner is a tool designed to automate version management for various project files including iOS Info.plist, Android AndroidManifest.xml, .cs files, and .csproj files. This README provides comprehensive instructions on how to use the ProjectVersioner tool.

## Features

- Automatically generate and apply version numbers based on provided arguments or Git tags.
- Update version information in iOS, Android, and .NET project files.
- Supports command-line arguments for flexibility and automation.

## Getting Started

### Prerequisites

- .NET SDK
- Git (optional for auto versioning using Git tags)

### Installation

Clone the repository to your local machine:
```sh
git clone <repository-url>
```

Navigate to the project directory:
```sh
cd ProjectVersioner
```

Build the project:
```sh
dotnet build
```

## Usage

### Command-Line Arguments

The ProjectVersioner tool accepts the following command-line arguments:

- `-auto`: Automatically fetch the latest Git tag as the version.
- `<file-paths>`: One or more paths to the files that need version updates.

### Examples

#### Using Auto Versioning

To use the auto versioning feature with Git tags, run the following command:
```sh
dotnet run -- -auto <file-path-1> <file-path-2> ...
```

#### Specifying a Version Manually

To specify a version manually, run the following command:
```sh
dotnet run -- <version> <file-path-1> <file-path-2> ...
```
Replace `<version>` with the desired version number (e.g., `1.0.0`).

#### Interactive Mode

If no valid files are passed via command-line arguments, the tool will prompt for file paths interactively.

### Supported Files

The tool supports the following file types:

- iOS Info.plist
- Android AndroidManifest.xml
- .NET .csproj files
- .NET .cs files (AssemblyInfo.cs)

### Version Formats

The tool supports the following version formats:

- `Major.Minor.Patch`
- `Major.Minor.Patch.Build`

## How It Works

### Version Generation

The version is generated based on the provided command-line arguments or the latest Git tag if `-auto` is specified. If no version is provided and `-auto` is not used, the tool prompts the user to enter a version.

### File Processing

The tool processes each file based on its type:

- **iOS Info.plist**: Updates `CFBundleVersion` and `CFBundleShortVersionString` with the generated version.
- **Android AndroidManifest.xml**: Updates `android:versionCode` and `android:versionName` with the generated version.
- **.NET .csproj**: Updates `Version`, `FileVersion`, and `AssemblyVersion` with the generated version.
- **.NET .cs (AssemblyInfo.cs)**: Updates `AssemblyVersion` and `AssemblyFileVersion` with the generated version.

### Helper Functions

- `MakeVersion(string[] args)`: Determines the version to use based on the command-line arguments or user input.
- `DriveFile(string path)`: Determines the file type and calls the appropriate method to update the version.
- `MakeIos(string path)`, `MakeAndroid(string path)`, `MakeProject(string path)`, `MakeAssembly(string path)`: Methods to update version information in respective file types.
- `RunCommand(string command, string args)`: Executes a command and returns its output.

## Error Handling

The tool includes basic error handling to catch and display errors during file processing. If an error occurs, the tool will display an error message and continue processing the next file.

## Contributing

Contributions are welcome! Please submit a pull request or open an issue to contribute to the project.

## License

This project is licensed under the MIT License.
