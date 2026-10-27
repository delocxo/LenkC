using System.Text.Json.Serialization;

record LenkCProject(
    [property: JsonPropertyName("sources")] string[] Sources,
    [property: JsonPropertyName("buildOptions")] BuildOptions BuildOptions
);

record BuildOptions(
    [property: JsonPropertyName("compiler")] string Compiler,
    [property: JsonPropertyName("compilerArgs")] string[] CompilerArgs,
    [property: JsonPropertyName("outputPath")] string OutputPath,
    [property: JsonPropertyName("outputExe")] string OutputExe,
    [property: JsonPropertyName("keepGeneratedC")] bool KeepGeneratedC,
    [property: JsonPropertyName("runAfterCompile")] bool RuntAfterCompile
);