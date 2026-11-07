# Teach-Game-Programming-Patterns

> 用真实可运行的 C# 项目，把《游戏编程模式》讲明白。

## 引言

本专栏 / 项目旨在讲解《Game Programming Patterns》（中文译名：**《游戏编程模式》**）一书的内容。

《Game Programming Patterns》由 Robert Nystrom 编写，是基于 Erich Gamma、Richard Helm、Ralph Johnson 和 John Vlissides（合称 **GoF**）合著的经典《设计模式：可复用面向对象软件的基础》在游戏领域的版本。书中涵盖**命令模式**、**组件模式**、**对象池**、**游戏循环**等核心模式，是游戏程序员的重要参考书。

## 原书资源

- 中文网页版：[https://gpp.tkchu.me/](https://gpp.tkchu.me/)
- 英文网页版：[https://gameprogrammingpatterns.com/](https://gameprogrammingpatterns.com/)

## 为什么做这个专栏

英文原版中有大量生僻词汇，母语非英语的读者读起来相当吃力；而中文译本在部分地方翻译欠佳，也不利于理解。

更关键的是，原书几乎没有真实的完整实例代码，只有少量 C++ 片段。对于希望依据真实代码理解设计模式的读者来说，这样过于抽象。

因此，本专栏结合一个个**可本地运行的真实项目**来讲解游戏编程模式，而不停留在概念层面。

## 如何配合使用

本专栏请配合 GitHub 仓库使用：

[https://github.com/gupengzu/Teach-Game-Programming-Patterns](https://github.com/gupengzu/Teach-Game-Programming-Patterns)

该仓库包含与本专栏相关的全部可运行实例。本项目主要使用 **C#** 编写（只涉及基本语法，只会C/C++也能无压力读懂）。

## 环境准备与运行方式

以下约定对**每一章**都适用：配好一次，后面直接复用。

### 需要什么

- 方式一：本机安装 **.NET SDK 8 或更高**（用 `dotnet --version` 自查）
- 方式二：什么都不装，到仓库的 **Releases** 页面下载对应平台（Windows / Linux / macOS）的压缩包，解压后直接运行

> Unity 用户注意：Unity 自带的是 Mono / .NET 运行时，并不等于装过 .NET SDK，命令行里可能没有 `dotnet` 命令。这种情况走方式二。

### 怎么跑

每个章节目录下的示例都是独立工程，可以直接运行：

```bash
cd 1.命令模式/0.改进前-硬编码输入
dotnet run
```

Windows 也可以直接双击目录下的 `运行.cmd`；macOS / Linux 执行 `./运行.sh`。
这两个脚本会先检查有没有 `dotnet`，没有就给出中文提示并指向 Releases。

### 两点约定

- 所有项目固定 `TargetFramework = net8.0`，装 .NET 8 / 9 / 10 的读者都能构建
- 仓库**不含** `global.json`，不会锁定 SDK 版本

## 作者

个人主页：[https://liblog.com.cn/](https://liblog.com.cn/)
