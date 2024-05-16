using System.Collections;
using System.Text;
using Base32Lite.Benchmarks;
using NATS.NKeys.Benchmarks;
using Xunit.Abstractions;

namespace Base32Lite.Tests;

public class Base32Tests(ITestOutputHelper output)
{
    void Log(string m) => output.WriteLine($"{m}");

    [Fact]
    public void EncodeDecode0()
    {
        var buffer = new byte[1];
        // var chars = new char[] { '7', 'A' };
        var chars = new char[] { 'A', '7' };
        var decodedLength = Base32Experimental.FromBase32_log(chars, buffer, Log);
        output.WriteLine($"decodedLength: {decodedLength}");
        output.WriteLine($"chars: {Base32Experimental.ConvertBase32ToBinaryString(new string(chars))}");
        output.WriteLine($"b0: {Base32Experimental.ConvertToBinaryString(buffer)}");

        var b1 = Base32Reference1.FromBase32(new string(chars));
        output.WriteLine($"b2: {Base32Experimental.ConvertToBinaryString(b1)}");
        var b2 = Base32Reference2.Decode(new string(chars));
        output.WriteLine($"b2: {Base32Experimental.ConvertToBinaryString(b2)}");
    }

    [Fact]
    public void EncodeDecode()
    {
        // var data = new byte[] { 0xff, 0x0f };
        // var data = new byte[] { 0x01 };
        // var data = new byte[] { 0xf1, 0x53, 0x87, 0x41 };
        var data = new byte[] { 0xf1, 0x53 };
        // var data = new byte[] { 0xf1, 0x53, 0xa7, 0xc8, 0xff };
        var length = Base32.GetEncodedLength(data);
        var chars = new char[length];
        var encodedLength = Base32.ToBase32(data, chars);
        Assert.Equal(length, encodedLength);

        output.WriteLine($"enco: {new string(chars)}");
        output.WriteLine($"data: {Base32Experimental.ConvertToBinaryString(data)}");
        output.WriteLine($"base: {Base32Experimental.ConvertBase32ToBinaryString(new string(chars))}");
        output.WriteLine($"data: {Base32Experimental.ConvertToBinaryString(data, spacer: false, fiveBitChunks: true)}");
        output.WriteLine($"base: {Base32Experimental.ConvertBase32ToBinaryString(new string(chars), spacer: true, verbose: false)}");
        
        var buffer = new byte[Base32.GetDataLength(chars)];
        var decodedLength = Base32Experimental.FromBase32_log(chars, buffer, Log);
        Assert.Equal(data.Length, decodedLength);
        Assert.Equal(data, buffer);
        var builder = new StringBuilder();
        foreach (var b in buffer)
        {
            builder.Append($"{b:X2}-");
        }
        output.WriteLine($"decoded: {builder}");

        output.WriteLine($"decoded base32 binary:\n{Base32Experimental.ConvertBase32ToBinaryString(new string(chars))}");
    }

    [Fact]
    public void Encode_decode_empty()
    {
        CompareEncodeDecodeAll([]);
    }
    
    [Fact]
    public void Encode_decode_all_of_two_bytes()
    {
        for (byte i = 0; i < 255; i++)
        {
            CompareEncodeDecodeAll([i]);
            for (byte j = 0; j < 255; j++)
            {
                CompareEncodeDecodeAll([i, j]);
            }
        }
    }
    
    [Fact]
    public void Encode_decode_predictable_random()
    {
        foreach (var seed in new int[] { 42, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 })
        {
            var random = new Random(seed);

            for (int i = 0; i < 128; i++)
            {
                var next = random.Next(1, 1024);
                var buffer = new byte[next];
                random.NextBytes(buffer);
                CompareEncodeDecodeAll(buffer);
            }
        }
    }

    private static void CompareEncodeDecodeAll(byte[] bytes1)
    {
        var base321 = Base32Reference1.ToBase32(bytes1).Trim('=');
        var base322 = Base32Reference2.Encode(bytes1).Trim('=');
        Assert.Equal(base321, base322);

        var length = Base32.GetEncodedLength(bytes1);
        var chars = new char[length];
        var encodedLength = Base32.ToBase32(bytes1, chars);
        Assert.Equal(length, encodedLength);
        Assert.Equal(base321, new string(chars));

        var bytes2 = Base32Reference1.FromBase32(base321);
        Assert.Equal(bytes1, bytes2);
        var bytes3 = Base32Reference2.Decode(base322);
        Assert.Equal(bytes1, bytes3);

        var length1 = Base32.GetDataLength(base321.AsSpan());
        var buffer = new byte[length1];
        var decodedLength = Base32.FromBase32(base321.AsSpan(), buffer);
        Assert.Equal(length1, decodedLength);
        Assert.Equal(bytes1, buffer);
    }

    [Fact]
    public void Decode()
    {
        var input = "MZXW6YTBOI";
        var expected = new byte[] { 0x66, 0x6f, 0x6f, 0x62, 0x61, 0x72 };
        var length = Base32.GetDataLength(input.AsSpan());
        var actual = new byte[length];
        Base32.FromBase32(input.AsSpan(), actual);
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            var b1 = expected[index];
            var b2 = actual[index];
            Assert.Equal(b1, b2);
        }
    }

    [Fact]
    public void Decode2()
    {
        var input = "MY======";
        var expected = new byte[] { 0x66 };
        var encoded = input.AsSpan();
        var length = Base32.GetDataLength(encoded);
        var actual = new byte[length];
        Base32.FromBase32(encoded, actual);
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            var b1 = expected[index];
            var b2 = actual[index];
            Assert.Equal(b1, b2);
        }
    }
    
    [Fact]
    public void Decode3()
    {
        var input = "MZXW6YTBOI";
        var expected = new byte[] { 0x66, 0x6F, 0x6F, 0x62, 0x61, 0x72 };

        var actual1 = Base32Reference2.Decode(input);
        foreach (var b in actual1)
        {
            output.WriteLine($"b: {b:x2}");
        }
        Assert.Equal(expected.Length, actual1.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            var b1 = expected[index];
            var b2 = actual1[index];
            Assert.Equal(b1, b2);
        }

        var encoded1 = Base32Reference2.Encode(actual1);
        output.WriteLine($"encoded1: {encoded1}");
        Assert.Equal(input, encoded1);

        var encoded = input.AsSpan();
        var length = Base32.GetDataLength(encoded);
        var actual = new byte[length];
        Base32.FromBase32(encoded, actual);
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            var b1 = expected[index];
            var b2 = actual[index];
            Assert.Equal(b1, b2);
        }
    }

    [Fact]
    public void Decode4()
    {
        var bytes = Encoding.ASCII.GetBytes("foobar");
        var encoded = Base32Reference2.Encode(bytes);
        output.WriteLine($"encoded: {encoded}");
        
        var actual = Base32Reference2.Decode(encoded);
        foreach (var b in actual)
        {
            output.WriteLine($"b: {b:x2}");
        }
    }

    [Fact]
    public void Decode5()
    {
        var bytes = Encoding.ASCII.GetBytes("foobar");
        var encoded = Base32Reference1.ToBase32(bytes);
        output.WriteLine($"encoded: {encoded}");
        
        var actual = Base32Reference1.FromBase32(encoded);
        foreach (var b in actual)
        {
            output.WriteLine($"b: {b:x2}");
        }
    }
    
    [Fact]
    public void Encode()
    {
        var input = new byte[] { 0x66, 0x6f, 0x6f, 0x62, 0x61, 0x72 };
        var expected = "MZXW6YTBOI";
        Span<char> actual = stackalloc char[Base32.GetEncodedLength(input)];
        Base32.ToBase32(input, actual);
        Assert.Equal(expected, new string(actual.ToArray()));
    }

    [Fact]
    public void Encode2()
    {
        var input = new byte[] { 0x66, 0x6f, 0x6f, 0x62, 0x61, 0x72 };
        var expected = "MZXW6YTBOI";
        var actual = Base32Experimental.ToBase32_2(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Encode3()
    {
        var input = new byte[] { 0x66, 0x6f, 0x6f, 0x62, 0x61, 0x72 };
        var expected = "MZXW6YTBOI";
        var actual = Base32Experimental.ToBase32_3(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Encode4()
    {
        var input = new byte[] { 0x66, 0x6f, 0x6f, 0x62, 0x61, 0x72 };
        var expected = "MZXW6YTBOI";
        var actual = Base32Experimental.ToBase32_4(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Encode6()
    {
        var input = new byte[] { 0x66, 0x6f, 0x6f, 0x62, 0x61, 0x72 };
        var expected = "MZXW6YTBOI";
        var actual = Base32Experimental.ToBase32_6(input);
        Assert.Equal(expected, actual);
    }
}