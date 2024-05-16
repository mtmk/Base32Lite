using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Base32Lite.Benchmarks;

public static class Base32Experimental
{
    // private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string ToBase32(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return string.Empty;

        int outputLength = (int)Math.Ceiling(data.Length / 5d) * 8;

        // ReSharper disable once HeapView.ObjectAllocation.Evident
        string result = new string('\0', outputLength);

        unsafe
        {
            fixed (byte* base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"u8)
            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(data))
            fixed (char* charsPtr = result)
            {
                byte* dataPtr = bytesPtr;
                char* encodedPtr = charsPtr;

                int currentByte = 0;
                int bitsRemaining = 8;

                for (int i = 0; i < data.Length; i++)
                {
                    if (bitsRemaining > 5)
                    {
                        int mask = 0xFF >> (bitsRemaining - 5);
                        *encodedPtr++ = (char)base32[(currentByte >> (bitsRemaining - 5)) & mask];
                        bitsRemaining -= 5;
                    }
                    else
                    {
                        int mask = 0xFF >> (5 - bitsRemaining);
                        *encodedPtr++ = (char)base32[(currentByte & mask) << (5 - bitsRemaining) | (*dataPtr >> (8 - (5 - bitsRemaining)))];
                        currentByte = *dataPtr;
                        bitsRemaining += 3;
                    }

                    dataPtr++;
                }

                if (encodedPtr < charsPtr + outputLength)
                {
                    int mask = 0xFF >> (bitsRemaining - 5);
                    *encodedPtr++ = (char)base32[(currentByte >> (bitsRemaining - 5)) & mask];

                    while (encodedPtr < charsPtr + outputLength)
                    {
                        *encodedPtr++ = '=';
                    }
                }
            }
        }

        return result;
    }

    public static byte[] FromBase32(ReadOnlySpan<char> encoded)
    {
        var buffer = new byte[GetLength(encoded)];
        FromBase32(encoded, buffer);
        return buffer;
    }

    public static int GetLength(ReadOnlySpan<char> encoded)
    {
        var len = 0;
        foreach (var currentChar in encoded)
        {
            if (currentChar >= 'A' && currentChar <= 'Z')
                len++;
            else if (currentChar >= '2' && currentChar <= '7')
                len++;
            else if (currentChar != '=')
                ThrowInvalidBase32CharacterException();
        }
        
        return len * 5 / 8;
    }

    public static int FromBase32(ReadOnlySpan<char> encoded, Span<byte> result)
    {
        if (encoded.Length == 0)
            return 0;

        int outputLength = GetLength(encoded);
        if (result.Length < outputLength)
            throw new ArgumentException("Insufficient space in result buffer.", nameof(result));
        // byte[] result = new byte[outputLength];

        unsafe
        {
            fixed (char* encodedPtr = &MemoryMarshal.GetReference(encoded))
            fixed (byte* bytesPtr = &MemoryMarshal.GetReference(result))
            {
                char* inputPtr = encodedPtr;
                byte* outputPtr = bytesPtr;

                int currentByte = 0;
                int bitsRemaining = 8;

                while (*inputPtr != '\0')
                {
                    if (*inputPtr >= 'A' && *inputPtr <= 'Z')
                        currentByte = (currentByte << 5) | (*inputPtr - 'A');
                    else if (*inputPtr >= '2' && *inputPtr <= '7')
                        currentByte = (currentByte << 5) | (*inputPtr - '2' + 26);
                    else if (*inputPtr != '=')
                        throw new ArgumentException("Invalid base32 character.", nameof(encoded));

                    bitsRemaining -= 5;
                    if (bitsRemaining <= 0)
                    {
                        *outputPtr++ = (byte)(currentByte >> -bitsRemaining);
                        bitsRemaining += 8;
                    }

                    inputPtr++;
                }
            }
        }

        return outputLength;
    }

    public static int FromBase32_2(ReadOnlySpan<char> encoded, Span<byte> result)
    {
        if (encoded.Length == 0)
            return 0;

        int outputLength1 = GetLength(encoded);
        if (result.Length < outputLength1)
            ThrowInsufficientSpaceException();

        ref char inputRef = ref MemoryMarshal.GetReference(encoded);
        ref byte outputRef = ref MemoryMarshal.GetReference(result);

        int currentByte = 0;
        int bitsRemaining = 8;
        int outputLength = 0;
        for (int i = 0; i < encoded.Length; i++)
        // foreach (var currentChar in encoded) 
        {
            char currentChar = Unsafe.Add(ref inputRef, i);
            if (currentChar >= 'A' && currentChar <= 'Z')
                currentByte = (currentByte << 5) | (currentChar - 'A');
            else if (currentChar >= '2' && currentChar <= '7')
                currentByte = (currentByte << 5) | (currentChar - '2' + 26);
            else if (currentChar == '=')
                continue;
            else
                ThrowInvalidBase32CharacterException();

            bitsRemaining -= 5;
            if (bitsRemaining <= 0)
            {
                Unsafe.Add(ref outputRef, outputLength++) = (byte)(currentByte >> -bitsRemaining);
                // result[outputLength++] = (byte)(currentByte >> -bitsRemaining);
                currentByte &= (1 << -bitsRemaining) - 1;
                bitsRemaining += 8;
            }
        }

        return outputLength;
    }


    private static readonly char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public static unsafe string ToBase32_2(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        // int outputLen = (int)Math.Ceiling(data.Length / 5d) * 8;
        var bitsCount = data.Length * 8;
        var rem = bitsCount % 5;
        if (rem > 0)
        {
            bitsCount += 5 - rem;
        }

        int outputLen = bitsCount / 5;
        char[] output = new char[outputLen];
        // string output = new string('\0', outputLen);
        int buffer = 0;
        int bufferBits = 0;
        int outputIndex = 0;

        // fixed (byte* base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"u8)
        // fixed (byte* bytesPtr = &MemoryMarshal.GetReference(data))
        // fixed (char* charsPtr = result)
        fixed (byte* dataPtr = data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                buffer = (buffer << 8) | dataPtr[i];
                bufferBits += 8;

                while (bufferBits >= 5)
                {
                    output[outputIndex++] = Base32Chars[(buffer >> (bufferBits - 5)) & 0x1F];
                    bufferBits -= 5;
                }
            }

            if (bufferBits > 0)
            {
                output[outputIndex++] = Base32Chars[(buffer << (5 - bufferBits)) & 0x1F];
            }

            // while (outputIndex < outputLen)
            // {
            //     output[outputIndex++] = '=';
            // }
        }

        return new string(output);
        // return new string(output, 0, outputIndex);
    }

    public static unsafe string ToBase32_3(ReadOnlySpan<byte> data)
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        // int outputLen = (int)Math.Ceiling(data.Length / 5d) * 8;
        var bitsCount = data.Length * 8;
        var rem = bitsCount % 5;
        if (rem > 0)
        {
            bitsCount += 5 - rem;
        }

        int outputLen = bitsCount / 5;
        // char[] output = new char[outputLen];
        string output = new string('\0', outputLen);
        int buffer = 0;
        int bufferBits = 0;
        int outputIndex = 0;

        // fixed (byte* base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"u8)
        // fixed (byte* bytesPtr = &MemoryMarshal.GetReference(data))
        fixed (char* charsPtr = output)
        fixed (byte* dataPtr = data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                buffer = (buffer << 8) | dataPtr[i];
                bufferBits += 8;

                while (bufferBits >= 5)
                {
                    charsPtr[outputIndex++] = Base32Chars[(buffer >> (bufferBits - 5)) & 0x1F];
                    bufferBits -= 5;
                }
            }

            if (bufferBits > 0)
            {
                charsPtr[outputIndex++] = Base32Chars[(buffer << (5 - bufferBits)) & 0x1F];
            }

            // while (outputIndex < outputLen)
            // {
            //     output[outputIndex++] = '=';
            // }
        }

        return output;
        // return new string(output, 0, outputIndex);
    }

    public static unsafe string ToBase32_4(ReadOnlySpan<byte> data)
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        // int outputLen = (int)Math.Ceiling(data.Length / 5d) * 8;
        var bitsCount = data.Length * 8;
        var rem = bitsCount % 5;
        if (rem > 0)
        {
            bitsCount += 5 - rem;
        }

        int outputLen = bitsCount / 5;
        // char[] output = new char[outputLen];
        string output = new string('\0', outputLen);
        int buffer = 0;
        int bufferBits = 0;
        int outputIndex = 0;

        fixed (byte* base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"u8)
        // fixed (byte* bytesPtr = &MemoryMarshal.GetReference(data))
        fixed (char* charsPtr = output)
        fixed (byte* dataPtr = data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                buffer = (buffer << 8) | dataPtr[i];
                bufferBits += 8;

                while (bufferBits >= 5)
                {
                    charsPtr[outputIndex++] = (char)base32[(buffer >> (bufferBits - 5)) & 0x1F];
                    bufferBits -= 5;
                }
            }

            if (bufferBits > 0)
            {
                charsPtr[outputIndex] = (char)base32[(buffer << (5 - bufferBits)) & 0x1F];
            }
        }

        return output;
    }

    public static unsafe int GetLength(ReadOnlySpan<byte> data)
    {
        var bitsCount = data.Length * 8;
        var rem = bitsCount % 5;
        if (rem > 0)
        {
            bitsCount += 5 - rem;
        }

        int outputLen = bitsCount / 5;

        return outputLen;
    }

    public static unsafe int ToBase32_5(ReadOnlySpan<byte> data, Span<char> output)
    {
        if (data == null || data.Length == 0)
        {
            return 0;
        }

        int outputLen = GetLength(data);

        // char[] output = new char[outputLen];
        // string output = new string('\0', outputLen);
        if (output.Length < outputLen)
            throw new ArgumentException("Insufficient space in output buffer.", nameof(output));

        int buffer = 0;
        int bufferBits = 0;
        int outputIndex = 0;

        fixed (byte* base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"u8)
            // fixed (byte* bytesPtr = &MemoryMarshal.GetReference(data))
        fixed (char* charsPtr = output)
        fixed (byte* dataPtr = data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                buffer = (buffer << 8) | dataPtr[i];
                bufferBits += 8;

                while (bufferBits >= 5)
                {
                    charsPtr[outputIndex++] = (char)base32[(buffer >> (bufferBits - 5)) & 0x1F];
                    bufferBits -= 5;
                }
            }

            if (bufferBits > 0)
            {
                charsPtr[outputIndex++] = (char)base32[(buffer << (5 - bufferBits)) & 0x1F];
            }
        }

        return outputIndex;
    }

    public static unsafe string ToBase32_6(ReadOnlySpan<byte> data)
    {
        var outputLen = GetLength(data);
        var output = new string('\0', outputLen);
        fixed (char* charsPtr = output)
        {
            ToBase32_6(data, new Span<char>(charsPtr, outputLen));
        }

        return output;
    }

#if NET6_0_OR_GREATER
    [SkipLocalsInit]
#endif
    public static unsafe int ToBase32_6(ReadOnlySpan<byte> data, Span<char> output)
    {
        int outputLen = GetLength(data);
        if (output.Length < outputLen)
            ThrowInsufficientSpaceException();

        int buffer = 0;
        int bufferBits = 0;
        int outputIndex = 0;

        fixed (byte* base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"u8)
        fixed (char* charsPtr = output)
        fixed (byte* dataPtr = data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                buffer = (buffer << 8) | dataPtr[i];
                bufferBits += 8;

                while (bufferBits >= 5)
                {
                    charsPtr[outputIndex++] = (char)base32[(buffer >> (bufferBits - 5)) & 0x1F];
                    bufferBits -= 5;
                }
            }

            if (bufferBits > 0)
            {
                charsPtr[outputIndex++] = (char)base32[(buffer << (5 - bufferBits)) & 0x1F];
            }
        }

        return outputIndex;
    }

    
    public static unsafe int ToBase32_7(ReadOnlySpan<byte> data, Span<char> output)
    {
        int outputLen = GetLength(data);
        if (output.Length < outputLen)
            ThrowInsufficientSpaceException();

        int buffer = 0;
        int bufferBits = 0;
        int outputIndex = 0;

        var base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"u8;
        var charsPtr = output;
        var dataPtr = data;
        for (int i = 0; i < data.Length; i++)
        {
            buffer = (buffer << 8) | dataPtr[i];
            bufferBits += 8;

            while (bufferBits >= 5)
            {
                charsPtr[outputIndex++] = (char)base32[(buffer >> (bufferBits - 5)) & 0x1F];
                bufferBits -= 5;
            }
        }

        if (bufferBits > 0)
        {
            charsPtr[outputIndex++] = (char)base32[(buffer << (5 - bufferBits)) & 0x1F];
        }

        return outputIndex;
    }
    
    public static int FromBase32_log(ReadOnlySpan<char> encoded, Span<byte> result, Action<string>? log = null)
    {
        log?.Invoke($"___________________________________________________________________________");

        int currentByte = 0;
        int bitsRemaining = 8;
        int outputLength = 0;
        log?.Invoke($"FromBase32: encoded:{encoded.ToString()}");
        var iter = 0;
        foreach (var currentChar in encoded)
        {
            iter++;
            log?.Invoke($"           ________________________________________________________________");
            log?.Invoke($"FromBase32: LOOP BEGIN ({currentChar}) {iter} ({iter * 5}) ({iter * 5 / 8} = {iter * 5 / 8 * 8} + {(iter * 5) - (iter * 5 / 8 * 8)})");
            log?.Invoke($"FromBase32: BEFORE currentChar:{currentChar,5} {ConvertBase32ToBinaryString($"{currentChar}")}");
            log?.Invoke($"FromBase32: BEFORE bitsRemaining:{bitsRemaining}");
            log?.Invoke($"FromBase32: BEFORE currentByte:{currentByte,5} {Convert.ToString(currentByte, 2).PadLeft(32, '0')}");

            int val;
            if (currentChar is >= 'A' and <= 'Z')
            {
                var i = currentByte << 5;
                log?.Invoke($"FromBase32: SHIFT5 currentByte:{i,5} {Convert.ToString(i, 2).PadLeft(32, '0')}");
                val = currentChar - 'A';
                currentByte = i | val;
            }
            else if (currentChar is >= '2' and <= '7')
            {
                var i = currentByte << 5;
                log?.Invoke($"FromBase32: SHIFT5 currentByte:{i,5} {Convert.ToString(i, 2).PadLeft(32, '0')}");
                val = currentChar - '2' + 26;
                currentByte = i | val;
            }
            else if (currentChar == '=')
                continue;
            else
                ThrowInvalidBase32CharacterException();

            bitsRemaining -= 5;
            
            log?.Invoke($"FromBase32: AFTER  bitsRemaining:{bitsRemaining}");
            log?.Invoke($"FromBase32: AFTER  currentByte:{currentByte,5} {Convert.ToString(currentByte, 2).PadLeft(32, '0')}");

            if (bitsRemaining <= 0)
            {
                // var i = currentByte << bitsRemaining;
                var i = currentByte >> -bitsRemaining;
                log?.Invoke($"FromBase32: AFTER2 currentByte:{currentByte,5} {Convert.ToString(currentByte, 2).PadLeft(32, '0')}");
                log?.Invoke($"FromBase32: AFTER2                   i = currentByte >> -bitsRemaining;");
                log?.Invoke($"FromBase32: AFTER2  (result) i:{i,5} {Convert.ToString(i, 2).PadLeft(32, '0')}");
                result[outputLength++] = (byte)i;
                var i1 = 1 << -bitsRemaining;
                log?.Invoke($"FromBase32: AFTER2                   i1 = 1 << -bitsRemaining;");
                log?.Invoke($"FromBase32: AFTER2          i1:{i1,5} {Convert.ToString(i1, 2).PadLeft(32, '0')}");
                var b = i1 - 1;
                log?.Invoke($"FromBase32: AFTER2                   b = i1 - 1;");
                log?.Invoke($"FromBase32: AFTER2           b:{b,5} {Convert.ToString(b, 2).PadLeft(32, '0')}");
                currentByte = currentByte & b;
                
                log?.Invoke($"FromBase32: AFTER2                   currentByte = currentByte & b;");
                log?.Invoke($"FromBase32: AFTER2 currentByte:{currentByte,5} {Convert.ToString(currentByte, 2).PadLeft(32, '0')}");

                bitsRemaining += 8;
                log?.Invoke($"FromBase32: AFTER2                   bitsRemaining += 8;");
                log?.Invoke($"FromBase32: AFTER2 bitsRemaining:{bitsRemaining}");
            }
            
            log?.Invoke($"FromBase32: LOOP END");
        }

        log?.Invoke($"           ________________________________________________________________");
        log?.Invoke($"FromBase32: LOOP COMPLETE");
        log?.Invoke($"FromBase32: outputLength:{outputLength}");

        log?.Invoke($"___________________________________________________________________________");
        return outputLength;
    }

    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInsufficientSpaceException() => throw new ArgumentException("Insufficient space in output buffer.");
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidBase32CharacterException() => throw new ArgumentException("Invalid base32 character");
    
    public static string ConvertToBinaryString(byte[] byteArray, bool spacer = true, bool fiveBitChunks = false)
    {
        StringBuilder binaryString = new StringBuilder();

        if (fiveBitChunks)
        {
            int i = 0;
            foreach (var b in byteArray)
            {
                for (int j = 7; j >= 0; j--)
                {
                    binaryString.Append(((b >> j) & 1) == 1 ? "1" : "0");
                    if (i++ % 5 == 4)
                    {
                        binaryString.Append(" ");
                    }
                }
            }

            return binaryString.ToString();
        }

        foreach (byte b in byteArray)
        {
            // Convert each byte to a binary string and pad with leading zeros if necessary
            binaryString.Append(Convert.ToString(b, 2).PadLeft(8, '0') + (spacer ? " " : ""));
        }

        return binaryString.ToString();
    }
    
    public static string ConvertBase32ToBinaryString(string base32String, bool spacer = true, bool verbose = true)
    {
        const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        
        StringBuilder binaryString = new StringBuilder();

        foreach (char c in base32String)
        {
            if (c == ' ')
                continue;

            int index = Base32Alphabet.IndexOf(c);
            if (index < 0)
                throw new ArgumentException("Invalid character in Base32 string.");

            string binaryChunk = Convert.ToString(index, 2).PadLeft(5, '0');
            binaryString.Append(verbose ? $"{c}:{index:D2}:{binaryChunk}" : binaryChunk).Append(spacer ? " " : "");
        }

        // Trim the trailing space
        if (binaryString.Length > 0 && binaryString[binaryString.Length - 1] == ' ')
        {
            binaryString.Length--;
        }

        return binaryString.ToString();
    }

}
