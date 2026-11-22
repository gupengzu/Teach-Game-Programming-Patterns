using System.Text;

namespace PrototypePattern.Step2;

// 可变引用字段最能暴露浅克隆的问题。
internal sealed class Weapon
{
    public Weapon(string name, int damage)
    {
        Name = name;
        Damage = damage;
    }

    public string Name { get; }

    public int Damage { get; private set; }

    public void Enchant(int bonus) => Damage += bonus;

    public Weapon Clone() => new(Name, Damage);

    public override string ToString() => $"{Name}（伤害 {Damage}）";
}

internal abstract class Monster
{
    private static int _nextId = 1;

    protected Monster(string name, int health, Weapon weapon)
    {
        Name = name;
        Health = health;
        Weapon = weapon;
        Id = _nextId++;
    }

    public int Id { get; }

    public string Name { get; }

    public int Health { get; private set; }

    public Weapon Weapon { get; }

    // 契约：返回的怪物拥有自己的可变装备。
    public abstract Monster Clone();

    // 只用于对照错误语义：复制引用，两个怪物会共享武器。
    public abstract Monster CloneShallow();

    public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);

    public string Describe() => $"{Name} #{Id}｜生命 {Health}｜{Weapon}";
}

internal sealed class Demon : Monster
{
    public Demon(string name, int health, Weapon weapon) : base(name, health, weapon)
    {
    }

    public override Monster Clone() => new Demon(Name, Health, Weapon.Clone());

    public override Monster CloneShallow() => new Demon(Name, Health, Weapon);
}

internal sealed class Ghost : Monster
{
    public Ghost(string name, int health, Weapon weapon) : base(name, health, weapon)
    {
    }

    public override Monster Clone() => new Ghost(Name, Health, Weapon.Clone());

    public override Monster CloneShallow() => new Ghost(Name, Health, Weapon);
}

// C# 里可以直接保存一个生产函数，不必为它再写一层具体类。
internal sealed class Spawner
{
    private readonly Func<Monster> _spawn;

    public Spawner(Func<Monster> spawn) => _spawn = spawn;

    public Monster SpawnMonster() => _spawn();
}

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "原型模式 · 2 深克隆与生产函数";

        Console.WriteLine("原型模式 · 2 深克隆与生产函数");
        Console.WriteLine(new string('=', 62));
        Console.WriteLine("  克隆恶魔时，要不要连它手里的草叉一起克隆？\n");

        var original = new Demon("恶魔原型", health: 30, new Weapon("草叉", damage: 8));
        var shallow = original.CloneShallow();
        var deep = original.Clone();

        Console.WriteLine("  给原型的草叉附魔 +4：");
        original.Weapon.Enchant(4);
        Console.WriteLine($"    原型：  {original.Describe()}");
        Console.WriteLine($"    浅克隆：{shallow.Describe()}  ← 武器被一起改了");
        Console.WriteLine($"    深克隆：{deep.Describe()}  ← 仍保留自己的武器");
        Console.WriteLine($"    原型与浅克隆共享武器：{ReferenceEquals(original.Weapon, shallow.Weapon)}");
        Console.WriteLine($"    原型与深克隆共享武器：{ReferenceEquals(original.Weapon, deep.Weapon)}");

        original.TakeDamage(6);
        Console.WriteLine("\n  原型受到 6 点伤害后，两个克隆体的生命值仍为 30。");
        Console.WriteLine("  数值字段已经按值复制；需要决定所有权的是可变引用字段。");

        Console.WriteLine("\n  同一个 Spawner 也可以保存 Func<Monster> 生产函数：");
        var prototypeSpawner = new Spawner(original.Clone);
        var factorySpawner = new Spawner(() => new Ghost(
            "新生恶灵", health: 15, new Weapon("寒触", damage: 5)));

        Console.WriteLine($"    克隆生产：{prototypeSpawner.SpawnMonster().Describe()}");
        Console.WriteLine($"    函数生产：{factorySpawner.SpawnMonster().Describe()}");
        Console.WriteLine("  不需要复制已有状态时，生产函数通常比实现 Clone 更直接。");

        Console.WriteLine("\n  按任意键退出。");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
