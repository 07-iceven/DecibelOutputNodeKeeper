# Decibel Output Node Keeper

`Decibel Output Node Keeper` 是一个基于 `WPF` 与 `.NET 8` 的 Windows 桌面工具，用于持续锁定默认麦克风音量。

## 功能简介

- 锁定默认麦克风音量到指定百分比。
- 按固定间隔轮询默认录音设备，并自动拉回目标音量。
- 支持开机自启动，并以托盘模式在后台运行。
- 关闭主窗口时不会直接退出，而是最小化到系统托盘。

## 技术栈

- `.NET 8`
- `WPF`
- `NAudio`
- `WPF-UI`

## 适用场景

- 需要把默认麦克风长期维持在固定音量。
- 需要程序随 Windows 登录自动运行，并常驻后台。

## 运行方式

### 开发环境

要求：

- Windows
- `.NET SDK 8.0` 或更高版本

启动项目：

```bash
dotnet restore
dotnet run --project DecibelOutputNodeKeeper.csproj
```

发布示例：

```bash
dotnet publish DecibelOutputNodeKeeper.csproj -c Release
```

### 使用 Inno Setup 打包

建议先发布为 `win-x64` 自包含版本，再使用 `Inno Setup Compiler` 生成安装包：

```powershell
dotnet publish .\DecibelOutputNodeKeeper.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o .\publish\win-x64
```

发布完成后，使用 `Inno Setup Compiler` 打开并编译：

```text
installer\DecibelOutputNodeKeeper.iss
```

脚本默认约定：

- 发布目录为 `publish\win-x64`
- 安装包输出目录为 `dist\installer`
- 安装位置为 `%LOCALAPPDATA%\Programs\Decibel Output Node Keeper`

如果你想在命令行编译安装包，可执行：

```powershell
iscc .\installer\DecibelOutputNodeKeeper.iss
```

### 托盘行为

- 主窗口关闭时，程序会隐藏到系统托盘，而不是退出。
- 双击托盘图标可重新打开主窗口。

## 配置项

当前界面支持以下配置：

- `启用默认麦克风音量锁定`
- `目标音量`，范围为 `0-100`
- `检查间隔`，程序内部会限制在 `1-60` 秒之间
- `开机自启动`

## 配置文件

程序配置保存在：

```text
%LOCALAPPDATA%\Decibel Output Node Keeper\settings.json
```

配置内容包括：

- 麦克风音量锁定开关
- 目标音量百分比
- 检查间隔
- 自启动状态

## 实现说明

### 麦克风音量锁定

程序使用 `NAudio` 获取当前默认录音设备，并定时检查其音量；如果音量偏离目标值，则自动写回设定值。

### 自启动

启用自启动后，程序会写入当前用户注册表：

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

写入的启动参数会附带 `--minimized`，因此登录后会直接隐藏到托盘运行。

## 项目结构

```text
DecibelOutputNodeKeeper/
|- Models/
|  \- AppSettings.cs
|- Services/
|  |- AudioLockService.cs
|  |- SettingsService.cs
|  \- StartupService.cs
|- App.xaml
|- App.xaml.cs
|- MainWindow.xaml
|- MainWindow.xaml.cs
|- DecibelOutputNodeKeeper.csproj
\- README.md
```