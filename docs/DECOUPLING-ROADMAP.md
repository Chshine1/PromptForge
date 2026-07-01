# PromptForge 解耦改造 Roadmap

> 文档版本: v1.0  
> 日期: 2026-07-01  
> 关联问题文档: [DECOUPLING-PROBLEMS.md](./DECOUPLING-PROBLEMS.md)  

---

## 概览

本 Roadmap 分为 **6 个独立 Step**，每个 Step 对应一个可独立提交的改造单元。  
建议按顺序执行，每步完成后运行现有测试（或手动验证 CLI 输出）确保无回归。

```
Step 1 → Step 2 → Step 3 → Step 4 → Step 5 → Step 6
  修接口    移配置    加接口    接通管道   重构Builder  接入DI
```

**解决的问题对应关系**:

| Step | 解决问题 |
|------|---------|
| Step 1 | P1 — IPromptCompiler 违反 ISP |
| Step 2 | P5 — SerializeConfiguration 在错误层 |
| Step 3 | P4 — TypeMetadataBuilder 无接口 |
| Step 4 | P2 — _serializer 死代码，接通序列化管道 |
| Step 5 | P3 — PromptBuilder 内部直接 new |
| Step 6 | P6 — CLI 无 DI 组合根 |

---

## Step 1 — 修正 IPromptCompiler 接口，消除时序耦合

**解决**: P1、P2  
**改动范围**: `Abstractions`、`Core`、`Cli`  
**改动量**: 小

### 目标

将 `SetSerialization` 从接口移除，改为构造函数注入，消除可变状态和隐式调用顺序。

### 改动清单

#### 1.1 修改 `IPromptCompiler.cs`

```csharp
// src/PromptForge.Abstractions/IPromptCompiler.cs
public interface IPromptCompiler
{
    IPromptTemplate<T> Compile<T>(PromptContract contract);
    // 删除: void SetSerialization(ISerializer serializer);
}
```

#### 1.2 修改 `PromptCompiler.cs`

```csharp
// src/PromptForge.Core/PromptCompiler.cs
public partial class PromptCompiler(ISerializer serializer) : IPromptCompiler
{
    // 删除: private ISerializer _serializer = new Serializer([], []);
    // 删除: public void SetSerialization(ISerializer serializer) { ... }

    // Compile<T> 方法内部今后可以通过 serializer 字段访问序列化能力
}
```

#### 1.3 修改 `PromptBuilder.cs`（临时适配，Step 5 会彻底重构）

```csharp
// 删除这行（调用了已不存在的接口方法）
// compiler.SetSerialization(new Serializer(...));
```

> ⚠️ 注意：此时序列化配置暂时断开（但本来也是死代码，功能无变化）。Step 4 会正式接通。

### 验收标准

- [ ] 项目编译通过
- [ ] CLI 运行输出与改动前一致
- [ ] `IPromptCompiler` 只有 `Compile<T>` 一个方法

---

## Step 2 — 将 SerializeConfiguration 上移到 Abstractions

**解决**: P5  
**改动范围**: `Abstractions`、`Core`  
**改动量**: 小

### 目标

将序列化配置数据类从 `Core` 移至 `Abstractions`，让接口层可以引用配置数据。

### 改动清单

#### 2.1 在 Abstractions 中新建配置文件

新建 `src/PromptForge.Abstractions/Serialization/` 目录，创建：

```csharp
// src/PromptForge.Abstractions/Serialization/SerializeConfiguration.cs
namespace PromptForge.Abstractions.Serialization;

public class SerializeConfiguration
{
    public IEnumerable<string> IgnoredProperties { get; init; } = [];
    public Func<object, ISerializer, string>? TypeSerializer { get; init; }
    public Dictionary<string, Func<object, ISerializer, string>> PropertySerializers { get; init; } = [];
}
```

```csharp
// src/PromptForge.Abstractions/Serialization/DeserializeConfiguration.cs
namespace PromptForge.Abstractions.Serialization;

public class DeserializeConfiguration
{
    public Func<string, ISerializer, object?>? TypeDeserializer { get; init; }
    public Dictionary<string, Func<string, ISerializer, object?>> PropertyDeserializers { get; init; } = [];
}
```

#### 2.2 删除 Core 中的重复定义

从 `src/PromptForge.Core/Serializer.cs` 顶部删除 `SerializeConfiguration` 和 `DeserializeConfiguration` 两个类，改为 using：

```csharp
using PromptForge.Abstractions.Serialization;
```

#### 2.3 同步更新 TypeConfiguration.cs 中的 using

```csharp
// src/PromptForge.Core/TypeConfiguration.cs
using PromptForge.Abstractions.Serialization; // 新增
```

### 验收标准

- [ ] 项目编译通过
- [ ] `SerializeConfiguration` / `DeserializeConfiguration` 只在 Abstractions 中定义一份
- [ ] Core 中所有相关类 using 指向 Abstractions

---

## Step 3 — 为 TypeMetadataBuilder 提取 ITypeMetadataBuilder 接口

**解决**: P4  
**改动范围**: `Abstractions`、`Core`  
**改动量**: 小

### 目标

在 Abstractions 中定义 `ITypeMetadataBuilder` 接口，使元数据解析能力可被替换和测试。

### 改动清单

#### 3.1 在 Abstractions 中新建接口

```csharp
// src/PromptForge.Abstractions/ITypeMetadataBuilder.cs
using PromptForge.Abstractions.Model;

namespace PromptForge.Abstractions;

public interface ITypeMetadataBuilder
{
    ITypeDefinition FromClrType(Type clrType);
    IReadOnlyDictionary<Type, ITypeDefinition> ClrToTypeDefinitions { get; }
}
```

#### 3.2 修改 TypeMetadataBuilder.cs 实现接口

```csharp
// src/PromptForge.Core/TypeMetadataBuilder.cs
public class TypeMetadataBuilder : ITypeMetadataBuilder
{
    // 已有实现保持不变，确认 ClrToTypeDefinitions 属性签名匹配接口
}
```

### 验收标准

- [ ] 项目编译通过
- [ ] `TypeMetadataBuilder` 已声明实现 `ITypeMetadataBuilder`

---

## Step 4 — 接通序列化管道（消除死代码）

**解决**: P2（完整修复）  
**改动范围**: `Abstractions`、`Core`  
**改动量**: 中

### 目标

在 Abstractions 中新增 `ISerializerFactory` 接口；在 Core 中实现工厂类；让 `PromptCompiler.Compile<T>()` 真正使用注入的 `ISerializer`。

### 改动清单

#### 4.1 在 Abstractions 中新增 ISerializerFactory

```csharp
// src/PromptForge.Abstractions/ISerializerFactory.cs
using PromptForge.Abstractions.Serialization;

namespace PromptForge.Abstractions;

public interface ISerializerFactory
{
    ISerializer Create(
        IReadOnlyDictionary<Type, SerializeConfiguration> serializers,
        IReadOnlyDictionary<Type, DeserializeConfiguration> deserializers);
}
```

#### 4.2 在 Core 中实现 SerializerFactory

```csharp
// src/PromptForge.Core/SerializerFactory.cs
using PromptForge.Abstractions;
using PromptForge.Abstractions.Serialization;

namespace PromptForge.Core;

public class SerializerFactory : ISerializerFactory
{
    public ISerializer Create(
        IReadOnlyDictionary<Type, SerializeConfiguration> serializers,
        IReadOnlyDictionary<Type, DeserializeConfiguration> deserializers)
    {
        return new Serializer(
            new Dictionary<Type, SerializeConfiguration>(serializers),
            new Dictionary<Type, DeserializeConfiguration>(deserializers));
    }
}
```

#### 4.3 在 PromptCompiler.Compile<T>() 中使用 serializer

根据实际渲染逻辑，在值获取（`BuildValueGetter`）或输出格式化阶段接入 `serializer`：

```csharp
// src/PromptForge.Core/PromptCompiler.cs
public partial class PromptCompiler(ISerializer serializer) : IPromptCompiler
{
    // Compile 方法中，将 serializer 传递给需要序列化运行时值的部分
    // 具体接入点取决于运行时值渲染逻辑（如 BuildValueGetter 生成的委托）
}
```

> 📌 此步需要结合实际运行时渲染逻辑判断接入点，可能需要调整 `PromptTemplate<T>` 的构造。

### 验收标准

- [ ] `ISerializerFactory` 接口已在 Abstractions 中定义
- [ ] `SerializerFactory` 实现类在 Core 中
- [ ] `PromptCompiler` 中 `_serializer` 字段不再是死代码
- [ ] CLI 运行时值能通过自定义序列化器正确格式化

---

## Step 5 — 重构 PromptBuilder，消除内部 new

**解决**: P3  
**改动范围**: `Core`  
**改动量**: 中

### 目标

通过构造函数注入 `ITypeMetadataBuilder` 和 `ISerializerFactory`，让 `PromptBuilder` 不再创建任何具体依赖。

### 改动清单

#### 5.1 修改 PromptBuilder 构造函数

```csharp
// src/PromptForge.Core/PromptBuilder.cs
public class PromptBuilder<TInput, TOutput>(
    IPromptCompiler compiler,
    ITypeMetadataBuilder metadataBuilder,
    ISerializerFactory serializerFactory)
{
    private string? _template;
    private readonly Dictionary<Type, ITypeConfiguration> _types = [];

    public IPromptTemplate<TInput> Build()
    {
        if (_template == null) throw new InvalidOperationException("Template not set.");

        // 使用注入的 metadataBuilder，不再 new TypeMetadataBuilder()
        var inputDef = metadataBuilder.FromClrType(typeof(TInput));
        var outputDef = metadataBuilder.FromClrType(typeof(TOutput));

        var pairs = metadataBuilder.ClrToTypeDefinitions
            .Join(_types, kvp => kvp.Key, kvp => kvp.Key,
                (kvp1, kvp2) => (kvp1.Value, kvp2.Value));

        if (inputDef is not ObjectType objInputDef)
            throw new InvalidOperationException("Input type not supported.");

        foreach (var (definition, configuration) in pairs)
            configuration.OverrideType(definition);

        var contract = new PromptContract(_template, objInputDef, outputDef);

        // 使用注入的 serializerFactory，不再 new Serializer(...)
        // PromptCompiler 已通过构造函数持有 serializer，此处创建新的 Serializer
        // 实际上这里可能需要用 serializerFactory 重新创建一个带配置的 compiler
        // 详见下方「设计选择」
        var serializer = serializerFactory.Create(
            _types.Select(kvp => (kvp.Key, kvp.Value.GetSerializeConfiguration()))
                  .Where(x => x.Item2 is not null)
                  .ToDictionary()!,
            _types.Select(kvp => (kvp.Key, kvp.Value.GetDeserializeConfiguration()))
                  .Where(x => x.Item2 is not null)
                  .ToDictionary()!);

        // compiler 需要使用此次 Build 的 serializer 配置
        // 建议将 serializer 传入 Compile 调用（见 Step 4 接入点设计）
        return compiler.Compile<TInput>(contract);
    }
}
```

> **设计选择**: 由于每次 `Build()` 的序列化配置不同，需要决定 `ISerializer` 是 per-build 还是共享的。  
> 推荐方案：让 `Compile<T>(contract, serializer)` 接受 `ISerializer` 参数，而不是从构造函数注入（构造函数注入适合全局默认，per-build 配置适合方法参数传入）。  
> 具体接口调整见 Step 4。

### 验收标准

- [ ] `PromptBuilder.Build()` 中没有 `new TypeMetadataBuilder()` 或 `new Serializer()`
- [ ] 所有依赖通过构造函数注入
- [ ] CLI 运行正常

---

## Step 6 — 添加 DI 注册扩展，建立组合根

**解决**: P6  
**改动范围**: `Core`（新增）、`Cli`  
**改动量**: 小

### 目标

在 Core 层提供统一的 DI 注册入口；CLI 通过 DI 获取服务，不再手动 `new`。

### 改动清单

#### 6.1 新建 ServiceCollectionExtensions.cs

```csharp
// src/PromptForge.Core/Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using PromptForge.Abstractions;

namespace PromptForge.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPromptForge(this IServiceCollection services)
    {
        services.AddSingleton<ITypeMetadataBuilder, TypeMetadataBuilder>();
        services.AddSingleton<ISerializerFactory, SerializerFactory>();
        services.AddSingleton<ISerializer>(sp =>
            sp.GetRequiredService<ISerializerFactory>().Create(
                new Dictionary<Type, Serialization.SerializeConfiguration>(),
                new Dictionary<Type, Serialization.DeserializeConfiguration>()));
        services.AddSingleton<IPromptCompiler, PromptCompiler>();
        return services;
    }
}
```

> 📌 需要在 Core 的 .csproj 中添加 `Microsoft.Extensions.DependencyInjection.Abstractions` 包引用。

#### 6.2 修改 CLI Program.cs

```csharp
// src/PromptForge.Cli/Program.cs
using Microsoft.Extensions.DependencyInjection;
using PromptForge.Abstractions;
using PromptForge.Core.Extensions;

var services = new ServiceCollection()
    .AddPromptForge()
    .BuildServiceProvider();

var compiler = services.GetRequiredService<IPromptCompiler>();
var metadataBuilder = services.GetRequiredService<ITypeMetadataBuilder>();
var serializerFactory = services.GetRequiredService<ISerializerFactory>();

var builder = new PromptBuilder<EvaluationInput, string[]>(
    compiler, metadataBuilder, serializerFactory)
    .WithTemplate(...)
    .WithType<StructData>(...)
    .WithType<string[]>(...);
```

#### 6.3 调整 Cli 项目的 csproj 引用

```xml
<!-- src/PromptForge.Cli/PromptForge.Cli.csproj -->
<!-- 将直接引用 Core 改为只引用 Abstractions（若 PromptBuilder 留在 Core 则仍需引用 Core） -->
<ItemGroup>
  <ProjectReference Include="..\PromptForge.Core\PromptForge.Core.csproj" />
  <ProjectReference Include="..\PromptForge.Abstractions\PromptForge.Abstractions.csproj" />
</ItemGroup>
```

### 验收标准

- [ ] `Program.cs` 中无 `new PromptCompiler()` 或 `new TypeMetadataBuilder()`
- [ ] 所有服务通过 DI 解析
- [ ] 项目编译通过，CLI 运行正常

---

## 完成后架构图

```
PromptForge.Cli
  ├── 引用 ───► PromptForge.Abstractions (接口 + 配置数据类型)
  └── 引用 ───► PromptForge.Core (仅用于 AddPromptForge() 注册)

PromptForge.Core
  └── 引用 ───► PromptForge.Abstractions

PromptForge.Abstractions
  └── 零依赖
```

---

## 进度追踪

| Step | 标题 | 状态 |
|------|------|------|
| Step 1 | 修正 IPromptCompiler 接口 | ⬜ 待开始 |
| Step 2 | 将 SerializeConfiguration 上移 | ⬜ 待开始 |
| Step 3 | 提取 ITypeMetadataBuilder 接口 | ⬜ 待开始 |
| Step 4 | 接通序列化管道 | ⬜ 待开始 |
| Step 5 | 重构 PromptBuilder | ⬜ 待开始 |
| Step 6 | 添加 DI 注册，建立组合根 | ⬜ 待开始 |

将 `⬜` 改为 `✅` 标记已完成的步骤。
