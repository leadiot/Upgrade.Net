# Upgrade.Net 详细使用说明

## 一、项目概述

Upgrade.Net 是一个基于 .NET 开发的应用程序升级解决方案，采用**策略模式**和**配置驱动**的设计思想，支持通过 JSON 配置文件定义动态升级步骤序列。项目包含核心类库、控制台版本和 WPF 版本，适用于各种升级场景。

### 1.1 项目架构

```
┌─────────────────────────────────────────────────────────────┐
│                      Upgrade.Net (核心类库)                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │ UpgradeConfig│  │ StepConfig  │  │ UpgradeOption (16种)│ │
│  └──────┬──────┘  └──────┬──────┘  └──────────┬──────────┘ │
│         │                │                     │            │
│         └────────────────┼─────────────────────┘            │
│                          ▼                                  │
│              ┌──────────────────────┐                       │
│              │      Upgrade         │  动态步骤执行引擎      │
│              │  (策略模式 + 事件驱动)│                       │
│              └──────────┬───────────┘                       │
│                         │                                   │
│                         ▼                                   │
│              ┌──────────────────────┐                       │
│              │    UpgradeView       │  视图接口（解耦）     │
│              └──────────┬───────────┘                       │
└─────────────────────────┼───────────────────────────────────┘
                          │
           ┌──────────────┼──────────────┐
           ▼              ▼              ▼
┌────────────────┐ ┌────────────────┐ ┌────────────────┐
│ Upgrade.Cmd    │ │ Upgrade.Wpf    │ │ 用户自定义项目  │
│ 控制台版本      │ │   WPF版本      │ │   (Test示例)   │
└────────────────┘ └────────────────┘ └────────────────┘
```

### 1.2 核心特性

| 特性 | 说明 |
|------|------|
| **配置驱动** | 通过 JSON 文件定义升级步骤，无需修改代码 |
| **策略模式** | 16种操作类型封装为独立的 UpgradeAction |
| **事件驱动** | 通过 UpgradeView 接口实现视图解耦 |
| **重试机制** | 支持配置重试次数和延迟 |
| **等待时间** | 每个步骤可配置等待时间，支持倒计时 |
| **错误处理** | 支持 ContinueOnError 配置 |
| **进度反馈** | 实时进度和状态更新 |

---

## 二、快速开始

### 2.1 方式一：使用预编译版本

1. 下载发布版本
2. 创建 `upgrade.json` 配置文件
3. 运行升级程序

### 2.2 方式二：集成到现有项目

```bash
# 安装 NuGet 包
dotnet add package Com.Scm.Upgrade --version 1.0.1
```

### 2.3 方式三：源码引用

```xml
<!-- 在 csproj 中添加项目引用 -->
<ProjectReference Include="..\Upgrade.Net\Upgrade.Net.csproj" />
```

---

## 三、核心概念

### 3.1 UpgradeConfig（升级配置）

[UpgradeConfig.cs](file:///d:/workspace/Git/Upgrade.Net/Upgrade.Net/Config/UpgradeConfig.cs) 定义了升级程序的整体配置：

```csharp
public class UpgradeConfig
{
    public const string CONFIG_FILE = "upgrade.json";
    
    // 基础配置
    public string Icon { get; set; }
    public string Title { get; set; }
    public bool AutoStart { get; set; }
    public bool AutoClose { get; set; }
    public bool ShowSteps { get; set; }
    
    // 版本信息
    public string OldVersion { get; set; }
    public string NewVersion { get; set; }
    public string AppInfo { get; set; }
    public string VerInfo { get; set; }
    
    // 升级步骤（核心）
    public List<StepConfig> Steps { get; set; }
    
    // 方法
    public void LoadDefault();
    public static UpgradeConfig Load();
    public void Save(string baseDir);
}
```

### 3.2 StepConfig（步骤配置）

[StepConfig.cs](Upgrade.Net/Config/StepConfig.cs) 定义了每个升级步骤的详细配置：

```csharp
public class StepConfig
{
    // 公共属性
    public string Title { get; set; }           // 步骤标题
    public string Description { get; set; }     // 步骤描述
    public UpgradeOption Option { get; set; }   // 操作类型
    public int WaitTime { get; set; }          // 等待时间（秒）
    public bool ContinueOnError { get; set; }   // 错误时是否继续
    public int RetryCount { get; set; }        // 重试次数
    public int RetryDelay { get; set; }        // 重试延迟（毫秒）
    
    // 扩展属性（根据操作类型使用）
    public string Source { get; set; }         // 源路径
    public string Destination { get; set; }    // 目标路径
    public string File { get; set; }           // 文件名
    public string Path { get; set; }           // 目录路径
    public string Url { get; set; }            // 下载URL
    public string Command { get; set; }        // 命令
    public string Args { get; set; }           // 参数
    public string OldName { get; set; }        // 原名称
    public string NewName { get; set; }        // 新名称
    public bool Overwrite { get; set; }        // 是否覆盖
}
```

#### 静态工厂方法

StepConfig 提供了便捷的静态工厂方法创建步骤：

| 方法 | 操作类型 | 说明 |
|------|----------|------|
| `NewDownloadStep(title, url, file)` | Download | 创建下载步骤 |
| `NewUploadStep(title, url, file)` | Upload | 创建上传步骤 |
| `NewCommandStep(title, command, args, path)` | Command | 创建命令执行步骤（等待完成） |
| `NewLaunchStep(title, command, args, path)` | Launch | 创建启动程序步骤（不等待） |
| `NewZipStep(title, source, destination)` | Zip | 创建压缩步骤 |
| `NewUnzipStep(title, source, destination, overwrite)` | Unzip | 创建解压步骤 |
| `NewMoveDirStep(title, source, destination, overwrite)` | MoveDir | 创建移动目录步骤 |
| `NewMoveDocStep(title, source, destination, overwrite)` | MoveDoc | 创建移动文件步骤 |
| `NewCopyDirStep(title, source, destination, overwrite)` | CopyDir | 创建复制目录步骤 |
| `NewCopyDocStep(title, source, destination, overwrite)` | CopyDoc | 创建复制文件步骤 |
| `NewCreateDirStep(title, path)` | CreateDir | 创建创建目录步骤 |
| `NewCreateDocStep(title, path, overwrite)` | CreateDoc | 创建创建文件步骤 |
| `NewDeleteDirStep(title, path)` | DeleteDir | 创建删除目录步骤 |
| `NewDeleteDocStep(title, path)` | DeleteDoc | 创建删除文件步骤 |
| `NewRenameDirStep(title, oldName, newName, overwrite)` | RenameDir | 创建重命名目录步骤 |
| `NewRenameDocStep(title, oldName, newName, overwrite)` | RenameDoc | 创建重命名文件步骤 |
| `NewCommandOnceStep(title, command, args, path)` | CommandOnce | 创建命令执行步骤（不等待完成） |

### 3.3 UpgradeOption（操作类型枚举）

[UpgradeOption.cs](Upgrade.Net/Config/UpgradeOption.cs) 定义了17种操作类型：

```csharp
public enum UpgradeOption
{
    None,        // 无操作
    Download,    // 下载文件
    Command,     // 执行命令（等待完成）
    Launch,      // 启动程序（不等待）
    Zip,         // 压缩
    Unzip,       // 解压
    MoveDir,     // 移动目录
    MoveDoc,     // 移动文件
    CopyDir,     // 复制目录
    CopyDoc,     // 复制文件
    CreateDir,   // 创建目录
    CreateDoc,   // 创建文件
    DeleteDir,   // 删除目录
    DeleteDoc,   // 删除文件
    RenameDir,   // 重命名目录
    RenameDoc    // 重命名文件
}
```

### 3.4 UpgradeView（视图接口）

[UpgradeView.cs](Upgrade.Net/UpgradeView.cs) 定义了视图层需要实现的方法：

```csharp
public interface UpgradeView
{
    void Log(string message);                           // 普通消息
    void LogNewLine();                                  // 空行
    void LogStep(int step, int count, string message);  // 步骤概要
    void LogStepInfo(string info, string message);      // 步骤提示
    void LogStepWait(int time, string message);         // 等待时间变化
    void LogStepProgress(int progress, string message); // 进度变化
    void LogStepStatus(int stepNumber, StepStatus status, string title, string message); // 状态变化
    void ResetProgress();                               // 重置进度
}
```

---

## 四、配置文件详解

### 4.1 upgrade.json 完整示例

```json
{
  "icon": "logo.ico",
  "title": "应用升级程序",
  "oldVersion": "1.0.0",
  "newVersion": "2.0.0",
  "autoStart": true,
  "autoClose": false,
  "showSteps": true,
  "appInfo": "基于 .NET 开发的应用程序升级工具",
  "verInfo": "版本 2.0.0 更新说明：\n1. 新增自定义步骤功能\n2. 支持16种操作类型",
  "steps": [
    {
      "title": "创建临时目录",
      "description": "创建升级临时目录",
      "option": "CreateDir",
      "path": "D:\\MyApp\\temp"
    },
    {
      "title": "下载更新包",
      "option": "Download",
      "url": "https://example.com/upgrade.zip",
      "file": "D:\\MyApp\\temp\\upgrade.zip",
      "waitTime": 2
    },
    {
      "title": "备份现有文件",
      "option": "Zip",
      "source": "D:\\MyApp",
      "destination": "D:\\MyApp\\backup\\backup.zip"
    },
    {
      "title": "解压更新包",
      "option": "Unzip",
      "source": "D:\\MyApp\\temp\\upgrade.zip",
      "destination": "D:\\MyApp",
      "overwrite": true
    },
    {
      "title": "清理临时文件",
      "option": "DeleteDoc",
      "path": "D:\\MyApp\\temp\\upgrade.zip"
    },
    {
      "title": "删除临时目录",
      "option": "DeleteDir",
      "path": "D:\\MyApp\\temp"
    },
    {
      "title": "启动应用程序",
      "option": "Launch",
      "command": "dotnet",
      "args": "MyApp.dll --environment Production",
      "path": "D:\\MyApp"
    }
  ]
}
```

### 4.2 配置字段说明

#### 基础配置

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| icon | string | 否 | 无 | 应用图标路径 |
| title | string | 否 | "Upgrade.Wpf更新" | 升级程序标题 |
| autoStart | bool | 否 | false | 升级完成后自动启动应用 |
| autoClose | bool | 否 | false | 升级完成后自动关闭程序 |
| showSteps | bool | 否 | false | 是否显示步骤列表 |
| appInfo | string | 否 | null | 应用描述信息 |
| verInfo | string | 否 | null | 版本升级说明 |
| oldVersion | string | 否 | null | 当前版本号 |
| newVersion | string | 否 | null | 目标版本号 |

#### 步骤配置

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| title | string | 是 | - | 步骤标题 |
| description | string | 否 | - | 步骤描述 |
| option | string | 是 | - | 操作类型（UpgradeOption 枚举值） |
| waitTime | int | 否 | 0 | 完成后等待时间（秒） |
| continueOnError | bool | 否 | false | 失败时是否继续 |
| retryCount | int | 否 | 0 | 重试次数 |
| retryDelay | int | 否 | 1000 | 重试延迟（毫秒） |

#### 操作参数

| 参数 | 适用操作 | 说明 |
|------|----------|------|
| url | Download | 下载链接 |
| file | Download | 保存路径和文件名 |
| source | Zip, Unzip, MoveDir, MoveDoc, CopyDir, CopyDoc | 源路径 |
| destination | Zip, Unzip, MoveDir, MoveDoc, CopyDir, CopyDoc | 目标路径 |
| path | CreateDir, CreateDoc, DeleteDir, DeleteDoc | 目录/文件路径 |
| command | Command, Launch, CommandOnce | 命令/程序路径 |
| args | Command, Launch, CommandOnce | 命令参数 |
| oldName | RenameDir, RenameDoc | 原名称 |
| newName | RenameDir, RenameDoc | 新名称 |
| overwrite | Unzip, MoveDir, MoveDoc, CopyDir, CopyDoc, CreateDoc, RenameDir, RenameDoc | 是否覆盖 |

#### 操作类型说明

| 操作类型 | 说明 | 所需参数 |
|----------|------|----------|
| None | 无操作 | 无 |
| Download | 从URL下载文件 | url, file |
| Command | 执行命令行命令（等待执行完成） | command, args(可选), path(可选) |
| Launch | 启动外部程序（不等待执行完成） | command, args(可选), path(可选) |
| Zip | 压缩文件/目录 | source, destination |
| Unzip | 解压文件 | source, destination, overwrite(可选) |
| MoveDir | 移动目录 | source, destination, overwrite(可选) |
| MoveDoc | 移动文件 | source, destination, overwrite(可选) |
| CopyDir | 复制目录 | source, destination, overwrite(可选) |
| CopyDoc | 复制文件 | source, destination, overwrite(可选) |
| CreateDir | 创建目录 | path |
| CreateDoc | 创建文件 | path, overwrite(可选) |
| DeleteDir | 删除目录 | path |
| DeleteDoc | 删除文件 | path |
| RenameDir | 重命名目录 | oldName, newName, overwrite(可选) |
| RenameDoc | 重命名文件 | oldName, newName, overwrite(可选) |

---

## 五、代码集成示例

### 5.1 在控制台应用中使用

```csharp
using Com.Scm.Upgrade;
using Com.Scm.Upgrade.Config;

class Program
{
    static void Main(string[] args)
    {
        // 方式一：从配置文件加载
        var config = UpgradeConfig.Load();
        if (config == null)
        {
            Console.WriteLine("配置文件不存在");
            return;
        }
        
        // 创建升级实例，传入视图实现
        var upgrade = new Upgrade(new ConsoleView());
        
        // 执行升级（同步方式）
        upgrade.Start(config);
        
        Console.WriteLine("升级完成");
    }
}

// 实现 UpgradeView 接口
class ConsoleView : UpgradeView
{
    public void Log(string message) => Console.WriteLine(message);
    public void LogNewLine() => Console.WriteLine();
    public void LogStep(int step, int count, string message) 
        => Console.WriteLine($"[{step}/{count}] {message}");
    public void LogStepInfo(string info, string message) 
        => Console.WriteLine($"  [{info}] {message}");
    public void LogStepWait(int time, string message) 
        => Console.WriteLine($"  [等待] {message}");
    public void LogStepProgress(int progress, string message) 
        => Console.WriteLine($"  [进度] {progress}% - {message}");
    public void LogStepStatus(int stepNumber, StepStatus status, string title, string message)
    {
        var statusText = status switch
        {
            StepStatus.Pending => "等待",
            StepStatus.Running => "执行中",
            StepStatus.Success => "完成",
            StepStatus.Failed => "失败",
            StepStatus.Skipped => "跳过",
            _ => "未知"
        };
        Console.WriteLine($"  [状态] {statusText}: {message}");
    }
    public void ResetProgress() { }
}
```

### 5.2 在 WPF 应用中使用

```csharp
using Com.Scm.Upgrade;
using Com.Scm.Upgrade.Config;
using System.Windows;

public partial class MainWindow : Window, UpgradeView
{
    private Com.Scm.Upgrade.Upgrade _upgrade;
    
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private async void StartUpgrade_Click(object sender, RoutedEventArgs e)
    {
        // 方式二：代码创建配置
        var config = new UpgradeConfig
        {
            Title = "应用升级",
            OldVersion = "1.0.0",
            NewVersion = "2.0.0",
            Steps = new List<StepConfig>
            {
                StepConfig.NewDownloadStep("下载更新包", "https://example.com/upgrade.zip", "upgrade.zip"),
                StepConfig.NewUnzipStep("解压更新包", "upgrade.zip", "./", true),
                StepConfig.NewLaunchStep("启动应用", "MyApp.exe", "")
            }
        };
        
        _upgrade = new Com.Scm.Upgrade.Upgrade(this);
        
        // 异步执行升级
        await _upgrade.StartAsync(config);
        
        MessageBox.Show("升级完成");
    }
    
    // 实现 UpgradeView 接口
    public void Log(string message)
    {
        Dispatcher.Invoke(() => LogTextBox.AppendText(message + Environment.NewLine));
    }
    
    public void LogNewLine() => Log("");
    
    public void LogStep(int step, int count, string message)
        => Log($"[{step}/{count}] {message}");
    
    public void LogStepInfo(string info, string message)
        => Log($"  [{info}] {message}");
    
    public void LogStepWait(int time, string message)
        => Log($"  [等待] {message}");
    
    public void LogStepProgress(int progress, string message)
        => Dispatcher.Invoke(() => ProgressBar.Value = progress);
    
    public void LogStepStatus(int stepNumber, StepStatus status, string title, string message)
    {
        var statusText = status switch
        {
            StepStatus.Pending => "等待",
            StepStatus.Running => "执行中",
            StepStatus.Success => "完成",
            StepStatus.Failed => "失败",
            StepStatus.Skipped => "跳过",
            _ => "未知"
        };
        Log($"  [状态] {statusText}: {message}");
    }
    
    public void ResetProgress()
        => Dispatcher.Invoke(() => ProgressBar.Value = 0);
}
```

### 5.3 使用静态工厂方法构建步骤

```csharp
var config = new UpgradeConfig
{
    Title = "完整升级流程",
    Steps = new List<StepConfig>
    {
        // 创建临时目录
        StepConfig.NewCreateDirStep("创建临时目录", "D:\\MyApp\\temp"),
        
        // 下载更新包（带等待时间）
        StepConfig.NewDownloadStep("下载更新包", "https://example.com/upgrade.zip", "D:\\MyApp\\temp\\upgrade.zip"),
        
        // 备份现有文件
        StepConfig.NewZipStep("备份现有文件", "D:\\MyApp", "D:\\MyApp\\backup.zip"),
        
        // 解压更新包（不覆盖配置文件）
        StepConfig.NewUnzipStep("解压更新包", "D:\\MyApp\\temp\\upgrade.zip", "D:\\MyApp", true),
        
        // 执行安装脚本（带重试机制）
        StepConfig.NewCommandStep("执行安装脚本", "powershell", "-ExecutionPolicy Bypass -File install.ps1")
        {
            RetryCount = 2,
            RetryDelay = 2000,
            ContinueOnError = true
        },
        
        // 启动应用程序
        StepConfig.NewLaunchStep("启动应用程序", "dotnet", "MyApp.dll", "D:\\MyApp")
    }
};
```

---

## 六、升级流程详解

### 6.1 执行流程

```
┌─────────────────────────────────────────────────────────────────────┐
│                        StartAsync(config)                          │
│                              │                                      │
│                              ▼                                      │
│                    ┌─────────────────┐                              │
│                    │ 检查 Steps 是否 │                              │
│                    │    为空         │                              │
│                    └────────┬────────┘                              │
│                             │                                       │
│                   为空 ─────┴───── 不为空                            │
│                     │                  │                           │
│                     ▼                  ▼                           │
│              [警告：无步骤]    ┌─────────────────┐                   │
│                               │ ExecuteStepsAsync│                  │
│                               └────────┬────────┘                   │
│                                        │                            │
│                                        ▼                            │
│                    ┌────────────────────────────────┐               │
│                    │ 遍历 Steps 数组，逐个执行步骤   │               │
│                    └────────────────┬───────────────┘               │
│                                     │                               │
│          ┌──────────────────────────┼──────────────────────────┐    │
│          │                          │                          │    │
│          ▼                          ▼                          ▼    │
│   ┌──────────────┐          ┌──────────────┐          ┌────────────┐│
│   │ GetAction    │          │ LogStepStatus│          │ ExecuteStep │
│   │ (获取操作)   │          │ (Running)    │          │ WithRetry   ││
│   └──────┬───────┘          └──────────────┘          └──────┬──────┘│
│          │                                                  │       │
│          ▼                                                  ▼       │
│   ┌──────────────┐                              ┌──────────────────┐│
│   │ 未知操作？   │                              │ 执行成功？        ││
│   └──────┬───────┘                              └────────┬─────────┘│
│   是 ────┴─────── 否                                       │        │
│     │                  │                         是 ───────┴───── 否 │
│     ▼                  ▼                           │               ││
│ [跳过]          [记录步骤信息]              ┌──────────────┐    ┌────┴──┐│
│                                           │ waitTime>0?  │    │失败处理││
│                                           └──────┬───────┘    └───┬────┘│
│                                           是 ────┴─────── 否      │     │
│                                             │                  │     │   │
│                                             ▼                  │     │   │
│                                    ┌──────────────────┐         │     │   │
│                                    │ 倒计时显示        │         │     │   │
│                                    │ await Task.Delay │         │     │   │
│                                    └────────┬─────────┘         │     │   │
│                                             │                  │     │   │
│                                             ▼                  ▼     ▼   │
│                                    ┌──────────────────────────────────────┐│
│                                    │         LogStepStatus(Success)       ││
│                                    └──────────────────────────────────────┘│
│                                                                          │
│                                        ▼                                 │
│                    ┌────────────────────────────────┐                 │
│                    │ 所有步骤执行完成                │                 │
│                    └────────────────────────────────┘                 │
└───────────────────────────────────────────────────────────────────────┘
```

### 6.2 步骤执行状态

| 状态 | 触发时机 | 说明 |
|------|----------|------|
| **Pending** | 步骤初始化时 | 等待执行 |
| **Running** | 步骤开始执行时 | 正在执行 |
| **Success** | 步骤执行成功后 | 执行完成 |
| **Failed** | 步骤执行失败后 | 执行失败 |
| **Skipped** | 操作类型未知时 | 跳过此步骤 |

### 6.3 错误处理机制

```
步骤执行失败
     │
     ▼
┌──────────────┐
│ retryCount > 0 ?
└──────┬───────┘
 是 ───┴─────── 否
    │              │
    ▼              ▼
┌─────────┐  ┌──────────────┐
│ 重试执行 │  │ continueOnError ?
│ 等待重试 │  └──────┬───────┘
│ 延迟时间 │    是 ──┴─────── 否
└────┬────┘     │              │
     │          ▼              ▼
     │    [继续执行]     [抛出异常]
     │    [后续步骤]     [终止流程]
     ▼
重试次数耗尽？
     │
     ▼
┌──────────────┐
│ continueOnError ?
└──────┬───────┘
 是 ───┴─────── 否
    │              │
    ▼              ▼
[继续执行]     [抛出异常]
[后续步骤]     [终止流程]
```

---

## 七、高级特性

### 7.1 重试机制

```json
{
  "title": "执行数据库迁移",
  "option": "Command",
  "command": "dotnet",
  "args": "ef database update",
  "retryCount": 3,
  "retryDelay": 3000,
  "continueOnError": true
}
```

### 7.2 等待时间和倒计时

```json
{
  "title": "重启服务",
  "option": "Command",
  "command": "net",
  "args": "restart MyService",
  "waitTime": 10
}
```

### 7.3 错误继续执行

```json
{
  "title": "清理缓存",
  "option": "DeleteDir",
  "path": "D:\\MyApp\\cache",
  "continueOnError": true
}
```

### 7.4 不覆盖文件

```json
{
  "title": "解压更新包",
  "option": "Unzip",
  "source": "upgrade.zip",
  "destination": "D:\\MyApp",
  "overwrite": false
}
```

---

## 八、项目结构

```
Upgrade.Net/
├── Upgrade.Net/                    # 核心类库
│   ├── Config/
│   │   ├── UpgradeConfig.cs       # 升级配置管理
│   │   ├── StepConfig.cs          # 步骤配置及工厂方法
│   │   ├── UpgradeOption.cs       # 操作类型枚举（16种）
│   │   ├── UpgradeAction.cs       # 升级操作类（策略模式）
│   │   ├── UpgradeResult.cs       # 操作执行结果
│   │   └── StepStatus.cs          # 步骤状态枚举
│   ├── Upgrade.cs                 # 升级核心逻辑
│   ├── UpgradeView.cs             # 视图接口
│   └── Upgrade.Net.csproj
├── Upgrade.Cmd/                   # 控制台版本
│   ├── UpgradeCommand.cs          # 控制台视图实现
│   ├── Program.cs
│   ├── Upgrade.json
│   └── Upgrade.Cmd.csproj
├── Upgrade.Wpf/                   # WPF 版本
│   ├── Dvo/
│   │   ├── StepItemDvo.cs         # 步骤列表项模型
│   │   └── RelayCommand.cs        # 命令绑定
│   ├── UpgradeWindow.xaml         # 升级窗口
│   ├── UpgradeWindowViewModel.cs  # WPF视图模型实现
│   ├── Upgrade.json
│   └── Upgrade.Wpf.csproj
├── Test/                          # 使用示例
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs         # 完整示例代码
│   └── Test.csproj
├── DOCS/                          # 文档
│   └── 使用说明.md                 # 本文档
├── screenshots/                   # 界面截图
├── README.md
└── Upgrade.Net.slnx
```

---

## 九、常见问题

### 9.1 如何在升级完成后启动应用？

使用 `Launch` 操作类型，并配置 `command` 和 `args` 参数：

```json
{
  "option": "Launch",
  "command": "dotnet",
  "args": "MyApp.dll",
  "path": "D:\\MyApp"
}
```

### 9.2 如何备份现有文件？

使用 `Zip` 操作类型：

```json
{
  "option": "Zip",
  "source": "D:\\MyApp",
  "destination": "D:\\MyApp\\backup\\backup.zip"
}
```

### 9.3 如何处理大文件下载？

Upgrade.Net 使用 `HttpClient` 进行下载，已设置 30 分钟超时：

```csharp
private static readonly HttpClient _HttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
```

### 9.4 如何跳过某些文件的覆盖？

在 `UpgradeConfig` 中配置 `ignoreFiles` 列表，或在 `Unzip` 步骤中设置 `overwrite: false`。

### 9.5 如何自定义视图？

实现 `UpgradeView` 接口，在各个方法中处理 UI 更新逻辑。

---

## 十、版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0.0 | 2026-07-15 | 初始版本，支持16种操作类型 |
| 1.0.1 | 2026-07-15 | 新增 CommandOnce 操作，完善文档 |

---

## 十一、许可证

本项目遵循 MIT 许可证，详见 [LICENSE](../LICENSE)。
