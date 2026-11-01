using System;
using System.Collections.Generic;
using System.Threading;

namespace CommandPattern.Step1;

// 命令的抽象：一个只做一件事、不返回任何值的对象。
// 当你看到「只有一个方法的接口」时，多半就是命令模式。
internal interface ICommand
{
    void Execute();
}

internal sealed class JumpCommand : ICommand
{
    public void Execute() => Console.WriteLine("  [命令] JumpCommand       → 玩家跳了起来");
}

internal sealed class FireCommand : ICommand
{
    public void Execute() => Console.WriteLine("  [命令] FireCommand       → 砰！开火");
}

internal sealed class SwapWeaponCommand : ICommand
{
    private bool _rifle;

    public void Execute()
    {
        _rifle = !_rifle;
        Console.WriteLine($"  [命令] SwapWeaponCommand → 切换为{(_rifle ? "步枪" : "手枪")}");
    }
}

// 空对象模式：用「什么都不做的命令」替代 null，
// 这样绑定表里不需要为每个按键单独判断 null。
internal sealed class DoNothingCommand : ICommand
{
    public void Execute() => Console.WriteLine("  [命令] DoNothingCommand  → 什么也没发生");
}

internal sealed class InputHandler
{
    private readonly Dictionary<ConsoleKey, ICommand> _bindings = new();

    public IReadOnlyDictionary<ConsoleKey, ICommand> Bindings => _bindings;

    // ★ 改进点：按键和命令之间隔了一张表，换键只是换表的映射，不用改源码
    public void Bind(ConsoleKey key, ICommand command) => _bindings[key] = command;

    public void HandleInput(ConsoleKey key)
    {
        if (_bindings.TryGetValue(key, out var command))
        {
            command.Execute();
        }
    }
}

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "命令模式 · 1 命令对象";

        var input = new InputHandler();
        var jump = new JumpCommand();

        input.Bind(ConsoleKey.X, jump);
        input.Bind(ConsoleKey.Y, new FireCommand());
        input.Bind(ConsoleKey.A, new SwapWeaponCommand());
        input.Bind(ConsoleKey.B, new DoNothingCommand());

        Console.WriteLine("命令模式 · 1 命令对象（输入与动作解耦）");
        Console.WriteLine(new string('=', 54));
        PrintBindings(input);

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

            if (key == ConsoleKey.F1)
            {
                RemapJump(input, jump);
                continue;
            }

            input.HandleInput(key);
        }
    }

    // ★ 运行时改键：把同一个 JumpCommand 对象从 X 挪到 Z，
    //   一行源码都不用改，这正是「输入层与动作层解耦」带来的好处。
    private static void RemapJump(InputHandler input, ICommand jump)
    {
        input.Bind(ConsoleKey.X, new DoNothingCommand());
        input.Bind(ConsoleKey.Z, jump);

        Console.WriteLine();
        Console.WriteLine("  [重绑] 跳跃键已从 X 改为 Z —— 源码一行都没动");
        Console.WriteLine();
        PrintBindings(input);
    }

    private static void PrintBindings(InputHandler input)
    {
        Console.WriteLine("  按键 → 命令 的当前绑定：");
        foreach (var kv in input.Bindings)
        {
            Console.WriteLine($"    {kv.Key,-8} → {kv.Value.GetType().Name}");
        }

        Console.WriteLine();
        Console.WriteLine("  F1 = 把跳跃键从 X 改绑到 Z（运行时改键）    Esc = 退出");
        Console.WriteLine();
    }
}
