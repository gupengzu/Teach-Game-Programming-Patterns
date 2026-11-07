using System;
using System.Collections.Generic;

namespace FlyweightPattern.Step0;

// 真实项目里，Mesh 和 Texture 是几百 KB 到几 MB 的资源。
// 为了让示例跑得快，这里把体积缩小了；但「每棵树各存一份」的行为完全一样。
internal sealed class Mesh
{
    public Mesh(string name, int bytes)
    {
        Name = name;
        Data = new byte[bytes];
    }

    public string Name { get; }

    public byte[] Data { get; }
}

internal sealed class Texture
{
    public Texture(string name, int bytes)
    {
        Name = name;
        Data = new byte[bytes];
    }

    public string Name { get; }

    public byte[] Data { get; }
}

internal sealed class Tree
{
    public const int MeshBytes = 8 * 1024;
    public const int TextureBytes = 4 * 1024;

    private readonly Mesh _mesh;
    private readonly Texture _bark;
    private readonly Texture _leaves;

    public Tree(string species, float x, float y, float height, float thickness, string barkTint, string leafTint)
    {
        Species = species;
        _mesh = new Mesh($"{species}网格", MeshBytes);
        _bark = new Texture($"{species}树皮", TextureBytes);
        _leaves = new Texture($"{species}树叶", TextureBytes);
        X = x;
        Y = y;
        Height = height;
        Thickness = thickness;
        BarkTint = barkTint;
        LeafTint = leafTint;
    }

    public string Species { get; }

    public float X { get; }

    public float Y { get; }

    public float Height { get; }

    public float Thickness { get; }

    public string BarkTint { get; }

    public string LeafTint { get; }
}

internal static class Program
{
    private const int TreeCount = 2000;

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "享元模式 · 0 改进前";

        var speciesPool = new[] { "铁杉", "橡树", "枫树" };
        var tints = new[] { "深绿", "浅绿", "金黄" };
        var random = new Random(20261107);

        Console.WriteLine("享元模式 · 0 改进前（每棵树独立建模）");
        Console.WriteLine(new string('=', 58));
        Console.WriteLine($"  森林规模：{TreeCount} 棵树");
        Console.WriteLine($"  每棵树独占：网格 {Tree.MeshBytes / 1024} KB"
                          + $" + 树皮纹理 {Tree.TextureBytes / 1024} KB"
                          + $" + 树叶纹理 {Tree.TextureBytes / 1024} KB");
        Console.WriteLine();

        var before = GC.GetTotalMemory(forceFullCollection: true);

        var forest = new List<Tree>(TreeCount);
        for (var i = 0; i < TreeCount; i++)
        {
            forest.Add(new Tree(
                speciesPool[random.Next(speciesPool.Length)],
                random.NextSingle() * 1000,
                random.NextSingle() * 1000,
                4 + random.NextSingle() * 8,
                0.3f + random.NextSingle() * 0.4f,
                tints[random.Next(tints.Length)],
                tints[random.Next(tints.Length)]));
        }

        var after = GC.GetTotalMemory(forceFullCollection: true);
        var usedMb = (after - before) / 1024.0 / 1024.0;
        var expectedMb = TreeCount * (Tree.MeshBytes + Tree.TextureBytes * 2) / 1024.0 / 1024.0;

        Console.WriteLine($"  加载完成：{forest.Count} 棵树，{forest.Count * 3} 份资源");
        Console.WriteLine($"  理论资源体积：{expectedMb:F1} MB");
        Console.WriteLine($"  实际托管堆增长：{usedMb:F1} MB");
        Console.WriteLine();
        Console.WriteLine("  问题不在「树」本身：这 2000 棵树里，网格和纹理几乎全是重复的。");
        Console.WriteLine("  按真实资源（网格 500 KB + 两张纹理各 1 MB）计算：");
        Console.WriteLine($"  2000 × 2.5 MB = {2000 * 2.5:F0} MB，光是一片森林就装不下。");
        Console.WriteLine();
        Console.WriteLine("  按任意键退出。");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
