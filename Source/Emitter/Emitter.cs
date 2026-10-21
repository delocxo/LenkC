using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

class Emitter
{
    public StringBuilder Ouput { get; } = new();
    Lowerer _lowerer;

    int indentLevel = 1;
    int indentSize = 4;

    public Emitter(Lowerer lowerer)
    {
        _lowerer = lowerer;
    }

    public void EmitInstructions()
    {
        foreach (var instruction in _lowerer.Instructions)
            EmitInstruction(instruction);
    }

    void EmitInstruction(CIrInstruction cIrInstruction)
    {
        switch (cIrInstruction)
        {
            case CIrVariable cIrVariable:
                {
                    string cName = TypeToCType(cIrVariable.Type);
                    string value = EmitValue(cIrVariable.Value);
                    Emit($"{cName} {GetMangledName(cIrVariable.Name)} = {value};");
                    break;
                }
        }
    }

    string EmitValue(CIrValue cIrValue)
    {
        switch (cIrValue)
        {
            case CIrLocal cIrLocal:
                return GetMangledName(cIrLocal.Name);

            case CIrLiteral cIrLiteral:
                return cIrLiteral.Value switch
                {
                    BigInteger bi => bi.ToString(CultureInfo.InvariantCulture),
                    double d => d.ToString(CultureInfo.InvariantCulture),
                    string s => $"\"{ToCString(s)}\"",
                    char c => $"'{ToCString(c.ToString(CultureInfo.InvariantCulture))}'",
                    bool b => b ? "1" : "0",
                    _ => throw new UnreachableException()
                };

            case CIrUnary cIrUnary:
                {
                    string value = EmitValue(cIrUnary.Right);
                    return cIrUnary.Op switch
                    {
                        TokenType.Sub => $"-({value})",
                        TokenType.Bang => $"!({value})",
                        _ => throw new UnreachableException()
                    };
                }

            case CIrBinary cIrBinary:
                {
                    string left = EmitValue(cIrBinary.Left);
                    string right = EmitValue(cIrBinary.Right);
                    string op = cIrBinary.Op switch
                    {
                        TokenType.Add => "+",
                        TokenType.Sub => "-",
                        TokenType.Mul => "*",
                        TokenType.Div => "/",
                        TokenType.Mod => "%",
                        TokenType.IsEqual => "==",
                        TokenType.NotEqual => "!=",
                        TokenType.Less => "<",
                        TokenType.Greater => ">",
                        TokenType.LessEq => "<=",
                        TokenType.GreaterEq => ">=",
                        TokenType.And => "&&",
                        TokenType.Or => "||",
                        TokenType.BitwiseLeftShift => "<<",
                        TokenType.BitwiseRightShift => ">>",
                        TokenType.BitwiseAnd => "&",
                        TokenType.BitwiseOr => "|",
                        TokenType.BitwiseXor => "^",
                        _ => throw new UnreachableException()
                    };
                    return $"({left} {op} {right})";
                }
        }

        throw new UnreachableException();
    }

    void Emit(string line)
    {
        Ouput
        .Append(' ', indentSize * indentLevel)
        .AppendLine(line);
    }

    string GetMangledName(string name)
        => $"_variable_{name}_";

    string ToCString(string str)
        => str
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace("\a", "\\a")
            .Replace("\0", "\\0")
            .Replace("\"", "\\\"")
            .Replace("\f", "\\f")
            .Replace("\v", "\\v")
            .Replace("\b", "\\b")
            .Replace("\e", "\\e")
            .Replace("\r", "\\r");

    string TypeToCType(TypeSymbol typeSymbol)
    {
        return typeSymbol.CName;
    }
}
