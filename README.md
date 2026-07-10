# Upgrade.Net

基于 .NET 开发的应用程序升级程序，支持控制台和 WPF 两种版本，提供完整的下载、备份、解压、启动流程。

## 项目简介

Upgrade.Net 是一个轻量级的 Windows 应用程序升级解决方案，支持从远程服务器下载更新包并自动完成版本升级。项目包含两个版本：

- **Upgrade.Net**：控制台版本，适合后台静默升级场景
- **Upgrade.Wpf**：WPF 界面版本，提供可视化升级进度和用户交互

## 功能特性

- **多安装模式**：支持从本地 ZIP 文件、远程 URL 下载或自动模式
- **文件备份**：支持升级前自动备份现有文件，可配置备份路径
- **忽略文件**：支持指定忽略文件列表，升级时不覆盖这些文件
- **离线模式**：支持复制离线展示文件并设置倒计时
- **自动启动**：支持升级完成后自动启动主程序
- **进度显示**：实时显示下载、备份、解压进度和状态信息
- **暂停/取消**：WPF 版本支持下载暂停、继续、取消功能
- **配置文件**：支持 JSON 格式的升级配置文件

## 软件架构

### 控制台版本 (Upgrade.Net)

- **目标框架**：.NET 10.0
- **输出类型**：控制台应用程序
- **网络请求**：HttpClient
- **数据交换**：System.Text.Json

### WPF 版本 (Upgrade.Wpf)

- **目标框架**：.NET 10.0-windows
- **输出类型**：WPF 桌面应用程序
- **架构模式**：MVVM
- **网络请求**：HttpClient
- **数据交换**：System.Text.Json

### 核心模块

| 模块 | 说明 |
|------|------|
| Upgrade | 升级核心逻辑类（控制台版本） |
| MainWindow | 主窗口，处理下载、解压、重启逻辑（WPF版本） |
| SplashWindow | 启动窗口（WPF版本） |
| UpgradeConfig | 升级配置管理类 |
| LaunchConfig | 启动配置 |
| BackupConfig | 备份配置 |
| OfflineConfig | 离线配置 |
| ScmAppInfo | 应用信息 DTO |
| ScmVerInfo | 版本信息 DTO |
| MainWindowDvo | 主窗口数据绑定对象（WPF版本） |

## 配置文件说明

### upgrade.json

```json
{
  "title": "应用更新",
  "installPath": "D:\\app",
  "installType": "Auto",
  "installFile": "D:\\local\\update.zip",
  "autoClose": true,
  "ignoreFiles": ["log", "temp", "database"],
  "launch": {
    "file": "app.exe",
    "args": "--start"
  },
  "backup": {
    "path": "D:\\backup"
  },
  "offline": {
    "file": "D:\\offline\\offline.html",
    "time": 10
  },
  "appInfo": {
    "types": 0,
    "code": "app_code",
    "name": "应用名称",
    "content": "应用描述"
  },
  "verInfo": {
    "ver_info": "1.0.1",
    "ver_date": "2026-07-09",
    "ver_code": "101",
    "ver_min": "1.0.0",
    "ver_max": "",
    "major": 1,
    "minor": 0,
    "patch": 1,
    "build": 101,
    "phase": 5,
    "forced": false,
    "current": false,
    "url": "http://example.com/update.zip",
    "size": 0,
    "remark": "更新说明"
  }
}
```

### 配置字段说明

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| title | string | 否 | 升级程序展示的标题 |
| installPath | string | 是 | 待升级应用的安装路径 |
| installType | string | 否 | 安装模式：Auto/FromZip/FromUrl，默认Auto |
| installFile | string | 否 | 本地安装文件路径（FromZip模式必填） |
| autoClose | bool | 否 | 升级完成后是否关闭更新程序，默认true |
| ignoreFiles | array | 否 | 忽略文件列表，升级时不覆盖这些文件 |
| launch.file | string | 否 | 启动文件路径 |
| launch.args | string | 否 | 启动参数 |
| backup.path | string | 否 | 备份文件路径 |
| offline.file | string | 否 | 离线展示文件路径 |
| offline.time | int | 否 | 离线文件倒计时（秒） |
| verInfo.url | string | 是 | 升级文件下载地址 |
| verInfo.remark | string | 否 | 版本升级说明 |

### InstallType 说明

| 值 | 说明 |
|----|------|
| Auto | 自动模式，优先使用本地文件，不存在则从URL下载 |
| FromZip | 仅使用本地 ZIP 文件安装 |
| FromUrl | 仅从远程 URL 下载安装 |

## 使用说明

### 配置升级信息

1. 编辑 `upgrade.json` 配置文件
2. 设置 `installPath` 为应用程序安装目录
3. 配置 `verInfo` 中的版本信息和下载链接
4. 根据需要设置 `launch`、`backup`、`offline` 等选项

### 运行升级程序

#### 控制台版本

```bash
cd Upgrade.Net
dotnet run
```

#### WPF 版本

```bash
cd Upgrade.Wpf
dotnet run
```

### 控制按钮（WPF版本）

- **开始**：开始升级流程
- **暂停**：暂停当前下载
- **取消**：取消升级并退出

## 升级流程

```
1. 读取配置文件 (upgrade.json)
2. 验证配置信息
3. 准备安装目录
4. 获取安装文件（本地文件或远程下载）
5. 复制离线文件（可选）
6. 备份现有文件（可选）
7. 解压文件到安装目录
8. 删除离线文件（可选）
9. 清理临时文件
10. 启动应用程序
```

## 项目结构

```
Upgrade.Net/
├── Upgrade.Net/              # 控制台版本
│   ├── Config/
│   │   └── UpgradeConfig.cs  # 配置管理
│   ├── Dto/
│   │   ├── ScmAppInfo.cs     # 应用信息
│   │   └── ScmVerInfo.cs     # 版本信息
│   ├── Resources/
│   │   └── logo.ico          # 程序图标
│   ├── Program.cs            # 程序入口
│   ├── Upgrade.cs            # 升级核心逻辑
│   ├── upgrade.json          # 配置文件
│   └── Upgrade.Net.csproj    # 项目文件
├── Upgrade.Wpf/              # WPF 版本
│   ├── Config/
│   │   └── UpgradeConfig.cs  # 配置管理
│   ├── Dto/
│   │   ├── ScmAppInfo.cs     # 应用信息
│   │   └── ScmVerInfo.cs     # 版本信息
│   ├── Dvo/
│   │   └── MainWindowDvo.cs  # 数据绑定对象
│   ├── Resources/
│   │   └── logo.ico          # 程序图标
│   ├── App.xaml              # 应用程序入口
│   ├── MainWindow.xaml       # 主窗口
│   ├── SplashWindow.xaml     # 启动窗口
│   ├── upgrade.json          # 配置文件
│   └── Upgrade.Wpf.csproj    # 项目文件
├── .gitignore
├── LICENSE
├── README.md
└── Upgrade.Net.slnx          # 解决方案文件
```

## 技术要求

- .NET 10.0+
- Windows 7 及以上操作系统

## 参与贡献

1. Fork 本仓库
2. 新建 Feat_xxx 分支
3. 提交代码
4. 新建 Pull Request

## 许可证

本项目遵循 MIT 许可证。