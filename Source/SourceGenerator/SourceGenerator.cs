using System.Text;

class SourceGenerator
{
    public StringBuilder Result { get; } = new();
    Emitter _emitter;

    public SourceGenerator(Emitter emitter)
    {
        _emitter = emitter;
    }

    public void Generate(string path, string mainName, int argumentCount)
    {
        mainName = Emitter.GetMangledName(mainName);

        Result.AppendLine("#include <stdlib.h>");
        Result.AppendLine("#include <stdint.h>");

        Result.AppendLine(_emitter.Ouput.ToString());

        Result.AppendLine(_emitter.Functions.ToString());

        Result.AppendLine($$"""
        int main(int argc, char** argv) {
            return {{mainName}}({{(argumentCount == 0 ? "" : "argc, argv")}});
        }
        """);
    }
}