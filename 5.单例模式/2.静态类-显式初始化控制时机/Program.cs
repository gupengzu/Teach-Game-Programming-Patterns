Console.WriteLine("主菜单：只创建窗口，不访问磁盘。");
Console.WriteLine($"文件系统已初始化：{FileSystem.IsInitialized}");

Console.WriteLine("进入关卡前，先用运行时配置显式初始化：");
FileSystem.Initialize("./game-data");

// 静态入口依旧全局可访问，但初始化时机由启动流程控制。
FileSystem.QueueDelete("slot0.sav");
FileSystem.QueueWrite("slot0.sav", "新周目");
FileSystem.Flush();
Console.WriteLine($"最终读取：{FileSystem.Read("slot0.sav") ?? "<文件不存在>"}");

try
{
    FileSystem.Initialize("./other-data");
}
catch (InvalidOperationException exception)
{
    Console.WriteLine($"重复初始化被拒绝：{exception.Message}");
}

internal static class FileSystem
{
    private static readonly Queue<Action> Pending = new();
    private static Disk? _disk;

    public static bool IsInitialized => _disk is not null;

    public static void Initialize(string rootDirectory)
    {
        if (_disk is not null)
        {
            throw new InvalidOperationException("文件系统只能初始化一次。");
        }

        // 根目录来自运行时配置，不必依赖首次访问发生在何处。
        _disk = new Disk(rootDirectory);
        Console.WriteLine($"  [显式初始化] 根目录：{rootDirectory}");
        _disk.Write("slot0.sav", "旧周目");
    }

    public static void QueueWrite(string path, string content)
    {
        var disk = GetDisk();
        Pending.Enqueue(() =>
        {
            Console.WriteLine($"  [共享队列] 写入 {path} = {content}");
            disk.Write(path, content);
        });
    }

    public static void QueueDelete(string path)
    {
        var disk = GetDisk();
        Pending.Enqueue(() =>
        {
            Console.WriteLine($"  [共享队列] 删除 {path}");
            disk.Delete(path);
        });
    }

    public static string? Read(string path) => GetDisk().Read(path);

    public static void Flush()
    {
        GetDisk();
        while (Pending.Count > 0)
        {
            Pending.Dequeue()();
        }
    }

    private static Disk GetDisk() =>
        _disk ?? throw new InvalidOperationException("请先调用 FileSystem.Initialize。");
}

internal sealed class Disk(string rootDirectory)
{
    private readonly Dictionary<string, string> _files = new();

    public string RootDirectory { get; } = rootDirectory;

    public void Write(string path, string content) => _files[path] = content;

    public void Delete(string path) => _files.Remove(path);

    public string? Read(string path) => _files.GetValueOrDefault(path);
}
