# 3. 观察者模式

> **一句话定义**
>
> 观察者模式，是让被观察者在状态变化时，通知一份名单上的订阅者，而不必知道它们是谁。它把「发生了什么」和「谁关心」拆开，让两个几乎不相干的系统通过一份名单通信。

本章用一个可以本地运行的小项目，把《游戏编程模式》第三章的演进过程完整走一遍。

完整工程地址：[https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.%E8%A7%82%E5%AF%9F%E8%80%85%E6%A8%A1%E5%BC%8F](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.%E8%A7%82%E5%AF%9F%E8%80%85%E6%A8%A1%E5%BC%8F)

> 运行环境与通用运行方式见[仓库根目录的「环境准备与运行方式」](../README.md#环境准备与运行方式)。

**目录**

- [0. 改进前：物理引擎直接调用成就](#0-改进前物理引擎直接调用成就)
- [1. 改进一：抽出 Observer 与 Subject](#1-改进一抽出-observer-与-subject)
- [2. 改进二：用事件和委托](#2-改进二用事件和委托)
- [3. 改进三：谁都可能忘记注销](#3-改进三谁都可能忘记注销)
- [4. 小结](#4-小结)

---

## 0. 改进前：物理引擎直接调用成就

假设我们给游戏加了一套成就系统。它会有几十种徽章，玩家完成特定里程碑就能拿到，比如「杀死 100 只猴子恶魔」「从桥上掉下去」「只用一只死黄鼠狼通关」。

麻烦在于：这些成就由游戏的**许多不同方面**触发。想做得干净并不容易，一不留神，成就系统的触须就会伸进代码库的每个角落。

「从桥上掉落」显然和物理引擎有关。可是，你真的想在碰撞解析的线性代数运算中间，看到一行 `unlockFallOffBridge()` 吗？

任何有自尊的物理程序员，都不会允许游戏玩法这种平庸的东西玷污他们优美的数学。

先看看最直白的写法：

```csharp
internal sealed class Physics
{
    private readonly Achievements _achievements = new();
    private readonly AudioSystem _audio = new();
    private readonly Hud _hud = new();

    public void UpdateEntity(Entity entity)
    {
        var wasOnSurface = entity.IsOnSurface;
        entity.Accelerate(9.8f);

        if (wasOnSurface && !entity.IsOnSurface)
        {
            _achievements.OnEntityFell(entity);
            _audio.PlayFall();
            _hud.Shake();
        }
    }
}
```

运行本章的 [0.改进前-物理直接调用成就](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.观察者模式/0.改进前-物理直接调用成就)，输出是这样的：

```text
  英雄脚下的桥板消失了……
    [成就] 解锁「从桥上掉落」
    [音效] 播放「坠落」音效
    [HUD] 屏幕红闪一下
```

功能完全正常，但代价已经埋下了：

- 物理引擎同时依赖 **三个** 互不相干的系统；
- 通知顺序被写死在物理代码里：先成就、再音效、再 HUD；
- 想加第四个「任务日志」，或者临时关掉音效，只能继续改物理；
- 成就系统要判断「是不是在桥上」，可它需要的数据在物理和地图那边。

物理和成就，本来是几乎不相干的两个领域，很可能由不同的人实现。现在它们被硬绑在一起了。

> 完整代码：[0.改进前-物理直接调用成就](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.观察者模式/0.改进前-物理直接调用成就)

---

## 1. 改进一：抽出 Observer 与 Subject

观察者模式要做的事只有一句：**让一段代码宣布有趣的事情发生了，而不必真正关心谁会收到通知。**

物理引擎不需要认识成就、音效、HUD，它只需要维护一份「谁想听」的名单。

### 第一步：观察者接口

先从那个爱打听的类开始。它想知道别的对象何时做了什么有趣的事：

```csharp
internal abstract class Observer
{
    public abstract string Name { get; }

    public abstract void OnNotify(Entity entity, GameEvent e);
}
```

`OnNotify` 的参数由你决定，这也是它成为「模式」而不是「现成代码」的原因。这里我们传两样东西：发送通知的实体，以及一个描述发生了什么的枚举。

### 第二步：被观察者

通知由被观察的对象发出。书里把它叫做 **Subject**。它有三个任务：

```csharp
internal class Subject
{
    private readonly List<Observer> _observers = new();

    public int ObserverCount => _observers.Count;

    public void AddObserver(Observer observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void RemoveObserver(Observer observer) => _observers.Remove(observer);

    // 同步调用：直到所有观察者的 OnNotify 返回，Notify 才返回。
    protected void Notify(Entity entity, GameEvent e)
    {
        for (var i = 0; i < _observers.Count; i++)
        {
            _observers[i].OnNotify(entity, e);
        }
    }
}
```

三件事：持有一份观察者列表、公开增删接口、有事时挨个通知。

**为什么是「列表」而不是「一个观察者」？** 这是整个模式最关键的设计。假设物理解析只支持一个观察者，那么音频系统注册时就会把成就系统顶掉，两个系统开始互相干扰。有了列表，每个观察者都以为自己是世界上唯一盯着物理的东西。

### 第三步：让物理继承 Subject

```csharp
internal sealed class Physics : Subject
{
    public void UpdateEntity(Entity entity)
    {
        var wasOnSurface = entity.IsOnSurface;
        entity.Accelerate(9.8f);

        if (wasOnSurface && !entity.IsOnSurface)
        {
            Notify(entity, GameEvent.EntityFell);
        }
    }
}
```

它不再提任何具体系统，只负责说一句：「有东西掉下去了，你们自己看着办。」

### 效果

现在成就、音效、任务日志各自实现 `Observer`，注册到物理引擎上：

```csharp
physics.AddObserver(achievements);
physics.AddObserver(audio);
physics.AddObserver(questLog);
```

运行 [1.观察者接口-被观察者列表](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.观察者模式/1.观察者接口-被观察者列表)：

```text
  注册观察者：成就 / 音效 / 任务日志（共 3 个）

  英雄脚下的桥板消失了……
    [成就] 解锁「从桥上掉落」
    [音效] 播放「坠落」音效
    [任务日志] 英雄 从桥上掉了下去
```

接着把音效系统拆掉：

```csharp
physics.RemoveObserver(audio);
```

再掉一次，只剩两个系统响应。**物理代码一行都没改**——它甚至不知道音效系统已经不存在了。

### 注意通知顺序

上面输出里，观察者的响应顺序就是注册顺序。如果换成别的实现，顺序可能完全不同。

书里给了一条设计准则：**观察同一个被观察者的两个观察者，彼此之间不应有任何顺序依赖。** 如果顺序真的重要，说明这两个观察者之间存在某种微妙的耦合，早晚会反过来咬你一口。

### 「太慢了」？

这是观察者模式最常听到的批评。但看过实现就知道：发送通知不过是遍历列表、调用几个虚方法。它比静态调用慢一点，但除了最热的代码路径，这点开销可以忽略。

本章工程里带了个微基准：500 万次通知，单观察者。在我的机器上（Release 模式）：

```text
直接调用（静态分派）：         10.3 ms   约 2.06 ns/次
观察者通知（虚方法 + 遍历）：   77.3 ms   约 15.46 ns/次

每次通知多花约 13.4 ns，500 万次累计多花 67.0 ms。
```

每次多花十几纳秒。除非你把通知塞进每帧几十万次的循环里，否则根本测不出来。

真正慢的「事件」「消息」系统，慢在排队和动态分配，不在这一层虚调用。**这就是为什么术语要分清**：你说「观察者」，别人听成「事件队列」，讨论就永远对不上。

### 「太快了」？

反过来的问题更值得注意：观察者模式是**同步**的。被观察者直接调用观察者，在所有 `OnNotify` 返回之前，物理都没法继续。

一个缓慢的观察者会卡住被观察者。UI 程序员对此有句老话：「别在 UI 线程上干重活。」如果响应事件确实耗时，就把它丢到线程或工作队列里去。

多线程时还要小心：观察者如果去抢被观察者持有的锁，就可能死锁。高度多线程的引擎里，通常该用[事件队列](https://gpp.tkchu.me/event-queue.html)做异步通信，而不是同步观察者。

### 动态分配多不多？

上面的实现里，`List<Observer>` 只在注册时扩容，**发送通知本身不分配内存**。

如果你连注册时那点分配都想省掉，书里给了两条路：

1. **链式观察者**：观察者对象自己就是链表节点，内部存一个 `next` 指针。`Subject` 只保存头指针，增删都改指针，完全不碰堆。代价是打破了「Observer 是纯接口」的设计，而且一个观察者同时只能挂在一个被观察者上。
2. **链表节点池**：节点是独立的小对象，预先分配一池复用。节点和数据分开，一个观察者就能同时出现在多个链表里。

这两种做法在 C# 里都不常见——GC 语言里注册时的一次分配通常可以接受。但知道它们存在，能帮你理解「零分配」到底牺牲了什么。

> 完整代码：[1.观察者接口-被观察者列表](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.观察者模式/1.观察者接口-被观察者列表)
>
> 运行后会演示注册、通知、注销，以及直接调用与观察者通知的微基准对比。

---

## 2. 改进二：用事件和委托

手写 `Observer` 接口当然能跑，但它有个时代烙印：**为了收一条通知，你得实现一整个接口。**

观察者模式诞生于 1994 年，那是个以类为中心的年代。今天的语言大多有了闭包和一等函数，观察者可以只是「一个方法引用」。

C# 甚至把它做进了语言：`event` 关键字。

### 事件就是「一列表委托」

```csharp
internal sealed class Physics
{
    public event Action<Entity>? EntityFell;
    public event Action<Entity>? EntityLanded;
    public event Action<Entity, int>? EntityDamaged;

    public void UpdateEntity(Entity entity)
    {
        var wasOnSurface = entity.IsOnSurface;
        entity.Accelerate(9.8f);

        if (wasOnSurface && !entity.IsOnSurface)
        {
            EntityFell?.Invoke(entity);
        }
    }
}
```

对比一下手写版本，事件带来了三件事：

- **订阅更轻**：`physics.EntityFell += OnFall;`，不用实现接口；
- **封装更严**：外部只能 `+=` / `-=`，不能触发；`Invoke` 只有声明类内部能调用；
- **粒度更细**：不同事件可以有不同签名，不必挤进一个 `OnNotify(entity, event)`。

订阅方可以是一个方法，也可以是一个 lambda：

```csharp
physics.EntityFell += achievements.OnFell;          // 具名方法
physics.EntityFell += _ => hud.Flash();             // lambda
physics.EntityDamaged += (_, amount) => audio.Hurt(amount);
```

### 坑：lambda 注销不掉

这是实际项目里最常见的 bug 之一：

```csharp
var first = 1;
var second = 2;

physics.EntityFell += _ => Console.Write(first);
physics.EntityFell -= _ => Console.Write(second);   // 没注销掉
```

两个 lambda 看起来一样，但它们是**两个不同的委托实例**，`-=` 匹配的是「目标对象 + 方法」，匹配不上就什么都不做。

工程里实测：

```text
订阅后 1 个，注销后 1 个（两个闭包不是同一个委托实例，-= 匹配不上）
```

结论很简单：**lambda 订阅时，一定要先把委托存到字段里**，注销时用同一个字段。具名方法没这个问题，因为方法组每次转换出的委托比较起来是相等的。

### 一个类订阅多个事件

事件系统还有一个好处：同一个对象可以订阅不同主题。

```csharp
public void Subscribe(Physics physics)
{
    physics.EntityFell += OnFell;
    physics.EntityLanded += OnLanded;
    physics.EntityDamaged += OnDamaged;
}
```

任务日志就该这么写：它对「坠落」「落地」「受伤」都感兴趣，但处理逻辑各不相同。换成 `Observer` 接口，它只能在 `OnNotify` 里再 `switch` 一次。

运行 [2.角色多订阅-事件与委托](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.观察者模式/2.角色多订阅-事件与委托)，可以按 `1`～`4` 实时开关每个订阅者：

```text
注册四个订阅者：成就 开 / 音效 开 / 任务日志 开 / HUD 开，监听者 5 个。

  英雄脚下的桥板消失了……
    [成就] 解锁「从桥上掉落」
    [音效] 播放「坠落」音效
    [HUD] 屏幕红闪一下
    [任务日志] 英雄 从桥上掉了下去
```

关掉音效后，监听者从 5 个降到 3 个，物理依旧不知道谁在听。

### 观察者，还是事件？

书里提了一个很容易忽略的区分：

- 用**观察者**，你观察的是「做了有趣事情的那个东西」，比如物理引擎；
- 用**事件**，你观察的对象代表「发生的那件有趣的事」，比如 `entityFell`。

前者的接口是 `physics.AddObserver(this)`，后者是 `physics.EntityFell += this`。两者实现几乎一样，但语义不同：事件更细，观察者更整体。

> 完整代码：[2.角色多订阅-事件与委托](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.观察者模式/2.角色多订阅-事件与委托)
>
> 运行后按空格让英雄坠落，`1`～`4` 切换订阅者，观察监听者数量的变化。

---

## 3. 改进三：谁都可能忘记注销

前两步解决了耦合，也解决了「太慢」和「分配」的质疑。但观察者模式还有一个更隐蔽的问题：**生命周期**。

### 悬空指针与失效监听者

先看非 GC 语言。你 `delete` 了一个观察者，但被观察者手里还攥着指向它的指针。下一次 `Notify`，就是往已释放的内存里写东西。

被观察者先死也一样麻烦：观察者还在等通知，却不知道这份通知永远不会来了。它已经不是一个观察者了，只是自己还这么以为。

书里给了几种做法：

- **最简单的**：观察者自己负责。在析构函数里调用 `removeObserver()`，难点不在怎么写，而在**记得写**；
- **临终通知**：被观察者在销毁前，最后通知一次所有观察者，让它们自己处理；
- **自动注销**：在观察者基类里统一实现，代价是每个观察者都要记住自己观察了谁，最后变成双向指针。

### 有 GC 就高枕无忧了？

恰恰相反。GC 语言里这个问题更常见，因为它不会崩，只会慢慢漏。

想象一个显示血量的 UI 面板：玩家打开时创建，关闭时你把它从界面上移除，然后忘了注销。

结果：

- 面板不可见，却回收不掉——玩家的事件列表还引用着它；
- 每开关一次，列表里就多一个僵尸面板；
- 玩家每次受伤，所有僵尸面板都在更新不可见的 UI，白烧 CPU；
- 如果它们还负责播音效，你会听到诡异的声音。

这就是**失效监听者**（lapsed listener）问题。它常见到有自己的名字。

工程的 [3.失效监听者-注销与生命周期](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.观察者模式/3.失效监听者-注销与生命周期) 把两种关闭方式各跑 1000 次：

```text
场景 A：Dispose 里记得注销
    打开并关闭 1000 个面板后：
      残留监听者：0 个
      强制 GC 后仍存活的面板：0 个
      再受伤一次，收到通知的面板：0 个

场景 B：只关闭界面，忘记注销
    打开并关闭 1000 个面板后：
      残留监听者：1000 个
      强制 GC 后仍存活的面板：1000 个
      再受伤一次，收到通知的面板：1000 个
```

同样关了 1000 次面板，一个干净收场，一个留下 1000 个僵尸。而且这个数字会随着玩家操作一直涨。

### 正确的姿势

C# 里最实用的做法是 `IDisposable`：把「订阅」和「注销」放在同一个对象的生命周期里。

```csharp
internal sealed class HealthPanel : IDisposable
{
    private readonly Player _player;

    public HealthPanel(Player player)
    {
        _player = player;
        player.Damaged += OnDamaged;   // 打开时订阅
    }

    public void Dispose()
    {
        _player.Damaged -= OnDamaged;  // 关闭时注销，和订阅严格配对
    }
}
```

调用方写成 `using`，或者由 UI 框架在销毁时统一 `Dispose`。规则只有一条：**谁订阅，谁负责注销，而且要成对出现。**

> 完整代码：[3.失效监听者-注销与生命周期](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.观察者模式/3.失效监听者-注销与生命周期)
>
> 运行后按 `O` 打开会注销的面板，按 `X` 打开忘记注销的面板，按 `T` 跑 1000 次泄漏对比。

### 另一个代价：你再也读不懂调用链

观察者模式放松了耦合，也放松了「可推理」这件事。

如果代码是显式调用，你顺着方法点进去，就知道发生了什么。IDE 也开心，因为这是静态的。

但通过观察者列表通信，想知道谁会收到通知，唯一的办法是看运行时列表里恰好有谁。你没法再静态地推理程序的通信结构，只能推理它动态的行为。

书里的指导原则很干脆：

> 如果你经常需要同时考虑某次通信的两端，才能理解程序的某一部分，那就别用观察者，用更显式的方式。

反过来，如果两块代码本来就几乎不相干（成就和物理就是典型），只是偶尔要说一句话，那观察者正合适。

---

## 4. 小结

回头看这三步，观察者模式其实只做了一件事：

> 把「发生了什么」和「谁关心」拆开，中间用一份名单连接。

它带来的能力，正好对应三次改进：

- **改进一 · 解耦**：被观察者只广播事件，观察者各自实现响应；
- **改进二 · 变轻**：用事件和委托代替接口，订阅粒度更细，代码更短；
- **改进三 · 管住生命周期**：订阅和注销必须成对，否则就是失效监听者。

代价也要说清楚：

- **同步**：慢观察者会阻塞被观察者，重活要挪到别的线程或队列；
- **动态**：通信结构只能靠运行时观察，调试比显式调用难；
- **生命周期**：忘记注销不会报错，只会慢慢泄漏；
- **顺序**：观察者之间不该有顺序依赖，有依赖说明耦合没拆干净；
- **不是所有关系都该用它**：本来就很紧密的两块代码，用它只会让调用链更难读。

顺手区分几个容易混的东西：

| 名字 | 通知时机 | 典型实现 |
| ---- | -------- | -------- |
| 观察者 | 事件发生时**同步**调用 | 接口列表 / C# `event` |
| 事件队列 | 先入队，稍后**异步**处理 | 消息队列、环形缓冲 |
| 发布订阅 | 中间多一个**消息中心**，双方互不认识 | 事件总线、消息中间件 |

观察者的判断标准：当两块代码**几乎不相干**，却需要在一件事发生时互相知会，而且你不希望它们互相认识时，就该用它。反过来，如果每次改 bug 都要同时盯着通信的两端，那就该换成显式调用。

---

全部四个可运行工程见本章目录：[3.观察者模式](https://github.com/gupengzu/Teach-Game-Programming-Patterns/tree/main/3.观察者模式)
