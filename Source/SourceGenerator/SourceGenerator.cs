using System.Text;

class SourceGenerator
{
    public StringBuilder Result { get; } = new();
    Emitter _emitter;

    public SourceGenerator(Emitter emitter)
    {
        _emitter = emitter;
    }

    public void Generate(string path)
    {
        Result.AppendLine("#include <stdlib.h>");
        Result.AppendLine("#include <stdint.h>");

        Result.AppendLine("int main() {");
        Result.AppendLine(_emitter.Ouput.ToString());
        Result.AppendLine("}");
    }
}