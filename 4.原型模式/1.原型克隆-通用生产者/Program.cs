using System.Text;

namespace PrototypePattern.Step1;

internal abstract class Monster
{
    private static int _nextId = 1;

    protected Monster(string name, int health, int speed)
    {
        Name = name;
        Health = health;
        Speed = speed;
        Id = _nextId++;
    }

    public int Id { get; }

    public string Name { get; }

    public int Health { get; private set; }

    public int Speed { get; }

    // 每个子类都知道怎样复制自己的具体类型和全部状态。
    public abstract Monster Clone();

    public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);

    public string Describe() => $"{Name} #{Id}｜生命 {Health}｜速度 {Speed}";
}

internal sealed class Ghost : Monster
{
    public Ghost(string name, int health, int speed) : base(name, health, speed)
    {
    }

    public override Monster Clone() => new Ghost(Name, Health, Speed);
}

internal sealed class Demon : Monster
{
    public Demon(string name, int health, int speed) : base(name, health, speed)
    {
    }

    public override Monster Clone() => new Demon(Name, Health, Speed);
}

internal sealed class Sorcerer : Monster
{
    public Sorcerer(string name, int health, int speed) : base(name, health, speed)
    {
    }

    public override Monster Clone() => new Sorcerer(Name, Health, Speed);
}

// 不再认识任何具体怪物，只保存一个作为模板的对象。
internal sealed class Spawner
{
    private readonly Monster _prototype;

    public Spawner(Monster prototype) => _prototype = prototype;

    public string Name => $"{_prototype.Name}生产者";

    public Monster SpawnMonster() => _prototype.Clone();
}

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "原型模式 · 1 原型克隆";

        Console.WriteLine("原型模式 · 1 原型克隆（一个通用生产者）");
        Console.WriteLine(new string('=', 62));
        Console.WriteLine("  Spawner 不再 new 具体类，而是调用 prototype.Clone()。\n");

        // 原型不只携带类型，也携带配置状态。
        var swiftGhost = new Ghost("迅捷恶灵", health: 10, speed: 6);
        Spawner[] spawners =
        [
            new Spawner(swiftGhost),
            new Spawner(new Demon("重甲恶魔", health: 45, speed: 1)),
            new Spawner(new Sorcerer("火焰术士", health: 20, speed: 2)),
        ];

        foreach (var spawner in spawners)
        {
            Console.WriteLine($"  {spawner.Name}：");
            Console.WriteLine($"    {spawner.SpawnMonster().Describe()}");
            Console.WriteLine($"    {spawner.SpawnMonster().Describe()}");
        }

        var clone = swiftGhost.Clone();
        clone.TakeDamage(4);
        Console.WriteLine("\n  给迅捷恶灵的克隆体造成 4 点伤害：");
        Console.WriteLine($"    原型：{swiftGhost.Describe()}");
        Console.WriteLine($"    克隆：{clone.Describe()}");
        Console.WriteLine($"    原型与克隆是否同一对象：{ReferenceEquals(swiftGhost, clone)}");

        Console.WriteLine("\n  3 种怪物只需 1 个具体生产者类；不同属性也不必再写新生产者。");
        Console.WriteLine("\n  按任意键退出。");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
