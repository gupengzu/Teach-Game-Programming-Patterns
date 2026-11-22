using System.Text;

namespace PrototypePattern.Step3;

// 数据对象既保存自己的属性，也可以把缺失属性的查找委托给原型。
internal sealed class MonsterData
{
    private readonly Dictionary<string, int> _properties = new();

    public MonsterData(string name, MonsterData? prototype = null)
    {
        Name = name;
        Prototype = prototype;
    }

    public string Name { get; }

    public MonsterData? Prototype { get; }

    public MonsterData Set(string key, int value)
    {
        _properties[key] = value;
        return this;
    }

    public int Get(string key)
    {
        for (MonsterData? current = this; current != null; current = current.Prototype)
        {
            if (current._properties.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        throw new KeyNotFoundException($"{Name} 及其原型都没有属性 {key}");
    }

    public string SourceOf(string key)
    {
        for (MonsterData? current = this; current != null; current = current.Prototype)
        {
            if (current._properties.ContainsKey(key))
            {
                return current.Name;
            }
        }

        return "未定义";
    }
}

// 配置通过原型链共享；生命等运行状态仍然属于每个怪物自己。
internal sealed class Monster
{
    private static int _nextId = 1;

    public Monster(MonsterData data)
    {
        Data = data;
        Health = data.Get("health");
        Id = _nextId++;
    }

    public int Id { get; }

    public MonsterData Data { get; }

    public int Health { get; private set; }

    public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);

    public string Describe() => $"{Data.Name} #{Id}｜生命 {Health}/{Data.Get("health")}"
                                + $"｜速度 {Data.Get("speed")}｜攻击 {Data.Get("attack")}";
}

internal sealed class Spawner
{
    private readonly MonsterData _prototype;

    public Spawner(MonsterData prototype) => _prototype = prototype;

    public Monster SpawnMonster() => new(_prototype);
}

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "原型模式 · 3 数据原型";

        Console.WriteLine("原型模式 · 3 数据原型（属性查找委托）");
        Console.WriteLine(new string('=', 62));

        var goblin = new MonsterData("哥布林杂兵")
            .Set("health", 20)
            .Set("speed", 3)
            .Set("attack", 4)
            .Set("coldResist", 2);

        // 特殊个体只记录差异，其余属性沿原型链查找。
        var warrior = new MonsterData("哥布林战士", prototype: goblin)
            .Set("health", 40)
            .Set("attack", 7);
        var archer = new MonsterData("哥布林弓手", prototype: goblin)
            .Set("attack", 5)
            .Set("range", 6);

        Console.WriteLine("  属性来源：");
        Console.WriteLine($"    战士生命 40 来自「{warrior.SourceOf("health")}」");
        Console.WriteLine($"    战士速度  3 来自「{warrior.SourceOf("speed")}」");
        Console.WriteLine($"    弓手冰抗  2 来自「{archer.SourceOf("coldResist")}」");
        Console.WriteLine($"    弓手射程  6 来自「{archer.SourceOf("range")}」");

        var spawner = new Spawner(warrior);
        var first = spawner.SpawnMonster();
        var second = spawner.SpawnMonster();
        first.TakeDamage(9);

        Console.WriteLine("\n  同一数据原型产出两个战士，其中一个受到 9 点伤害：");
        Console.WriteLine($"    {first.Describe()}");
        Console.WriteLine($"    {second.Describe()}");
        Console.WriteLine("    配置共享，运行状态不共享。");

        Console.WriteLine("\n  修改基础哥布林的速度：3 → 4");
        goblin.Set("speed", 4);
        Console.WriteLine($"    战士速度：{warrior.Get("speed")}");
        Console.WriteLine($"    弓手速度：{archer.Get("speed")}");
        Console.WriteLine("    两个特殊个体都没有复制 speed，因此无需逐份同步。");

        Console.WriteLine("\n  按任意键退出。");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
