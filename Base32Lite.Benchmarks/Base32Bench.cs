using BenchmarkDotNet.Attributes;
using NATS.NKeys.Benchmarks;

namespace Base32Lite.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[PlainExporter]
public class Base32Bench
{
    private readonly byte[] _buffer = new byte[128];
    private readonly string _base32 = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA";
    private readonly byte[] _expected = "12345678901234567890123456789012"u8.ToArray();

    [Benchmark]
    public int Decode()
    {
        return Base32.FromBase32(_base32, _buffer);
    }
    
    [Benchmark]
    public int DecodeNext()
    {
        return Base32Experimental.FromBase32_2(_base32, _buffer);
    }
    
    [Benchmark]
    public int DecodeRef1()
    {
        var bytes = Base32Reference1.FromBase32(_base32);
        return bytes.Length;
    }
    
    [Benchmark]
    public int DecodeRef2()
    {
        var bytes = Base32Reference2.Decode(_base32);
        return bytes.Length;
    }

    [Benchmark]
    public int Encode()
    {
        Span<char> output = stackalloc char[Base32.GetEncodedLength(_expected)];
        return Base32.ToBase32(_expected, output);
    }

    [Benchmark]
    public int EncodeNext()
    {
        Span<char> output = stackalloc char[Base32Experimental.GetLength(_expected)];
        return Base32Experimental.ToBase32_7(_expected, output);
    }

    [Benchmark]
    public int EncodeRef1()
    {
        return Base32Reference1.ToBase32(_expected).Length;
    }

    [Benchmark]
    public int EncodeRef2()
    {
        return Base32Reference2.Encode(_expected).Length;
    }
}
