# PromptForge 解耦问题分析

> 文档版本: v1.0  
> 日期: 2026-07-01  
> 项目分支: main  

---

## 背景

当前 PromptForge 项目已建立三层架构（Abstractions → Core → Cli），方向正确，但在接口设计、依赖创建和职责划分上存在若干耦合问题，导致可测试性差、实现难以替换。

---

## 架构现状

```
PromptForge.Cli
  └── 直接依赖 ───► PromptForge.Core
                        └── 依赖 ───► PromptForge.Abstractions
```

理想状态下，Cli 应**只依赖 Abstractions**，通过 DI 注入 Core 的实现。

---

## 问题清单

### P1 — IPromptCompiler 接口违反接口隔离原则 (ISP)

**严重程度**: 高  
**所在文件**: [src/PromptForge.Abstractions/IPromptCompiler.cs](../src/PromptForge.Abstractions/IPromptCompiler.cs)

**问题代码**:
```csharp
public interface IPromptCompiler
{
    IPromptTemplate<T> Compile<T>(PromptContract contract);
    void SetSerialization(ISerializer serializer);  // ← 配置职责混入编译接口
}
```

**根因**: `SetSerialization` 是一个构建期配置操作，不属于"编译"的语义范畴。调用方需要先 `SetSerialization` 才能 `Compile`，形成了隐式的调用顺序依赖（时序耦合）。  

**影响**:
- 实现 `IPromptCompiler` 的类必须维护可变状态 `_serializer`
- 任何使用 `IPromptCompiler` 的地方都必须知晓并执行配置步骤
- 接口无法安全地用于多线程场景（共享 `_serializer` 状态）

---

### P2 — PromptCompiler 内的 _serializer 是死代码

**严重程度**: 高  
**所在文件**: [src/PromptForge.Core/PromptCompiler.cs](../src/PromptForge.Core/PromptCompiler.cs)

**问题代码**:
```csharp
public partial class PromptCompiler : IPromptCompiler
{
    private ISerializer _serializer = new Serializer([], []);  // ← 初始化但从不使用

    public void SetSerialization(ISerializer serializer)
    {
        _serializer = serializer;  // ← 设置了，但 Compile() 中从不读取
    }
}
```

**根因**: `Compile<T>()` 方法全程未引用 `_serializer`，序列化管道尚未接通，但接口层已为此预留了方法，形成"幽灵设计"。  

**影响**:
- 调用方在 `PromptBuilder.Build()` 中构造了 `Serializer` 并注入，但实际上完全无效
- 误导后续开发者，增加理解成本

---

### P3 — PromptBuilder.Build() 内部直接 new 具体类，违反依赖倒置 (DIP)

**严重程度**: 高  
**所在文件**: [src/PromptForge.Core/PromptBuilder.cs](../src/PromptForge.Core/PromptBuilder.cs)

**问题代码**:
```csharp
public IPromptTemplate<TInput> Build()
{
    var builder = new TypeMetadataBuilder();        // ← 直接 new 具体类
    ...
    compiler.SetSerialization(new Serializer(...)); // ← 再次直接 new 具体类
}
```

**根因**: `PromptBuilder` 承担了"配置者"和"工厂"的双重职责，自行决定使用哪个具体实现。  

**影响**:
- 无法在测试中替换 `TypeMetadataBuilder` 或 `Serializer`
- `PromptBuilder` 与 `Core` 层所有实现类形成强绑定，牵一发而动全身

---

### P4 — TypeMetadataBuilder 没有对应接口

**严重程度**: 中  
**所在文件**: [src/PromptForge.Core/TypeMetadataBuilder.cs](../src/PromptForge.Core/TypeMetadataBuilder.cs)

**根因**: `TypeMetadataBuilder` 是元数据解析的核心组件，但没有在 Abstractions 层定义接口，任何依赖它的类都必须直接引用具体实现。  

**影响**:
- 无法 mock，单元测试困难
- 未来若需要支持其他元数据来源（如特性驱动 vs 配置驱动），无法扩展

---

### P5 — SerializeConfiguration / DeserializeConfiguration 定义在 Core 层

**严重程度**: 中  
**所在文件**: [src/PromptForge.Core/Serializer.cs](../src/PromptForge.Core/Serializer.cs)

**根因**: 这两个类是序列化契约的数据结构（配置对象），属于"接口层语义"，却被放在实现层。导致 `Abstractions` 层的 `ISerializer` 无法引用它们，形成接口与配置数据的割裂。  

**影响**:
- `ISerializerFactory`（未来需要添加）无法在 Abstractions 中定义，因为参数类型不在 Abstractions 中
- 调用方必须同时引用 Abstractions 和 Core 才能完成序列化配置

---

### P6 — CLI 直接依赖实现层，没有组合根

**严重程度**: 中  
**所在文件**: [src/PromptForge.Cli/Program.cs](../src/PromptForge.Cli/Program.cs)

**问题代码**:
```csharp
var compiler = new PromptCompiler();        // ← 跨层直接实例化
var builder = new PromptBuilder<...>(compiler);
```

**根因**: 没有依赖注入容器，没有组合根（Composition Root），所有对象由调用方手动创建。  

**影响**:
- 替换任何实现都需要修改调用方代码
- 无法复用服务注册逻辑（如集成到 ASP.NET Core 项目时）

---

## 问题关系图

```
P1 (接口混职责)
  └── 导致 ──► P2 (死代码 _serializer)
                └── 导致 ──► P3 (Builder 内部 new)
                              └── 依赖 ──► P4 (无 ITypeMetadataBuilder 接口)

P5 (配置类在错误层)
  └── 阻碍 ──► 未来的 ISerializerFactory 定义

P6 (CLI 无 DI)
  └── 依赖 ──► 所有上述问题未解决前无法改善
```

---

## 衡量标准

解耦完成后应满足：

| 检查项 | 当前 | 目标 |
|--------|------|------|
| Cli 项目是否可以不引用 Core，只引用 Abstractions | ❌ | ✅ |
| PromptBuilder 构造函数中是否存在 `new` 具体类 | ❌ | ✅ |
| IPromptCompiler 是否只包含编译相关方法 | ❌ | ✅ |
| TypeMetadataBuilder 是否有接口 | ❌ | ✅ |
| SerializeConfiguration 是否在 Abstractions 中 | ❌ | ✅ |
| 是否有统一的 DI 注册入口 | ❌ | ✅ |
