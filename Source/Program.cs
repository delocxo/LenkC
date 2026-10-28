using System.Diagnostics;
using System.Text.Json;

TypeRegistery.RegisterPrimitives();

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: LenkC <project-path> ");
    return 1;
}

string projectPath = args[0];

if (args.Length > 1)
{
    if (args[1] == "new")
    {
        string lenkcPath = Path.Join(projectPath, "lenkc.json");

        string projectName = Path.GetFileName(Path.TrimEndingDirectorySeparator(projectPath));

        if (File.Exists(lenkcPath))
        {
            Console.Error.WriteLine($"Project '{projectName}' already exist");
            return 1;
        }

        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Join(projectPath, "dist"));

        string jsonTemplate = $$"""
        {
            "sources": [],
            "buildOptions": {
                "compiler": "your-compiler",
                "compilerArgs": [],
                "outputPath": "dist",
                "outputExe": "{{projectName}}.exe",
                "keepGeneratedC": true,
                "runArgs": [],
                "runAfterCompile": true
            }
        }
        """;

        File.WriteAllText(Path.Join(projectPath, "lenkc.json"), jsonTemplate);

        string lenkcMainTemplate = """
        @int main(int argc, string` argv)
        {
            return 0;
        }
        """;

        File.WriteAllText(Path.Join(projectPath, "main.lenkc"), lenkcMainTemplate);
        return 0;
    }
}

if (!Directory.Exists(projectPath))
{
    Console.Error.WriteLine($"'{projectPath}' does not exist");
    return 1;
}

string jsonPath = Path.Join(projectPath, "lenkc.json");

if (!File.Exists(jsonPath))
{
    Console.Error.WriteLine("Could not find lenkc.json");
    return 1;
}

using var stream = File.OpenRead(jsonPath);

LenkCProject? lenkCProject = JsonSerializer.Deserialize<LenkCProject>(stream);
if (lenkCProject == null)
{
    Console.Error.WriteLine("Failed to load lenkc.json");
    return 1;
}

string outputPath = Path.Join(projectPath, lenkCProject.BuildOptions.OutputPath);
Directory.CreateDirectory(outputPath);

try
{
    List<Token> combinedTokens = [];
    Position lastPosition = new();

    var combinedPaths = lenkCProject.Sources
        .Select(path => Path.GetFullPath(Path.Join(projectPath, path)))
        .ToList();
    var sourceFiles = Directory
        .GetFiles(projectPath, "*.lenkc", SearchOption.AllDirectories)
        .Where(path => !Path.GetFullPath(path).StartsWith(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(outputPath)) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase
        ))
        .Select(Path.GetFullPath)
        .ToList();

    combinedPaths.AddRange(sourceFiles);

    foreach (string path in combinedPaths.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"'{path}' does not exist");
            return 1;
        }
        var lexer = new Lexer(File.ReadAllText(path), path);
        combinedTokens.AddRange(lexer.Lex());
        lastPosition = lexer.CurrentPos;
    }

    combinedTokens.Add(new Token(TokenType.Eof, "End of File", lastPosition));

    var parser = new Parser(combinedTokens);
    var stmts = parser.Parse();

    var semantics = new Semantics();
    var checkedStmts = semantics.Check(stmts);
    var mainInfo = semantics.CheckMain();

    var lowerer = new Lowerer();
    lowerer.LowererStmts(checkedStmts);

    var emitter = new Emitter(lowerer);
    emitter.EmitInstructions();

    string generatedCPath = Path.Join(outputPath, "generated_c.c");

    var generator = new SourceGenerator(emitter);
    generator.Generate(generatedCPath, mainInfo.Item1, mainInfo.Item2);

    File.WriteAllText(generatedCPath, generator.Result.ToString());

    var options = lenkCProject.BuildOptions;

    string exePath = Path.Join(outputPath, options.OutputExe);

    var startInfo = new ProcessStartInfo
    {
        FileName = options.Compiler,
        UseShellExecute = false
    };

    startInfo.ArgumentList.Add(generatedCPath);
    startInfo.ArgumentList.Add("-o");
    startInfo.ArgumentList.Add(exePath);

    foreach (string arg in options.CompilerArgs)
        startInfo.ArgumentList.Add(arg);

    using var process = Process.Start(startInfo);
    if (process == null)
    {
        Console.Error.WriteLine("Failed to start c compiler");
        return 1;
    }

    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        Console.Error.WriteLine($"C compiler exited with code {process.ExitCode}");
        return 1;
    }

    if (!options.KeepGeneratedC)
        File.Delete(generatedCPath);

    if (options.RuntAfterCompile)
    {
        var proccess = Process.Start($"{exePath}", options.CompilerArgs);
        proccess.WaitForExit();
        return proccess.ExitCode;
    }

    return 0;
}
catch (Error error)
{
    error.Exit();
    return 1;
}
