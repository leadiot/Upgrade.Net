

# Upgrade.Net

基于 .NET (WPF) 开发的应用程序升级程序，支持断点续传、版本管理、自动更新等功能。

## 项目简介

Upgrade.Net 是一个轻量级的 Windows 应用程序升级解决方案，采用 MVVM 架构设计，支持从远程服务器下载更新包并自动完成版本升级。

## 功能特性

- **断点续传**：支持下载暂停、继续、取消功能
- **版本管理**：支持版本号比较、最小/最大版本限制
- **多版本类型**：支持正式版、Alpha、Beta、强制更新
- **自动执行**：支持升级完成后自动启动主程序
- **进度显示**：实时显示下载进度和状态信息
- **配置文件**：支持 JSON 格式的升级配置文件

## 软件架构

- **UI 层**：WPF (XAML)
- **架构模式**：MVVM
- **网络请求**：HttpClient
- **数据交换**：JSON

### 核心模块

| 模块 | 说明 |
|------|------|
| MainWindow | 主窗口，处理下载、解压、重启逻辑 |
| SplashWindow | 升级过程提示窗口 |
| UpgradeConfig | 升级配置管理类 |
| ScmAppInfo | 应用程序信息 DTO |
| ScmVerInfo | 版本信息 DTO |
| MainWindowDvo | 主窗口数据绑定对象 |

## 配置文件说明

### upgrade.json

```json
{
  "Title": "应用名称",
  "InstallPath": "安装路径",
  "AutoStart": true,
  "AutoClose": true,
  "ExecuteFile": "主程序文件名",
  "ExecuteArgs": "启动参数",
  "AppInfo": {
    "types": 0,
    "code": "app_code",
    "name": "应用名称",
    "content": "应用描述"
  },
  "VerInfo": {
    "ver": "1.0.0",
    "date": "2024-01-01",
    "build": "100",
    "ver_min": "1.0.0",
    "ver_max": "",
    "alpha": false,
    "beta": false,
    "forced": false,
    "current": false,
    "url": "升级包下载地址",
    "remark": "更新说明"
  }
}
```

## 使用说明

### 配置升级信息

1. 编辑 `upgrade.json` 配置文件
2. 设置 `InstallPath` 为应用程序安装目录
3. 配置 `VerInfo` 中的版本信息和下载链接
4. 根据需要设置 `AutoStart`、`Forced` 等选项

### 运行升级程序

1. 启动 Upgrade.Net
2. 点击"开始升级"按钮
3. 等待下载和解压完成
4. 程序自动重启主应用程序

### 控制按钮

- **开始**：开始下载升级包
- **暂停**：暂停当前下载（支持断点续传）
- **取消**：取消下载并退出

## 技术要求

- .NET Framework 4.5+ 或 .NET 6+
- Windows 7 及以上操作系统

## 项目结构

```
Upgrade.Net/
├── Config/
│   └── UpgradeConfig.cs      # 配置管理
├── Dto/
│   ├── ScmAppInfo.cs         # 应用信息
│   └── ScmVerInfo.cs         # 版本信息
├── Dvo/
│   └── MainWindowDvo.cs      # 数据绑定对象
├── Resources/
│   └── logo.ico              # 程序图标
├── App.xaml                  # 应用程序入口
├── MainWindow.xaml           # 主窗口
├── SplashWindow.xaml         # 启动窗口
└── Upgrade.Net.csproj        # 项目文件
```

## 参与贡献

1. Fork 本仓库
2. 新建 Feat_xxx 分支
3. 提交代码
4. 新建 Pull Request

## 许可证

本项目遵循 MIT 许可证。