Console.WriteLine("首次访问前：文件系统尚未创建。");

// Lazy<T> 默认保证多个线程同时访问时也只构造一次。
var accessTasks = Enumerable.Range(1, 8)
    .Select(_ => Task.Run(() => FileSystem.Instance))
    .ToArray();
var instances = await Task.WhenAll(accessTasks);

Console.WriteLine($"8 个并发访问得到同一实例：{instances.All(x => ReferenceEquals(x, instances[0]))}");
Console.WriteLine($"FileSystem 构造次数：{FileSystem.CreatedCount}");
Console.WriteLine($"静态属性再次访问仍是同一实例：{ReferenceEquals(instances[0], FileSystem.Instance)}");

// 各模块仍然从全局入口取对象，但现在共享同一条操作队列。
FileSystem.Instance.QueueDelete("slot0.sav");
FileSystem.Instance.QueueWrite("slot0.sav", "新周目");

Console.WriteLine("共享队列按请求顺序冲刷：");
FileSystem.Instance.Flush();
Console.WriteLine($"最终读取：{FileSystem.Instance.Read("slot0.sav") ?? "<文件不存在>"}");
Console.WriteLine("注意：Lazy 只保证初始化线程安全，上面的 Queue 并不支持多线程并发写入。");

internal sealed class FileSystem
{
    private static readonly Lazy<FileSystem> LazyInstance = new(() => new FileSystem());
    private readonly Disk _disk = new();
    private readonly Queue<Action> _pending = new();

    public static int CreatedCount { get; private set; }

    public static FileSystem Instance => LazyInstance.Value;

    private FileSystem()
    {
        CreatedCount++;
        Console.WriteLine("  [FileSystem] 私有构造函数执行，读取磁盘配置。");
        _disk.Write("slot0.sav", "旧周目");
    }

    public void QueueWrite(string path, string content) =>
        _pending.Enqueue(() =>
        {
            Console.WriteLine($"  [共享队列] 写入 {path} = {content}");
            _disk.Write(path, content);
        });

    public void QueueDelete(string path) =>
        _pending.Enqueue(() =>
        {
            Console.WriteLine($"  [共享队列] 删除 {path}");
            _disk.Delete(path);
        });

    public string? Read(string path) => _disk.GetValueOrDefault(path);

    public void Flush()
    {
        while (_pending.Count > 0)
        {
            _pending.Dequeue()();
        }
    }
}

internal sealed class Disk
{
    private readonly Dictionary<string, string> _files = new();

    public void Write(string path, string content) => _files[path] = content;

    public void Delete(string path) => _files.Remove(path);

    public string? GetValueOrDefault(string path) => _files.GetValueOrDefault(path);
}
