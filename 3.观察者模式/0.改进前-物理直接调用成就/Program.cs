using System;

namespace ObserverPattern.Step0;

internal enum GameEvent
{
    EntityFell,
    EntityLanded,
    EntityDamaged,
}

internal sealed class Entity
{
    public Entity(string name, bool isHero)
    {
        Name = name;
        IsHero = isHero;
    }

    public string Name { get; }

    public bool IsHero { get; }

    public int Health { get; private set; } = 100;

    public bool IsOnSurface { get; private set; } = true;

    public string Zone { get; set; } = "地面";

    public void Accelerate(float gravity) => IsOnSurface = false;

    public void Land() => IsOnSurface = true;

    public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);
}

// 成就系统：关心「谁掉下去了」「谁倒下了」。
internal sealed class Achievements
{
    public void OnEntityFell(Entity entity)
    {
        if (entity.IsHero && entity.Zone == "桥")
        {
            Console.WriteLine("    [成就] 解锁「从桥上掉落」");
        }
    }

    public void OnEntityDamaged(Entity entity)
    {
        if (entity.Health <= 0)
        {
            Console.WriteLine($"    [成就] 解锁「{entity.Name} 倒下了」");
        }
    }
}

internal sealed class AudioSystem
{
    public void PlayFall() => Console.WriteLine("    [音效] 播放「坠落」音效");

    public void PlayHurt() => Console.WriteLine("    [音效] 播放「受伤」音效");
}

internal sealed class Hud
{
    public void Shake() => Console.WriteLine("    [HUD] 屏幕红闪一下");

    public void FlashRed() => Console.WriteLine("    [HUD] 血条抖动");
}

// ★ 改进前：物理引擎直接持有并调用三个系统。
//   每多一个「关心物理事件」的系统，都要回来改这个类的字段和方法体。
internal sealed class Physics
{
    private readonly Achievements _achievements = new();
    private readonly AudioSystem _audio = new();
    private readonly Hud _hud = new();

    public void UpdateEntity(Entity entity)
    {
        var wasOnSurface = entity.IsOnSurface;
        entity.Accelerate(9.8f);

        if (wasOnSurface && !entity.IsOnSurface)
        {
            // ↓ 物理代码里塞进了游戏玩法
            _achievements.OnEntityFell(entity);
            _audio.PlayFall();
            _hud.Shake();
        }
    }

    public void TakeDamage(Entity entity, int amount)
    {
        entity.TakeDamage(amount);

        // ↓ 同一套逻辑，还要再抄一遍
        _achievements.OnEntityDamaged(entity);
        _audio.PlayHurt();
        _hud.FlashRed();
    }
}

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "观察者模式 · 0 改进前";

        var hero = new Entity("英雄", isHero: true) { Zone = "桥" };
        var physics = new Physics();

        Console.WriteLine("观察者模式 · 0 改进前（物理直接调用成就）");
        Console.WriteLine(new string('=', 62));
        Console.WriteLine("  需求：英雄从桥上掉下去时，解锁「从桥上掉落」。");
        Console.WriteLine("  做法：在物理代码里直接调用成就、音效和 HUD。");
        Console.WriteLine();

        Console.WriteLine("  英雄脚下的桥板消失了……");
        physics.UpdateEntity(hero);
        Console.WriteLine();

        Console.WriteLine("  怪物砍了英雄一刀……");
        physics.TakeDamage(hero, 12);
        Console.WriteLine();

        Console.WriteLine("  Physics 现在直接依赖三个系统：");
        Console.WriteLine("    Physics → Achievements / AudioSystem / Hud");
        Console.WriteLine("  每次通知都在物理代码里写死：先成就、再音效、再 HUD。");
        Console.WriteLine();
        Console.WriteLine("  现在想加第四个「任务日志」，或者临时关掉音效？");
        Console.WriteLine("  只能继续改 Physics —— 而物理程序员并不想知道这些事。");
        Console.WriteLine();
        Console.WriteLine("  更糟的是：成就系统要判断「是不是在桥上」，");
        Console.WriteLine("  它需要的数据（Zone）却要到物理和地图那边去拿。");
        Console.WriteLine();
        Console.WriteLine("  按任意键退出。");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
