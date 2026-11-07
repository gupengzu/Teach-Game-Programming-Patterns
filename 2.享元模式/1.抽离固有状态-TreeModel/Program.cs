using System;
using System.Collections.Generic;

namespace FlyweightPattern.Step1;

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

// ★ 享元的本体：所有「铁杉」共享同一份网格和纹理。
//   这些字段与「树长在哪里」无关，所以书里称之为「固有状态」。
internal sealed class TreeModel
{
    public const int MeshBytes = 8 * 1024;
    public const int TextureBytes = 4 * 1024;

    public TreeModel(string species)
    {
        Species = species;
        Mesh = new Mesh($"{species}网格", MeshBytes);
        Bark = new Texture($"{species}树皮", TextureBytes);
        Leaves = new Texture($"{species}树叶", TextureBytes);
    }

    public string Species { get; }

    public Mesh Mesh { get; }

    public Texture Bark { get; }

    public Texture Leaves { get; }
}

// 每棵树只保留「变化状态」：位置、大小、颜色，以及一个指向共享模型的引用。
internal sealed class Tree
{
    private readonly TreeModel _model;

    public Tree(TreeModel model, float x, float y, float height, float thickness, string barkTint, string leafTint)
    {
        _model = model;
        X = x;
        Y = y;
        Height = height;
        Thickness = thickness;
        BarkTint = barkTint;
        LeafTint = leafTint;
    }

    public TreeModel Model => _model;

    public float X { get; }

    public float Y { get; }

    public float Height { get; }

    public float Thickness { get; }

    public string BarkTint { get; }

    public string LeafTint { get; }

    public void Render() =>
        Console.WriteLine($"  · {_model.Species} @ ({X:F0},{Y:F0})  模型：{_model.Mesh.Name}  "
                          + $"树皮：{BarkTint}  树叶：{LeafTint}");
}

internal static class Program
{
    private const int TreeCount = 2000;

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "享元模式 · 1 抽离固有状态";

        var speciesPool = new[] { "铁杉", "橡树", "枫树" };
        var tints = new[] { "深绿", "浅绿", "金黄" };
        var random = new Random(20261107);

        Console.WriteLine("享元模式 · 1 抽离固有状态（TreeModel）");
        Console.WriteLine(new string('=', 62));
        Console.WriteLine($"  森林规模：{TreeCount} 棵树");
        Console.WriteLine("  做法：网格和纹理只做 3 份，每棵树持有一个 TreeModel 引用。");
        Console.WriteLine();

        var before = GC.GetTotalMemory(forceFullCollection: true);

        // 每种树只创建一个模型实例
        var models = new Dictionary<string, TreeModel>();
        foreach (var species in speciesPool)
        {
            models[species] = new TreeModel(species);
        }

        var forest = new List<Tree>(TreeCount);
        for (var i = 0; i < TreeCount; i++)
        {
            var species = speciesPool[random.Next(speciesPool.Length)];
            forest.Add(new Tree(
                models[species],
                random.NextSingle() * 1000,
                random.NextSingle() * 1000,
                4 + random.NextSingle() * 8,
                0.3f + random.NextSingle() * 0.4f,
                tints[random.Next(tints.Length)],
                tints[random.Next(tints.Length)]));
        }

        var after = GC.GetTotalMemory(forceFullCollection: true);
        var usedMb = (after - before) / 1024.0 / 1024.0;
        var unsharedMb = TreeCount * (TreeModel.MeshBytes + TreeModel.TextureBytes * 2) / 1024.0 / 1024.0;

        Console.WriteLine($"  模型实例：{models.Count} 个（网格 {models.Count} 份，纹理 {models.Count * 2} 份）");
        Console.WriteLine($"  树木实例：{forest.Count} 个，全部引用这 {models.Count} 个模型");
        Console.WriteLine($"  若每棵树独立建模：{unsharedMb:F1} MB");
        Console.WriteLine($"  共享后托管堆增长：{usedMb:F2} MB");
        Console.WriteLine();
        Console.WriteLine("  随机挑三棵树看看：");
        for (var i = 0; i < 3; i++)
        {
            forest[random.Next(forest.Count)].Render();
        }

        Console.WriteLine();
        Console.WriteLine("  树的种类只有 3 种，却种了 2000 棵——这正是享元模式最典型的场景。");
        Console.WriteLine("  按任意键退出。");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
