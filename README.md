# Base32Lite

A zero allocation, simple, and fast Base32 no-padding encoding and decoding library for .NET.

| Method     | Mean      | Error     | StdDev   | Gen0   | Allocated |
|----------- |----------:|----------:|---------:|-------:|----------:|
| Decode     |  65.91 ns |  2.773 ns | 0.152 ns |      - |         - |
| DecodeNext |  90.52 ns | 23.641 ns | 1.296 ns |      - |         - |
| DecodeRef1 | 405.94 ns | 31.810 ns | 1.744 ns | 0.0038 |      56 B |
| DecodeRef2 |  86.32 ns | 74.400 ns | 4.078 ns | 0.0041 |      56 B |
| Encode     |  68.22 ns | 12.111 ns | 0.664 ns |      - |         - |
| EncodeNext |  68.66 ns |  1.139 ns | 0.062 ns |      - |         - |
| EncodeRef1 | 144.13 ns |  2.711 ns | 0.149 ns | 0.0348 |     480 B |
| EncodeRef2 |  87.68 ns |  8.303 ns | 0.455 ns | 0.0192 |     264 B |
