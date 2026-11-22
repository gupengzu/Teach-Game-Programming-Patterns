using System;
using System.Threading;

namespace ObserverPattern.Step2;

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

// ★ C# 把观察者内建进了语言：event 本质上是「一列表委托」。
//   和手写 Observer 列表相比：
//     · 订阅用 +=，注销用 -=，不用自己维护 List；
//     · 外面只能订阅 / 注销，不能触发，封装更严；
//     · 观察者可以是方法、lambda、甚至是别的对象的方法。
internal sealed class Physics
{
    public event Action<Entity>? EntityFell;

    public event Action<Entity>? EntityLanded;

    public event Action<Entity, int>? EntityDamaged;

    public int FellListeners => EntityFell?.GetInvocationList().Length ?? 0;

    public int LandedListeners => EntityLanded?.GetInvocationList().Length ?? 0;

    public int DamageListeners => EntityDamaged?.GetInvocationList().Length ?? 0;

    public int ListenerCount => FellListeners + LandedListeners + DamageListeners;

    public void UpdateEntity(Entity entity)
    {
        var wasOnSurface = entity.IsOnSurface;
        entity.Accelerate(9.8f);

        if (wasOnSurface && !entity.IsOnSurface)
        {
            // 没有订阅者时是 null，?.Invoke 直接跳过，不需要判空 if。
            EntityFell?.Invoke(entity);
        }
    }

    public void Land(Entity entity)
    {
        entity.Land();
        EntityLanded?.Invoke(entity);
    }

    public void Damage(Entity entity, int amount)
    {
        entity.TakeDamage(amount);
        EntityDamaged?.Invoke(entity, amount);
    }
}

internal sealed class Achievements
{
    public int Unlocked { get; private set; }

    // 具名方法做观察者：注销时也要传这个方法，-+ 才能对上。
    public void OnFell(Entity entity)
    {
        if (!entity.IsHero || entity.Zone != "桥")
        {
            return;
        }

        Unlocked++;
        Console.WriteLine("    [成就] 解锁「从桥上掉落」");
    }
}

internal sealed class Demo
{
    private readonly Physics _physics = new();
    private readonly Achievements _achievements = new();
    private readonly Entity _hero = new("英雄", isHero: true) { Zone = "桥" };

    // lambda 做观察者：必须存下来，否则没法注销。
    private readonly Action<Entity> _audioFall;
    private readonly Action<Entity, int> _audioHurt;
    private readonly Action<Entity> _hudFall;
    private readonly Action<Entity> _questFall;

    private bool _achievementsOn;
    private bool _audioOn;
    private bool _hudOn;
    private bool _questOn;

    public Demo()
    {
        _audioFall = _ => Console.WriteLine("    [音效] 播放「坠落」音效");
        _audioHurt = (_, amount) => Console.WriteLine($"    [音效] 播放「受伤」音效（-{amount}）");
        _hudFall = _ => Console.WriteLine("    [HUD] 屏幕红闪一下");
        _questFall = entity => Console.WriteLine($"    [任务日志] {entity.Name} 从{entity.Zone}上掉了下去");
    }

    public int ListenerCount => _physics.ListenerCount;

    public string State => $"成就 {On(_achievementsOn)} / 音效 {On(_audioOn)}"
                           + $" / 任务日志 {On(_questOn)} / HUD {On(_hudOn)}";

    public void ToggleAchievements()
    {
        if (_achievementsOn)
        {
            _physics.EntityFell -= _achievements.OnFell;
        }
        else
        {
            _physics.EntityFell += _achievements.OnFell;
        }

        _achievementsOn = !_achievementsOn;
    }

    public void ToggleAudio()
    {
        if (_audioOn)
        {
            _physics.EntityFell -= _audioFall;
            _physics.EntityDamaged -= _audioHurt;
        }
        else
        {
            _physics.EntityFell += _audioFall;
            _physics.EntityDamaged += _audioHurt;
        }

        _audioOn = !_audioOn;
    }

    public void ToggleQuestLog()
    {
        if (_questOn)
        {
            _physics.EntityFell -= _questFall;
        }
        else
        {
            _physics.EntityFell += _questFall;
        }

        _questOn = !_questOn;
    }

    public void ToggleHud()
    {
        if (_hudOn)
        {
            _physics.EntityFell -= _hudFall;
        }
        else
        {
            _physics.EntityFell += _hudFall;
        }

        _hudOn = !_hudOn;
    }

    public string Drop()
    {
        if (!_hero.IsOnSurface)
        {
            return "英雄还在下落中，先按 R 落地。";
        }

        Console.WriteLine();
        Console.WriteLine("  英雄脚下的桥板消失了……");
        _physics.UpdateEntity(_hero);
        Console.WriteLine();
        return $"完成一次坠落通知，当前监听者 {_physics.ListenerCount} 个。";
    }

    public string Land()
    {
        _physics.Land(_hero);
        Console.WriteLine();
        Console.WriteLine("  英雄落地。");
        Console.WriteLine();
        return $"完成一次落地通知，当前监听者 {_physics.ListenerCount} 个。";
    }

    public string Damage(int amount)
    {
        Console.WriteLine();
        Console.WriteLine($"  怪物砍了英雄一刀（-{amount}）。");
        _physics.Damage(_hero, amount);
        Console.WriteLine();
        return $"英雄剩余生命 {_hero.Health}，当前监听者 {_physics.ListenerCount} 个。";
    }

    public void ShowCounts()
    {
        Console.WriteLine($"    坠落事件：{_physics.FellListeners} 个监听者");
        Console.WriteLine($"    落地事件：{_physics.LandedListeners} 个监听者");
        Console.WriteLine($"    受伤事件：{_physics.DamageListeners} 个监听者");
    }

    private static string On(bool value) => value ? "开" : "关";
}

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "观察者模式 · 2 事件与委托";

        var demo = new Demo();

        if (Console.IsInputRedirected)
        {
            RunScripted(demo);
            return;
        }

        demo.ToggleAchievements();
        demo.ToggleAudio();
        demo.ToggleHud();
        demo.ToggleQuestLog();

        Draw(demo, "空格 = 让英雄从桥上掉落，R = 落地，D = 受伤，1~4 = 切换订阅。");

        while (true)
        {
            if (!Console.KeyAvailable)
            {
                Thread.Sleep(15);
                continue;
            }

            var key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.Escape)
            {
                break;
            }

            switch (key)
            {
                case ConsoleKey.Spacebar:
                    Draw(demo, demo.Drop());
                    break;

                case ConsoleKey.R:
                    Draw(demo, demo.Land());
                    break;

                case ConsoleKey.D:
                    Draw(demo, demo.Damage(20));
                    break;

                case ConsoleKey.D1:
                    demo.ToggleAchievements();
                    Draw(demo, "已切换成就系统。");
                    break;

                case ConsoleKey.D2:
                    demo.ToggleAudio();
                    Draw(demo, "已切换音效系统。");
                    break;

                case ConsoleKey.D3:
                    demo.ToggleQuestLog();
                    Draw(demo, "已切换任务日志。");
                    break;

                case ConsoleKey.D4:
                    demo.ToggleHud();
                    Draw(demo, "已切换 HUD。");
                    break;
            }
        }
    }

    private static void Draw(Demo demo, string message)
    {
        Console.Clear();
        Console.WriteLine("观察者模式 · 2 事件与委托（C# event）");
        Console.WriteLine(new string('=', 66));
        Console.WriteLine("  空格 = 坠落    R = 落地    D = 受伤");
        Console.WriteLine("  1 = 成就   2 = 音效   3 = 任务日志   4 = HUD    Esc = 退出");
        Console.WriteLine();
        Console.WriteLine($"  订阅状态：{demo.State}");
        Console.WriteLine($"  监听者总数：{demo.ListenerCount}");
        demo.ShowCounts();
        Console.WriteLine();
        Console.WriteLine($"  {message}");
        Console.WriteLine();
        Console.WriteLine("  提示：事件是同步的。所有订阅者返回后，物理才会继续往下走。");
    }

    private static void RunScripted(Demo demo)
    {
        Console.WriteLine("观察者模式 · 2 事件与委托（C# event）");
        Console.WriteLine(new string('=', 66));
        Console.WriteLine("  演示模式：用脚本走一遍订阅、通知、注销。");
        Console.WriteLine();

        DemonstrateLambdaPitfall();

        demo.ToggleAchievements();
        demo.ToggleAudio();
        demo.ToggleHud();
        demo.ToggleQuestLog();
        Console.WriteLine($"  注册四个订阅者：{demo.State}，监听者 {demo.ListenerCount} 个。");

        Console.WriteLine(demo.Drop());
        Console.WriteLine(demo.Damage(20));
        Console.WriteLine(demo.Land());

        Console.WriteLine();
        Console.WriteLine("  关掉音效系统（-= 两个 lambda）……");
        var before = demo.ListenerCount;
        demo.ToggleAudio();
        demo.ShowCounts();
        Console.WriteLine($"  监听者从 {before} 个降到 {demo.ListenerCount} 个，Physics 依旧不知道谁在听。");

        Console.WriteLine();
        Console.WriteLine("  按任意键退出。");
    }

    private static void DemonstrateLambdaPitfall()
    {
        var physics = new Physics();
        var first = 1;
        var second = 2;

        physics.EntityFell += _ => Console.Write(first);
        var before = physics.FellListeners;
        physics.EntityFell -= _ => Console.Write(second);

        Console.WriteLine("  先看一个常见坑：用 lambda 订阅，却用另一个「长得一样」的 lambda 注销。");
        Console.WriteLine($"    订阅后 {before} 个，注销后 {physics.FellListeners} 个"
                          + "（两个闭包不是同一个委托实例，-= 匹配不上）");
        Console.WriteLine("    结论：lambda 订阅时一定要先把委托存到字段里，注销时用同一个。");
        Console.WriteLine();
    }
}
