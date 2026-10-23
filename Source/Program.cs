TypeRegistery.RegisterPrimitives();

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: LenkC <source-file> [output-file]");
    return;
}

string sourcePath = args[0];
string outputPath = args.Length > 1 ? args[1] : Path.ChangeExtension(sourcePath, ".c");

try
{
    string source = File.ReadAllText(sourcePath);

    var lexer = new Lexer(source, sourcePath);
    var tokens = lexer.Lex();

    var parser = new Parser(tokens);
    var stmts = parser.Parse();

    var semantics = new Semantics();
    var checkedStmts = semantics.Check(stmts);
    var mainInfo = semantics.CheckMain();

    var lowerer = new Lowerer();
    lowerer.LowererStmts(checkedStmts);

    var emitter = new Emitter(lowerer);
    emitter.EmitInstructions();

    var generator = new SourceGenerator(emitter);
    generator.Generate(outputPath, mainInfo.Item1, mainInfo.Item2);

    File.WriteAllText(outputPath, generator.Result.ToString());
}
catch (Error error)
{
    error.Exit();
}
