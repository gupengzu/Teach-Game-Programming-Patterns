# 4. 原型模式

> **一句话定义**
>
> 原型模式，是拿一个已经配置好的对象当模板，通过克隆它来创建同类、同状态的新对象。它把「要造什么」从生产者类挪到了原型对象里。

本章用四个可以本地运行的小项目，把《游戏编程模式》第四章的主要路径完整走一遍：先拆掉平行的生产者类，再处理克隆语义，最后把原型思想用于游戏数据。

完整工程地址：[https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式)

> 运行环境与通用运行方式见[仓库根目录的「环境准备与运行方式」](../README.md#环境准备与运行方式)。

**目录**

- [0. 改进前：每种怪物都配一个生产者类](#0-改进前每种怪物都配一个生产者类)
- [1. 改进一：让对象自己克隆自己](#1-改进一让对象自己克隆自己)
- [2. 改进二：先想清楚深克隆，再考虑生产函数](#2-改进二先想清楚深克隆再考虑生产函数)
- [3. 改进三：不复制数据，改为向原型委托](#3-改进三不复制数据改为向原型委托)
- [4. 原型还是一种语言范式](#4-原型还是一种语言范式)
- [5. 小结](#5-小结)

---

## 0. 改进前：每种怪物都配一个生产者类

假设我们在做一款《圣铠传说》风格的游戏。恶灵、恶魔、术士不断从「生产者」里进入竞技场，每种怪物都有自己的 C# 类：

```csharp
internal abstract class Monster
{
    protected Monster(string name, int health, int speed)
    {
        Name = name;
        Health = health;
        Speed = speed;
    }

    public string Name { get; }
    public int Health { get; private set; }
    public int Speed { get; }
}

internal sealed class Ghost : Monster
{
    public Ghost(int health, int speed) : base("恶灵", health, speed) { }
}
```

如果生产者也按怪物类型拆开，就会出现一套平行结构：`Ghost` 配 `GhostSpawner`，`Demon` 配 `DemonSpawner`，`Sorcerer` 配 `SorcererSpawner`。

```csharp
internal abstract class Spawner
{
    public abstract Monster SpawnMonster();
}

internal sealed class GhostSpawner : Spawner
{
    public override Monster SpawnMonster() => new Ghost(health: 15, speed: 3);
}

internal sealed class DemonSpawner : Spawner
{
    public override Monster SpawnMonster() => new Demon(health: 30, speed: 2);
}
```

这些生产者的区别几乎只有 `new` 后面的类名。怪物种类越多，重复越多；想生产「速度快一点的恶灵」，还可能再冒出一个 `SwiftGhostSpawner`。

运行 [0.改进前-平行生产者类](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式/0.改进前-平行生产者类)，输出为：

```text
  怪物类：Ghost / Demon / Sorcerer
  生产者类：GhostSpawner / DemonSpawner / SorcererSpawner
  新增一种怪物，就要同时新增一个几乎相同的生产者。

  恶灵生产者：
    恶灵 #1｜生命 15｜速度 3
    恶灵 #2｜生命 15｜速度 3
  恶魔生产者：
    恶魔 #3｜生命 30｜速度 2
    恶魔 #4｜生命 30｜速度 2
  术士生产者：
    术士 #5｜生命 20｜速度 2
    术士 #6｜生命 20｜速度 2

  3 种怪物需要 3 个具体生产者类；样板代码随怪物种类线性增长。
```

问题不在于代码不能运行，而在于「生产哪种对象」这件事被编码成了**类的种类**。如果它能改成一个对象持有的状态，我们就不需要这些平行类。

> 完整代码：[0.改进前-平行生产者类](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式/0.改进前-平行生产者类)

---

## 1. 改进一：让对象自己克隆自己

原型模式的关键思路是：**一个对象可以产出与自身相似的对象。**

手里有一只恶灵，就拿它当模板制造更多恶灵；手里有一只重甲恶魔，就拿它制造更多重甲恶魔。生产者不再需要知道具体类型。

### 给怪物增加 Clone

```csharp
internal abstract class Monster
{
    protected Monster(string name, int health, int speed)
    {
        Name = name;
        Health = health;
        Speed = speed;
    }

    public string Name { get; }
    public int Health { get; private set; }
    public int Speed { get; }

    public abstract Monster Clone();
}

internal sealed class Ghost : Monster
{
    public Ghost(string name, int health, int speed)
        : base(name, health, speed) { }

    public override Monster Clone() => new Ghost(Name, Health, Speed);
}
```

每个子类负责返回一个与自己**具体类型相同、状态相同、身份不同**的新对象。`Id` 在构造时重新分配，所以原型和克隆体不是同一个实例。

### 一个通用 Spawner

```csharp
internal sealed class Spawner
{
    private readonly Monster _prototype;

    public Spawner(Monster prototype) => _prototype = prototype;

    public Monster SpawnMonster() => _prototype.Clone();
}
```

`Spawner` 里没有 `Ghost`、`Demon`、`Sorcerer`，也没有 `switch`。它只保存一个原型，需要新怪物时就让原型克隆自己。

```csharp
var swiftGhost = new Ghost("迅捷恶灵", health: 10, speed: 6);
var ghostSpawner = new Spawner(swiftGhost);
Monster first = ghostSpawner.SpawnMonster();
Monster second = ghostSpawner.SpawnMonster();
```

这个原型有点像一只从不离开蜂巢的蜂后：它不一定要上场战斗，唯一职责就是充当模板。

### 克隆的不只是类型，还有状态

这是原型模式最有价值的一点。`new Ghost()` 只能表达「造一只恶灵」，而 `prototype.Clone()` 可以表达「造一只和这只一样、生命 10、速度 6 的迅捷恶灵」。

因此，快速恶灵、虚弱恶灵、重甲恶魔都不必各配一个生产者类。先配置好原型，再把它交给同一个 `Spawner` 即可。

运行 [1.原型克隆-通用生产者](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式/1.原型克隆-通用生产者)：

```text
  迅捷恶灵生产者：
    迅捷恶灵 #4｜生命 10｜速度 6
    迅捷恶灵 #5｜生命 10｜速度 6
  重甲恶魔生产者：
    重甲恶魔 #6｜生命 45｜速度 1
    重甲恶魔 #7｜生命 45｜速度 1
  火焰术士生产者：
    火焰术士 #8｜生命 20｜速度 2
    火焰术士 #9｜生命 20｜速度 2

  给迅捷恶灵的克隆体造成 4 点伤害：
    原型：迅捷恶灵 #1｜生命 10｜速度 6
    克隆：迅捷恶灵 #10｜生命 6｜速度 6
    原型与克隆是否同一对象：False

  3 种怪物只需 1 个具体生产者类；不同属性也不必再写新生产者。
```

克隆体受伤，原型生命值不变。这里的 `int` 字段按值复制，独立性很直观；下一阶段的武器字段就没有这么简单了。

### 它真的减少代码了吗？

生产者类确实消失了，但每个怪物子类仍要实现 `Clone()`。如果只是把 `new Ghost()` 从 `GhostSpawner` 搬进 `Ghost.Clone()`，节省的代码有限。

而且，这个例子人为地假设了「每种怪物都有独立类」。现代游戏更常用[组件](https://gpp.tkchu.me/component.html)或[类型对象](https://gpp.tkchu.me/type-object.html)组织实体，未必值得维护一棵庞大的怪物继承树。

所以，原型模式不是「凡是要创建对象就用 Clone」。它真正适合的是：

- 新对象要以某个**现有对象的类型和配置**为起点；
- 创建方不该依赖具体类；
- 游戏需要在运行时保存、传递各种已配置模板。

> 完整代码：[1.原型克隆-通用生产者](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式/1.原型克隆-通用生产者)

---

## 2. 改进二：先想清楚深克隆，再考虑生产函数

`Clone()` 看起来只有一个方法，真正难的是它的语义：**克隆恶魔时，要不要连它手里的草叉一起克隆？**

### 浅克隆共享了可变武器

如果 `Demon.Clone()` 只是把 `Weapon` 引用传给新对象：

```csharp
public override Monster CloneShallow() => new Demon(Name, Health, Weapon);
```

那么原型和克隆体是两个怪物，却拿着同一把武器。给原型的草叉附魔，克隆体的草叉也会变强。

本章的 `Weapon` 是可变类：

```csharp
internal sealed class Weapon
{
    public Weapon(string name, int damage)
    {
        Name = name;
        Damage = damage;
    }

    public string Name { get; }
    public int Damage { get; private set; }

    public void Enchant(int bonus) => Damage += bonus;
    public Weapon Clone() => new(Name, Damage);
}
```

如果每只怪物拥有自己的装备，正确的深克隆应当复制武器：

```csharp
public override Monster Clone() => new Demon(Name, Health, Weapon.Clone());
```

运行 [2.深克隆与生产函数](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式/2.深克隆与生产函数)，实测输出：

```text
  给原型的草叉附魔 +4：
    原型：  恶魔原型 #1｜生命 30｜草叉（伤害 12）
    浅克隆：恶魔原型 #2｜生命 30｜草叉（伤害 12）  ← 武器被一起改了
    深克隆：恶魔原型 #3｜生命 30｜草叉（伤害 8）  ← 仍保留自己的武器
    原型与浅克隆共享武器：True
    原型与深克隆共享武器：False

  原型受到 6 点伤害后，两个克隆体的生命值仍为 30。
```

这里同时能看到两层独立性：生命值是数值字段，天然各有一份；武器是引用字段，共享与否由克隆实现决定。

### 深克隆并不总是正确答案

如果 `Weapon` 是不可变配置，共享反而更安全、更省内存；如果多个怪物本来就应该共同持有一件装备，强行深复制反而会破坏设计。

因此不要机械地把整个对象图复制一遍。先问每个字段属于哪一种：

- **实例运行状态**：当前生命、冷却时间，通常归新对象自己；
- **独占的可变资源**：本例武器，需要复制或重新构造；
- **可共享的不可变配置**：名称、模型描述、技能表，通常可以共享；
- **外部世界引用**：场景、玩家、任务管理器，通常不能跟着递归克隆。

还要留意循环引用和共享子对象。`MemberwiseClone()` 只能做浅复制；`record` 的 `with` 表达式也不是自动深克隆。它们都没有替你做所有权决策。

### C# 里可以直接保存生产函数

如果并不需要复制一个已有对象，只想让生产者与具体类型解耦，C# 的委托往往更轻：

```csharp
internal sealed class Spawner
{
    private readonly Func<Monster> _spawn;

    public Spawner(Func<Monster> spawn) => _spawn = spawn;

    public Monster SpawnMonster() => _spawn();
}

var ghostSpawner = new Spawner(() => new Ghost(
    "新生恶灵", health: 15, new Weapon("寒触", damage: 5)));
```

委托既可以包住 `new`，也可以包住原型的 `Clone`：

```csharp
var prototypeSpawner = new Spawner(original.Clone);
```

同一工程的实测输出：

```text
  同一个 Spawner 也可以保存 Func<Monster> 生产函数：
    克隆生产：恶魔原型 #4｜生命 24｜草叉（伤害 12）
    函数生产：新生恶灵 #5｜生命 15｜寒触（伤害 5）
  不需要复制已有状态时，生产函数通常比实现 Clone 更直接。
```

原书还提到 C++ 模板。对应到 C#，可以写泛型 `SpawnerFor<T> where T : Monster, new()`；但它更适合「按类型调用无参构造」，不擅长保留某个实例的配置状态。

在 JavaScript、Python 等语言里，类本身可以像普通值一样传递，「把要创建的类型交给生产者」会更直接。语言提供了什么，会直接影响哪种方案最省事。

> 完整代码：[2.深克隆与生产函数](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式/2.深克隆与生产函数)

---

## 3. 改进三：不复制数据，改为向原型委托

原型思想并不止于 `Clone()`。在游戏数据里，往往更有用的是另一件事：**查不到的属性，就到原型对象上找。**

### 重复的数据和重复的代码一样难维护

假设游戏用 JSON 描述哥布林。杂兵、术士、弓手都有相同的生命范围、抗性和弱点，只各自多一个技能或武器字段。

最直白的做法是把公共属性复制到每份 JSON。之后如果想让「所有哥布林都变强」，就得找到并修改所有副本；漏掉任何一份，数据就悄悄不一致。

可以给数据增加一条规则：

> 如果对象有 `prototype` 字段，它就是另一个数据对象的名字；当前对象没有的属性，去原型上找。

于是术士和弓手只记录差异：

```json
{
  "name": "goblin archer",
  "prototype": "goblin grunt",
  "attacks": ["short bow"]
}
```

生命、抗性、弱点仍然从 `goblin grunt` 取得。`prototype` 是数据模型的元数据，不是哥布林在游戏世界里的身体属性。

### 用 C# 实现属性委托

本章用 `MonsterData` 模拟这一机制。每个数据对象保存自己的属性，并可选地指向一个原型：

```csharp
internal sealed class MonsterData
{
    private readonly Dictionary<string, int> _properties = new();

    public MonsterData(string name, MonsterData? prototype = null)
    {
        Name = name;
        Prototype = prototype;
    }

    public string Name { get; }
    public MonsterData? Prototype { get; }

    public int Get(string key)
    {
        for (MonsterData? current = this;
             current != null;
             current = current.Prototype)
        {
            if (current._properties.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        throw new KeyNotFoundException($"{Name} 及其原型都没有属性 {key}");
    }
}
```

定义一个基础哥布林，再定义只覆盖差异的战士与弓手：

```csharp
var goblin = new MonsterData("哥布林杂兵")
    .Set("health", 20)
    .Set("speed", 3)
    .Set("attack", 4)
    .Set("coldResist", 2);

var warrior = new MonsterData("哥布林战士", prototype: goblin)
    .Set("health", 40)
    .Set("attack", 7);

var archer = new MonsterData("哥布林弓手", prototype: goblin)
    .Set("attack", 5)
    .Set("range", 6);
```

`warrior.Get("health")` 在自身找到 40；`warrior.Get("speed")` 找不到，就向 `goblin` 委托，得到 3。

### 共享配置，不共享运行状态

数据原型可以共享，怪物当前生命却必须各自独立：

```csharp
internal sealed class Monster
{
    public Monster(MonsterData data)
    {
        Data = data;
        Health = data.Get("health");
    }

    public MonsterData Data { get; }
    public int Health { get; private set; }
}
```

运行 [3.数据原型-属性委托](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式/3.数据原型-属性委托)：

```text
  属性来源：
    战士生命 40 来自「哥布林战士」
    战士速度  3 来自「哥布林杂兵」
    弓手冰抗  2 来自「哥布林杂兵」
    弓手射程  6 来自「哥布林弓手」

  同一数据原型产出两个战士，其中一个受到 9 点伤害：
    哥布林战士 #1｜生命 31/40｜速度 3｜攻击 7
    哥布林战士 #2｜生命 40/40｜速度 3｜攻击 7
    配置共享，运行状态不共享。

  修改基础哥布林的速度：3 → 4
    战士速度：4
    弓手速度：4
    两个特殊个体都没有复制 speed，因此无需逐份同步。
```

这个方案不必虚构一个「抽象基础哥布林类」。拿最简单的普通个体当原型，再精化出特殊个体即可。它尤其适合 Boss、独特武器这类内容：一把「断头剑」可以只是普通长剑加 20 点伤害，而不是从空白重新配置。

代价也要说清楚：

- 属性来源不再全部写在一个对象里，查错时要沿原型链追踪；
- 修改共享原型会影响所有未覆盖该属性的后代；
- 原型成环会导致无限查找，数据加载时需要校验；
- 如果游戏运行中允许修改配置，要明确修改会何时、对哪些实体生效。

> 完整代码：[3.数据原型-属性委托](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/4.原型模式/3.数据原型-属性委托)

---

## 4. 原型还是一种语言范式

「原型」除了是 GoF 设计模式，也是一种面向对象语言的组织方式。不要把这两层含义混为一谈。

### Self：没有类，对象自己保存状态和行为

基于类的语言通常把状态放在实例里，把方法放在类里。调用方法时，要从实例找到类，再找到方法。

Self 去掉了这层区分：字段和方法都可以直接放在对象上。对象找不到某个属性时，就把查找**委托**给父对象；父对象也找不到，就继续向上。

类还负责创建实例。Self 没有类，就通过克隆已有对象来制造相似的新对象。可以先把一个对象调整成想要的样子，再不断克隆它。换句话说，原型模式的克隆能力被做成了语言内建机制。

这种设计很优雅，但也非常动态。结构不再集中在类里，理解程序时可能更难回答「这个对象究竟有哪些字段和行为」。Self 为即时编译、垃圾回收和方法派发优化带来的技术影响，比「日常业务都用纯原型编程」更深远。

### JavaScript 的 prototype 不等于 Clone

JavaScript 从 Self 吸收了原型委托，但常见写法并不是不断克隆对象。典型代码用构造函数初始化状态，把方法放到共享原型上：

```javascript
function Weapon(range, damage) {
  this.range = range;
  this.damage = damage;
}

Weapon.prototype.attack = function (target) {
  target.health -= this.damage;
};
```

`new Weapon()` 创建的对象会委托给 `Weapon.prototype`，从而共享 `attack` 方法。状态仍在实例，方法仍通过另一层对象共享，这和基于类的组织方式很相似。

所以看到 JavaScript 的 `prototype`，不要直接理解成「每个对象都实现了原型设计模式」。**委托查找**和**克隆创建**可以一起出现，也可以分别使用；本章第 3 个工程用的就是前者。

---

## 5. 小结

本章的四个工程对应四种不同的选择：

- **改进前**：每种怪物一个生产者类，结构平行，样板代码随类型增长；
- **改进一**：对象实现 `Clone()`，一个通用 `Spawner` 按原型生产，同时复制类型与配置；
- **改进二**：根据所有权选择深克隆或共享；不需要复制状态时，`Func<Monster>` 往往更直接；
- **改进三**：数据对象只保存差异，缺失属性向原型委托，共享配置但保留独立运行状态。

原型模式最适合回答的是：**「给我再来一个，以这个为蓝本。」** 如果需求只是「按已知类型 new 一个」，普通构造或生产函数就够了；如果需求是「共享一大类实体的定义」，类型对象、组件或数据原型通常比庞大的继承树更合适。

无论采用哪一种，都要把两件事分开：**对象的创建方式**，以及**对象之间哪些状态应该共享**。前者由原型、工厂或委托解决，后者只能靠清晰的所有权设计解决。
