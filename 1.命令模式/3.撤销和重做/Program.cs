using System;
using System.Collections.Generic;
using System.Threading;

namespace CommandPattern.Step3;

internal interface ICommand
{
    void Execute();

    void Undo();
}

internal sealed class Unit
{
    public int X { get; private set; }

    public int Y { get; private set; }

    public void MoveTo(int x, int y)
    {
        X = x;
        Y = y;
    }
}

// 注意这里和前面几步的差别：
// 命令被「绑定」到具体单位上，代表「这一次具体的移动」，
// 而不是一个可以在任何场景复用的「移动某物」操作。
internal sealed class MoveUnitCommand : ICommand
{
    private readonly Unit _unit;
    private readonly int _toX;
    private readonly int _toY;
    private int _beforeX;
    private int _beforeY;

    public MoveUnitCommand(Unit unit, int toX, int toY)
    {
        _unit = unit;
        _toX = toX;
        _toY = toY;
    }

    public void Execute()
    {
        // 只记住被修改的那一小部分状态，而不是给整个对象做快照
        _beforeX = _unit.X;
        _beforeY = _unit.Y;
        _unit.MoveTo(_toX, _toY);
    }

    public void Undo() => _unit.MoveTo(_beforeX, _beforeY);
}

internal sealed class CommandHistory
{
    private readonly List<ICommand> _commands = new();
    private int _cursor;

    public int Undoable => _cursor;

    public int Redoable => _commands.Count - _cursor;

    public void Execute(ICommand command)
    {
        // 撤销过几步之后又执行新命令：丢弃「当前指针」之后的分支
        if (_cursor < _commands.Count)
        {
            _commands.RemoveRange(_cursor, _commands.Count - _cursor);
        }

        command.Execute();
        _commands.Add(command);
        _cursor = _commands.Count;
    }

    public bool Undo()
    {
        if (_cursor <= 0)
        {
            return false;
        }

        _cursor--;
        _commands[_cursor].Undo();
        return true;
    }

    public bool Redo()
    {
        if (_cursor >= _commands.Count)
        {
            return false;
        }

        _commands[_cursor].Execute();
        _cursor++;
        return true;
    }
}

internal static class Program
{
    private const int Size = 5;

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "命令模式 · 3 撤销和重做";

        var unit = new Unit();
        var history = new CommandHistory();

        Draw(unit, history, "用方向键移动单位");

        while (true)
        {
            if (!Console.KeyAvailable)
            {
                Thread.Sleep(15);
                continue;
            }

            var info = Console.ReadKey(intercept: true);
            var key = info.Key;
            var ctrl = (info.Modifiers & ConsoleModifiers.Control) != 0;

            if (key == ConsoleKey.Escape)
            {
                break;
            }

            if (ctrl && key == ConsoleKey.Z)
            {
                Draw(unit, history, history.Undo() ? "已撤销一步" : "没有可撤销的命令");
                continue;
            }

            if (ctrl && key == ConsoleKey.Y)
            {
                Draw(unit, history, history.Redo() ? "已重做一步" : "没有可重做的命令");
                continue;
            }

            var toX = unit.X;
            var toY = unit.Y;

            switch (key)
            {
                case ConsoleKey.UpArrow: toY--; break;
                case ConsoleKey.DownArrow: toY++; break;
                case ConsoleKey.LeftArrow: toX--; break;
                case ConsoleKey.RightArrow: toX++; break;
                default: continue;
            }

            if (toX < 0 || toX >= Size || toY < 0 || toY >= Size)
            {
                Draw(unit, history, "已经到边界了");
                continue;
            }

            // 每一次操作都创建一个新命令实例：它代表「这一次具体的移动」
            history.Execute(new MoveUnitCommand(unit, toX, toY));
            Draw(unit, history, $"移动到 ({toX},{toY})");
        }

        Console.WriteLine();
        Console.WriteLine("已退出。");
    }

    private static void Draw(Unit unit, CommandHistory history, string message)
    {
        Console.Clear();
        Console.WriteLine("命令模式 · 3 撤销和重做");
        Console.WriteLine(new string('=', 50));
        Console.WriteLine("  方向键移动    Ctrl+Z 撤销    Ctrl+Y 重做    Esc 退出");
        Console.WriteLine();

        for (var y = 0; y < Size; y++)
        {
            Console.Write("   ");
            for (var x = 0; x < Size; x++)
            {
                Console.Write(unit.X == x && unit.Y == y ? " ● " : " · ");
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine($"  可撤销 {history.Undoable} 步    可重做 {history.Redoable} 步");
        Console.WriteLine($"  {message}");
    }
}
