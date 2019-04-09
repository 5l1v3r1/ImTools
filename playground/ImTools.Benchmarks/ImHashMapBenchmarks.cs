using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using ImTools;
using Microsoft.Collections.Extensions;

namespace Playground
{
    public class ImHashMapBenchmarks
    {
        private static readonly Type[] _keys = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

        public struct TypeVal : IEquatable<TypeVal>
        {
            public static implicit operator TypeVal(Type t) => new TypeVal(t);

            public readonly Type Type;
            public TypeVal(Type type) => Type = type;
            public bool Equals(TypeVal other) => Type == other.Type;
            public override bool Equals(object obj) => !ReferenceEquals(null, obj) && obj is TypeVal other && Equals(other);
            public override int GetHashCode() => Type.GetHashCode();
        }

        [MemoryDiagnoser]
        public class Populate
        {
            /*
            ## 15.01.2019:

                     Method |     Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            --------------- |---------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                AddOrUpdate | 15.84 us | 0.1065 us | 0.0944 us |  1.00 |    0.00 |      7.3242 |           - |           - |            33.87 KB |
             AddOrUpdate_v1 | 27.00 us | 0.1792 us | 0.1588 us |  1.71 |    0.02 |      7.7515 |           - |           - |            35.77 KB |

            
            ## 16.01.2019: Total test against ImHashMap V1, System ImmutableDictionary and ConcurrentDictionary

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.523 (1803/April2018Update/Redstone4)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
Frequency=2156249 Hz, Resolution=463.7683 ns, Timer=TSC
.NET Core SDK=2.2.100
  [Host]     : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT


         Method | Count |           Mean |         Error |        StdDev |         Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |---------------:|--------------:|--------------:|---------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |    10 |       987.6 ns |      36.53 ns |      99.99 ns |       934.3 ns |  0.94 |    0.17 |      0.5589 |           - |           - |             2.58 KB |
    AddOrUpdate |    10 |     1,067.5 ns |      50.43 ns |     136.33 ns |     1,073.5 ns |  1.00 |    0.00 |      0.4044 |           - |           - |             1.87 KB |
 ConcurrentDict |    10 |     1,943.9 ns |      92.31 ns |      81.83 ns |     1,921.3 ns |  1.85 |    0.25 |      0.6371 |           - |           - |             2.95 KB |
 AddOrUpdate_v2 |    10 |     2,688.3 ns |     229.79 ns |     677.55 ns |     2,342.9 ns |  2.50 |    0.77 |      0.4349 |           - |           - |             2.02 KB |
  ImmutableDict |    10 |     5,903.1 ns |     749.28 ns |     801.72 ns |     5,607.8 ns |  5.66 |    1.06 |      0.5875 |           - |           - |             2.73 KB |
                |       |                |               |               |                |       |         |             |             |             |                     |
 ConcurrentDict |   100 |    14,476.1 ns |   1,184.05 ns |   1,215.93 ns |    14,193.3 ns |  0.48 |    0.21 |      3.6011 |      0.0305 |           - |            16.66 KB |
 AddOrUpdate_v1 |   100 |    16,999.4 ns |   1,522.75 ns |   4,441.93 ns |    14,281.8 ns |  0.59 |    0.25 |      8.4686 |           - |           - |            39.05 KB |
 AddOrUpdate_v2 |   100 |    28,695.4 ns |      41.78 ns |      32.62 ns |    28,697.6 ns |  0.94 |    0.33 |      7.7515 |           - |           - |            35.81 KB |
    AddOrUpdate |   100 |    31,854.4 ns |   2,882.03 ns |   8,497.72 ns |    36,547.9 ns |  1.00 |    0.00 |      7.3242 |           - |           - |            33.91 KB |
  ImmutableDict |   100 |    89,602.2 ns |   1,873.19 ns |   2,229.89 ns |    88,767.5 ns |  2.98 |    1.05 |      9.3994 |           - |           - |            43.68 KB |
                |       |                |               |               |                |       |         |             |             |             |                     |
 ConcurrentDict |  1000 |   219,064.7 ns |     559.14 ns |     466.91 ns |   218,894.7 ns |  0.69 |    0.01 |     49.3164 |     17.8223 |           - |           254.29 KB |
 AddOrUpdate_v1 |  1000 |   297,651.6 ns |   1,073.37 ns |     838.02 ns |   297,421.1 ns |  0.93 |    0.01 |    120.6055 |      3.4180 |           - |           556.41 KB |
    AddOrUpdate |  1000 |   319,478.3 ns |   2,768.11 ns |   2,161.16 ns |   319,079.8 ns |  1.00 |    0.00 |    113.2813 |      0.9766 |           - |           526.48 KB |
 AddOrUpdate_v2 |  1000 |   615,321.8 ns |  72,410.43 ns | 213,503.80 ns |   467,207.0 ns |  1.97 |    0.66 |    118.6523 |      0.4883 |           - |            547.3 KB |
  ImmutableDict |  1000 | 1,516,613.7 ns | 107,376.35 ns | 290,298.20 ns | 1,387,325.2 ns |  4.95 |    1.19 |    140.6250 |      1.9531 |           - |           648.02 KB |

## 21.01.2019 - all versions compared

         Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |    30 |   3.326 us | 0.0098 us | 0.0092 us |  0.84 |    0.01 |      2.0409 |           - |           - |             9.42 KB |
 AddOrUpdate_v2 |    30 |   3.515 us | 0.0700 us | 0.0688 us |  0.89 |    0.02 |      1.9302 |           - |           - |             8.91 KB |
    AddOrUpdate |    30 |   3.945 us | 0.0261 us | 0.0231 us |  1.00 |    0.00 |      2.1591 |           - |           - |             9.98 KB |
 AddOrUpdate_v3 |    30 |   4.607 us | 0.0875 us | 0.0819 us |  1.17 |    0.02 |      1.7624 |           - |           - |             8.13 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   150 |  24.883 us | 0.4840 us | 0.4527 us |  0.86 |    0.02 |     13.5193 |           - |           - |            62.39 KB |
 AddOrUpdate_v2 |   150 |  26.862 us | 0.5042 us | 0.4717 us |  0.92 |    0.02 |     13.7024 |           - |           - |             63.2 KB |
    AddOrUpdate |   150 |  29.054 us | 0.1205 us | 0.1127 us |  1.00 |    0.00 |     15.1978 |           - |           - |            70.17 KB |
 AddOrUpdate_v3 |   150 |  35.585 us | 0.3740 us | 0.3498 us |  1.22 |    0.01 |     12.5732 |           - |           - |            58.05 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   500 | 124.728 us | 0.7225 us | 0.6759 us |  0.91 |    0.01 |     54.4434 |           - |           - |           251.95 KB |
 AddOrUpdate_v2 |   500 | 128.650 us | 1.4938 us | 1.3973 us |  0.94 |    0.01 |     58.1055 |      0.2441 |           - |           267.97 KB |
    AddOrUpdate |   500 | 137.325 us | 1.9010 us | 1.7782 us |  1.00 |    0.00 |     63.2324 |      0.2441 |           - |           291.42 KB |
 AddOrUpdate_v3 |   500 | 166.994 us | 1.7109 us | 1.6004 us |  1.22 |    0.02 |     52.7344 |           - |           - |           243.81 KB |

## Inlining With and removing not necessary call to Balance in case of Update. 
 
         Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |    30 |   3.323 us | 0.0121 us | 0.0107 us |  0.91 |    0.00 |      2.0409 |           - |           - |             9.42 KB |
    AddOrUpdate |    30 |   3.647 us | 0.0111 us | 0.0093 us |  1.00 |    0.00 |      2.0409 |           - |           - |             9.42 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   150 |  24.605 us | 0.2023 us | 0.1690 us |  0.92 |    0.02 |     13.5193 |           - |           - |            62.39 KB |
    AddOrUpdate |   150 |  26.832 us | 0.5300 us | 0.5443 us |  1.00 |    0.00 |     13.5193 |           - |           - |            62.39 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   500 | 121.799 us | 0.9309 us | 0.8252 us |  0.92 |    0.01 |     54.4434 |           - |           - |           251.95 KB |
    AddOrUpdate |   500 | 132.886 us | 2.3470 us | 2.0805 us |  1.00 |    0.00 |     54.4434 |           - |           - |           251.95 KB |


## Special fast logic for adding to empty branch.

         Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |    30 |   3.369 us | 0.0102 us | 0.0090 us |  0.94 |    0.01 |      2.0409 |           - |           - |             9.42 KB |
    AddOrUpdate |    30 |   3.587 us | 0.0244 us | 0.0228 us |  1.00 |    0.00 |      2.0409 |           - |           - |             9.42 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   150 |  25.025 us | 0.4927 us | 0.4609 us |  0.95 |    0.02 |     13.5193 |           - |           - |            62.39 KB |
    AddOrUpdate |   150 |  26.235 us | 0.1058 us | 0.0989 us |  1.00 |    0.00 |     13.5193 |           - |           - |            62.39 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   500 | 126.106 us | 1.0204 us | 0.9545 us |  1.00 |    0.01 |     54.4434 |           - |           - |           251.95 KB |
    AddOrUpdate |   500 | 126.844 us | 0.8788 us | 0.7339 us |  1.00 |    0.00 |     54.4434 |           - |           - |           251.95 KB |

## Removing not necessary imbalanced tree creation before balance - first memory win.

         Method | Count |       Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    AddOrUpdate |    30 |   3.357 us | 0.0163 us | 0.0145 us |  1.00 |      1.9417 |           - |           - |             8.95 KB |
 AddOrUpdate_v1 |    30 |   3.359 us | 0.0124 us | 0.0116 us |  1.00 |      2.0409 |           - |           - |             9.42 KB |
                |       |            |           |           |       |             |             |             |                     |
 AddOrUpdate_v1 |   150 |  24.513 us | 0.0420 us | 0.0393 us |  0.98 |     13.5193 |           - |           - |            62.39 KB |
    AddOrUpdate |   150 |  25.074 us | 0.1160 us | 0.1085 us |  1.00 |     12.9395 |      0.0305 |           - |            59.72 KB |
                |       |            |           |           |       |             |             |             |                     |
    AddOrUpdate |   500 | 122.624 us | 0.6553 us | 0.6130 us |  1.00 |     52.4902 |      0.2441 |           - |           242.06 KB |
 AddOrUpdate_v1 |   500 | 122.656 us | 0.8018 us | 0.7500 us |  1.00 |     54.4434 |           - |           - |           251.95 KB |

## More variety to benchmark input

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
    AddOrUpdate |     5 |     472.4 ns |     2.139 ns |     1.786 ns |  1.00 |      0.2537 |           - |           - |             1.17 KB |
 AddOrUpdate_v1 |     5 |     488.1 ns |     2.258 ns |     2.112 ns |  1.03 |      0.2737 |           - |           - |             1.27 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,855.4 ns |    19.395 ns |    17.194 ns |  0.98 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   4,974.1 ns |    17.098 ns |    15.157 ns |  1.00 |      2.8000 |           - |           - |            12.94 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  35,267.4 ns |   100.767 ns |    84.145 ns |  0.98 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  35,874.3 ns |   173.786 ns |   162.560 ns |  1.00 |     18.0054 |           - |           - |            83.02 KB |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |  1000 | 302,900.4 ns | 1,116.252 ns | 1,044.143 ns |  1.00 |    115.7227 |      2.4414 |           - |           535.08 KB |
 AddOrUpdate_v1 |  1000 | 303,552.8 ns |   906.769 ns |   803.827 ns |  1.00 |    120.6055 |      3.4180 |           - |           556.41 KB |

## Remove unnecessary temporary left leaf(y) branch creation before balancing - memory win

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |     5 |     491.4 ns |     2.121 ns |     1.984 ns |  1.00 |      0.2737 |           - |           - |             1.27 KB |
    AddOrUpdate |     5 |     493.4 ns |     2.199 ns |     2.057 ns |  1.00 |      0.2537 |           - |           - |             1.17 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,871.0 ns |    26.907 ns |    23.852 ns |  0.98 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   4,949.1 ns |    10.957 ns |     9.713 ns |  1.00 |      2.7542 |           - |           - |             12.7 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  35,540.5 ns |   271.146 ns |   240.364 ns |  0.95 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  37,344.9 ns |   127.081 ns |   118.871 ns |  1.00 |     17.6392 |           - |           - |            81.38 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |  1000 | 308,033.2 ns | 1,643.015 ns | 1,536.877 ns |  0.98 |    120.6055 |      3.4180 |           - |           556.41 KB |
    AddOrUpdate |  1000 | 314,370.2 ns | 1,781.632 ns | 1,666.540 ns |  1.00 |    113.7695 |      0.4883 |           - |           525.09 KB |


## Remove unnecessary temporary right leaf(y) branch creation before balancing - memory win

         Method | Count |         Mean |        Error |       StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |     5 |     489.0 ns |     2.395 ns |     2.241 ns |  0.99 |    0.01 |      0.2737 |           - |           - |             1.27 KB |
    AddOrUpdate |     5 |     496.0 ns |     3.869 ns |     3.619 ns |  1.00 |    0.00 |      0.2432 |           - |           - |             1.13 KB |
                |       |              |              |              |       |         |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,873.1 ns |    35.313 ns |    33.032 ns |  0.91 |    0.01 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   5,346.9 ns |    18.590 ns |    16.480 ns |  1.00 |    0.00 |      2.6779 |           - |           - |            12.38 KB |
                |       |              |              |              |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  36,484.4 ns |   309.583 ns |   274.437 ns |  0.90 |    0.01 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  40,641.9 ns |   628.973 ns |   588.342 ns |  1.00 |    0.00 |     17.1509 |      0.0610 |           - |            79.08 KB |
                |       |              |              |              |       |         |             |             |             |                     |
 AddOrUpdate_v1 |  1000 | 316,678.8 ns | 5,722.780 ns | 5,353.092 ns |  0.98 |    0.02 |    120.6055 |      3.4180 |           - |           556.41 KB |
    AddOrUpdate |  1000 | 323,403.6 ns | 1,483.940 ns | 1,315.474 ns |  1.00 |    0.00 |    111.8164 |     32.7148 |           - |           515.39 KB |


## Parity with v1 via handling one more special case - where `Height == 1`

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
    AddOrUpdate |     5 |     480.3 ns |     3.645 ns |     3.410 ns |  1.00 |      0.2432 |           - |           - |             1.13 KB |
 AddOrUpdate_v1 |     5 |     487.1 ns |     2.547 ns |     2.382 ns |  1.01 |      0.2737 |           - |           - |             1.27 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,844.9 ns |    23.494 ns |    21.976 ns |  0.99 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   4,908.8 ns |    29.374 ns |    27.477 ns |  1.00 |      2.6779 |           - |           - |            12.38 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  35,727.8 ns |    84.717 ns |    75.099 ns |  0.99 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  36,231.4 ns |   136.671 ns |   121.156 ns |  1.00 |     17.1509 |      0.0610 |           - |            79.08 KB |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |  1000 | 304,238.0 ns | 1,636.331 ns | 1,366.410 ns |  1.00 |    111.8164 |     32.7148 |           - |           515.39 KB |
 AddOrUpdate_v1 |  1000 | 307,233.3 ns | 1,487.373 ns | 1,391.290 ns |  1.01 |    120.6055 |      3.4180 |           - |           556.41 KB |


## Some fixes for updating things

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
    AddOrUpdate |     5 |     469.8 ns |     4.613 ns |     3.602 ns |  1.00 |      0.2432 |           - |           - |             1.13 KB |
 AddOrUpdate_v1 |     5 |     480.6 ns |     2.648 ns |     2.477 ns |  1.02 |      0.2737 |           - |           - |             1.27 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,731.4 ns |    22.512 ns |    21.058 ns |  0.96 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   4,945.0 ns |    12.489 ns |    10.429 ns |  1.00 |      2.6779 |           - |           - |            12.38 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  35,094.6 ns |    70.945 ns |    66.362 ns |  0.98 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  35,821.9 ns |   129.417 ns |   121.057 ns |  1.00 |     17.1509 |      0.0610 |           - |            79.08 KB |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |  1000 | 303,448.5 ns | 2,017.835 ns | 1,887.484 ns |  1.00 |    111.8164 |     32.7148 |           - |           515.39 KB |
 AddOrUpdate_v1 |  1000 | 304,471.0 ns | 1,549.817 ns | 1,449.700 ns |  1.00 |    120.6055 |      3.4180 |           - |           556.41 KB |


## Inlining the Balance and removing its not used if-branches

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v2 |     5 |     418.2 ns |     2.540 ns |     2.376 ns |  0.90 |      0.2170 |           - |           - |              1024 B |
    AddOrUpdate |     5 |     464.6 ns |     2.301 ns |     2.039 ns |  1.00 |      0.2437 |           - |           - |              1152 B |
 AddOrUpdate_v1 |     5 |     487.7 ns |     1.769 ns |     1.655 ns |  1.05 |      0.2737 |           - |           - |              1296 B |
 AddOrUpdate_v3 |     5 |     516.5 ns |     2.289 ns |     2.141 ns |  1.11 |      0.1993 |           - |           - |               944 B |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |    40 |   4,621.6 ns |    19.899 ns |    18.614 ns |  1.00 |      2.6779 |           - |           - |             12672 B |
 AddOrUpdate_v1 |    40 |   4,910.9 ns |    19.805 ns |    18.525 ns |  1.06 |      2.9678 |           - |           - |             14016 B |
 AddOrUpdate_v2 |    40 |   5,115.0 ns |    26.410 ns |    23.411 ns |  1.11 |      2.8458 |           - |           - |             13456 B |
 AddOrUpdate_v3 |    40 |   6,953.3 ns |    24.391 ns |    22.815 ns |  1.50 |      2.6016 |           - |           - |             12304 B |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |   200 |  34,250.7 ns |    87.221 ns |    81.586 ns |  1.00 |     17.1509 |      0.0610 |           - |             80976 B |
 AddOrUpdate_v1 |   200 |  35,425.8 ns |   137.918 ns |   129.008 ns |  1.03 |     18.7378 |      0.0610 |           - |             88608 B |
 AddOrUpdate_v2 |   200 |  37,942.8 ns |   188.112 ns |   175.960 ns |  1.11 |     19.2261 |           - |           - |             91008 B |
 AddOrUpdate_v3 |   200 |  50,640.8 ns |    66.485 ns |    55.518 ns |  1.48 |     17.6392 |      0.0610 |           - |             83352 B |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |  1000 | 290,830.8 ns |   915.770 ns |   811.806 ns |  1.00 |    111.8164 |     32.7148 |           - |            527760 B |
 AddOrUpdate_v2 |  1000 | 308,260.3 ns | 2,278.822 ns | 2,020.116 ns |  1.06 |    130.8594 |      0.9766 |           - |            619056 B |
 AddOrUpdate_v1 |  1000 | 308,355.6 ns | 2,046.461 ns | 1,914.261 ns |  1.06 |    120.6055 |      3.4180 |           - |            569760 B |
 AddOrUpdate_v3 |  1000 | 375,880.3 ns | 1,710.716 ns | 1,600.205 ns |  1.29 |    118.6523 |      0.4883 |           - |            560440 B |


         Method | Count |         Mean |        Error |        StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|--------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v2 |     5 |     404.8 ns |     2.504 ns |     2.0907 ns |  0.91 |    0.01 |      0.2170 |           - |           - |                1 KB |
    AddOrUpdate |     5 |     447.1 ns |     4.673 ns |     4.1429 ns |  1.00 |    0.00 |      0.2437 |           - |           - |             1.13 KB |
 AddOrUpdate_v1 |     5 |     481.5 ns |     1.112 ns |     0.9854 ns |  1.08 |    0.01 |      0.2737 |           - |           - |             1.27 KB |
                |       |              |              |               |       |         |             |             |             |                     |
    AddOrUpdate |    40 |   4,563.0 ns |    91.302 ns |    89.6712 ns |  1.00 |    0.00 |      2.6779 |           - |           - |            12.38 KB |
 AddOrUpdate_v1 |    40 |   4,712.5 ns |    27.017 ns |    25.2716 ns |  1.03 |    0.02 |      2.9678 |           - |           - |            13.69 KB |
 AddOrUpdate_v2 |    40 |   4,943.3 ns |    22.715 ns |    21.2474 ns |  1.08 |    0.02 |      2.8458 |           - |           - |            13.14 KB |
                |       |              |              |               |       |         |             |             |             |                     |
    AddOrUpdate |   200 |  33,629.4 ns |   647.198 ns |   635.6353 ns |  1.00 |    0.00 |     17.1509 |      0.0610 |           - |            79.08 KB |
 AddOrUpdate_v1 |   200 |  34,643.5 ns |   155.549 ns |   145.5005 ns |  1.03 |    0.02 |     18.7378 |      0.0610 |           - |            86.53 KB |
 AddOrUpdate_v2 |   200 |  36,726.4 ns |   419.812 ns |   372.1521 ns |  1.10 |    0.01 |     19.2261 |           - |           - |            88.88 KB |
                |       |              |              |               |       |         |             |             |             |                     |
    AddOrUpdate |  1000 | 291,376.3 ns | 3,419.191 ns | 3,198.3136 ns |  1.00 |    0.00 |    111.8164 |     32.7148 |           - |           515.39 KB |
 AddOrUpdate_v2 |  1000 | 302,027.2 ns | 4,981.373 ns | 4,659.5798 ns |  1.04 |    0.02 |    130.8594 |      0.9766 |           - |           604.55 KB |
 AddOrUpdate_v1 |  1000 | 304,899.6 ns | 4,634.673 ns | 4,335.2763 ns |  1.05 |    0.02 |    120.6055 |      3.4180 |           - |           556.41 KB |

##  Some base line results

|                   Method | Count |           Mean |        Error |       StdDev |         Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------- |------ |---------------:|-------------:|-------------:|---------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|    ImHashMap_AddOrUpdate |    10 |       887.4 ns |     3.617 ns |     3.206 ns |       887.2 ns |  1.00 |    0.00 |      0.4978 |           - |           - |              2.3 KB |
| ImHashMap_V1_AddOrUpdate |    10 |       935.7 ns |     1.756 ns |     1.466 ns |       936.0 ns |  1.05 |    0.00 |      0.5589 |           - |           - |             2.58 KB |
|          DictSlim_TryAdd |    10 |       607.5 ns |     1.668 ns |     1.479 ns |       607.6 ns |  0.68 |    0.00 |      0.2365 |           - |           - |             1.09 KB |
|              Dict_TryAdd |    10 |       675.1 ns |    13.422 ns |    20.497 ns |       677.2 ns |  0.75 |    0.02 |      0.2203 |           - |           - |             1.02 KB |
|    ConcurrentDict_TryAdd |    10 |     1,960.2 ns |    41.256 ns |    40.519 ns |     1,941.3 ns |  2.21 |    0.05 |      0.6371 |           - |           - |             2.95 KB |
|        ImmutableDict_Add |    10 |     5,841.7 ns |   110.674 ns |   108.696 ns |     5,858.8 ns |  6.59 |    0.13 |      0.5875 |           - |           - |             2.73 KB |
|                          |       |                |              |              |                |       |         |             |             |             |                     |
|    ImHashMap_AddOrUpdate |   100 |    14,010.7 ns |   277.074 ns |   329.837 ns |    14,093.4 ns |  1.00 |    0.00 |      7.6904 |           - |           - |            35.48 KB |
| ImHashMap_V1_AddOrUpdate |   100 |    14,531.7 ns |   242.810 ns |   215.244 ns |    14,492.5 ns |  1.05 |    0.03 |      8.4686 |           - |           - |            39.05 KB |
|          DictSlim_TryAdd |   100 |     4,530.6 ns |    74.934 ns |    70.093 ns |     4,521.4 ns |  0.33 |    0.01 |      1.5945 |           - |           - |             7.36 KB |
|              Dict_TryAdd |   100 |     5,406.3 ns |    47.278 ns |    44.224 ns |     5,398.7 ns |  0.39 |    0.01 |      2.1667 |           - |           - |               10 KB |
|    ConcurrentDict_TryAdd |   100 |    14,861.7 ns |   548.482 ns |   931.365 ns |    14,335.9 ns |  1.09 |    0.08 |      3.6011 |      0.0153 |           - |            16.66 KB |
|        ImmutableDict_Add |   100 |    90,922.3 ns |   342.680 ns |   320.543 ns |    90,936.7 ns |  6.54 |    0.15 |      9.3994 |           - |           - |            43.68 KB |
|                          |       |                |              |              |                |       |         |             |             |             |                     |
|    ImHashMap_AddOrUpdate |  1000 |   282,986.9 ns | 1,500.444 ns | 1,403.516 ns |   282,819.6 ns |  1.00 |    0.00 |    111.8164 |      0.9766 |           - |           516.19 KB |
| ImHashMap_V1_AddOrUpdate |  1000 |   300,616.8 ns |   960.056 ns |   898.037 ns |   300,469.6 ns |  1.06 |    0.01 |    120.6055 |      1.9531 |           - |           557.11 KB |
|          DictSlim_TryAdd |  1000 |    41,977.2 ns |   690.173 ns |   645.588 ns |    42,189.1 ns |  0.15 |    0.00 |     12.2070 |           - |           - |             56.5 KB |
|              Dict_TryAdd |  1000 |    55,159.1 ns |   134.543 ns |   125.852 ns |    55,200.5 ns |  0.19 |    0.00 |     21.5454 |      0.0610 |           - |            99.87 KB |
|    ConcurrentDict_TryAdd |  1000 |   223,008.7 ns |   640.296 ns |   567.606 ns |   222,865.0 ns |  0.79 |    0.00 |     49.3164 |     17.8223 |           - |           254.29 KB |
|        ImmutableDict_Add |  1000 | 1,403,209.7 ns | 6,072.856 ns | 5,383.428 ns | 1,401,542.6 ns |  4.96 |    0.03 |    140.6250 |      1.9531 |           - |           648.84 KB |

 */
            [Params(10, 100, 1_000)]
            public int Count;

            [Benchmark(Baseline = true)]
            public ImHashMap<Type, string> ImHashMap_AddOrUpdate()
            {
                var map = ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key, "a", out _, out _);

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!", out _, out _);
            }

            [Benchmark]
            public ImTools.OldVersions.V1.ImHashMap<Type, string> ImHashMap_V1_AddOrUpdate()
            {
                var map = ImTools.OldVersions.V1.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key, "a");

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!");
            }

            [Benchmark]
            public DictionarySlim<TypeVal, string> DictSlim_TryAdd()
            {
                var map = new DictionarySlim<TypeVal, string>();

                foreach (var key in _keys.Take(Count))
                    map.GetOrAddValueRef(key) = "a";

                map.GetOrAddValueRef(typeof(ImHashMapBenchmarks)) = "!";
                return map;
            }

            [Benchmark]
            public Dictionary<Type, string> Dict_TryAdd()
            {
                var map = new Dictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            [Benchmark]
            public ConcurrentDictionary<Type, string> ConcurrentDict_TryAdd()
            {
                var map = new ConcurrentDictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            [Benchmark]
            public ImmutableDictionary<Type, string> ImmutableDict_Add()
            {
                var map = ImmutableDictionary<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.Add(key, "a");

                return map.Add(typeof(ImHashMapBenchmarks), "!");
            }
        }

        [MemoryDiagnoser]
        public class Lookup
        {
            /*
## 21.01.2019: All versions.

               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 | 13.74 ns | 0.0686 ns | 0.0642 ns |  0.79 |           - |           - |           - |                   - |
    GetValueOrDefault | 17.43 ns | 0.0924 ns | 0.0864 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 | 19.15 ns | 0.0786 ns | 0.0656 ns |  1.10 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 | 25.73 ns | 0.0711 ns | 0.0665 ns |  1.48 |           - |           - |           - |                   - |

## For some reason dropping lookup speed with only changes to AddOrUpdate

               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 | 13.89 ns | 0.0938 ns | 0.0877 ns |  0.80 |           - |           - |           - |                   - |
    GetValueOrDefault | 17.40 ns | 0.0888 ns | 0.0831 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 | 19.04 ns | 0.0712 ns | 0.0666 ns |  1.09 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 | 25.93 ns | 0.0474 ns | 0.0420 ns |  1.49 |           - |           - |           - |                   - |

## Got back some perf by moving GetValueOrDefault to static method and specializing for Type

               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 | 13.87 ns | 0.0400 ns | 0.0355 ns |  0.85 |           - |           - |           - |                   - |
    GetValueOrDefault | 16.34 ns | 0.0932 ns | 0.0826 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 | 19.18 ns | 0.0460 ns | 0.0430 ns |  1.17 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 | 25.96 ns | 0.0756 ns | 0.0707 ns |  1.59 |           - |           - |           - |                   - |

## Benchmark against variety of inputs on par with Populate benchmark

               Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 |     5 |  6.155 ns | 0.0321 ns | 0.0301 ns |  0.98 |    0.01 |           - |           - |           - |                   - |
    GetValueOrDefault |     5 |  6.267 ns | 0.0510 ns | 0.0452 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |     5 |  7.439 ns | 0.0763 ns | 0.0676 ns |  1.19 |    0.02 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |     5 |  9.558 ns | 0.0409 ns | 0.0383 ns |  1.52 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
 GetValueOrDefault_v1 |    40 | 10.897 ns | 0.0673 ns | 0.0629 ns |  0.95 |    0.01 |           - |           - |           - |                   - |
    GetValueOrDefault |    40 | 11.467 ns | 0.0325 ns | 0.0304 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |    40 | 14.012 ns | 0.1092 ns | 0.1022 ns |  1.22 |    0.01 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |    40 | 19.945 ns | 0.1032 ns | 0.0965 ns |  1.74 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
 GetValueOrDefault_v1 |   200 | 13.664 ns | 0.0291 ns | 0.0258 ns |  0.97 |    0.00 |           - |           - |           - |                   - |
    GetValueOrDefault |   200 | 14.051 ns | 0.0524 ns | 0.0491 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |   200 | 16.722 ns | 0.0568 ns | 0.0531 ns |  1.19 |    0.01 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |   200 | 24.473 ns | 0.0792 ns | 0.0702 ns |  1.74 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
 GetValueOrDefault_v1 |  1000 | 14.213 ns | 0.1528 ns | 0.1354 ns |  0.96 |    0.01 |           - |           - |           - |                   - |
    GetValueOrDefault |  1000 | 14.805 ns | 0.0518 ns | 0.0485 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |  1000 | 16.645 ns | 0.0447 ns | 0.0419 ns |  1.12 |    0.01 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |  1000 | 27.489 ns | 0.0890 ns | 0.0832 ns |  1.86 |    0.01 |           - |           - |           - |                   - |

## Adding aggressive inlining to the Data { Hash, Key, Value } properties

               Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    GetValueOrDefault |     5 |  5.853 ns | 0.0685 ns | 0.0607 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |     5 |  5.913 ns | 0.0373 ns | 0.0349 ns |  1.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |    40 |  9.649 ns | 0.0235 ns | 0.0220 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 10.266 ns | 0.0236 ns | 0.0221 ns |  1.06 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |   200 | 11.613 ns | 0.0554 ns | 0.0491 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 12.052 ns | 0.0555 ns | 0.0520 ns |  1.04 |           - |           - |           - |                   - |

## Using  `!= Empty` instead of `.Height != 0` drops some perf

               Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 |     5 |  5.933 ns | 0.0310 ns | 0.0290 ns |  0.93 |    0.03 |           - |           - |           - |                   - |
    GetValueOrDefault |     5 |  6.386 ns | 0.1807 ns | 0.1602 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
    GetValueOrDefault |    40 |  9.820 ns | 0.0521 ns | 0.0488 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 10.257 ns | 0.0300 ns | 0.0266 ns |  1.05 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
    GetValueOrDefault |   200 | 11.717 ns | 0.0689 ns | 0.0644 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 12.104 ns | 0.0548 ns | 0.0486 ns |  1.03 |    0.01 |           - |           - |           - |                   - |

## Removing `.Height != 0` check completely did not change much, but let it stay cause less code is better

               Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    GetValueOrDefault |     5 |  5.903 ns | 0.0612 ns | 0.0573 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |     5 |  5.931 ns | 0.0503 ns | 0.0470 ns |  1.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |    40 |  9.636 ns | 0.0419 ns | 0.0392 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 10.231 ns | 0.0333 ns | 0.0312 ns |  1.06 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |   200 | 11.637 ns | 0.0721 ns | 0.0602 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 12.042 ns | 0.0607 ns | 0.0568 ns |  1.03 |           - |           - |           - |                   - |

## TryFind

             Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ----------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
         TryFind_v1 |     5 |  5.229 ns | 0.0307 ns | 0.0257 ns |  1.00 |           - |           - |           - |                   - |
            TryFind |     5 |  6.766 ns | 0.0695 ns | 0.0650 ns |  1.30 |           - |           - |           - |                   - |
                    |       |           |           |           |       |             |             |             |                     |
         TryFind_v1 |    40 |  9.268 ns | 0.0116 ns | 0.0108 ns |  1.00 |           - |           - |           - |                   - |
            TryFind |    40 |  9.755 ns | 0.0219 ns | 0.0205 ns |  1.05 |           - |           - |           - |                   - |
                    |       |           |           |           |       |             |             |             |                     |
         TryFind_v1 |   200 | 11.773 ns | 0.0558 ns | 0.0494 ns |  1.00 |           - |           - |           - |                   - |
            TryFind |   200 | 12.212 ns | 0.0456 ns | 0.0380 ns |  1.04 |           - |           - |           - |                   - |

     Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
----------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    TryFind |     5 |  5.906 ns | 0.0191 ns | 0.0178 ns |  0.97 |           - |           - |           - |                   - |
 TryFind_v1 |     5 |  6.079 ns | 0.0947 ns | 0.0839 ns |  1.00 |           - |           - |           - |                   - |
            |       |           |           |           |       |             |             |             |                     |
    TryFind |    40 |  9.211 ns | 0.0214 ns | 0.0200 ns |  0.87 |           - |           - |           - |                   - |
 TryFind_v1 |    40 | 10.566 ns | 0.0149 ns | 0.0132 ns |  1.00 |           - |           - |           - |                   - |
            |       |           |           |           |       |             |             |             |                     |
    TryFind |   200 | 11.400 ns | 0.1152 ns | 0.1078 ns |  0.88 |           - |           - |           - |                   - |
 TryFind_v1 |   200 | 12.929 ns | 0.0712 ns | 0.0666 ns |  1.00 |           - |           - |           - |                   - |

    ## GetOrDefault a bit optimized
    
               Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    GetValueOrDefault |     5 |  6.782 ns | 0.0449 ns | 0.0398 ns |  0.96 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |     5 |  7.042 ns | 0.0659 ns | 0.0616 ns |  1.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |    40 | 10.962 ns | 0.0866 ns | 0.0768 ns |  0.99 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 11.094 ns | 0.0973 ns | 0.0813 ns |  1.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |   200 | 13.329 ns | 0.0338 ns | 0.0299 ns |  0.97 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 13.722 ns | 0.0537 ns | 0.0448 ns |  1.00 |           - |           - |           - |                   - |

    ## The whole result for the docs

                Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
---------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
            TryFind_v1 |    10 |  7.274 ns | 0.0410 ns | 0.0384 ns |  0.98 |    0.01 |           - |           - |           - |                   - |
               TryFind |    10 |  7.422 ns | 0.0237 ns | 0.0222 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |    10 | 21.664 ns | 0.0213 ns | 0.0189 ns |  2.92 |    0.01 |           - |           - |           - |                   - |
  ImmutableDict_TryGet |    10 | 71.199 ns | 0.1312 ns | 0.1228 ns |  9.59 |    0.03 |           - |           - |           - |                   - |
                       |       |           |           |           |       |         |             |             |             |                     |
            TryFind_v1 |   100 |  8.426 ns | 0.0236 ns | 0.0221 ns |  0.91 |    0.00 |           - |           - |           - |                   - |
               TryFind |   100 |  9.304 ns | 0.0305 ns | 0.0270 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |   100 | 21.791 ns | 0.1072 ns | 0.0951 ns |  2.34 |    0.01 |           - |           - |           - |                   - |
  ImmutableDict_TryGet |   100 | 74.985 ns | 0.1053 ns | 0.0879 ns |  8.06 |    0.03 |           - |           - |           - |                   - |
                       |       |           |           |           |       |         |             |             |             |                     |
               TryFind |  1000 | 13.837 ns | 0.0291 ns | 0.0272 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
            TryFind_v1 |  1000 | 16.108 ns | 0.0415 ns | 0.0367 ns |  1.16 |    0.00 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |  1000 | 21.876 ns | 0.0325 ns | 0.0288 ns |  1.58 |    0.00 |           - |           - |           - |                   - |
  ImmutableDict_TryGet |  1000 | 83.563 ns | 0.1046 ns | 0.0873 ns |  6.04 |    0.01 |           - |           - |           - |                   - |


    ## 2019-03-28: Comparing vs `Dictionary<K, V>`:

                           Method | Count |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------------------- |------ |----------:|----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                          TryFind |    10 |  7.722 ns | 0.0451 ns | 0.0422 ns |  7.718 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |    10 | 18.475 ns | 0.0502 ns | 0.0470 ns | 18.470 ns |  2.39 |    0.01 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |    10 | 22.661 ns | 0.0463 ns | 0.0433 ns | 22.653 ns |  2.93 |    0.02 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |    10 | 72.911 ns | 1.5234 ns | 2.1355 ns | 74.134 ns |  9.25 |    0.23 |           - |           - |           - |                   - |
                                  |       |           |           |           |           |       |         |             |             |             |                     |
                          TryFind |   100 |  9.987 ns | 0.0543 ns | 0.0508 ns |  9.978 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |   100 | 18.110 ns | 0.0644 ns | 0.0602 ns | 18.088 ns |  1.81 |    0.01 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |   100 | 24.402 ns | 0.0978 ns | 0.0915 ns | 24.435 ns |  2.44 |    0.02 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |   100 | 76.689 ns | 0.3632 ns | 0.3397 ns | 76.704 ns |  7.68 |    0.04 |           - |           - |           - |                   - |
                                  |       |           |           |           |           |       |         |             |             |             |                     |
                          TryFind |  1000 | 12.600 ns | 0.1506 ns | 0.1335 ns | 12.551 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |  1000 | 19.023 ns | 0.0575 ns | 0.0538 ns | 19.036 ns |  1.51 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |  1000 | 22.651 ns | 0.1238 ns | 0.1097 ns | 22.613 ns |  1.80 |    0.02 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |  1000 | 83.608 ns | 0.3105 ns | 0.2904 ns | 83.612 ns |  6.64 |    0.07 |           - |           - |           - |                   - |

    ## 2019-03-29: Comparing vs `DictionarySlim<K, V>`:

                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
       DictionarySlim_TryGetValue |    10 |  8.228 ns | 0.0682 ns | 0.0604 ns |  1.00 |    0.01 |           - |           - |           - |                   - |
                          TryFind |    10 |  8.257 ns | 0.0796 ns | 0.0706 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |    10 | 19.615 ns | 0.0251 ns | 0.0209 ns |  2.38 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |    10 | 22.339 ns | 0.0922 ns | 0.0863 ns |  2.71 |    0.03 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |    10 | 69.872 ns | 0.2699 ns | 0.2524 ns |  8.46 |    0.07 |           - |           - |           - |                   - |
                                  |       |           |           |           |       |         |             |             |             |                     |
       DictionarySlim_TryGetValue |   100 |  8.351 ns | 0.1613 ns | 0.1508 ns |  0.69 |    0.01 |           - |           - |           - |                   - |
                          TryFind |   100 | 12.144 ns | 0.0570 ns | 0.0533 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |   100 | 17.985 ns | 0.0880 ns | 0.0823 ns |  1.48 |    0.01 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |   100 | 22.312 ns | 0.0564 ns | 0.0471 ns |  1.84 |    0.01 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |   100 | 75.374 ns | 0.3042 ns | 0.2846 ns |  6.21 |    0.04 |           - |           - |           - |                   - |
                                  |       |           |           |           |       |         |             |             |             |                     |
       DictionarySlim_TryGetValue |  1000 |  8.202 ns | 0.0713 ns | 0.0667 ns |  0.55 |    0.01 |           - |           - |           - |                   - |
                          TryFind |  1000 | 14.919 ns | 0.1101 ns | 0.0919 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |  1000 | 18.073 ns | 0.2415 ns | 0.2141 ns |  1.21 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |  1000 | 22.406 ns | 0.1039 ns | 0.0921 ns |  1.50 |    0.01 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |  1000 | 84.215 ns | 0.2835 ns | 0.2513 ns |  5.65 |    0.04 |           - |           - |           - |                   - |

## 2019-04-08: Full test

|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|                ImHashMap_TryFind |    10 |  9.072 ns | 0.0301 ns | 0.0282 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |    10 |  8.405 ns | 0.0124 ns | 0.0116 ns |  0.93 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |    10 |  8.199 ns | 0.0118 ns | 0.0105 ns |  0.90 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |    10 | 18.151 ns | 0.0724 ns | 0.0677 ns |  2.00 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |    10 | 22.281 ns | 0.1872 ns | 0.1462 ns |  2.45 |    0.02 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |    10 | 70.143 ns | 0.2833 ns | 0.2650 ns |  7.73 |    0.04 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |   100 | 12.698 ns | 0.0545 ns | 0.0510 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |   100 | 12.440 ns | 0.0145 ns | 0.0129 ns |  0.98 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |   100 |  8.197 ns | 0.0157 ns | 0.0139 ns |  0.65 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |   100 | 18.108 ns | 0.0263 ns | 0.0205 ns |  1.43 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |   100 | 22.834 ns | 0.0627 ns | 0.0524 ns |  1.80 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |   100 | 76.253 ns | 0.2767 ns | 0.2311 ns |  6.00 |    0.03 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |  1000 | 14.960 ns | 0.0457 ns | 0.0427 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |  1000 | 14.614 ns | 0.0508 ns | 0.0451 ns |  0.98 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |  1000 |  8.209 ns | 0.0534 ns | 0.0499 ns |  0.55 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |  1000 | 18.256 ns | 0.0383 ns | 0.0320 ns |  1.22 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |  1000 | 22.261 ns | 0.1509 ns | 0.1411 ns |  1.49 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |  1000 | 83.095 ns | 0.3395 ns | 0.3176 ns |  5.55 |    0.03 |           - |           - |           - |                   - |

## 2019-04-08: Different variants tested

|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|               ImHashMap_TryFind2 |    10 |  7.979 ns | 0.0446 ns | 0.0417 ns |  0.93 |    0.01 |           - |           - |           - |                   - |
|               ImHashMap_TryFind3 |    10 |  7.908 ns | 0.0295 ns | 0.0262 ns |  0.92 |    0.00 |           - |           - |           - |                   - |
|                ImHashMap_TryFind |    10 |  8.608 ns | 0.0186 ns | 0.0174 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |    10 |  8.248 ns | 0.0262 ns | 0.0232 ns |  0.96 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |    10 |  7.212 ns | 0.0194 ns | 0.0181 ns |  0.84 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |    10 | 17.707 ns | 0.0631 ns | 0.0590 ns |  2.06 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |    10 | 22.210 ns | 0.1074 ns | 0.1004 ns |  2.58 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |    10 | 72.093 ns | 0.4162 ns | 0.3893 ns |  8.37 |    0.05 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|               ImHashMap_TryFind2 |   100 | 11.245 ns | 0.0174 ns | 0.0163 ns |  0.92 |    0.00 |           - |           - |           - |                   - |
|               ImHashMap_TryFind3 |   100 | 11.488 ns | 0.0801 ns | 0.0710 ns |  0.94 |    0.01 |           - |           - |           - |                   - |
|                ImHashMap_TryFind |   100 | 12.165 ns | 0.0141 ns | 0.0118 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |   100 | 12.272 ns | 0.0367 ns | 0.0343 ns |  1.01 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |   100 |  7.019 ns | 0.0516 ns | 0.0458 ns |  0.58 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |   100 | 17.825 ns | 0.1278 ns | 0.1196 ns |  1.47 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |   100 | 22.189 ns | 0.1034 ns | 0.0968 ns |  1.82 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |   100 | 75.564 ns | 0.3778 ns | 0.3534 ns |  6.21 |    0.03 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|               ImHashMap_TryFind2 |  1000 | 15.909 ns | 0.0924 ns | 0.0864 ns |  1.07 |    0.01 |           - |           - |           - |                   - |
|               ImHashMap_TryFind3 |  1000 | 13.643 ns | 0.0715 ns | 0.0669 ns |  0.92 |    0.01 |           - |           - |           - |                   - |
|                ImHashMap_TryFind |  1000 | 14.819 ns | 0.0465 ns | 0.0412 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |  1000 | 14.541 ns | 0.0555 ns | 0.0520 ns |  0.98 |    0.01 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |  1000 |  7.678 ns | 0.0341 ns | 0.0302 ns |  0.52 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |  1000 | 17.664 ns | 0.0403 ns | 0.0377 ns |  1.19 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |  1000 | 22.010 ns | 0.0497 ns | 0.0465 ns |  1.48 |    0.00 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |  1000 | 83.661 ns | 0.4033 ns | 0.3772 ns |  5.65 |    0.03 |           - |           - |           - |                   - |

## Selecting the 3rd variant:

|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|                ImHashMap_TryFind |    10 |  8.078 ns | 0.0405 ns | 0.0379 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |    10 |  8.224 ns | 0.0114 ns | 0.0101 ns |  1.02 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |    10 |  7.387 ns | 0.0406 ns | 0.0380 ns |  0.91 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |    10 | 17.917 ns | 0.0429 ns | 0.0401 ns |  2.22 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |    10 | 22.256 ns | 0.0726 ns | 0.0643 ns |  2.75 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |    10 | 70.638 ns | 0.6266 ns | 0.5861 ns |  8.74 |    0.09 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |   100 | 11.577 ns | 0.0168 ns | 0.0141 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |   100 | 12.329 ns | 0.0295 ns | 0.0276 ns |  1.07 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |   100 |  7.410 ns | 0.0398 ns | 0.0353 ns |  0.64 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |   100 | 17.890 ns | 0.0425 ns | 0.0377 ns |  1.55 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |   100 | 22.240 ns | 0.0654 ns | 0.0580 ns |  1.92 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |   100 | 78.697 ns | 1.1242 ns | 1.0516 ns |  6.78 |    0.08 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |  1000 | 13.731 ns | 0.0292 ns | 0.0258 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |  1000 | 14.553 ns | 0.0370 ns | 0.0346 ns |  1.06 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |  1000 |  7.345 ns | 0.0208 ns | 0.0194 ns |  0.53 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |  1000 | 18.672 ns | 0.0483 ns | 0.0451 ns |  1.36 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |  1000 | 22.150 ns | 0.1141 ns | 0.1068 ns |  1.61 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |  1000 | 82.402 ns | 0.9798 ns | 0.9165 ns |  6.01 |    0.07 |           - |           - |           - |                   - |

*/
            [Params(10, 100, 1_000)]// the 1000 does not add anything as the LookupKey stored higher in the tree, 1000)]
            public int Count;

            [GlobalSetup]
            public void Populate()
            {
                _map = AddOrUpdate();
                _mapV1 = AddOrUpdate_v1();
                _dict = Dict();
                _dictSlim = DictSlim();
                _concurrentDict = ConcurrentDict();
                _immutableDict = ImmutableDict();
            }

            #region Population

            public ImHashMap<Type, string> AddOrUpdate()
            {
                var map = ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key, "a", out _, out _);

                map = map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!", out _, out _);
                return map;
            }

            private ImHashMap<Type, string> _map;

            public ImTools.OldVersions.V1.ImHashMap<Type, string> AddOrUpdate_v1()
            {
                var map = ImTools.OldVersions.V1.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key, "a");

                map = map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            private ImTools.OldVersions.V1.ImHashMap<Type, string> _mapV1;

            public Dictionary<Type, string> Dict()
            {
                var map = new Dictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");

                return map;
            }

            private Dictionary<Type, string> _dict;

            public DictionarySlim<TypeVal, string> DictSlim()
            {
                var dict = new DictionarySlim<TypeVal, string>();

                foreach (var key in _keys.Take(Count))
                    dict.GetOrAddValueRef(key) = "a";

                dict.GetOrAddValueRef(typeof(ImHashMapBenchmarks)) = "!";
                return dict;
            }

            private DictionarySlim<TypeVal, string> _dictSlim;

            public ConcurrentDictionary<Type, string> ConcurrentDict()
            {
                var map = new ConcurrentDictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            private ConcurrentDictionary<Type, string> _concurrentDict;

            public ImmutableDictionary<Type, string> ImmutableDict()
            {
                var map = ImmutableDictionary<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.Add(key, "a");

                return map.Add(typeof(ImHashMapBenchmarks), "!");
            }

            private ImmutableDictionary<Type, string> _immutableDict;

            #endregion

            public static Type LookupKey = typeof(ImHashMapBenchmarks);

            [Benchmark(Baseline = true)]
            public string ImHashMap_TryFind()
            {
                _map.TryFind3(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ImHashMap_TryFind_V1()
            {
                _mapV1.TryFind(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string DictionarySlim_TryGetValue()
            {
                _dictSlim.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string Dictionary_TryGetValue()
            {
                _dict.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ConcurrentDictionary_TryGetValue()
            {
                _concurrentDict.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ImmutableDict_TryGet()
            {
                _immutableDict.TryGetValue(LookupKey, out var result);
                return result;
            }
        }
    }
}
