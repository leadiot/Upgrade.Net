# Upgrade.Net

An application upgrade program based on .NET, supporting both console and WPF versions, providing complete download, backup, decompression, and launch processes with custom upgrade step configuration.

## Project Introduction

Upgrade.Net is a lightweight Windows application upgrade solution that supports downloading update packages from remote servers and automatically completing version upgrades. The project includes two versions:

- **Upgrade.Cmd**: Console version, suitable for background silent upgrade scenarios
- **Upgrade.Wpf**: WPF UI version, providing visual upgrade progress and user interaction

## Features

- **Custom Upgrade Steps**: Support defining dynamic upgrade step sequences via JSON configuration
- **15 Operation Types**: Download, Command Execution, Zip, Unzip, Move, Copy, Create, Delete, Rename, etc.
- **Wait Time**: Each step can be configured with wait time and countdown display
- **Retry Mechanism**: Support configuring retry count and retry delay
- **Auto Launch**: Support automatically launching the main program after upgrade, including `dotnet` command support
- **Progress Display**: Real-time display of download, backup, and decompression progress and status information
- **Pause/Cancel**: WPF version supports download pause, resume, and cancel functions
- **Configuration File**: Support JSON format upgrade configuration file

## Screenshots

| Version | Screenshot |
|---------|------------|
| WPF Version | ![WPF Start Interface](screenshots/wpf_step_0.png) |
| WPF Version | ![WPF Upgrade Interface](screenshots/wpf_step_1.png) |
| WPF Version | ![WPF Complete Interface](screenshots/wpf_step_3.png) |
| Console Version | ![Console Start Interface](screenshots/cmd_step_0.png) |
| Console Version | ![Console Upgrade Interface](screenshots/cmd_step_1.png) |
| Console Version | ![Console Complete Interface](screenshots/cmd_step_3.png) |

## Software Architecture

### Console Version (Upgrade.Net)

- **Target Framework**: .NET 10.0
- **Output Type**: Console Application
- **Network Requests**: HttpClient (static reuse)
- **Data Exchange**: System.Text.Json

### WPF Version (Upgrade.Wpf)

- **Target Framework**: .NET 10.0-windows
- **Output Type**: WPF Desktop Application
- **Architecture Pattern**: MVVM
- **Network Requests**: HttpClient (static reuse)
- **Data Exchange**: System.Text.Json
- **UI Style**: China Blue theme, borderless window design

### Core Modules

| Module | Description |
|--------|-------------|
| UpgradeCommand | Upgrade command class, handling download, decompression, restart logic (Console version) |
| UpgradeWindow | Upgrade window, handling download, decompression, restart logic (WPF version) |
| SplashWindow | Splash screen window (WPF version) |
| UpgradeConfig | Upgrade configuration management class |
| StepConfig | Step configuration class |
| UpgradeAction | Upgrade operation class (Strategy Pattern) |
| UpgradeOption | Operation type enumeration |
| ScmAppInfo | Application information DTO |
| ScmVerInfo | Version information DTO |

## Configuration File

### upgrade.json

```json
{
  "icon": "your_icon.ico",
  "title": "your_app_title",
  "oldVersion": "1.0.0",
  "newVersion": "2.0.0",
  "autoStart": true,
  "autoClose": false,
  "showSteps": true,
  "appInfo": "your_app_description",
  "verInfo": "your_app_version_info",
  "steps": [
    {
      "title": "Download Update Package",
      "description": "Download the latest update package from server",
      "option": "Download",
      "url": "https://example.com/upgrade.zip",
      "file": "upgrade.zip",
      "waitTime": 5
    },
    {
      "title": "Backup Existing Files",
      "description": "Backup all files in current installation directory",
      "option": "Zip",
      "source": "your_app_install_path",
      "file": "backup.zip",
      "waitTime": 0
    },
    {
      "title": "Extract Update Package",
      "description": "Extract the update package to installation directory",
      "option": "Unzip",
      "source": "upgrade.zip",
      "destination": "your_app_install_path",
      "overwrite": true,
      "waitTime": 0
    },
    {
      "title": "Clean Up Temporary Files",
      "description": "Delete downloaded temporary files",
      "option": "DeleteDoc",
      "file": "upgrade.zip",
      "waitTime": 0
    },
    {
      "title": "Launch Application",
      "description": "Launch the upgraded application",
      "option": "Command",
      "command": "dotnet MyApp.dll",
      "args": "--environment Production",
      "waitTime": 0
    }
  ]
}
```

### Configuration Fields

#### Basic Configuration

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| icon | string | Yes | Path to the application icon |
| title | string | Yes | Display title of the upgrade program |
| oldVersion | string | Yes | Current version of the application |
| newVersion | string | Yes | New version of the application |
| autoStart | bool | No | Whether to start the application after upgrade, default false |
| autoClose | bool | No | Whether to close the updater after upgrade, default false |
| showSteps | bool | No | Whether to show upgrade step list, default true |
| appInfo | string | No | Application description, supports longer text scrolling |
| verInfo | string | No | Version upgrade description, supports longer text scrolling |

#### Step Configuration (steps)

Each step contains the following properties:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| title | string | Yes | Step title, displayed in the step list |
| description | string | No | Step description, detailed explanation of the step's purpose |
| option | string | Yes | Operation type, see UpgradeOption enumeration |
| waitTime | int | No | Wait time after step completion (seconds), supports countdown display, default 0 |
| continueOnError | bool | No | Whether to continue with subsequent steps when this step fails, default false |
| retryCount | int | No | Number of retries, default 0 |
| retryDelay | int | No | Retry delay in milliseconds, default 1000 |
| source | string | No | Source path/file, used according to different operation types |
| destination | string | No | Destination path/file, used according to different operation types |
| file | string | No | File name, used for download, delete, create file operations |
| path | string | No | Directory path, used for create, delete directory operations |
| url | string | No | Download URL, used for Download operation |
| command | string | No | Command line command, used for Command operation |
| args | string | No | Command arguments, used for Command operation |
| oldName | string | No | Original name, used for rename operations |
| newName | string | No | New name, used for rename operations |
| overwrite | bool | No | Whether to overwrite, used for unzip, copy operations, default true |

### UpgradeOption Operation Types

| Operation Type | Description | Required Parameters |
|----------------|-------------|---------------------|
| None | No operation | None |
| Download | Download file from URL | url, file |
| Command | Execute command line | command, args(optional) |
| Launch | Launch application after upgrade | command, args(optional) |
| Zip | Compress file/directory | source, file |
| Unzip | Extract file | source, destination, overwrite(optional) |
| MoveDir | Move directory | source, destination |
| MoveDoc | Move file | source, destination |
| CopyDir | Copy directory | source, destination, overwrite(optional) |
| CopyDoc | Copy file | source, destination, overwrite(optional) |
| CreateDir | Create directory | path |
| CreateDoc | Create file | file |
| DeleteDir | Delete directory | path |
| DeleteDoc | Delete file | file |
| RenameDir | Rename directory | oldName, newName |
| RenameDoc | Rename file | oldName, newName |

### Complete Example Configuration

Here is a complete configuration example with all operation types:

```json
{
  "icon": "logo.ico",
  "title": "Application Updater",
  "oldVersion": "1.0.0",
  "newVersion": "2.0.0",
  "autoStart": true,
  "autoClose": false,
  "showSteps": true,
  "appInfo": "This is a .NET-based application upgrade tool supporting custom step configuration.",
  "verInfo": "Version 2.0.0 update notes:\n1. Added custom step feature\n2. Supports 15 operation types\n3. Added retry mechanism\n4. Optimized UI layout",
  "steps": [
    {
      "title": "Create Temp Directory",
      "description": "Create upgrade temporary directory",
      "option": "CreateDir",
      "path": "D:\\MyApp\\temp",
      "waitTime": 0
    },
    {
      "title": "Download Update Package",
      "description": "Download the latest update package from server",
      "option": "Download",
      "url": "https://example.com/upgrade.zip",
      "file": "D:\\MyApp\\temp\\upgrade.zip",
      "waitTime": 2
    },
    {
      "title": "Backup Existing Files",
      "description": "Backup all files in current installation directory",
      "option": "Zip",
      "source": "D:\\MyApp",
      "file": "D:\\MyApp\\backup\\backup_20240101.zip",
      "waitTime": 3
    },
    {
      "title": "Extract Update Package",
      "description": "Extract the update package to installation directory",
      "option": "Unzip",
      "source": "D:\\MyApp\\temp\\upgrade.zip",
      "destination": "D:\\MyApp",
      "overwrite": true,
      "waitTime": 0
    },
    {
      "title": "Copy Config File",
      "description": "Copy additional configuration file",
      "option": "CopyDoc",
      "source": "D:\\MyApp\\temp\\appsettings.json",
      "destination": "D:\\MyApp\\appsettings.json",
      "overwrite": false,
      "waitTime": 0
    },
    {
      "title": "Rename Old File",
      "description": "Rename old version log file",
      "option": "RenameDoc",
      "oldName": "D:\\MyApp\\logs\\app.log",
      "newName": "D:\\MyApp\\logs\\app_old.log",
      "waitTime": 0
    },
    {
      "title": "Execute Install Script",
      "description": "Execute post-installation script",
      "option": "Command",
      "command": "powershell",
      "args": "-ExecutionPolicy Bypass -File install.ps1",
      "retryCount": 2,
      "retryDelay": 2000,
      "waitTime": 5
    },
    {
      "title": "Clean Up Temporary Files",
      "description": "Delete downloaded temporary files",
      "option": "DeleteDoc",
      "file": "D:\\MyApp\\temp\\upgrade.zip",
      "waitTime": 0
    },
    {
      "title": "Delete Temp Directory",
      "description": "Delete upgrade temporary directory",
      "option": "DeleteDir",
      "path": "D:\\MyApp\\temp",
      "waitTime": 0
    },
    {
      "title": "Launch Application",
      "description": "Launch the upgraded application",
      "option": "Command",
      "command": "dotnet MyApp.dll",
      "args": "--environment Production",
      "waitTime": 0
    }
  ]
}
```

## Usage

### Configure Upgrade Settings

1. Edit `upgrade.json` configuration file
2. Set basic configuration items (icon, title, installPath, etc.)
3. Configure `steps` array to define upgrade step sequence as needed
4. Select appropriate `option` operation type for each step and provide corresponding parameters

### Run the Upgrade Program

#### Console Version

```bash
cd Upgrade.Net
dotnet run
```

#### WPF Version

```bash
cd Upgrade.Wpf
dotnet run
```

## Upgrade Flow

### Dynamic Step Execution Flow

The upgrade program executes each step sequentially according to the order defined in the `steps` array:

```
1. Parse configuration file and load steps array
2. Iterate through each step, create corresponding UpgradeAction based on option
3. Execute step's Execute method
4. If waitTime > 0, display countdown prompt
5. If retryCount > 0 and execution fails, perform retry
6. Decide whether to continue with subsequent steps based on continueOnError
7. After all steps are completed, decide whether to launch the application and close the program based on autoStart and autoClose configuration
```

### Step Execution Status

Each step displays the following status during execution:

| Status | Description |
|--------|-------------|
| Pending | Step has not started yet |
| Executing | Step is currently executing |
| Completed | Step executed successfully |
| Failed | Step execution failed |

## Project Structure

```
Upgrade.Net/
├── Upgrade.Net/              # Console Version
│   ├── Config/
│   │   └── UpgradeConfig.cs  # Configuration Management (includes StepConfig, UpgradeOption, UpgradeAction)
│   ├── Dto/
│   │   ├── ScmAppInfo.cs     # Application Information
│   │   └── ScmVerInfo.cs     # Version Information
│   ├── Resources/
│   │   └── logo.ico          # Application Icon
│   ├── Program.cs            # Program Entry
│   ├── Upgrade.cs            # Upgrade Core Logic (dynamic step execution)
│   ├── upgrade.json          # Configuration File
│   └── Upgrade.Net.csproj    # Project File
├── Upgrade.Wpf/              # WPF Version
│   ├── Config/
│   │   └── UpgradeConfig.cs  # Configuration Management
│   ├── Dto/
│   │   ├── ScmAppInfo.cs     # Application Information
│   │   └── ScmVerInfo.cs     # Version Information
│   ├── Dvo/
│   │   ├── MainWindowDvo.cs  # Data Binding Object
│   │   └── StepItemDvo.cs    # Step List Item Data Model
│   ├── Resources/
│   │   └── logo.ico          # Application Icon
│   ├── App.xaml              # Application Entry (Resource Dictionary)
│   ├── MainWindow.xaml       # Main Window
│   ├── UpgradeWindow.xaml    # Upgrade Window
│   ├── UpgradeWindowViewModel.cs # Upgrade Window ViewModel
│   ├── SplashWindow.xaml     # Splash Screen
│   ├── Upgrade.cs            # Upgrade Core Logic (reuse Upgrade.Net)
│   ├── upgrade.json          # Configuration File
│   └── Upgrade.Wpf.csproj    # Project File
├── .gitignore
├── LICENSE
├── README.md
├── README.en.md
└── Upgrade.Net.slnx          # Solution File
```

## Requirements

- .NET 10.0+
- Windows 7 or later
- Console encoding support: GBK/UTF-8

## Contribution

1. Fork the repository
2. Create Feat_xxx branch
3. Commit your code
4. Create Pull Request

## License

This project is licensed under the MIT License.