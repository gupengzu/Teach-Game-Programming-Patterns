using System;
using System.Threading;

namespace CommandPattern.Step0;

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "命令模式 · 0 改进前";

        var player = new Player();

        Console.WriteLine("命令模式 · 0 改进前（硬编码输入）");
        Console.WriteLine(new string('=', 54));
        Console.WriteLine("  X = 跳跃    Y = 开火    A = 换武器    B = 无效动作    Esc = 退出");
        Console.WriteLine();
        Console.WriteLine("  四个按键直接写死在 HandleInput 里，玩家无法改键。");
        Console.WriteLine();

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

            HandleInput(key, player);
        }

        Console.WriteLine();
        Console.WriteLine("已退出。下一步：把动作封装成命令对象，见 1.命令对象-按键重映射。");
    }

    // ★ 全章要解决的问题就在这四行：
    //   按键 → 动作 的绑定被写死，既不能在运行时改键，
    //   也无法让别的角色、AI 或网络消息复用同一套动作。
    private static void HandleInput(ConsoleKey key, Player player)
    {
        if (key == ConsoleKey.X) player.Jump();
        else if (key == ConsoleKey.Y) player.FireGun();
        else if (key == ConsoleKey.A) player.SwapWeapon();
        else if (key == ConsoleKey.B) player.LurchIneffectively();
    }
}

internal sealed class Player
{
    private int _actionCount;
    private bool _rifle;

    public void Jump()
    {
        _actionCount++;
        Console.WriteLine($"  [动作] 跳跃            （第 {_actionCount} 次动作）");
    }

    public void FireGun()
    {
        _actionCount++;
        Console.WriteLine($"  [动作] 开火            （武器：{(_rifle ? "步枪" : "手枪")}）");
    }

    public void SwapWeapon()
    {
        _rifle = !_rifle;
        Console.WriteLine($"  [动作] 换武器 → {(_rifle ? "步枪" : "手枪")}");
    }

    public void LurchIneffectively()
    {
        Console.WriteLine("  [动作] 无效地晃了一下……（什么也没发生）");
    }
}
