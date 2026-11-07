# 2. 享元模式

> **一句话定义**
>
> 享元模式，是把对象的状态拆成「所有实例共享的固有状态」和「每个实例独有的变化状态」；固有状态只存一份，实例只保留自己的差异。它用共享换内存，通常也换带宽。

本章用一个可以本地运行的小项目，把《游戏编程模式》第二章的演进过程完整走一遍。

完整工程地址：[https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.享元模式](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.%E4%BA%AB%E5%85%83%E6%A8%A1%E5%BC%8F)

> 运行环境与通用运行方式见[仓库根目录的「环境准备与运行方式」](../README.md#环境准备与运行方式)。

**目录**

- [0. 改进前：每棵树都带着一整套模型](#0-改进前每棵树都带着一整套模型)
- [1. 改进一：拆开固有状态与变化状态](#1-改进一拆开固有状态与变化状态)
- [2. 改进二：用工厂按需共享](#2-改进二用工厂按需共享)
- [3. 改进三：没有明显身份的对象——地形区块](#3-改进三没有明显身份的对象地形区块)
- [4. 小结](#4-小结)

---

## 0. 改进前：每棵树都带着一整套模型

想象一片森林：成千上万棵树，每棵都有自己的网格、树皮纹理、树叶纹理，还有位置、高度、粗细和颜色。

最直觉的写法，就是把这些东西全放进一个 `Tree` 类：

```csharp
internal sealed class Tree
{
    private readonly Mesh _mesh;
    private readonly Texture _bark;
    private readonly Texture _leaves;
    private readonly float _x;
    private readonly float _y;
    private readonly float _height;
    private readonly float _thickness;

    public Tree(string species, float x, float y, float height, float thickness)
    {
        // 每棵树都 new 一份网格和两张纹理
        _mesh = new Mesh($"{species}网格", 8 * 1024);
        _bark = new Texture($"{species}树皮", 4 * 1024);
        _leaves = new Texture($"{species}树叶", 4 * 1024);
        _x = x;
        _y = y;
        _height = height;
        _thickness = thickness;
    }
}
```

看起来没问题，直到你真的种下两千棵树。运行本章的 [0.改进前-每棵树独立建模](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.享元模式/0.改进前-每棵树独立建模)，控制台会打印：

```text
森林规模：2000 棵树
每棵树独占：网格 8 KB + 树皮纹理 4 KB + 树叶纹理 4 KB

加载完成：2000 棵树，6000 份资源
理论资源体积：31.2 MB
实际托管堆增长：31.9 MB
```

示例为了跑得快，已经把资源缩小到了 KB 级。按真实项目算——一个网格 500 KB、两张纹理各 1 MB——两千棵树就是 **5000 MB**，光是一片森林就装不下。

内存只是第一道坎。就算装得下，每帧还要把这些数据从 CPU 送到 GPU；六千份重复资源挤在总线上，帧率一样会垮。

但停下来想一想：这 2000 棵树里，真正不同的到底是什么？

- 网格：只有三种树，却存了 2000 份，而且大量重复；
- 纹理：同理；
- 位置、高度、粗细、颜色：每棵树确实不一样。

也就是说，一个 `Tree` 对象里，大部分数据是**大家一样的**，只有少部分数据是**每个实例独有的**。享元模式要做的，就是把这两类数据明确地切开。

> 完整代码：[0.改进前-每棵树独立建模](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.享元模式/0.改进前-每棵树独立建模)

---

## 1. 改进一：拆开固有状态与变化状态

思路：把「和这棵树长在哪里无关」的数据抽出来，做成一份可以被无数棵树共享的模型。

### 第一步：抽出 TreeModel

```csharp
internal sealed class TreeModel
{
    public TreeModel(string species)
    {
        Species = species;
        Mesh = new Mesh($"{species}网格", 8 * 1024);
        Bark = new Texture($"{species}树皮", 4 * 1024);
        Leaves = new Texture($"{species}树叶", 4 * 1024);
    }

    public string Species { get; }
    public Mesh Mesh { get; }
    public Texture Bark { get; }
    public Texture Leaves { get; }
}
```

整个游戏只需要为每个树种创建一个 `TreeModel`。书里把这类数据称为 **固有状态**（intrinsic state），或者更直白一点——**上下文无关**的部分：它不关心这棵树具体是哪一棵、站在哪里。

### 第二步：让 Tree 只保留差异

```csharp
internal sealed class Tree
{
    private readonly TreeModel _model;   // 指向共享模型
    private readonly float _x;
    private readonly float _y;
    private readonly float _height;
    private readonly float _thickness;

    public Tree(TreeModel model, float x, float y, float height, float thickness)
    {
        _model = model;
        _x = x;
        _y = y;
        _height = height;
        _thickness = thickness;
    }
}
```

留在 `Tree` 里的，是 **变化状态**（extrinsic state）：位置、大小、颜色这些每个实例独有的东西。

两者可以这样对照：

| 数据 | 书里的叫法 | 例子 | 存几份 |
| ---- | ---------- | ---- | ------ |
| 固有状态 | intrinsic state，上下文无关 | 网格、树皮纹理、树叶纹理 | 每个树种一份 |
| 变化状态 | extrinsic state，实例独有 | 位置、高度、粗细、颜色 | 每个实例一份 |

### 效果

换成共享模型之后，同样的 2000 棵树：

```text
模型实例：3 个（网格 3 份，纹理 6 份）
树木实例：2000 个，全部引用这 3 个模型
若每棵树独立建模：31.2 MB
共享后托管堆增长：0.17 MB
```

从 31.2 MB 降到 0.17 MB，省掉了大约 **99.5%**。树越多，差距越大。

### 它和「类型对象模式」不是一回事

第一次看到 `TreeModel`，很容易联想到类型对象模式：两者都把状态委托给另一个类，都在实例之间分享数据。但意图不同：

- **类型对象**：为了减少需要定义的类，把「类型」本身变成对象；
- **享元**：纯粹为了效率，把「大家一样的数据」抽出来共享。

类型对象的分享是顺带的好处；享元的分享就是全部目的。

### 为什么显卡也喜欢这个思路

享元不只是省内存。共享的网格和纹理只需要上传一次，之后每个实例只送位置、大小、颜色这些差异数据，最后告诉 GPU「用同一份模型画这些实例」。

今天的 Direct3D 和 OpenGL 都直接支持这件事，叫**实例渲染**（instanced rendering）。所以有一种说法：享元模式可能是唯一一个有硬件直接支持的 GoF 设计模式。

但要说清楚边界：享元优化的是**内存和带宽**，不是绘制调用次数。树还是那么多棵，只是每棵变「轻」了。实例渲染能减少 API 调用，但那是图形 API 的功劳，不是享元本身。

> 完整代码：[1.抽离固有状态-TreeModel](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.享元模式/1.抽离固有状态-TreeModel)
>
> 运行后会打印模型实例数、树木实例数，以及共享前后的内存对比。

---

## 2. 改进二：用工厂按需共享

上一步的模型是提前写死的：三种树，建三个模型。但真实项目里，你往往不知道玩家会走到哪片地图、会加载哪些树种。

一开始就把所有享元都创建出来，既不现实，也浪费——很多资源可能整局都用不到。书里的建议是：**需要时再创建，但创建前先查表。**

### 工厂 + 缓存

```csharp
internal sealed class TreeModelFactory
{
    private readonly Dictionary<string, TreeModel> _models = new(StringComparer.Ordinal);

    public int Created { get; private set; }
    public int Hits { get; private set; }

    public TreeModel Get(string species)
    {
        // 已经有了：直接返回同一个实例
        if (_models.TryGetValue(species, out var model))
        {
            Hits++;
            return model;
        }

        // 第一次遇到：创建并登记
        model = new TreeModel(species);
        _models.Add(species, model);
        Created++;
        return model;
    }
}
```

这段代码里有两个关键词：

- **工厂方法**：调用方不再 `new TreeModel(...)`，而是向 `Get` 要一个模型，构造过程被藏起来了；
- **对象池**：`Dictionary` 就是那个池子，用来追踪「已经创建过哪些实例」。

种树时只跟工厂打交道：

```csharp
var model = _factory.Get(species);
var tree = new Tree(model, x, y, height, thickness);
```

运行本章的 [2.享元工厂-按需共享](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.享元模式/2.享元工厂-按需共享)：

- 按 `空格`：种一棵随机树种的树，界面会显示它是新建模型还是复用缓存；
- 按 `B`：一次种 100 棵，看看模型实例数会不会跟着涨；
- 按 `C`：清空森林，但模型缓存保留。

你会发现：树可以种到几千、几万棵，**模型实例数始终等于出现过的树种数**。界面上还会显示缓存命中次数，以及「共享资源」和「每棵独立建模」的体积对比。

### 共享的前提：不可变

这是享元模式最容易被忽略、也最容易出事的一点。

既然成百上千个对象引用同一个模型，那么只要有人改了模型，**所有引用它的地方都会一起变**。这不是优化，这是 bug。

所以享元对象几乎总是**不可变**的：

```csharp
public string Species { get; }   // 只读
public Mesh Mesh { get; }        // 只读
public Texture Bark { get; }     // 只读
public Texture Leaves { get; }   // 只读
```

书里在讲地形时特意强调：`Terrain` 的所有方法都是 `const`，这不是巧合。共享对象不能提供修改入口，否则「共享」就会变成「全局变量」。

### 生命周期归谁管

共享对象的生命周期也比普通对象复杂：它被很多地方引用，谁都可以用，但谁都不该随便销毁。

常见做法是让一个长期存在的拥有者持有它们：

- 工厂持有模型缓存（本章的做法）；
- `World` 直接持有几种地形的实例；
- 资源管理器 / 目录对象持有全部享元。

不要在每个使用点各自 `new`，否则「共享」会退化成「碰巧创建了多个相同对象」。

> 完整代码：[2.享元工厂-按需共享](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.享元模式/2.享元工厂-按需共享)
>
> 运行后按 `空格` / `B` / `C`，观察模型实例数、缓存命中数和资源体积的变化。

---

## 3. 改进三：没有明显身份的对象——地形区块

森林例子里，共享的东西有一个很清晰的身份：`TreeModel`。你一眼就能看出「这是模型，那是实例」。

但享元并不总是这么明显。书里给了第二个例子：**地形区块**。

### 先用枚举顶一阵

世界地图被切成一个个区块，每个区块由一种地形覆盖。每种地形有一些玩法属性：移动开销、是不是水域、用什么纹理。

一个很常见的写法是枚举加 `switch`：

```csharp
internal enum TerrainKind { Grass, Hill, River, Mountain }

private static int MovementCost(TerrainKind kind) => kind switch
{
    TerrainKind.Grass => 1,
    TerrainKind.Hill => 3,
    TerrainKind.River => 2,
    TerrainKind.Mountain => 5,
};

private static bool IsWater(TerrainKind kind) => kind == TerrainKind.River;
```

能跑，但很丑：地形的数据被拆散在多个方法里，每加一种地形，就要改一圈 `switch`。而且 `World` 里的每个区块都只是一个枚举值，想知道「这片草地用什么纹理」，还得再写一个分支。

### 把地形变成对象

既然对象就是用来封装数据的，那就给地形一个真正的类：

```csharp
internal sealed class Terrain
{
    public Terrain(string name, char symbol, int movementCost, bool isWater,
                   string texture, ConsoleColor color)
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
```

所有字段都是只读的——原因和上一节一样：它会被无数区块引用。

然后，世界网格里不再存枚举，而是存 `Terrain` **引用**：

```csharp
internal sealed class World
{
    public const int Width = 24;
    public const int Height = 14;

    private readonly Terrain[,] _tiles = new Terrain[Width, Height];

    public Terrain GetTile(int x, int y) => _tiles[x, y];

    public void Generate(Random random)
    {
        // 随机铺草地、丘陵、山地，再放一条河
        // 每种地形都指向 TerrainCatalog 里的同一个实例
        _tiles[x, y] = TerrainCatalog.Grass;
    }
}
```

四种地形只需要四个实例，由 `TerrainCatalog` 统一持有：

```csharp
internal static class TerrainCatalog
{
    public static readonly Terrain Grass = new("草地", ',', 1, false, "草原纹理", ConsoleColor.Green);
    public static readonly Terrain Hill = new("丘陵", '^', 3, false, "丘陵纹理", ConsoleColor.DarkYellow);
    public static readonly Terrain River = new("河流", '~', 2, true, "河水纹理", ConsoleColor.Cyan);
    public static readonly Terrain Mountain = new("山地", '#', 5, false, "山岩纹理", ConsoleColor.Gray);

    public static readonly Terrain[] All = { Grass, Hill, River, Mountain };
}
```

一个 24 × 14 的地图有 336 个格子，却只有 4 个 `Terrain` 对象。这就是「没有明显身份」的享元：草地区块本身没有身份，它的**全部状态**都是固有状态，所以同种地形只需要一份。

而且 `World` 从此不再耦合地形的细节。想知道某个格子的移动开销，直接问那个对象：

```csharp
int cost = world.GetTile(2, 3).MovementCost;
```

如果把地形属性加回 `World` 的方法里，`World` 就会重新变成一个巨大的 `switch` 集合；现在它只负责「哪里是什么地形」，地形自己负责「我是什么」。

> 完整代码：[3.地形区块-无身份共享](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.享元模式/3.地形区块-无身份共享)
>
> 运行后用方向键移动，控制台会实时显示脚下地形的移动开销、是否水域和纹理；按 `R` 重新生成地图，按 `B` 跑一次枚举与享元的性能对照。

### 性能：别想当然

共享减少的是内存，但读取数据时多了一次指针跳转：

```text
枚举：网格 → 枚举值 → switch → 数据
享元：网格 → Terrain 指针 → 数据
```

指针追逐可能带来缓存不命中，所以「享元一定更快」是错的。

本章工程里带了一个微基准：100 万个格子、20 轮，读取移动开销。在我的机器上跑出来是：

```text
枚举 + switch ：   133.3 ms   约 6.67 ns/次
享元对象引用  ：     7.0 ms   约 0.35 ns/次
```

这次享元反而快得多。原因很多：地形实例只有 4 个，全部在缓存里；`switch` 有分支；JIT 还可能把循环优化得很好。

但别把这个结果当成普遍结论。书里的态度很明确：**优化的金科玉律是需求优先**。现代硬件太复杂，性能取决于对象在内存里怎么排、怎么访问，必须实测。二维网格随机访问时，指针追逐的代价会更明显；而如果对象很少、访问很集中，享元可能几乎没有额外成本。

所以判断顺序应该是：

1. 先确认内存或带宽真的是瓶颈；
2. 再测量；
3. 最后才决定要不要引入这一层间接。

> 完整代码：[3.地形区块-无身份共享](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.享元模式/3.地形区块-无身份共享)

---

## 4. 小结

回头看这三步，享元模式其实只做了一件事：

> 把对象的状态切成「大家一样的」和「每个实例独有的」，前者只存一份，后者跟着实例走。

它带来的能力，正好对应三次改进：

- **改进一 · 识别与拆分**：找出上下文无关的固有状态，抽成可共享的对象；
- **改进二 · 按需创建**：用工厂 + 缓存，让共享发生在真正需要的时候；
- **改进三 · 推广到无身份对象**：连地形区块这种没有明显身份的东西，也能用引用共享。

代价也要说清楚，别只看好处：

- **间接层**：每次访问共享数据都要多跳一次指针，可能缓存不命中；
- **不可变约束**：共享对象一旦可修改，改一处就会影响所有引用，必须强制只读；
- **生命周期**：享元被很多地方引用，必须由长期存在的拥有者统一管理；
- **不是所有对象都能共享**：如果每个实例的状态都不同，就没有可共享的东西，强行套模式只会添乱。

还有两个容易混淆的模式，顺手区分一下：

| 模式 | 复用什么 | 什么时候复用 |
| ---- | -------- | ------------ |
| 享元 | 同一份**数据/对象** | 多个实例**同时**共享 |
| 对象池 | 同一个**实例** | 一个实例在**不同时间**被反复使用 |

两者经常配合：工厂用对象池缓存享元，就是这个思路。

享元模式的核心判断标准：当你发现**对象数量巨大**，而其中**大部分字段在所有实例间完全相同**，并且内存或 CPU→GPU 带宽已经成为瓶颈时，就该考虑把那些字段抽出来共享了。

---

全部四个可运行工程见本章目录：[2.享元模式](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/2.享元模式)
