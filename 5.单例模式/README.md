# 5. 单例模式

> **一句话定义**
>
> 单例模式同时做两件事：保证一个类只有一个实例，并提供一个全局访问点。真正值得学的，不是这两句定义，而是什么时候该把它们拆开。

本章用四个可运行阶段，从多个异步文件系统包装器出发，依次尝试经典单例、静态类和依赖注入。代码为 .NET 8 控制台程序，无第三方依赖。

完整工程地址：[https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.%E5%8D%95%E4%BE%8B%E6%A8%A1%E5%BC%8F](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.%E5%8D%95%E4%BE%8B%E6%A8%A1%E5%BC%8F)

> 运行环境与通用运行方式见[仓库根目录的「环境准备与运行方式」](../README.md#环境准备与运行方式)。

**目录**

- [0. 改进前：多个文件系统实例](#0-改进前多个文件系统实例)
- [1. 改进一：经典单例，全局访问与延迟初始化](#1-改进一经典单例全局访问与延迟初始化)
- [2. 改进二：静态类，显式控制初始化时机](#2-改进二静态类显式控制初始化时机)
- [3. 改进三：依赖注入，缩小作用域与可替换](#3-改进三依赖注入缩小作用域与可替换)
- [4. 小结](#4-小结)

---

## 0. 改进前：多个文件系统实例

文件系统常被包装成异步接口：写入、删除先进入队列，稍后才真正访问磁盘。这样能把缓慢 IO 移出关键帧，但前提是操作顺序仍然可控。

最直觉的写法是各模块自己创建包装器：

```csharp
var saveFiles = new AsyncFileSystem("存档模块", disk);
var migrationFiles = new AsyncFileSystem("迁移模块", disk);

saveFiles.QueueDelete("slot0.sav");          // 业务上先发生
migrationFiles.QueueWrite("slot0.sav", "新周目"); // 业务上后发生
```

两个对象共享底层 `Disk`，却各自持有 `Queue<Action>`。迁移队列先冲刷、存档队列后冲刷时，后创建的包装器并没有让操作顺序自动变得正确。

运行 [0.改进前-多个文件系统实例](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.单例模式/0.改进前-多个文件系统实例)，实测输出：

```text
两个模块各自 new 一个异步文件系统包装器：
  是同一包装器：False
业务请求顺序：1.删除旧周目  2.写入新周目
迁移模块先冲刷，存档模块后冲刷：
  [迁移模块] 写入 slot0.sav = 新周目
  [存档模块] 删除 slot0.sav
最终读取：<文件不存在>
问题：全局顺序被两个私有队列打乱，新周目被旧删除请求抹掉。
```

这里需要唯一的不是「磁盘」这个物理概念，而是**同一个后端之上的队列、缓存或连接协调者**。如果只是想少写参数，随便 `new` 当然方便；一旦状态本身要求协调，多实例就会破坏业务约束。

> 完整代码：[0.改进前-多个文件系统实例](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.单例模式/0.改进前-多个文件系统实例)

---

## 1. 改进一：经典单例，全局访问与延迟初始化

经典单例把两条规则绑在一起：

1. 私有构造，外部不能自行创建；
2. 静态 `Instance`，任何代码都能取得同一对象。

C# 中可以用 `Lazy<T>` 表达延迟初始化：

```csharp
internal sealed class FileSystem
{
    private static readonly Lazy<FileSystem> LazyInstance = new(() => new FileSystem());
    private readonly Queue<Action> _pending = new();

    public static FileSystem Instance => LazyInstance.Value;

    private FileSystem()
    {
        CreatedCount++;
        // 读取配置、建立队列等初始化工作。
    }
}
```

第一次访问 `Instance` 才构造，之后所有模块都向同一队列提交操作：

```csharp
FileSystem.Instance.QueueDelete("slot0.sav");
FileSystem.Instance.QueueWrite("slot0.sav", "新周目");
FileSystem.Instance.Flush();
```

运行 [1.经典单例-全局访问与延迟初始化](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.单例模式/1.经典单例-全局访问与延迟初始化)，实测：

```text
首次访问前：文件系统尚未创建。
  [FileSystem] 私有构造函数执行，读取磁盘配置。
8 个并发访问得到同一实例：True
FileSystem 构造次数：1
静态属性再次访问仍是同一实例：True
共享队列按请求顺序冲刷：
  [共享队列] 删除 slot0.sav
  [共享队列] 写入 slot0.sav = 新周目
最终读取：新周目
注意：Lazy 只保证初始化线程安全，上面的 Queue 并不支持多线程并发写入。
```

### 这条线程安全边界不能跨过去

`Lazy<T>` 默认保证「构造一次」的线程安全，不会让 `Queue<T>`、字典或文件句柄自动线程安全。初始化安全与对象操作安全是两件事。多线程提交时仍需锁、并发队列或专用 IO 线程。

### 全局访问的账单

单例消除了多实例问题，也引入了新的问题：

- **可理解性下降**：任何方法都可能偷偷读写它，只看函数签名无法知道依赖；
- **封装与耦合恶化**：模块绕过参数直接相连，修改全局状态会影响所有调用者；
- **并发冲突扩大**：全局对象是天然共享状态，测试也可能互相污染；
- **未来需求被封死**：日志系统最初似乎只需一个，后来却要按平台、关卡或玩家分多个输出；单例把昨天的假设变成明天的障碍。

经典写法也可以把构造函数设为 `protected`，在启动时选定某个平台派生实现，从而兼顾多态。但本例为了突出最小结构使用私有构造；它本身并不支持继承替换。

### 延迟初始化并非总是礼物

它省掉了用不到时的成本，却也剥夺了你对**何时付出成本**的控制。如果第一次访问发生在战斗中，读取磁盘配置就会造成卡顿；大对象还会影响内存布局与峰值。运行时才知道的平台实现适合在初始化阶段选择，而不是让某段 gameplay 代码偶然触发。

> 完整代码：[1.经典单例-全局访问与延迟初始化](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.单例模式/1.经典单例-全局访问与延迟初始化)

---

## 2. 改进二：静态类，显式控制初始化时机

如果不需要实例多态，可以把全局入口改成静态类，并把初始化从「第一次访问」移到启动流程：

```csharp
internal static class FileSystem
{
    private static Disk? _disk;

    public static bool IsInitialized => _disk is not null;

    public static void Initialize(string rootDirectory)
    {
        if (_disk is not null)
            throw new InvalidOperationException("文件系统只能初始化一次。");

        _disk = new Disk(rootDirectory);
    }
}
```

主菜单阶段不碰磁盘；读完运行时配置、进入关卡之前，明确调用：

```csharp
FileSystem.Initialize("./game-data");
FileSystem.QueueDelete("slot0.sav");
FileSystem.QueueWrite("slot0.sav", "新周目");
FileSystem.Flush();
```

运行 [2.静态类-显式初始化控制时机](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.单例模式/2.静态类-显式初始化控制时机)，实测：

```text
主菜单：只创建窗口，不访问磁盘。
文件系统已初始化：False
进入关卡前，先用运行时配置显式初始化：
  [显式初始化] 根目录：./game-data
  [共享队列] 删除 slot0.sav
  [共享队列] 写入 slot0.sav = 新周目
最终读取：新周目
重复初始化被拒绝：文件系统只能初始化一次。
```

这一步解决的是**初始化顺序和时机**，不是全局耦合。静态字段初始化、静态构造函数以及多个类型之间的初始化顺序都不适合承载复杂业务依赖；显式 `Initialize` 更容易读，也能在调试版用异常或 `Debug.Assert` 检查「未初始化」「重复初始化」。这是实践中常见的折中：保留方便的全局入口，同时尽早暴露生命周期错误。

代价仍然存在：静态类不能实现接口、不能按实例替换，测试之间需要重置状态；全局可访问依旧让依赖藏在代码内部。

> 完整代码：[2.静态类-显式初始化控制时机](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.单例模式/2.静态类-显式初始化控制时机)

---

## 3. 改进三：依赖注入，缩小作用域与可替换

最后把「唯一性」和「全局访问」拆开。模块只依赖接口，通过构造函数说出真实需求：

```csharp
internal sealed class SaveService(IFileSystem files)
{
    public void Save(string path, string content) => files.QueueWrite(path, content);
}

internal sealed class CleanupService(IFileSystem files)
{
    public void DeleteOldSave(string path) => files.QueueDelete(path);
}
```

组合根决定生命周期，并为同一个后端只创建一个协调包装器：

```csharp
var productionFiles = new AsyncFileSystem(new Disk());
var cleanup = new CleanupService(productionFiles);
var save = new SaveService(productionFiles);
```

这不是声称整个进程永远只能有一个 `AsyncFileSystem`，而是保证**同一个 Disk 的作用域内只有一个**。另一个回放世界可以有自己的 Disk 和包装器，测试则可传入 `RecordingFileSystem`。

运行 [3.依赖注入-缩小作用域与可替换](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.单例模式/3.依赖注入-缩小作用域与可替换)，实测：

```text
正式世界：组合根只创建一个包装器，并把它显式传给两个模块。
  最终读取：新周目
另一个隔离世界：新建一套对象，不污染正式世界。
  最终读取：回放周目
正式世界仍为：新周目
回放世界内容：回放周目
测试：用记录型替身替换磁盘，不断言全局单例。
替身记录：写入 unit.sav=测试数据
```

参数让依赖可见，接口让实现可换，组合根让生命周期可审查。代价是调用链需要传递对象；如果几十层函数只为搬运同一个参数，也可以把服务放进已有的 `Game`、`World` 或关卡上下文，而不是再发明一个全局静态入口。

### 不只有这四条路

原书给出的替代方向，本质都是缩小单例职责：

- **删除 Manager**：如果只是把其他对象的行为集中到一个类，把方法还给真正拥有数据的对象；
- **只保证唯一，不提供全局访问**：用构造保护、工厂或运行时断言守住约束，通过参数取得对象；
- **从已有对象获取**：`Game`、`World` 往往已经是自然的组合边界；
- **服务定位器**：把静态访问改成可替换的注册表，但依赖仍不直观，也不天然解决生命周期；
- **折中断言**：保留单例，在开发版检查重复创建、错误初始化顺序，发布版减少检查成本。

> 完整代码：[3.依赖注入-缩小作用域与可替换](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.单例模式/3.依赖注入-缩小作用域与可替换)

---

## 4. 小结

单例模式真正难的地方，是如何处理两个经常不应绑死的承诺：

- **唯一实例**：来自底层资源或业务不变量，应该有清晰作用域；
- **全局访问**：只是调用方便，却牺牲依赖可见性、隔离性和可替换性。

四个阶段对应四次判断：多实例破坏队列顺序；经典单例修复协调但扩大全局状态；静态类把初始化时机拿回启动流程；依赖注入进一步把唯一性限定在组合根管理的作用域内。

使用前可以问三句：这个对象真的只能有一个吗？这个限制属于进程、世界还是某个后端？调用者是否必须通过静态属性才能拿到它？前两句可能是真需求，最后一句通常不是。能显式传参就不要为了少写几个字符付出全局状态的长期成本。

完整可运行代码：[Teach-Game-Programming-Patterns / 5.单例模式](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/5.%E5%8D%95%E4%BE%8B%E6%A8%A1%E5%BC%8F)
