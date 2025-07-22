# STM32CubeMX C++ Converter

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version: v0.1.0](https://img.shields.io/badge/version-v0.1.0-blue.svg)](https://github.com/lc6464/STM32CubeMX-CPPConvertor/releases/tag/v0.1.0)
[![Platforms](https://img.shields.io/badge/platform-win%20%7C%20linux%20%7C%20macos-green.svg)](https://github.com/lc6464/STM32CubeMX-CPPConvertor)

一个简单、高效的命令行工具，旨在**全自动**地将由 **STM32CubeMX** 生成的基于 **CMake** 的 **C** 语言项目，无缝转换为 **C++** 兼容项目。

## 解决了什么问题？

STM32CubeMX 是一个强大的图形化配置工具，但它生成的 CMake 项目默认是纯 C 语言环境。当开发者希望在项目中使用 C++ 的特性时，需要手动修改由 CubeMX 管理的 `cmake/stm32cubemx/CMakeLists.txt` 文件。

更糟糕的是，**每当您在 CubeMX 中重新生成代码时，这些手动修改都会被覆盖**。

本工具通过与 CubeMX 的**用户操作 (User Actions)** 功能深度集成，实现了“一次配置，永久有效”的自动化转换。

## 它是如何工作的？

当被 CubeMX 自动调用时，本工具会执行以下操作：
1. **扫描项目**：验证当前目录是否为一个有效的、由 CubeMX 生成的 CMake 项目。
2. **解析文件**：读取 `cmake/stm32cubemx/CMakeLists.txt` 文件。
3. **智能匹配**：对于文件中的每一行 `.c` 源文件引用（如 `Core/Src/main.c`），它会检查是否存在对应的 `.cpp` 文件（如 `Core/Src/main.cpp`）。
4. **安全更新**：如果找到匹配的 `.cpp` 文件，它会：
    - 将原始的 `.c` 文件重命名为 `.c.bak` 作为备份。
    - 在一个临时文件中生成新的 `CMakeLists.txt` 内容，将 `.c` 路径替换为 `.cpp` 路径。
    - 使用临时文件的内容安全地覆盖原始的 `CMakeLists.txt` 文件。
5. **完成**！您的项目现在已经无缝地链接了 C++ 源文件，无需任何手动干预。

## ✨ 功能特性

- **全自动转换**：通过 CubeMX 的 **User Actions** 集成，在每次生成代码后自动运行。
- **项目验证**：在执行操作前，会检查项目名称、工具链设置 (CMake) 和 CubeMX 版本。
- **安全备份**：自动将原始的 `.c` 文件备份为 `.c.bak`。
- **强大的文件操作**：通过“写入-复制”模式，即使目标文件被其他进程锁定，也能成功更新。
- **跨平台**：
  - 为 Windows x64 提供编译版本。
  - 支持在 Windows/Linux/macOS (x64 & arm64) 上自行编译运行。

## 🚀 使用指南

### 推荐方法: 与 STM32CubeMX 集成 (一次配置，永久有效)

1. **准备项目**: 在 STM32CubeMX 中配置您的 MCU，并在 **Project Manager** -> **Toolchain/IDE** 中选择 `CMake`。
2. **重命名文件**: 在您的文件管理器或 IDE 中，手动将您希望用 C++ 编写的源文件从 `.c` 重命名为 `.cpp`。例如，将 `Core/Src/main.c` 重命名为 `Core/Src/main.cpp`。
3. **下载/编译工具**:
    - **Windows x64**: 从本项目的 [Releases 页面](https://github.com/lc6464/STM32CubeMX-CPPConvertor/releases)下载 `STM32CubeMXCPPConvertor.exe`。
    - **Linux/macOS**: 请参照下文的“从源码编译”指南。
4. **放置工具**: 将可执行文件放置在您 STM32 项目的 **根目录** (与 `.ioc` 文件同级)。
5. **配置 CubeMX**:
    - 在 STM32CubeMX 中，打开 **Project Manager** -> **Code Generator** 标签页。
    - 找到 **User Actions** 面板。
    - 在 **After Code Generation** 输入框中，填入可执行文件的名称：`STM32CubeMXCPPConvertor.exe` *(对于 Linux/macOS 用户，应为 `STM32CubeMXCPPConvertor`)*
6. **生成代码**: 点击 **Generate Code**。现在，每次生成代码后，转换器都会自动运行，更新您的 `CMakeLists.txt`！

### 备用方法: 手动执行

如果您只想进行一次性转换，可以跳过第 5 步，直接在项目根目录的终端中手动运行该工具。

``` bash
# 在 Windows 上
.\STM32CubeMXCPPConvertor.exe

# 在 Linux/macOS 上
./STM32CubeMXCPPConvertor
```

## 🛠️ 从源码编译

对于 Linux, macOS, 或 arm64 用户，可以轻松地从源码编译：
1. 安装 [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)。
2. 克隆本仓库：`git clone https://github.com/lc6464/STM32CubeMX-CPPConvertor.git`
3. 进入项目目录：`cd STM32CubeMX-CPPConvertor`
4. 运行 `publish` 命令。请将 `<RID>` 替换为您的目标平台标识符 (例如 `linux-x64`, `osx-arm64`)：
    ```
	dotnet publish -c Release -r <RID> /p:PublishSingleFile=true
	```
    - Windows x64: `win-x64`
    - Linux x64: `linux-x64`
    - macOS x64: `osx-x64`
    - macOS Arm64 (Apple Silicon): `osx-arm64`
5. 生成的可执行文件位于 `bin/Release/net9.0/<RID>/publish/` 目录下。

## 许可协议

本项目基于 [MIT License](LICENSE) 授权。