using System.Runtime.CompilerServices;

#if DEBUG
[assembly: InternalsVisibleTo("ReflectionTools.Tests")]
#endif

#if DEBUG
[assembly: InternalsVisibleTo("ReflectionTools.Harmony")]
#else
[assembly: InternalsVisibleTo("ReflectionTools.Harmony, PublicKey=" +
                              "00240000048000009400000006020000002400005253413100040000010001006d253c34557fe6" +
                              "702a74a088199f5ea78d49d9d657e69169dadc0e8d3a24ab2d7b3d85df22a52bc78ad5fb2c55d6" +
                              "acddf9ac781bc36a321ea8fdfe019eb05832476f6708f445fb3d634556d9c8b76ffe6e83ca3e7a" +
                              "8106216f5165c05850333c6257ca7297e7ddefa3ff0b5bb3a21e4a108289576296763b17522bf2" +
                              "e491b2f9")]
#endif