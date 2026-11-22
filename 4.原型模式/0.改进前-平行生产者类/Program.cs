using System.Text;

namespace PrototypePattern.Step0;

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

    public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);

    public string Describe() => $"{Name} #{Id}｜生命 {Health}｜速度 {Speed}";
}

internal sealed class Ghost : Monster
{
    public Ghost(int health, int speed) : base("恶灵", health, speed)
    {
    }
}

internal sealed class Demon : Monster
{
    public Demon(int health, int speed) : base("恶魔", health, speed)
    {
    }
}

internal sealed class Sorcerer : Monster
{
    public Sorcerer(int health, int speed) : base("术士", health, speed)
    {
    }
}

// 每一种怪物都配一个生产者类，形成两套平行的继承层次。
internal abstract class Spawner
{
    public abstract string Name { get; }

    public abstract Monster SpawnMonster();
}

internal sealed class GhostSpawner : Spawner
{
    public override string Name => "恶灵生产者";

    public override Monster SpawnMonster() => new Ghost(health: 15, speed: 3);
}

internal sealed class DemonSpawner : Spawner
{
    public override string Name => "恶魔生产者";

    public override Monster SpawnMonster() => new Demon(health: 30, speed: 2);
}

internal sealed class SorcererSpawner : Spawner
{
    public override string Name => "术士生产者";

    public override Monster SpawnMonster() => new Sorcerer(health: 20, speed: 2);
}

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "原型模式 · 0 平行生产者类";

        Console.WriteLine("原型模式 · 0 改进前（每种怪物一个生产者类）");
        Console.WriteLine(new string('=', 62));
        Console.WriteLine("  怪物类：Ghost / Demon / Sorcerer");
        Console.WriteLine("  生产者类：GhostSpawner / DemonSpawner / SorcererSpawner");
        Console.WriteLine("  新增一种怪物，就要同时新增一个几乎相同的生产者。\n");

        Spawner[] spawners = [new GhostSpawner(), new DemonSpawner(), new SorcererSpawner()];
        foreach (var spawner in spawners)
        {
            Console.WriteLine($"  {spawner.Name}：");
            Console.WriteLine($"    {spawner.SpawnMonster().Describe()}");
            Console.WriteLine($"    {spawner.SpawnMonster().Describe()}");
        }

        Console.WriteLine("\n  3 种怪物需要 3 个具体生产者类；样板代码随怪物种类线性增长。");
        Console.WriteLine("\n  按任意键退出。");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
