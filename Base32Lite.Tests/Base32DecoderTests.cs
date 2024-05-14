using Xunit.Abstractions;

namespace Base32Lite.Tests;

public class Base32DecoderTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("", new byte[0])]
    [InlineData("===", new byte[0])]
    [InlineData("MY======", new byte[] { 0x66 })] // Decodes to "f"
    [InlineData("MZXQ====", new byte[] { 0x66, 0x6F })] // Decodes to "fo"
    [InlineData("MZXW6===", new byte[] { 0x66, 0x6F, 0x6F })] // Decodes to "foo"
    [InlineData("MZXW6YQ=", new byte[] { 0x66, 0x6F, 0x6F, 0x62 })] // Decodes to "foob"
    [InlineData("MZXW6YTB", new byte[] { 0x66, 0x6F, 0x6F, 0x62, 0x61 })] // Decodes to "foobar"
    [InlineData("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA", new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30, 0x31, 0x32 })] // Decodes to "12345678901234567890123456789012"
    public void FromBase32_ValidInput_ReturnsExpectedResult(string encoded, byte[] expected)
    {
        ReadOnlySpan<char> encodedSpan = encoded.AsSpan();
        Span<byte> result = new byte[expected.Length];
        
        var length = Base32.GetDataLength(encodedSpan);
        output.WriteLine($"length: {length}");
        int outputLength = Base32.FromBase32(encodedSpan, result);

        Assert.Equal(expected.Length, outputLength);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], result[i]);
        }
    }

    [Theory]
    [InlineData("MZXW6YTBOI======", new byte[] { 0x66, 0x6F, 0x6F, 0x62, 0x61, 0x72 })] // Decodes to "foobar" with padding
    [InlineData("MZXW6YTBOI", new byte[] { 0x66, 0x6F, 0x6F, 0x62, 0x61, 0x72 })] // Decodes to "foobar" without padding
    public void FromBase32_ValidInputWithoutPadding_ReturnsExpectedResult(string encoded, byte[] expected)
    {
        ReadOnlySpan<char> encodedSpan = encoded.AsSpan();
        Span<byte> result = new byte[expected.Length];

        int outputLength = Base32.FromBase32(encodedSpan, result);

        Assert.Equal(expected.Length, outputLength);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], result[i]);
        }
    }

    [Theory]
    [InlineData("M0=")]
    [InlineData("MY1=====")]
    [InlineData("MY!@====")]
    public void FromBase32_InvalidInput_ThrowsArgumentException(string encoded)
    {
        var result = new byte[encoded.Length];
        Assert.Throws<ArgumentException>(() => Base32.FromBase32(encoded.AsSpan(), result));
    }

    [Fact]
    public void FromBase32_InsufficientResultBuffer_ThrowsArgumentException()
    {
        var encoded = "MZXW6YTB";
        var result = new byte[3]; // Insufficient space

        Assert.Throws<IndexOutOfRangeException>(() => Base32.FromBase32(encoded.AsSpan(), result));
    }

    private static int GetLength(ReadOnlySpan<char> encoded)
    {
        return (encoded.Length * 5 + 7) / 8;
    }
}