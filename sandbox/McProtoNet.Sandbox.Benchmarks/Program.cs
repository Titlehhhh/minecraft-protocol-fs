// Benchmarks the candidate mechanisms for nested-type dispatch (`reader.ReadType<T>(version)`):
//
//   Direct         — call Vec3f.Read directly (lower bound; what hand-written code does)
//   StaticAbstract — generic constrained to IProtocolType<T>, `T.Read(...)` (our proposal)
//   TypeofChain    — hand-maintained `if (typeof(T) == typeof(X))` chain returning through
//                    `(T)(object)` casts — the shape McProtoNet's TypeDispatch.cs uses today
//   Reflection     — cached MethodInfo.Invoke (the sandbox's previous implementation)
//
// Run: dotnet run -c Release --project sandbox/McProtoNet.Sandbox.Benchmarks

using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using McProtoNet.Protocol;
using McProtoNet.Primitives;

BenchmarkRunner.Run<DispatchBench>();

[MemoryDiagnoser]
[ShortRunJob]
public class DispatchBench
{
    private byte[] _vec3f = null!;
    private static readonly MethodInfo ReadMethod = typeof(Vec3f).GetMethod("Read")!;

    [GlobalSetup]
    public void Setup()
    {
        var w = new MinecraftPrimitiveWriter();
        new Vec3f(1.5f, -2.0f, 3.25f).Write(w, 772);
        _vec3f = w.ToArray();
    }

    [Benchmark(Baseline = true)]
    public Vec3f Direct()
    {
        var r = new MinecraftPrimitiveReader(_vec3f);
        return Vec3f.Read(ref r, 772);
    }

    [Benchmark]
    public Vec3f StaticAbstract()
    {
        var r = new MinecraftPrimitiveReader(_vec3f);
        return ReadGeneric<Vec3f>(ref r, 772);
    }

    [Benchmark]
    public Vec3f TypeofChain()
    {
        var r = new MinecraftPrimitiveReader(_vec3f);
        return ReadChain<Vec3f>(ref r, 772);
    }

    [Benchmark]
    public Vec3f Reflection()
    {
        var r = new MinecraftPrimitiveReader(_vec3f);
        return ReadReflection<Vec3f>(ref r, 772);
    }

    private static T ReadGeneric<T>(ref MinecraftPrimitiveReader reader, int protocolVersion)
        where T : IProtocolType<T>
        => T.Read(ref reader, protocolVersion);

    // Faithful reproduction of McProtoNet's ProtocolSerializationExtensions.TypeDispatch.cs shape:
    // linear typeof checks, results funneled through (T)(object) — which boxes value types.
    private static T ReadChain<T>(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        if (typeof(T) == typeof(Vec3i))
            return (T)(object)Vec3i.Read(ref reader, protocolVersion);
        if (typeof(T) == typeof(Vec2f))
            return (T)(object)Vec2f.Read(ref reader, protocolVersion);
        if (typeof(T) == typeof(Rotations))
            return (T)(object)Rotations.Read(ref reader, protocolVersion);
        if (typeof(T) == typeof(Vec4f))
            return (T)(object)Vec4f.Read(ref reader, protocolVersion);
        if (typeof(T) == typeof(Vec3f))
            return (T)(object)Vec3f.Read(ref reader, protocolVersion);
        throw new NotSupportedException(typeof(T).Name);
    }

    private static T ReadReflection<T>(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        object[] args = { reader, protocolVersion };
        var value = (T)ReadMethod.Invoke(null, args)!;
        reader = (MinecraftPrimitiveReader)args[0]!;
        return value;
    }
}
