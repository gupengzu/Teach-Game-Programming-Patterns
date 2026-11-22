using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ObserverPattern.Step1;

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

// ★ 观察者接口：只声明「有事发生时叫我一声」。
//   接口里没有成就、音效、UI 的任何痕迹。
internal abstract class Observer
{
    public abstract string Name { get; }

    public abstract void OnNotify(Entity entity, GameEvent e);
}

// ★ 被观察者（书里叫 Subject）：
//   1. 持有一列表观察者；
//   2. 对外提供 addObserver / removeObserver；
//   3. 有事时遍历列表，挨个通知。
internal class Subject
{
    private readonly List<Observer> _observers = new();

    public int ObserverCount => _observers.Count;

    public void AddObserver(Observer observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void RemoveObserver(Observer observer) => _observers.Remove(observer);

    // 同步调用：直到所有观察者的 OnNotify 返回，Notify 才返回。
    protected void Notify(Entity entity, GameEvent e)
    {
        for (var i = 0; i < _observers.Count; i++)
        {
            _observers[i].OnNotify(entity, e);
        }
    }
}

// 物理引擎只负责「发生了什么」，不负责「谁想知道」。
internal sealed class Physics : Subject
{
    public void UpdateEntity(Entity entity)
    {
        var wasOnSurface = entity.IsOnSurface;
        entity.Accelerate(9.8f);

        if (wasOnSurface && !entity.IsOnSurface)
        {
            Notify(entity, GameEvent.EntityFell);
        }
    }

    public void Land(Entity entity)
    {
        entity.Land();
        Notify(entity, GameEvent.EntityLanded);
    }

    public void Damage(Entity entity, int amount)
    {
        entity.TakeDamage(amount);
        Notify(entity, GameEvent.EntityDamaged);
    }
}

internal sealed class Achievements : Observer
{
    public override string Name => "成就";

    public int Unlocked { get; private set; }

    public override void OnNotify(Entity entity, GameEvent e)
    {
        if (e != GameEvent.EntityFell || !entity.IsHero || entity.Zone != "桥")
        {
            return;
        }

        Unlocked++;
        Console.WriteLine("    [成就] 解锁「从桥上掉落」");
    }
}

internal sealed class AudioSystem : Observer
{
    public override string Name => "音效";

    public override void OnNotify(Entity entity, GameEvent e)
    {
        if (e == GameEvent.EntityFell)
        {
            Console.WriteLine("    [音效] 播放「坠落」音效");
        }
        else if (e == GameEvent.EntityDamaged)
        {
            Console.WriteLine("    [音效] 播放「受伤」音效");
        }
    }
}

internal sealed class QuestLog : Observer
{
    public override string Name => "任务日志";

    public int Entries { get; private set; }

    public override void OnNotify(Entity entity, GameEvent e)
    {
        var text = e switch
        {
            GameEvent.EntityFell => $"{entity.Name} 从{entity.Zone}上掉了下去",
            GameEvent.EntityLanded => $"{entity.Name} 落到了地面",
            GameEvent.EntityDamaged => $"{entity.Name} 受到了伤害（剩余 {entity.Health}）",
            _ => null,
        };

        if (text != null)
        {
            Entries++;
            Console.WriteLine($"    [任务日志] {text}");
        }
    }
}

// 基准用的两个落地类：一个走静态分派，一个走观察者接口。
internal sealed class DirectSink
{
    public long Total { get; private set; }

    public void OnNotify(Entity entity, GameEvent e) => Total += (int)e + 1;
}

internal sealed class CountingObserver : Observer
{
    public override string Name => "基准";

    public long Total { get; private set; }

    public override void OnNotify(Entity entity, GameEvent e) => Total += (int)e + 1;
}

internal sealed class BenchSubject : Subject
{
    public void Fire(Entity entity, GameEvent e) => Notify(entity, e);

    public void WarmUp(Entity entity)
    {
        for (var i = 0; i < 100_000; i++)
        {
            Notify(entity, GameEvent.EntityFell);
        }
    }
}

internal static class Program
{
    private const int Iterations = 5_000_000;

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "观察者模式 · 1 观察者接口";

        var physics = new Physics();
        var achievements = new Achievements();
        var audio = new AudioSystem();
        var questLog = new QuestLog();

        Console.WriteLine("观察者模式 · 1 观察者接口（Subject + Observer）");
        Console.WriteLine(new string('=', 62));
        Console.WriteLine("  物理引擎不认识成就、音效、任务日志中的任何一个。");
        Console.WriteLine("  它只维护一份观察者列表，有事就挨个通知。");
        Console.WriteLine();

        physics.AddObserver(achievements);
        physics.AddObserver(audio);
        physics.AddObserver(questLog);
        Console.WriteLine($"  注册观察者：{achievements.Name} / {audio.Name} / {questLog.Name}"
                          + $"（共 {physics.ObserverCount} 个）");
        Console.WriteLine();

        var hero = new Entity("英雄", isHero: true) { Zone = "桥" };

        Console.WriteLine("  英雄脚下的桥板消失了……");
        physics.UpdateEntity(hero);
        Console.WriteLine();

        Console.WriteLine("  拆除音效系统：physics.RemoveObserver(audio)");
        physics.RemoveObserver(audio);
        Console.WriteLine($"  剩余观察者：{physics.ObserverCount} 个");
        Console.WriteLine("  英雄回到桥面，然后再掉一次：");
        physics.Land(hero);
        physics.UpdateEntity(hero);
        Console.WriteLine();
        Console.WriteLine("  物理代码一行都没有改 —— 它甚至不知道音效系统已经不存在了。");
        Console.WriteLine();

        RunBenchmark(hero);

        Console.WriteLine();
        Console.WriteLine("  按任意键退出。");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }

    private static void RunBenchmark(Entity hero)
    {
        var direct = new DirectSink();
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < Iterations; i++)
        {
            direct.OnNotify(hero, (GameEvent)(i & 1));
        }

        stopwatch.Stop();
        var directMs = stopwatch.Elapsed.TotalMilliseconds;

        var observer = new CountingObserver();
        var subject = new BenchSubject();
        subject.AddObserver(observer);
        subject.WarmUp(hero);

        stopwatch.Restart();
        for (var i = 0; i < Iterations; i++)
        {
            subject.Fire(hero, (GameEvent)(i & 1));
        }

        stopwatch.Stop();
        var observerMs = stopwatch.Elapsed.TotalMilliseconds;

        Console.WriteLine($"  微基准：{Iterations / 10_000} 万次通知（Release 模式，单观察者）");
        Console.WriteLine($"    直接调用（静态分派）：      {directMs,7:F1} ms"
                          + $"   约 {directMs * 1_000_000 / Iterations:F2} ns/次");
        Console.WriteLine($"    观察者通知（虚方法 + 遍历）：{observerMs,7:F1} ms"
                          + $"   约 {observerMs * 1_000_000 / Iterations:F2} ns/次");
        Console.WriteLine();
        Console.WriteLine($"    每次通知多花约 {(observerMs - directMs) * 1_000_000 / Iterations:F1} ns，"
                          + $"500 万次累计多花 {observerMs - directMs:F1} ms。");
        Console.WriteLine("    这个数量级，除非你把通知塞进每帧几十万次的热点循环，否则根本测不出来。");
        Console.WriteLine("    真正慢的「事件 / 消息」系统，慢在排队和动态分配，");
        Console.WriteLine("    不在这一层虚调用本身。");
        Console.WriteLine($"    （累计值 {direct.Total + observer.Total}，防止编译器把循环整个优化掉）");
    }
}
