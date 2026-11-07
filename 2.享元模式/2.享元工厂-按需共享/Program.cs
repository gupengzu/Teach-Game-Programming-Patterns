using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FlyweightPattern.Step2;

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

internal sealed class Tree
{
    public Tree(TreeModel model, float x, float y, float height, float thickness, string barkTint, string leafTint)
    {
        Model = model;
        X = x;
        Y = y;
        Height = height;
        Thickness = thickness;
        BarkTint = barkTint;
        LeafTint = leafTint;
    }

    public TreeModel Model { get; }

    public float X { get; }

    public float Y { get; }

    public float Height { get; }

    public float Thickness { get; }

    public string BarkTint { get; }

    public string LeafTint { get; }
}

// ★ 工厂方法 + 缓存：第一次遇到某个树种时创建模型，之后直接返回同一个实例。
//   书里提到的「对象池」在这里就是这张 Dictionary。
internal sealed class TreeModelFactory
{
    private readonly Dictionary<string, TreeModel> _models = new(StringComparer.Ordinal);

    public int Created { get; private set; }

    public int Hits { get; private set; }

    public IReadOnlyDictionary<string, TreeModel> Models => _models;

    public TreeModel Get(string species)
    {
        if (_models.TryGetValue(species, out var model))
        {
            Hits++;
            return model;
        }

        model = new TreeModel(species);
        _models.Add(species, model);
        Created++;
        return model;
    }
}

internal sealed class Forest
{
    private readonly List<Tree> _trees = new();
    private readonly TreeModelFactory _factory;

    public Forest(TreeModelFactory factory) => _factory = factory;

    public int Count => _trees.Count;

    public Tree Plant(string species, Random random)
    {
        var model = _factory.Get(species);
        var tree = new Tree(
            model,
            random.NextSingle() * 1000,
            random.NextSingle() * 1000,
            4 + random.NextSingle() * 8,
            0.3f + random.NextSingle() * 0.4f,
            "深绿",
            "浅绿");

        _trees.Add(tree);
        return tree;
    }

    public void Clear() => _trees.Clear();

    public int CountOf(string species)
    {
        var count = 0;
        foreach (var tree in _trees)
        {
            if (tree.Model.Species == species)
            {
                count++;
            }
        }

        return count;
    }
}

internal static class Program
{
    private static readonly string[] Species = { "铁杉", "橡树", "枫树", "白桦", "云杉" };

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "享元模式 · 2 享元工厂";

        var random = new Random(20261107);
        var factory = new TreeModelFactory();
        var forest = new Forest(factory);

        Draw(forest, factory, "空格种一棵，B 一次种 100 棵，C 清空森林。");

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
                {
                    var species = Species[random.Next(Species.Length)];
                    var createdBefore = factory.Created;
                    var tree = forest.Plant(species, random);
                    var reused = factory.Created == createdBefore;
                    Draw(forest, factory,
                        $"种下 {species} #{forest.Count}，模型 #{RuntimeHelpers.GetHashCode(tree.Model):X4}"
                        + $"（{(reused ? "复用缓存" : "首次创建")}）");
                    break;
                }

                case ConsoleKey.B:
                {
                    var createdBefore = factory.Created;
                    for (var i = 0; i < 100; i++)
                    {
                        forest.Plant(Species[random.Next(Species.Length)], random);
                    }

                    Draw(forest, factory,
                        $"一次种下 100 棵，新建模型 {factory.Created - createdBefore} 个。");
                    break;
                }

                case ConsoleKey.C:
                    forest.Clear();
                    Draw(forest, factory, "森林已清空；模型缓存保留，下次种植继续复用。");
                    break;
            }
        }
    }

    private static void Draw(Forest forest, TreeModelFactory factory, string message)
    {
        Console.Clear();
        Console.WriteLine("享元模式 · 2 享元工厂（按需共享）");
        Console.WriteLine(new string('=', 64));
        Console.WriteLine("  空格 = 种一棵    B = 一次种 100 棵    C = 清空森林    Esc = 退出");
        Console.WriteLine();
        Console.WriteLine($"  树木：{forest.Count} 棵");
        Console.WriteLine($"  模型实例：{factory.Created} 个    缓存命中：{factory.Hits} 次");

        var sharedKb = factory.Created * (TreeModel.MeshBytes + TreeModel.TextureBytes * 2) / 1024.0;
        var naiveKb = forest.Count * (TreeModel.MeshBytes + TreeModel.TextureBytes * 2) / 1024.0;
        Console.WriteLine($"  共享资源：{sharedKb:F0} KB（只跟模型个数有关）");
        Console.WriteLine($"  若每棵独立建模：{naiveKb:F0} KB");
        Console.WriteLine();

        Console.WriteLine("  模型表：");
        if (factory.Created == 0)
        {
            Console.WriteLine("    （空）");
        }

        foreach (var pair in factory.Models)
        {
            Console.WriteLine($"    {pair.Key,-4} × {forest.CountOf(pair.Key),-5} 棵"
                              + $"   模型 #{RuntimeHelpers.GetHashCode(pair.Value):X4}");
        }

        Console.WriteLine();
        Console.WriteLine($"  {message}");
        Console.WriteLine();
        Console.WriteLine("  提示：模型实例数只跟「树种」有关，和你种多少棵树无关。");
    }
}
