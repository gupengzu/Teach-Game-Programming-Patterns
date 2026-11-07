using System;
using System.Diagnostics;
using System.Threading;

namespace FlyweightPattern.Step3;

// 地形享元：所有字段只读，创建后不可修改。
// 共享对象一旦可变，改一处就会影响所有引用它的区块。
internal sealed class Terrain
{
    public Terrain(string name, char symbol, int movementCost, bool isWater, string texture, ConsoleColor color)
    {
        Name = name;
        Symbol = symbol;
        MovementCost = movementCost;
        IsWater = isWater;
        Texture = texture;
        Color = color;
    }

    public string Name { get; }

    public char Symbol { get; }

    public int MovementCost { get; }

    public bool IsWater { get; }

    public string Texture { get; }

    public ConsoleColor Color { get; }
}

// 这些实例的生命周期由目录统一持有，不需要每个区块各自 new 一份。
internal static class TerrainCatalog
{
    public static readonly Terrain Grass = new("草地", ',', 1, false, "草原纹理", ConsoleColor.Green);
    public static readonly Terrain Hill = new("丘陵", '^', 3, false, "丘陵纹理", ConsoleColor.DarkYellow);
    public static readonly Terrain River = new("河流", '~', 2, true, "河水纹理", ConsoleColor.Cyan);
    public static readonly Terrain Mountain = new("山地", '#', 5, false, "山岩纹理", ConsoleColor.Gray);

    public static readonly Terrain[] All = { Grass, Hill, River, Mountain };
}

// 网格里存的是 Terrain 引用，而不是地形枚举。
// World 不再知道「草地走多快」「河流能不能过船」这些细节。
internal sealed class World
{
    public const int Width = 24;
    public const int Height = 14;

    private readonly Terrain[,] _tiles = new Terrain[Width, Height];

    public Terrain GetTile(int x, int y) => _tiles[x, y];

    public void Generate(Random random)
    {
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var roll = random.Next(100);
                _tiles[x, y] = roll switch
                {
                    < 12 => TerrainCatalog.Hill,
                    < 17 => TerrainCatalog.Mountain,
                    _ => TerrainCatalog.Grass,
                };
            }
        }

        // 放一条蜿蜒的河
        var riverX = random.Next(3, Width - 3);
        for (var y = 0; y < Height; y++)
        {
            _tiles[riverX, y] = TerrainCatalog.River;
            if (random.Next(3) == 0)
            {
                riverX = Math.Clamp(riverX + random.Next(-1, 2), 1, Width - 2);
            }
        }
    }
}

internal static class Program
{
    private const int Width = World.Width;
    private const int Height = World.Height;

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "享元模式 · 3 地形区块";

        var random = new Random(20261107);
        var world = new World();
        world.Generate(random);

        var playerX = Width / 2;
        var playerY = Height / 2;

        Draw(world, playerX, playerY, "方向键移动，R 重新生成地图，B 运行微基准，Esc 退出。");

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
                case ConsoleKey.UpArrow:
                    playerY = Math.Max(0, playerY - 1);
                    break;
                case ConsoleKey.DownArrow:
                    playerY = Math.Min(Height - 1, playerY + 1);
                    break;
                case ConsoleKey.LeftArrow:
                    playerX = Math.Max(0, playerX - 1);
                    break;
                case ConsoleKey.RightArrow:
                    playerX = Math.Min(Width - 1, playerX + 1);
                    break;
                case ConsoleKey.R:
                    world.Generate(random);
                    Draw(world, playerX, playerY, "地图已重新生成：仍然只有 4 个 Terrain 实例。");
                    continue;
                case ConsoleKey.B:
                    Console.Clear();
                    RunBenchmark();
                    Console.WriteLine();
                    Console.WriteLine("  按任意键返回地图。");
                    Console.ReadKey(intercept: true);
                    Draw(world, playerX, playerY, "微基准跑完了，回到地图。");
                    continue;
                default:
                    continue;
            }

            Draw(world, playerX, playerY, " ");
        }
    }

    private static void Draw(World world, int playerX, int playerY, string message)
    {
        Console.Clear();
        Console.WriteLine("享元模式 · 3 地形区块（没有明显身份的对象也要共享）");
        Console.WriteLine(new string('=', 68));
        Console.WriteLine("  方向键移动    R 重新生成地图    B 运行微基准    Esc 退出");
        Console.WriteLine();

        for (var y = 0; y < Height; y++)
        {
            Console.Write("  ");
            for (var x = 0; x < Width; x++)
            {
                if (x == playerX && y == playerY)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("@ ");
                    continue;
                }

                var tile = world.GetTile(x, y);
                Console.ForegroundColor = tile.Color;
                Console.Write(tile.Symbol);
                Console.Write(' ');
            }

            Console.WriteLine();
        }

        Console.ResetColor();

        var current = world.GetTile(playerX, playerY);
        Console.WriteLine();
        Console.WriteLine($"  当前位置 ({playerX},{playerY})：{current.Name}");
        Console.WriteLine($"  移动开销 {current.MovementCost}    水域：{(current.IsWater ? "是" : "否")}"
                          + $"    渲染纹理：{current.Texture}");
        Console.WriteLine();
        Console.WriteLine($"  地图 {Width * Height} 个格子，只用了 {TerrainCatalog.All.Length} 个 Terrain 实例。");
        Console.WriteLine();
        Console.WriteLine($"  {message}");
    }

    private static void RunBenchmark()
    {
        const int Count = 1_000_000;
        const int Rounds = 20;

        var random = new Random(42);
        var kinds = new TerrainKind[Count];
        var tiles = new Terrain[Count];

        for (var i = 0; i < Count; i++)
        {
            var kind = (TerrainKind)random.Next(4);
            kinds[i] = kind;
            tiles[i] = TerrainCatalog.All[(int)kind];
        }

        // 预热，避免把 JIT 编译时间算进结果
        long warmup = 0;
        for (var round = 0; round < 2; round++)
        {
            for (var i = 0; i < Count; i++)
            {
                warmup += EnumCost(kinds[i]) + tiles[i].MovementCost;
            }
        }

        var stopwatch = Stopwatch.StartNew();
        long enumSum = 0;
        for (var round = 0; round < Rounds; round++)
        {
            for (var i = 0; i < Count; i++)
            {
                enumSum += EnumCost(kinds[i]);
            }
        }

        stopwatch.Stop();
        var enumMs = stopwatch.Elapsed.TotalMilliseconds;

        stopwatch.Restart();
        long flyweightSum = 0;
        for (var round = 0; round < Rounds; round++)
        {
            for (var i = 0; i < Count; i++)
            {
                flyweightSum += tiles[i].MovementCost;
            }
        }

        stopwatch.Stop();
        var flyweightMs = stopwatch.Elapsed.TotalMilliseconds;

        var operations = (double)Count * Rounds;

        Console.WriteLine("  性能对照：1,000,000 个格子 × 20 轮，读取「移动开销」");
        Console.WriteLine(new string('-', 68));
        Console.WriteLine($"  枚举 + switch ：{enumMs,8:F1} ms   约 {enumMs * 1_000_000 / operations:F2} ns/次");
        Console.WriteLine($"  享元对象引用  ：{flyweightMs,8:F1} ms   约 {flyweightMs * 1_000_000 / operations:F2} ns/次");
        Console.WriteLine();
        Console.WriteLine($"  （校验和：枚举 {enumSum} / 享元 {flyweightSum} / 预热 {warmup}）");
        Console.WriteLine();
        Console.WriteLine("  结果会随机器、JIT 和内存布局变化。这里用的是连续数组，");
        Console.WriteLine("  已经是对指针追逐最友好的情况；二维网格随机访问时，");
        Console.WriteLine("  享元的间接跳转更容易触发缓存不命中。先测量，再决定。");
    }

    private enum TerrainKind
    {
        Grass,
        Hill,
        River,
        Mountain,
    }

    private static int EnumCost(TerrainKind kind) => kind switch
    {
        TerrainKind.Grass => 1,
        TerrainKind.Hill => 3,
        TerrainKind.River => 2,
        _ => 5,
    };
}
