using Xunit.Abstractions;

namespace Base32Lite.Tests;

public class Base32EncoderTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData(new byte[0], "")]
    [InlineData(new byte[] { 0x66 }, "MY")] // "f"
    [InlineData(new byte[] { 0x66, 0x6F }, "MZXQ")] // "fo"
    [InlineData(new byte[] { 0x66, 0x6F, 0x6F }, "MZXW6")] // "foo"
    [InlineData(new byte[] { 0x66, 0x6F, 0x6F, 0x62 }, "MZXW6YQ")] // "foob"
    [InlineData(new byte[] { 0x66, 0x6F, 0x6F, 0x62, 0x61, 0x72 }, "MZXW6YTBOI")] // "foobar"
    public void ToBase32_ValidInput_ReturnsExpectedResult(byte[] data, string expected)
    {
        ReadOnlySpan<byte> dataSpan = new ReadOnlySpan<byte>(data);
        Span<char> output = new char[Base32.GetEncodedLength(dataSpan)];

        int outputLength = Base32.ToBase32(dataSpan, output);

        outputHelper.WriteLine($"output: {new string(output.ToArray())}");
        Assert.Equal(expected.Length, outputLength);
        Assert.Equal(expected, new string(output.Slice(0, outputLength).ToArray()));
    }

    [Fact]
    public void ToBase32_InsufficientOutputBuffer_ThrowsArgumentException()
    {
        var dataSpan = new byte[] { 0x66, 0x6F, 0x6F, 0x62, 0x61, 0x72 }; // "foobar"
        var output = new char[3]; // Insufficient space

        Assert.Throws<ArgumentException>(() => Base32.ToBase32(dataSpan, output));
    }

}