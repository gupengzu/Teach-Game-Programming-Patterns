# 1. 命令模式

> **一句话定义**
>
> 命令模式，是把「一次方法调用」封装成一个可执行对象。调用者只负责持有并触发它，不必知道它内部做了什么——因此可以随时执行、延迟执行、记录，甚至撤销。

本章用一个可以本地运行的小项目，把《游戏编程模式》第一章的演进过程完整走一遍。

完整工程地址：[https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/1.命令模式](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/1.%E5%91%BD%E4%BB%A4%E6%A8%A1%E5%BC%8F)

> 运行环境与通用运行方式见[仓库根目录的「环境准备与运行方式」](../README.md#环境准备与运行方式)。

**目录**

- [0. 改进前：硬编码输入](#0-改进前硬编码输入)
- [1. 改进一：把方法调用封装成命令对象](#1-改进一把方法调用封装成命令对象)
- [2. 改进二：给角色的指令——把作用对象变成参数](#2-改进二给角色的指令把作用对象变成参数)
- [3. 改进三：撤销和重做](#3-改进三撤销和重做)
- [4. 小结](#4-小结)

---

## 0. 改进前：硬编码输入

我们的目标很简单：按下某个键，角色做出对应动作。

- `X` → 跳跃
- `Y` → 开枪
- `A` → 切换武器
- `B` → 无效地晃了一下

最容易写的版本，就是在输入处理函数里直接 `if` 到底：

```csharp
private static void HandleInput(ConsoleKey key, Player player)
{
    if (key == ConsoleKey.X) player.Jump();
    else if (key == ConsoleKey.Y) player.FireGun();
    else if (key == ConsoleKey.A) player.SwapWeapon();
    else if (key == ConsoleKey.B) player.LurchIneffectively();
}
```

代码能跑，也只有四行，看起来很舒服。但它有一个致命问题：**按键和动作被焊死在了一起**。

现代游戏几乎都有**按键自定义**功能。比如我们想让玩家把「跳跃」从 `X` 键改成 `Z` 键——用上面这种写法就无能为力了，
因为绑定关系写死在源码里，改键就意味着改代码、重新编译。

而玩家想要的，是在设置界面里点两下就能换。

> 完整代码：[0.改进前-硬编码输入](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/1.命令模式/0.改进前-硬编码输入)

---

## 1. 改进一：把方法调用封装成命令对象

思路：既然**按键**是数据，那就让**动作**也变成数据。

不要把动作写死在 `if` 分支里，而是让每个动作本身成为一个对象；按键只负责找到对应的对象，然后调用它的 `Execute()`。
这样一来，按键与动作之间就多了一张「表」，而表是可以随时改的。

### 第一步：定义命令的抽象

一个只做一件事、不返回任何值的对象：

```csharp
internal interface ICommand
{
    void Execute();
}
```

> 小经验：当你看到「只有一个方法、返回 `void`、名字叫 `Execute` / `Run` / `Invoke`」的接口时，多半就是在用命令模式。

### 第二步：每个动作写一个实现

```csharp
internal sealed class JumpCommand : ICommand
{
    public void Execute() => Console.WriteLine("  [命令] JumpCommand → 玩家跳了起来");
}

internal sealed class FireCommand : ICommand
{
    public void Execute() => Console.WriteLine("  [命令] FireCommand → 砰！开火");
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
```

注意 `SwapWeaponCommand` 里的 `_rifle`——它带了自己的状态。
这正是「把动作做成对象」的好处之一：命令对象可以像普通对象一样持有成员变量，而一个普通函数做不到这件事。

### 第三步：用一张表把按键和命令连起来

```csharp
internal sealed class InputHandler
{
    private readonly Dictionary<ConsoleKey, ICommand> _bindings = new();

    public IReadOnlyDictionary<ConsoleKey, ICommand> Bindings => _bindings;

    public void Bind(ConsoleKey key, ICommand command) => _bindings[key] = command;

    public void HandleInput(ConsoleKey key)
    {
        if (_bindings.TryGetValue(key, out var command))
        {
            command.Execute();
        }
    }
}
```

使用时就只是往表里注册：

```csharp
var input = new InputHandler();
var jump = new JumpCommand();

input.Bind(ConsoleKey.X, jump);
input.Bind(ConsoleKey.Y, new FireCommand());
input.Bind(ConsoleKey.A, new SwapWeaponCommand());
input.Bind(ConsoleKey.B, new DoNothingCommand());
```

而改键变成了两行：

```csharp
input.Bind(ConsoleKey.X, new DoNothingCommand());
input.Bind(ConsoleKey.Z, jump);
```

**源码一行都不用动。** 而且这张表还可以从配置文件读进来，做成设置界面里的按键自定义——
这就是绝大多数游戏键位设置最底层的实现方式。

### 顺带一提：空对象模式

上面改键时，`X` 键被腾空，我们并没有把它从表里删掉，而是绑了一个 `DoNothingCommand`：

```csharp
internal sealed class DoNothingCommand : ICommand
{
    public void Execute() => Console.WriteLine("  [命令] DoNothingCommand → 什么也没发生");
}
```

这是设计模式里的「空对象模式」：用一个**什么都不做但合法**的对象替代 `null`。
这样调用方就不需要到处写 `if (command != null)` 这类防御代码，主循环会干净很多。

> 完整代码：[1.命令对象-按键重映射](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/1.命令模式/1.命令对象-按键重映射)
>
> 运行后按 `F1`，可以看到跳跃键从 `X` 一键改绑到 `Z`，控制台会打印出当前的绑定表。

---

## 2. 改进二：给角色的指令——把作用对象变成参数

上一步解决了「换键」，但也埋下了一个新问题：**命令不知道自己该作用于谁。**

`JumpCommand` 里写死的是「玩家跳了起来」。如果场景里还有 NPC、还有小怪，它们想复用同一套动作，
这套命令就发不出去了——因为命令和「玩家」这个对象焊在了一起。

书里给出的改法是两步：

1. 命令把「要操作的角色」作为参数接进来；
2. 输入处理器不再直接执行命令，而是把命令**返回**出去。

### 第一步：把角色变成参数

```csharp
internal interface ICommand
{
    void Execute(Actor actor);
}

internal sealed class JumpCommand : ICommand
{
    public void Execute(Actor actor) => actor.Jump();
}

internal sealed class FireCommand : ICommand
{
    public void Execute(Actor actor) => actor.Fire();
}
```

命令从此变成一个**纯粹的配方**：它知道「怎么跳」，但不知道「谁来跳」。
谁来跳，由调用方在真正执行的那一刻决定。

### 第二步：从「执行」改成「返回」

```csharp
internal sealed class InputHandler
{
    private readonly ICommand _jump = new JumpCommand();
    private readonly ICommand _fire = new FireCommand();

    public ICommand? HandleInput(ConsoleKey key) => key switch
    {
        ConsoleKey.X => _jump,
        ConsoleKey.Y => _fire,
        _ => null,
    };
}
```

原来 `HandleInput` 是「输入 → 立刻执行」，现在是「输入 → 得到一个命令」。

这个改动看起来很小，却把**决定做什么**和**什么时候、对谁做**拆到了两个不同的地方。主循环变成：

```csharp
var command = input.HandleInput(key);
command?.Execute(current);
```

`current` 是当前选中的角色，按 `Tab` 可以切换。于是同一套按键、同一批命令对象，既能控制玩家，也能控制小怪。

### 为什么这一步很重要：AI 也能发命令

```csharp
if (aiTimer.Elapsed.TotalSeconds >= 3)
{
    aiTimer.Restart();
    new FireCommand().Execute(actors[1]);
}
```

AI 想开火，做的事和玩家按键做的事**一模一样**——产生一条 `FireCommand` 并执行。

在这套体系里，玩家和 AI 只是「命令的两种生产者」，下游完全不需要区分。这一点是很多系统的地基：

- **网络同步**：客户端把命令打包发给服务器，服务器统一执行，天然防作弊；
- **录像与回放**：记录整局的所有命令，播放时按时间轴重放即可；
- **观战 / 幽灵车**：把别人的命令流喂给本地角色，就能看到对方在做什么。

反过来说，如果命令一产生就被执行（也就是上一步的写法），上面这三件事**一件都做不了**——
因为它们都要求「命令可以被暂存、被搬运、被延后」。

> 完整代码：[2.给角色的指令-延迟执行](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/1.命令模式/2.给角色的指令-延迟执行)
>
> 运行后按 `Tab` 切换角色，再按 `X` / `Y`，会看到动作发生在不同角色身上；小怪 A 每 3 秒还会自己开一枪。

---

## 3. 改进三：撤销和重做

命令已经能被暂存和记录，那「撤销」就是顺理成章的下一步：
既然每条命令都留着，把它反过来执行一次，不就撤回了？

先给接口补一个方法：

```csharp
internal interface ICommand
{
    void Execute();
    void Undo();
}
```

但真正的难点不在接口，而在 **Undo 怎么实现**。常见的有两种做法。

| 做法     | 思路                                           | 代价                                               |
| -------- | ---------------------------------------------- | -------------------------------------------------- |
| 快照     | 执行前把整个对象的状态存一份，撤销时整体拷回去 | 写起来最省事，但状态越大、操作越频繁，内存开销越重 |
| 只记改动 | 只记录这次操作真正改变的那部分                 | 需要命令自己清楚「我改了什么」，但开销最小         |

书里推荐第二种，而它也正好体现了命令对象的价值。以一次移动为例：

```csharp
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
```

这里有两点值得停下来看。

**第一，它只记了 `x`、`y` 两个整数**，而不是把 `Unit` 整个对象拷一份。
因为一次移动真正会改变的，就只有这两个字段。「这次操作影响了什么」这件事，本来就只有命令自己最清楚。

**第二，这条命令把自己绑死在了某一个具体的 `Unit` 上。**
这和前面几步「命令是可复用配方」的思路正好相反。书里专门强调了这一点：
一旦引入撤销，命令往往就从「通用操作」退化成「这一次具体发生过的事」。
它不是「移动任意单位」的指令，而是「我刚把 A 从 (1,2) 挪到了 (2,2)」这条历史记录。

所以撤销类命令通常不放进那张可复用的绑定表，而是**每次操作时新建一个实例**：

```csharp
history.Execute(new MoveUnitCommand(unit, toX, toY));
```

### 命令历史：一个列表 + 一个游标

光有 `Undo()` 还不够——玩家想连续撤好几步，甚至撤销之后又想重做。实现只需要一个列表和一个游标：

```csharp
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
        if (_cursor <= 0) return false;

        _cursor--;
        _commands[_cursor].Undo();
        return true;
    }

    public bool Redo()
    {
        if (_cursor >= _commands.Count) return false;

        _commands[_cursor].Execute();
        _cursor++;
        return true;
    }
}
```

看懂这个类的关键，是把 `_cursor` 理解成**时间轴上的当前位置**：

- `_commands[0 .. _cursor)`：已经发生过、可以撤销的部分；
- `_commands[_cursor .. end)`：已经被撤销、可以重做的部分；
- 只有在中间插入了新命令时，后半段才会被丢弃。

「丢弃分支」这条规则，几乎是所有编辑器、绘图软件、关卡编辑器的共同行为：
撤销三步之后又做了一个新动作，那被撤销的三步就永远回不去了。

### 顺带一提：命令历史就是操作日志

有了 `CommandHistory`，想加「回放最近 5 秒」「生成一段 GIF 分享」这类功能，难度会低一个数量级——
因为需要重放的东西，本来就已经被完整记录下来了。

> 完整代码：[3.撤销和重做](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/1.命令模式/3.撤销和重做)
>
> 运行后用方向键移动方块，`Ctrl+Z` 撤销、`Ctrl+Y` 重做，控制台会实时显示「可撤销几步 / 可重做几步」。

---

## 4. 小结

回头看这四步，命令模式其实只做了一件事：

> 把「调用一个函数」这个瞬间动作，变成「创建一个对象、稍后再调用它」。

它带来的三个能力，正好对应三次改进：

- **改进一 · 解耦**：输入与动作之间隔了一层可替换的映射，所以能改键、能读配置；
- **改进二 · 延迟与复用**：命令可以被暂存、传递、交给任意对象执行，所以 AI、网络、录像都能接进来；
- **改进三 · 可逆**：命令知道自己改了什么，所以能撤销、重做、回放。

代价也要说清楚，别只看好处：

- **内存**：每个动作都是一次堆分配。高频动作（比如每帧一次）要谨慎，必要时用对象池复用命令对象；
- **间接层**：一次按键的调用栈从「两个函数」变成「按键 → 表 → 命令 → 动作」，调试时多跳一层；
- **状态负担**：带撤销的命令必须自己维护「改之前是什么」，忘了写就会撤出「幽灵状态」。

命令模式的核心判断标准：当你发现自己在某个地方反复写「如果是 A 就做 a，如果是 B 就做 b」，
并且这些分支之外的代码（输入来源、触发时机、操作对象、是否要记录）正在不断膨胀时，
就是该把它们变成命令对象的时候了。

---

全部四个可运行工程见本章目录：[1.命令模式](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/1.命令模式)
