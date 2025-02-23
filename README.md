# MemoryPack
[![GitHub Actions](https://github.com/Cysharp/MemoryPack/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/MemoryPack/actions) [![Releases](https://img.shields.io/github/release/Cysharp/MemoryPack.svg)](https://github.com/Cysharp/MemoryPack/releases)

Zero encoding extreme performance binary serializer for C# and Unity.

![image](https://user-images.githubusercontent.com/46207/192748136-262ac2e7-4646-46e1-afb8-528a51a4a987.png)

For standard object, MemoryPack is x3 faster than MessagePack for C#. For struct array, MemoryPack gots boosted power, x50~100 faster than other serializers.

MemoryPack is my 4th serializer, previously I've created well known serializers, ~~[ZeroFormatter](https://github.com/neuecc/ZeroFormatter)~~, ~~[Utf8Json](https://github.com/neuecc/Utf8Json)~~, [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp). The reason for MemoryPack's speed is due to its C#-specific, C#-optimized binary format and a well tuned implementation based on my past experience. It is also a completely new design utilizing .NET 7 and C# 11 and the Incremental Source Generator(.NET Standard 2.1(.NET 5, 6) and Unity support is also exists).

Other serializers performs many encoding operations such as VarInt encoding, tag, string, etc. MemoryPack format uses a zero-encoding design that copies as much of the C# memory as possible. zero-encoding is similar as FlatBuffers but don't need special type, MemoryPack's serialize target is POCO.

Other than performance, MemoryPack has these features.

* Support modern I/O APIs(`IBufferWriter<byte>`, `ReadOnlySpan<byte>`, `ReadOnlySequence<byte>`)
* Native AOT friendly Source Generator based code generation, no Dynamic CodeGen(IL.Emit)
* Reflectionless non-generics APIs
* Deserialize into existing instance
* Polymorphism(Union) serialization
* PipeWriter/Reader based streaming serialization
* TypeScript code generation and ASP.NET Core Formatter
* Unity(2021.3) IL2CPP Support via .NET Source Generator

Installation
---
This library is distributed via NuGet. For best performance, recommend to use `.NET 7`. Minimum requirement is `.NET Standard 2.1`.

> PM> Install-Package [MemoryPack](https://www.nuget.org/packages/MemoryPack)

And also editor requires Roslyn 4.3.0 support, for example Visual Studio 2022 version 17.3. For details, see [Roslyn Version Support](https://learn.microsoft.com/en-us/visualstudio/extensibility/roslyn-version-support) document.

For Unity, the requirements and installation process are completely different. See the [Unity](#unity) section for details.

Quick Start
---
Define the struct or class to be serialized and annotate it with a `[MemoryPackable]` attribute and `partial` keyword.

```csharp
using MemoryPack;

[MemoryPackable]
public partial class Person
{
    public int Age { get; set; }
    public string Name { get; set; }
}
```

Serialization code is generated via C# source generator feature, that implements `IMemoryPackable<T>` interface. In Visual Studio, you can check generated code via `Ctrl+K, R` on class name and select `*.MemoryPackFormatter.g.cs`.

Call `MemoryPackSerializer.Serialize<T>/Deserialize<T>` to serialize/deserialize your object instance.

```csharp
var v = new Person { Age = 40, Name = "John" };

var bin = MemoryPackSerializer.Serialize(v);
var val = MemoryPackSerializer.Deserialize<Person>(bin);
```

Serialize method supports return `byte[]` and serialize to `IBufferWriter<byte>` or `Stream`. Deserialize method supports `ReadOnlySpan<byte>`, `ReadOnlySeqeunce<byte>` and `Stream`. And also there have non-generics version.

Built-in supported types
---
These types can serialize by default:

* .NET primitives (`byte`, `int`, `bool`, `char`, `double`, etc...)
* Unmanaged types(Any `enum`, Any user-defined `strcut` that no contains reference type)
* `string`, `decimal`, `Half`, `Int128`, `UInt128`, `Guid`, `Rune`, `BigInteger`
* `TimeSpan`,  `DateTime`, `DateTimeOffset`, `TimeOnly`, `DateOnly`, `TimeZoneInfo`
* `Complex`, `Plane`, `Quaternion` `Matrix3x2`, `Matrix4x4`, `Vector2`, `Vector3`, `Vector4`
* `Uri`, `Version`, `StringBuilder`, `Type`, `BitArray`
* `T[]`, `T[,]`, `T[,,]`, `T[,,,]`, `Memory<>`, `ReadOnlyMemory<>`, `ArraySegment<>`, `ReadOnlySequence<>`
* `Nullable<>`, `Lazy<>`, `KeyValuePair<,>`, `Tuple<,...>`, `ValueTuple<,...>`
* `List<>`, `LinkedList<>`, `Queue<>`, `Stack<>`, `HashSet<>`, `PriorityQueue<,>`
* `Dictionary<,>`, `SortedList<,>`, `SortedDictionary<,>`,  `ReadOnlyDictionary<,>` 
* `Collection<>`, `ReadOnlyCollection<>`,`ObservableCollection<>`, `ReadOnlyObservableCollection<>`
* `IEnumerable<>`, `ICollection<>`, `IList<>`, `IReadOnlyCollection<>`, `IReadOnlyList<>`, `ISet<>`
* `IDictionary<,>`, `IReadOnlyDictionary<,>`, `ILookup<,>`, `IGrouping<,>`,
* `ConcurrentBag<>`, `ConcurrentQueue<>`, `ConcurrentStack<>`, `ConcurrentDictionary<,>`, `BlockingCollection<>`
* Immutable collections (`ImmutableList<>`, etc) and interfaces (`IImmutableList<>`, etc)

Define `[MemoryPackable]` `class` / `struct` / `record` / `record struct`
---
`[MemoryPackable]` can annotate to any `class`, `struct`, `record`, `record struct` and `interface`. If type is `struct` or `record struct` and that contains no reference type([C# Unmanaged types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/unmanaged-types)), any additional annotation(ignore, include, constructor, callbacks) is not used, that serialize/deserialize directly from the memory.

Otherwise, in the default, `[MemoryPackable]` serializes public instance property or field. You can use `[MemoryPackIgnore]` to remove serialization target, `[MemoryPackInclude]` promotes a private member to serialization target.

```csharp
[MemoryPackable]
public partial class Sample
{
    // these types are serialized by default
    public int PublicField;
    public readonly int PublicReadOnlyField;
    public int PublicProperty { get; set; }
    public int PrivateSetPublicProperty { get; private set; }
    public int ReadOnlyPublicProperty { get; }
    public int InitProperty { get; init; }
    public required int RequiredInitProperty { get; init; }

    // these types are not serialized by default
    int privateProperty { get; set; }
    int privateField;
    readonly int privateReadOnlyField;

    // use [MemoryPackIgnore] to remove target of public member
    [MemoryPackIgnore]
    public int PublicProperty2 => PublicProperty + PublicField;

    // use [MemoryPackInclude] to promote private member to serialization target
    [MemoryPackInclude]
    int privateField2;
    [MemoryPackInclude]
    int privateProperty2 { get; set; }
}
```

Which members are serialized, you can check IntelliSense in type(code genreator makes serialization info to `<remarks />` comment).

![image](https://user-images.githubusercontent.com/46207/192393984-9af01fcb-872e-46fb-b08f-4783e8cef4ae.png)

All members must be memorypack-serializable, if not, code generator reports error.

![image](https://user-images.githubusercontent.com/46207/192413557-8a47d668-5339-46c5-a3da-a77841666f81.png)

MemoryPack has 24 diagnostics rules(`MEMPACK001` to `MEMPACK026`) to be define comfortably.

If target type is defined MemoryPack serialization externally and registered, use `[MemoryPackAllowSerialize]` to silent diagnostics.

```csharp
[MemoryPackable]
public partial class Sample2
{
    [MemoryPackAllowSerialize]
    public NotSerializableType? NotSerializableProperty { get; set; }
}
```

Member order is **important**, MemoryPack does not serialize any member-name and other tags, serialize in the declared order. If the type is inherited, serialize in the order of parent → child. Member orders can not change for the deserialization. For the schema evolution, see [Version tolerant](#version-tolerant)  section.

Default order is sequential but you can choose explicit layout with `[MemoryPackable(SerializeLayout.Explicit)]` and `[MemoryPackOrder()]`.

```csharp
// serialize Prop0 -> Prop1
[MemoryPackable(SerializeLayout.Explicit)]
public partial class SampleExplicitOrder
{
    [MemoryPackOrder(1)]
    public int Prop1 { get; set; }
    [MemoryPackOrder(0)]
    public int Prop0 { get; set; }
}
```

### Constructor selection

MemoryPack supports parameterized constructor not only parameterless constructor. The selection of the constructor follows these rules. Both class and struct follows same.

* If has `[MemoryPackConstructor]`, use it
* If has no explicit constructor(includes private), use parameterless one
* If has a one parameterless/parameterized constructor(includes private), use it
* If has multiple constructors, must apply `[MemoryPackConstructor]` attribute(no automatically choose one), otherwise generator error it.
* If choosed parameterized constructor, all parameter name must match with member name(case-insensitive)

```csharp
[MemoryPackable]
public partial class Person
{
    public readonly int Age;
    public readonly string Name;

    // You can use parametarized constructor(paramter name must match with member names)
    public Person(int age, string name)
    {
        this.Age = age;
        this.Name = name;
    }
}

// also supports record primary constructor
[MemoryPackable]
public partial record Person2(int Age, string Name);

public partial class Person3
{
    public int Age { get; set; }
    public string Name { get; set; }

    public Person3()
    {
    }

    // If exists multiple constructors, must use [MemoryPackConstructor]
    [MemoryPackConstructor]
    public Person3(int age, string name)
    {
        this.Age = age;
        this.Name = name;
    }
}
```

### Serialization callbacks

When serialize, deserialize, MemoryPack can hook before/after event with `[MemoryPackOnSerializing]`, `[MemoryPackOnSerialized]`, `[MemoryPackOnDeserializing]`, `[MemoryPackOnDeserialized]` attributes. It can annotate both static and instance, public and private method but must be paramterless method.

```csharp
[MemoryPackable]
public partial class MethodCallSample
{
    // method call order is static -> instance
    [MemoryPackOnSerializing]
    public static void OnSerializing1()
    {
        Console.WriteLine(nameof(OnSerializing1));
    }

    // also allows private method
    [MemoryPackOnSerializing]
    void OnSerializing2()
    {
        Console.WriteLine(nameof(OnSerializing2));
    }

    // serializing -> /* serialize */ -> serialized
    [MemoryPackOnSerialized]
    static void OnSerialized1()
    {
        Console.WriteLine(nameof(OnSerialized1));
    }

    [MemoryPackOnSerialized]
    public void OnSerialized2()
    {
        Console.WriteLine(nameof(OnSerialized2));
    }

    [MemoryPackOnDeserializing]
    public static void OnDeserializing1()
    {
        Console.WriteLine(nameof(OnDeserializing1));
    }

    // Note: instance method with MemoryPackOnDeserializing, that not called if instance is not passed by `ref`
    [MemoryPackOnDeserializing]
    public void OnDeserializing2()
    {
        Console.WriteLine(nameof(OnDeserializing2));
    }

    [MemoryPackOnDeserialized]
    public static void OnDeserialized1()
    {
        Console.WriteLine(nameof(OnDeserialized1));
    }

    [MemoryPackOnDeserialized]
    public void OnDeserialized2()
    {
        Console.WriteLine(nameof(OnDeserialized2));
    }
}
```

Define custom collection
---
In default, annotated `[MemoryPackObject]` type try to search members. However if type is collection(`ICollection<>`, `ISet<>`, `IDictionary<,>`), you can change `GenreateType.Collection` to serialize correctly.

```csharp
[MemoryPackable(GenerateType.Collection)]
public partial class MyList<T> : List<T>
{
}

[MemoryPackable(GenerateType.Collection)]
public partial class MyStringDictionary<TValue> : Dictionary<string, TValue>
{

}
```

Polymorphism(Union)
---
MemoryPack supports serializing interface and abstract class objects for polymorphism serialization. In MemoryPack these are called Union. Only interfaces and abstracts classes are allowed to be annotated with `[MemoryPackUnion]` attributes. Unique union tags are required.

```csharp
// Annotate [MemoryPackable] and inheritance types by [MemoryPackUnion]
// Union also supports abstract class
[MemoryPackable]
[MemoryPackUnion(0, typeof(FooClass))]
[MemoryPackUnion(1, typeof(BarClass))]
public partial interface IUnionSample
{
}

[MemoryPackable]
public partial class FooClass : IUnionSample
{
    public int XYZ { get; set; }
}

[MemoryPackable]
public partial class BarClass : IUnionSample
{
    public string? OPQ { get; set; }
}
// ---

IUnionSample data = new FooClass() { XYZ = 999 };

// Serialize as interface type.
var bin = MemoryPackSerializer.Serialize(data);

// Deserialize as interface type.
var reData = MemoryPackSerializer.Deserialize<IUnionSample>(bin);

switch (reData)
{
    case FooClass x:
        Console.WriteLine(x.XYZ);
        break;
    case BarClass x:
        Console.WriteLine(x.OPQ);
        break;
    default:
        break;
}
```

Serialize API
---
Serialize has three overloads.

```csharp
// Non generic API also available, these version is first argument is Type and value is object?
byte[] Serialize<T>(in T? value, MemoryPackSerializeOptions? options = default)
void Serialize<T, TBufferWriter>(in TBufferWriter bufferWriter, in T? value, MemoryPackSerializeOptions? options = default)
async ValueTask SerializeAsync<T>(Stream stream, T? value, MemoryPackSerializeOptions? options = default, CancellationToken cancellationToken = default)
```

The recommended way to do this in Performance is to use `BufferWriter`. This serializes directly into the buffer. It can be applied to `PipeWriter` in `System.IO.Pipelines`, `BodyWriter` in ASP .NET Core, etc.

If a `byte[]` is required (e.g. `RedisValue` in [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)), return `byte[]` API is simple and almostly fast.

Note that `SerializeAsync` for `Stream` is asynchronous only for Flush; it serializes everything once into MemoryPack's internal pool buffer and then writes it out with WriteAsync. Therefore, BufferWriter overloading, which separates and controls buffer and flush, is better.

If you want to do complete streaming write, see [Streaming Serialization](#streaming-serialization) section.

### MemoryPackSerializeOptions

`MemoryPackSerializeOptions` configures how serialize string as Utf16 or Utf8. If passing null then uses `MemoryPackSerializeOptions.Default`, it is same as `MemoryPackSerializeOptions.Utf8`, in other words, serialize the string as Utf8. If you want to serialize with Utf16, you can use `MemoryPackSerializeOptions.Utf16`.

Since C#'s internal string representation is UTF16, UTF16 performs better. However, the payload tends to be larger; in UTF8, an ASCII string is one byte, while in UTF16 it is two bytes. Because the difference in size of this payload is so large, UTF8 is set by default.

If the data is non-ASCII (e.g. Japanese, which can be more than 3 bytes, and UTF8 is larger), or if you have to compress it separately, UTF16 may give better results.

Whether UTF8 or UTF16 is selected during serialization, it is not necessary to specify it during deserialization. It will be automatically detected and deserialized normally.

Deserialize API
---
Deserialize has `ReadOnlySpan<byte>` and `ReadOnlySequence<byte>`, `Stream` overload and `ref` support.

```csharp
T? Deserialize<T>(ReadOnlySpan<byte> buffer)
void Deserialize<T>(ReadOnlySpan<byte> buffer, ref T? value)
T? Deserialize<T>(in ReadOnlySequence<byte> buffer)
void Deserialize<T>(in ReadOnlySequence<byte> buffer, ref T? value)
async ValueTask<T?> DeserializeAsync<T>(Stream stream)
```

`ref` overload overwrite existing instance, for details see [Overwrite](#overwrite) section.

`DeserializeAsync(Stream)` is not completely streaming read, first read into MemoryPack's internal pool up to the end-of-stream, then deserialize.

If you want to do complete streaming read, see [Streaming Serialization](#streaming-serialization) section.

Overwrite
---
MemoryPack supports deserialize to existing instance, that reduce new instance allocation. It can use by `Deserialize(ref T? value)` overload.

```csharp
var person = new Person();
var bin = MemoryPackSerializer.Serialize(person);

// overwrite data to existing instance.
MemoryPackSerializer.Deserialize(bin, ref person);
```

MemoryPack will attempt to overwrite as much as possible, but if the conditions do not match, it will create a new instance (as in normal deserialization).

* ref value(includes members in object graph) is null, set new instance
* only allows parameterless constructor, if parametarized constructor is used, create new instance
* if value is `T[]`, reuse only if the length is the same, otherwise create new instance
* if value is collection that has `.Clear()` method(`List<>`, `Stack<>`, `Queue<>`, `LinkedList<>`, `HashSet<>`, `PriorityQueue<,>`, `ObservableCollection`, `Collection`, `ConcurrentQueue<>`, `ConcurrentStack<>`, `ConcurrentBag<>`, `Dictionary<,>`, `SoretedDictionary<,>`, `SortedList<,>`, `ConcurrentDictionary<,>`) call Clear() and reuse it, otherwise create new instance

Version tolerant
---
MemoryPack supports schema evolution limitedly.

* unmanaged struct can't change any more
* MemoryPackable objects, members can be added, but not deleted
* MemoryPackable objects, can't change member order

```csharp
[MemoryPackable]
public partial class VersionCheck
{
    public int Prop1 { get; set; }
    public long Prop2 { get; set; }
}

// Add is OK.
[MemoryPackable]
public partial class VersionCheck
{
    public int Prop1 { get; set; }
    public long Prop2 { get; set; }
    public int? AddedProp { get; set; }
}

// Remove is NG.
[MemoryPackable]
public partial class VersionCheck
{
    // public int Prop1 { get; set; }
    public long Prop2 { get; set; }
}

// Change order is NG.
[MemoryPackable]
public partial class VersionCheck
{
    public long Prop2 { get; set; }
    public int Prop1 { get; set; }
}
```

In use-case, store old data(to file, to redis, etc...) and read to new schema is always ok. In RPC scenario, schema exists both client server, the client must be updated before the server. An updated client has no problem connecting to the old server but old client can not connect to new server.

There are plans to include an option in the future to allow client/server bidirectional version tolerance, although the performance will be inferior. However, currently there is none.

Next [Serialization info](#serialization-info) section shows how to check for schema changes, e.g., by CI, to prevent accidents.

Serialization info
----
Which members are serialized, you can check IntelliSense in type. There is an option to write that information to a file at compile time. Set `MemoryPackGenerator_SerializationInfoOutputDirectory` as follows.

```xml
<!-- output memorypack serialization info to directory -->
<ItemGroup>
    <CompilerVisibleProperty Include="MemoryPackGenerator_SerializationInfoOutputDirectory" />
</ItemGroup>
<PropertyGroup>
    <MemoryPackGenerator_SerializationInfoOutputDirectory>$(MSBuildProjectDirectory)\MemoryPackLogs</MemoryPackGenerator_SerializationInfoOutputDirectory>
</PropertyGroup>
```

The following info is written to the file.

![image](https://user-images.githubusercontent.com/46207/192460684-c2fd8bcb-375e-41dd-9960-58205d5b1b7a.png)

If the type is unmanaged, showed `unmanaged` before type name.

```txt
unmanaged FooStruct
---
int x
int y
```

By checking the differences in this file, dangerous schema changes can be prevented. For example, you may want to use CI to detect the following rules

* modify unmanaged type
* member order change
* member deletion

Performance
---
TODO for describe details, stay tuned.

Payload size and compression
---
Payload size depends on the target value; unlike JSON, there are no keys and it is a binary format, so the payload size is likely to be smaller than JSON.

For those with varint encoding, such as MessagePack and Protobuf, MemoryPack tends to be larger if ints are used a lot (in MemoryPack, ints are always 4 bytes due to fixed size encoding, while MsgPack is 1~5 bytes).

float and double are 4 bytes and 8 bytes in MemoryPack, but 5 bytes and 9 bytes in MsgPack. So MemoryPack is smaller, for example, for Vector3 (float, float, float) arrays.

String is UTF8 by default, which is similar to other serializers, but if the UTF16 option is chosen, it will be of a different nature.

In any case, if the payload size is large, compression should be considered. LZ4, ZStandard and Brotli are recommended.

### Compression

MemoryPack provides an efficient helper for [Brotli](https://github.com/google/brotli) compression via [BrotliEncoder](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.brotliencoder) and [BrotliDecoder](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.brotlidecoder). MemoryPack's `BrotliCompressor` and `BrotliDecompressor` provide compression/decompression optimized for MemoryPack's internal behavior.

```csharp
using MemoryPack.Compression;

// Compression(require using)
using var compressor = new BrotliCompressor();
MemoryPackSerializer.Serialize(compressor, value);

// Get compressed byte[]
var compressedBytes = compressor.ToArray();

// Or write to other IBufferWriter<byte>(for example PipeWriter)
compressor.CopyTo(response.BodyWriter);
```

```csharp
using MemoryPack.Compression;

// Decompression(require using)
using var decompressor = new BrotliDecompressor();

// Get decompressed ReadOnlySequence<byte> from ReadOnlySpan<byte> or ReadOnlySequence<byte>
var decompressedBuffer = decompressor.Decompress(buffer);

var value = MemoryPackSerializer.Deserialize<T>(decompressedBuffer);
```

Both `BrotliCompressor` and `BrotliDecompressor` are struct, it does not allocate memory on heap. Both store compressed or decompressed data in an internal memory pool for Serialize/Deserialize.  Therefore, it is necessary to release the memory pooling, don't forget to use `using`.

Compression level is very important. The default is set to quality-1 (CompressionLevel.Fastest), which is different from the .NET default (CompressionLevel.Optimal, quality-4).

Fastest (quality-1) will be close to the speed of [LZ4](https://github.com/lz4/lz4), but 4 is much slower. This was determined to be critical in the serializer use scenario. Be careful when using the standard `BrotliStream`(quality-4 is the default). In any case, compression/decompression speeds and sizes will result in very different results for different data. Please prepare the data to be handled by your application and test it yourself.

Note that there is a several-fold speed penalty between MemoryPack's uncompressed and Brotli's added compression.

Packages
---
MemoryPack has thesed packages.

* MemoryPack
* MemoryPack.Core
* MemoryPack.Generator
* MemoryPack.Streaming
* MemoryPack.AspNetCoreMvcFormatter

Mainly you only reference `MemoryPack`, this both `MemoryPack.Core` and `MemoryPack.Generator`. If you want to use [Streaming Serialization](#streaming-serialization), additionaly use `MemoryPack.Streaming`. `MemoryPack.AspNetCoreMvcFormatter` is input/output formatter for ASP.NET Core.

TypeScript and ASP.NET Core Formatter
---
MemoryPack supports TypeScript code generation. It generates class and serialization code from C#, In other words, you can share types with the Browser without using OpenAPI, proto, etc.

Code generation is integrated with Source Generator, the following options(`MemoryPackGenerator_TypeScriptOutputDirectory`) set the output directory for TypeScript code. Runtime code is output at the same time, so no additional dependencies are required.

```xml
<!-- output memorypack TypeScript code to directory -->
<ItemGroup>
    <CompilerVisibleProperty Include="MemoryPackGenerator_TypeScriptOutputDirectory" />
</ItemGroup>
<PropertyGroup>
    <MemoryPackGenerator_TypeScriptOutputDirectory>$(MSBuildProjectDirectory)\wwwroot\js\memorypack</MemoryPackGenerator_TypeScriptOutputDirectory>
</PropertyGroup>
```

At first, require to annotate `[GenerateTypeScript]` to C# MemoryPackable type.

```csharp
[MemoryPackable]
[GenerateTypeScript]
public partial class Person
{
    public required Guid Id { get; init; }
    public required int Age { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required Gender Gender { get; init; }
    public required string[] Emails { get; init; }
}

public enum Gender
{
    Male, Female, Other
}
```

Runtime code and TypeScript type will generate to target directory.

![image](https://user-images.githubusercontent.com/46207/194916544-1b6bb5ed-966b-43c3-a378-3eac297c2b40.png)

The generated code is as follows, with simple fields and static methods for serialize/serializeArray and deserialize/deserializeArray.

```typescript
import { MemoryPackWriter } from "./MemoryPackWriter.js";
import { MemoryPackReader } from "./MemoryPackReader.js";
import { Gender } from "./Gender.js"; 

export class Person {
    id: string;
    age: number;
    firstName: string | null;
    lastName: string | null;
    dateOfBirth: Date;
    gender: Gender;
    emails: (string | null)[] | null;

    constructor() {
        // snip...
    }

    static serialize(value: Person | null): Uint8Array {
        // snip...
    }

    static serializeCore(writer: MemoryPackWriter, value: Person | null): void {
        // snip...
    }

    static serializeArray(value: (Person | null)[] | null): Uint8Array {
        // snip...
    }

    static serializeArrayCore(writer: MemoryPackWriter, value: (Person | null)[] | null): void {
        // snip...
    }
    static deserialize(buffer: ArrayBuffer): Person | null {
        // snip...
    }

    static deserializeCore(reader: MemoryPackReader): Person | null {
        // snip...
    }

    static deserializeArray(buffer: ArrayBuffer): (Person | null)[] | null {
        // snip...
    }

    static deserializeArrayCore(reader: MemoryPackReader): (Person | null)[] | null {
        // snip...
    }
}
```

You can use this type like following.

```typescript
let person = new Person();
person.id = crypto.randomUUID();
person.age = 30;
person.firstName = "foo";
person.lastName = "bar";
person.dateOfBirth = new Date(1999, 12, 31, 0, 0, 0);
person.gender = Gender.Other;
person.emails = ["foo@bar.com", "zoo@bar.net"];

// serialize to Uint8Array
let bin = Person.serialize(person);

let blob = new Blob([bin.buffer], { type: "application/x-memorypack" })

let response = await fetch("http://localhost:5260/api",
    { method: "POST", body: blob, headers: { "Content-Type": "application/x-memorypack" } });

let buffer = await response.arrayBuffer();

// deserialize from ArrayBuffer 
let person2 = Person.deserialize(buffer);
```

We're also providing `MemoryPack.AspNetCoreMvcFormatter` package, you can add `MemoryPackInputFormatter`, `MemoryPackOutputFormatter` to ASP.NET Core MVC.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, new MemoryPackInputFormatter());
    options.OutputFormatters.Insert(0, new MemoryPackOutputFormatter());
});
```

### TypeScript Type Mapping

There are a few restrictions on the types that can be generated. Among the primitives, `char` and `decimal` are not supported. Also, OpenGenerics type cannot be used.

|  C#  |  TypeScript  | Description |
| ---- | ---- | ---- |
| `bool` |  `boolean`  |
| `byte` |  `number`  |
| `sbyte` |  `number`  |
| `int` |  `number` |
| `uint` |  `number` |
| `short` |  `number` |
| `ushort` |  `number` |
| `long` |  `bigint` |
| `ulong` |  `bigint` |
| `float` |  `number` |
| `double` |  `number` |
| `string` |  `string \| null`  | 
| `Guid` |  `string`  | In TypeScript, represents as string but serialize/deserialize as 16byte binary
| `DateTime` | `Date` | DateTimeKind will be ignored
| `enum` | `const enum` | `long` and `ulong` underlying type is not supported
| `T?` | `T \| null` |
| `T[]` | `T[] \| null` |
| `byte[]` | `Uint8Array \| null` |
| `: ICollection<T>` | `T[] \| null` | Supports all `ICollection<T>` implemented type like `List<T>`
| `: ISet<T>` | `Set<T> \| null` | Supports all `ISet<T>` implemented type like `HashSet<T>`
| `: IDictionary<K,V>` | `Map<K, V> \| null` | Supports all `IDictionary<K,V>` implemented type like `Dictionary<K,V>`. If both `K` and `V` are unmanaged type, must be `KeyValuePair<K, V>` is non-padding data
| `[MemoryPackable]` | `class` | Supports class only
| `[MemoryPackUnion]` | `abstract class` |

`[GenerateTypeScript]` can only be applied to classes and is currently not supported by struct.

Streaming Serialization
---
`MemoryPack.Streaming` provides additional `MemoryPackStreamingSerializer`, it serialize/deserialize collection data streamingly.

```csharp
public static class MemoryPackStreamingSerializer
{
    public static async ValueTask SerializeAsync<T>(PipeWriter pipeWriter, int count, IEnumerable<T> source, int flushRate = 4096, CancellationToken cancellationToken = default)
    public static async ValueTask SerializeAsync<T>(Stream stream, int count, IEnumerable<T> source, int flushRate = 4096, CancellationToken cancellationToken = default)
    public static async IAsyncEnumerable<T?> DeserializeAsync<T>(PipeReader pipeReader, int bufferAtLeast = 4096, int readMinimumSize = 8192, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    public static IAsyncEnumerable<T?> DeserializeAsync<T>(Stream stream, int bufferAtLeast = 4096, int readMinimumSize = 8192, CancellationToken cancellationToken = default)
}
```

Formatter/Provider API
---
If you want to implement formatter manually, inherit `MemoryPackFormatter<T>` to recommended.

```csharp
public class SkeltonFormatter : MemoryPackFormatter<Skelton>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Skelton? value)
    {
        if (value == null)
        {
            writer.WriteNullObjectHeader();
            return;
        }

        // use writer method.
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref Skelton? value)
    {
        if (!reader.TryReadObjectHeader(out var count))
        {
            value = null;
            return;
        }

        // use reader method.
    }
}
```
The created formatter is registered with `MemoryPackFormatterProvider`.

```csharp
MemoryPackFormatterProvider.Register(new SkeltonFormatter());
```

Unity
---
Install via UPM git URL package or asset package(MemoryPack.*.*.*.unitypackage) available in [MemoryPack/releases](https://github.com/Cysharp/MemoryPack/releases) page.

* https://github.com/Cysharp/MemoryPack.git?path=src/MemoryPack.Unity/Assets/Plugins/MemoryPack

If you want to set a target version, MemoryPack uses the `*.*.*` release tag so you can specify a version like #1.4.2. For example `https://github.com/Cysharp/MemoryPack.git?path=src/MemoryPack.Unity/Assets/Plugins/MemoryPack#1.4.2`.

Supporting minimum Unity version is `2021.3`. The dependency managed DLL `System.Runtime.CompilerServices.Unsafe/6.0.0` is included with unitypackage. For git references, you will need to add them in another way as they are not included to avoid unnecessary dependencies; either extract the dll from unitypackage or download it from the [NuGet page](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.0.0).

As with the .NET version, the code is generated by a code generator(`MemoryPack.Generator.Roslyn3.dll`). Reflection-free implementation also provides the best performance in IL2CPP.

For more information on Unity and Source Generator, please refer to the [Unity documentation](https://docs.unity3d.com/Manual/roslyn-analyzers.html).

Source Generator is also used officially by Unity by [com.unity.properties](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html) and [com.unity.entities](https://docs.unity3d.com/Packages/com.unity.properties@2.0/changelog/CHANGELOG.html). In other words, it is the standard for code generation in the next generation of Unity.

Binary wire format specification
---
The type of `T` defined in `Serialize<T>` and `Deserialize<T>` is called C# schema. MemoryPack format is not self described format. Deserialize requires the corresponding C# schema. Six types exist as internal representations of binaries, but types cannot be determined without a C# schema.

Endian must be `Little Endian`. However reference C# implementation does not care endianness so can not use on big-endian machine. However modern computers are usually little-endian.

There are six types of format.

* Unmanaged struct
* Object
* Tuple
* Collection
* String
* Union

### Unmanaged struct

Unmanaged struct is C# struct that no contains reference type, similar constraint of [C# Unmanaged types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/unmanaged-types). Serializing struct layout as is, includes padding.

### Object

`(byte memberCount, [values...])`

Object has 1byte unsigned byte as member count in header. Member count allows `0` to `249`, `255` represents object is `null`. Values store memorypack value for the number of member count.

### Tuple

`(values...)`

Tuple is fixed-size, non-nullable value collection. In .NET, `KeyValuePair<TKey, TValue>` and `ValueTuple<T,...>` are serialized as Tuple.

### Collection

`(int length, [values...])`

Collection has 4byte signed interger as data count in header, `-1` represents `null`. Values store memorypack value for the number of length.

### String

`(int utf16-length, utf16-value)`  
`(int ~utf8-byte-count, int utf16-length, utf8-bytes)`

String has two-forms, UTF16 and UTF8. If first 4byte signed integer is `-1`, represents null. `0`, represents empty. UTF16 is same as collection(serialize as `ReadOnlySpan<char>`, utf16-value's byte count is utf16-length * 2). If first signed integer <= `-2`, value is encoded by UTF8. utf8-byte-count is encoded in complement, `~utf8-byte-count` to retrieve count of bytes. Next signed integer is utf16-length, it allows `-1` that represents unknown length. utf8-bytes store bytes for the number of utf8-byte-count.

### Union

`(byte tag, value)`

First unsigned byte is tag that for discriminated value type, tag allows `0` to `249`, `255` represents union is `null`.

License
---
This library is licensed under the MIT License.
