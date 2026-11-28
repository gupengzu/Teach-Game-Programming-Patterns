Console.WriteLine("正式世界：组合根只创建一个包装器，并把它显式传给两个模块。");
var productionFiles = new AsyncFileSystem(new Disk());
productionFiles.WriteDirect("slot0.sav", "旧周目");
RunMigration(productionFiles, "新周目");

Console.WriteLine("另一个隔离世界：新建一套对象，不污染正式世界。");
var replayFiles = new AsyncFileSystem(new Disk());
RunMigration(replayFiles, "回放周目");
Console.WriteLine($"正式世界仍为：{productionFiles.Read("slot0.sav")}");
Console.WriteLine($"回放世界内容：{replayFiles.Read("slot0.sav")}");

Console.WriteLine("测试：用记录型替身替换磁盘，不断言全局单例。");
var fakeFiles = new RecordingFileSystem();
var save = new SaveService(fakeFiles);
save.Save("unit.sav", "测试数据");
fakeFiles.Flush();
Console.WriteLine($"替身记录：{string.Join("、", fakeFiles.Records)}");

static void RunMigration(IFileSystem files, string content)
{
    var cleanup = new CleanupService(files);
    var save = new SaveService(files);

    cleanup.DeleteOldSave("slot0.sav");
    save.Save("slot0.sav", content);
    files.Flush();
    Console.WriteLine($"  最终读取：{files.Read("slot0.sav") ?? "<文件不存在>"}");
}

internal interface IFileSystem
{
    void QueueWrite(string path, string content);
    void QueueDelete(string path);
    string? Read(string path);
    void Flush();
}

internal sealed class SaveService(IFileSystem files)
{
    public void Save(string path, string content) => files.QueueWrite(path, content);
}

internal sealed class CleanupService(IFileSystem files)
{
    public void DeleteOldSave(string path) => files.QueueDelete(path);
}

internal sealed class AsyncFileSystem(Disk disk) : IFileSystem
{
    private readonly Queue<Action> _pending = new();

    public void WriteDirect(string path, string content) => disk.Write(path, content);

    public void QueueWrite(string path, string content) =>
        _pending.Enqueue(() => disk.Write(path, content));

    public void QueueDelete(string path) => _pending.Enqueue(() => disk.Delete(path));

    public string? Read(string path) => disk.Read(path);

    public void Flush()
    {
        while (_pending.Count > 0)
        {
            _pending.Dequeue()();
        }
    }
}

internal sealed class RecordingFileSystem : IFileSystem
{
    private readonly Queue<Action> _pending = new();

    public List<string> Records { get; } = [];

    public void QueueWrite(string path, string content) =>
        _pending.Enqueue(() => Records.Add($"写入 {path}={content}"));

    public void QueueDelete(string path) => _pending.Enqueue(() => Records.Add($"删除 {path}"));

    public string? Read(string path) => null;

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

    public string? Read(string path) => _files.GetValueOrDefault(path);
}
