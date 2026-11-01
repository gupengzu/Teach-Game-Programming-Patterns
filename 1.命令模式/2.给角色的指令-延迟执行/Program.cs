using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace CommandPattern.Step2;

// 与上一步的区别：命令不再去「找」要操作的对象，
// 而是由调用方把角色作为参数传进来。
internal interface ICommand
{
    void Execute(Actor actor);
}

internal sealed class Actor
{
    public Actor(string name) => Name = name;

    public string Name { get; }

    public void Jump() => Console.WriteLine($"  · {Name} 跳了起来");

    public void Fire() => Console.WriteLine($"  · {Name} 开火");
}

internal sealed class JumpCommand : ICommand
{
    public void Execute(Actor actor) => actor.Jump();
}

internal sealed class FireCommand : ICommand
{
    public void Execute(Actor actor) => actor.Fire();
}

internal sealed class InputHandler
{
    private readonly ICommand _jump = new JumpCommand();
    private readonly ICommand _fire = new FireCommand();

    // ★ 关键变化：从「直接执行」改成「返回命令」。
    //   此刻还不知道命令要作用于哪个角色，所以把执行时机往后推迟。
    public ICommand? HandleInput(ConsoleKey key) => key switch
    {
        ConsoleKey.X => _jump,
        ConsoleKey.Y => _fire,
        _ => null,
    };
}

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "命令模式 · 2 给角色的指令";

        var actors = new List<Actor> { new Actor("玩家"), new Actor("小怪A") };
        var current = actors[0];
        var input = new InputHandler();
        var aiTimer = Stopwatch.StartNew();

        Console.WriteLine("命令模式 · 2 给角色的指令（延迟执行 + 参数化）");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("  X = 跳跃    Y = 开火    Tab = 切换当前角色    Esc = 退出");
        Console.WriteLine();
        Console.WriteLine("  命令不再知道自己作用于谁：HandleInput 只返回命令，");
        Console.WriteLine("  由主循环决定交给哪个角色执行。小怪A 每 3 秒也会由 AI 产生一条命令。");
        Console.WriteLine();
        Console.WriteLine($"  当前角色：{current.Name}");
        Console.WriteLine();

        while (true)
        {
            // AI 与玩家共用同一条命令流，只是「生产者」不同
            if (aiTimer.Elapsed.TotalSeconds >= 3)
            {
                aiTimer.Restart();
                new FireCommand().Execute(actors[1]);
            }

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

            if (key == ConsoleKey.Tab)
            {
                var index = actors.IndexOf(current);
                current = actors[(index + 1) % actors.Count];
                Console.WriteLine($"\n  [切换] 当前角色 → {current.Name}\n");
                continue;
            }

            // ★ 命令在这里才被执行，并且作用于「当前」角色
            var command = input.HandleInput(key);
            command?.Execute(current);
        }
    }
}
