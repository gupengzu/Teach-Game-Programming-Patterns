using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ObserverPattern.Step3;

internal sealed class Player
{
    public event Action<int>? Damaged;

    public int Health { get; private set; } = 100;

    public int ListenerCount => Damaged?.GetInvocationList().Length ?? 0;

    public void TakeDamage(int amount)
    {
        Health -= amount;
        Damaged?.Invoke(amount);
    }
}

// 一个会被反复开关的 UI 面板：打开时订阅，关闭时销毁。
//   unsubscribe = true  ：Dispose 里记得 -=（正确姿势）
//   unsubscribe = false ：只从界面移除，忘了 -=（失效监听者）
internal sealed class HealthPanel : IDisposable
{
    public static int NotifiedCount;

    private readonly Player _player;
    private readonly bool _unsubscribe;

    public HealthPanel(Player player, bool unsubscribe)
    {
        _player = player;
        _unsubscribe = unsubscribe;
        player.Damaged += OnDamaged;
    }

    public int Updates { get; private set; }

    public void Dispose()
    {
        if (_unsubscribe)
        {
            _player.Damaged -= OnDamaged;
        }
    }

    private void OnDamaged(int amount)
    {
        Updates++;
        NotifiedCount++;
    }
}

internal sealed class Demo
{
    private readonly Player _player = new();
    private readonly List<HealthPanel> _openPanels = new();

    public int OpenPanels => _openPanels.Count;

    public int Listeners => _player.ListenerCount;

    public int Health => _player.Health;

    public void Open(bool unsubscribe) => _openPanels.Add(new HealthPanel(_player, unsubscribe));

    public void CloseAll()
    {
        foreach (var panel in _openPanels)
        {
            panel.Dispose();
        }

        _openPanels.Clear();
    }

    // 打开面板，立刻关闭，但只丢掉引用，不注销。
    public void OpenAndForget()
    {
        var panel = new HealthPanel(_player, unsubscribe: false);
        panel.Dispose();
    }

    public string Damage(int amount)
    {
        HealthPanel.NotifiedCount = 0;
        _player.TakeDamage(amount);
        return $"英雄受到 {amount} 点伤害，本次通知触达 {HealthPanel.NotifiedCount} 个面板"
               + $"（其中 {_openPanels.Count} 个还显示在屏幕上）。";
    }

    public void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

internal static class Program
{
    private const int PanelCount = 1000;

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "观察者模式 · 3 失效监听者";

        var demo = new Demo();

        if (Console.IsInputRedirected)
        {
            RunScripted();
            return;
        }

        Draw(demo, "O = 打开面板（会注销），X = 打开后忘记注销，C = 关闭全部，T = 泄漏对比，Esc = 退出。");

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
                case ConsoleKey.O:
                    demo.Open(unsubscribe: true);
                    Draw(demo, "打开了一个 HUD 面板（关闭时会注销）。");
                    break;

                case ConsoleKey.X:
                    demo.OpenAndForget();
                    Draw(demo, "打开面板后直接丢掉引用，没有注销 —— 看看监听者数量。");
                    break;

                case ConsoleKey.C:
                    demo.CloseAll();
                    Draw(demo, "关闭了所有面板；正常情况下监听者应该回到 0。");
                    break;

                case ConsoleKey.T:
                    RunScripted();
                    Draw(demo, "泄漏对比已跑完。");
                    break;

                case ConsoleKey.G:
                    demo.Collect();
                    Draw(demo, "已强制 GC 一次。");
                    break;
            }
        }
    }

    private static void Draw(Demo demo, string message)
    {
        Console.Clear();
        Console.WriteLine("观察者模式 · 3 失效监听者（注销与生命周期）");
        Console.WriteLine(new string('=', 64));
        Console.WriteLine("  O = 打开面板    X = 打开后忘记注销    C = 关闭全部");
        Console.WriteLine("  T = 1000 次泄漏对比    G = 强制 GC    Esc = 退出");
        Console.WriteLine();
        Console.WriteLine($"  打开的面板：{demo.OpenPanels} 个");
        Console.WriteLine($"  仍然挂在事件上的监听者：{demo.Listeners} 个");
        Console.WriteLine($"  英雄生命：{demo.Health}");
        Console.WriteLine();
        Console.WriteLine($"  {message}");
        Console.WriteLine();
        Console.WriteLine("  提示：事件持有订阅者的强引用。不注销，面板就永远不会被回收。");
    }

    private static void RunScripted()
    {
        Console.WriteLine("观察者模式 · 3 失效监听者（注销与生命周期）");
        Console.WriteLine(new string('=', 64));
        Console.WriteLine("  同一份 UI 代码，两种关闭方式，跑 1000 次。");
        Console.WriteLine();

        Console.WriteLine("  场景 A：Dispose 里记得注销");
        var a = RunLeakTest(unsubscribe: true);
        Print(a);

        Console.WriteLine();
        Console.WriteLine("  场景 B：只关闭界面，忘记注销");
        var b = RunLeakTest(unsubscribe: false);
        Print(b);

        Console.WriteLine();
        Console.WriteLine("  同样关了 1000 次面板，A 干净收场，B 留下 1000 个失效监听者。");
        Console.WriteLine("  它们再也看不见，却还在接收每一次伤害通知 —— 这就是失效监听者问题。");
    }

    private static void Print((int listeners, int alive, int notified) result)
    {
        Console.WriteLine($"    打开并关闭 {PanelCount} 个面板后：");
        Console.WriteLine($"      残留监听者：{result.listeners} 个");
        Console.WriteLine($"      强制 GC 后仍存活的面板：{result.alive} 个");
        Console.WriteLine($"      再受伤一次，收到通知的面板：{result.notified} 个");
    }

    private static (int listeners, int alive, int notified) RunLeakTest(bool unsubscribe)
    {
        var player = new Player();
        var references = new List<WeakReference>(PanelCount);
        CreateAndClosePanels(player, unsubscribe, references);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var alive = 0;
        foreach (var reference in references)
        {
            if (reference.IsAlive)
            {
                alive++;
            }
        }

        HealthPanel.NotifiedCount = 0;
        player.TakeDamage(1);

        return (player.ListenerCount, alive, HealthPanel.NotifiedCount);
    }

    // 单独一个方法：让面板的局部变量在返回时确实出栈，
    // 这样 GC 之后还活着的对象，只可能是被事件列表引用着的。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndClosePanels(Player player, bool unsubscribe, List<WeakReference> references)
    {
        for (var i = 0; i < PanelCount; i++)
        {
            var panel = new HealthPanel(player, unsubscribe);
            panel.Dispose();
            references.Add(new WeakReference(panel));
        }
    }
}
