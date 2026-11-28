var disk = new Disk();
disk.Write("slot0.sav", "旧周目");

var saveFiles = new AsyncFileSystem("存档模块", disk);
var migrationFiles = new AsyncFileSystem("迁移模块", disk);

Console.WriteLine("两个模块各自 new 一个异步文件系统包装器：");
Console.WriteLine($"  是同一包装器：{ReferenceEquals(saveFiles, migrationFiles)}");
Console.WriteLine("业务请求顺序：1.删除旧周目  2.写入新周目");

// 按业务顺序入队，但每个包装器只看得见自己的队列。
saveFiles.QueueDelete("slot0.sav");
migrationFiles.QueueWrite("slot0.sav", "新周目");

Console.WriteLine("迁移模块先冲刷，存档模块后冲刷：");
migrationFiles.Flush();
saveFiles.Flush();

Console.WriteLine($"最终读取：{disk.Read("slot0.sav") ?? "<文件不存在>"}");
Console.WriteLine("问题：全局顺序被两个私有队列打乱，新周目被旧删除请求抹掉。");

internal sealed class Disk
{
    private readonly Dictionary<string, string> _files = new();

    public void Write(string path, string content) => _files[path] = content;

    public void Delete(string path) => _files.Remove(path);

    public string? Read(string path) => _files.GetValueOrDefault(path);
}

internal sealed class AsyncFileSystem(string moduleName, Disk disk)
{
    private readonly Queue<Action> _pending = new();

    public void QueueWrite(string path, string content) =>
        _pending.Enqueue(() =>
        {
            Console.WriteLine($"  [{moduleName}] 写入 {path} = {content}");
            disk.Write(path, content);
        });

    public void QueueDelete(string path) =>
        _pending.Enqueue(() =>
        {
            Console.WriteLine($"  [{moduleName}] 删除 {path}");
            disk.Delete(path);
        });

    public void Flush()
    {
        while (_pending.Count > 0)
        {
            _pending.Dequeue()();
        }
    }
}
