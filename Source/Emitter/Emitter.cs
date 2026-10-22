using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

class Emitter
{
    public StringBuilder Ouput { get; } = new();
    Lowerer _lowerer;

    int _indentLevel = 1;
    int _indentSize = 4;
    int _ip = 0;

    public Emitter(Lowerer lowerer)
    {
        _lowerer = lowerer;
    }

    public void EmitInstructions()
    {
        while (_ip < _lowerer.Instructions.Count)
            EmitInstruction(_lowerer.Instructions[_ip++]);
    }

    void EmitInstructionsUntil<T>() where T : CIrInstruction
    {
        while (_ip < _lowerer.Instructions.Count)
        {
            var instruction = _lowerer.Instructions[_ip++];
            if (instruction is T)
                break;
            EmitInstruction(instruction);
        }
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

            case CIrValueStmt cIrValueStmt:
                {
                    string value = EmitValue(cIrValueStmt.Value);
                    if (cIrValueStmt.Value is CIrNative)
                        Emit($"{value}");
                    else
                        Emit($"{value};");
                    break;
                }

            case CIrControlFlowStart controlFlowStart:
                {
                    bool hasCondition = controlFlowStart.Condition != null;
                    string condition = hasCondition ? EmitValue(controlFlowStart.Condition!) : "";

                    Emit($"{controlFlowStart.Name}{(hasCondition ? $" ({condition})" : "")} {{");

                    _indentLevel++;

                    EmitInstructionsUntil<CIrControlFlowEnd>();

                    _indentLevel--;

                    Emit("}");
                    break;
                }

            case CIrBreak:
                Emit("break;");
                break;

            case CIrContinue:
                Emit("continue;");
                break;
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

            case CIrNull:
                return "NULL";

            case CIrUnary cIrUnary:
                {
                    string value = EmitValue(cIrUnary.Right);
                    return cIrUnary.Op switch
                    {
                        TokenType.Sub => $"-({value})",
                        TokenType.Bang => $"!({value})",
                        TokenType.BitwiseAnd => $"&{value}",
                        TokenType.BackTick => $"*{value}",
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

            case CIrAssign cIrAssign:
                {
                    var value = EmitValue(cIrAssign.Value);
                    return $"{GetMangledName(cIrAssign.Name)} = {value}";
                }

            case CIrPointerAssign cIrPointerAssign:
                {
                    var target = EmitValue(cIrPointerAssign.Target);
                    var value = EmitValue(cIrPointerAssign.Value);
                    return $"{target} = {value}";
                }

            case CIrNative cIrNative:
                {
                    var parts = cIrNative.Parts
                        .Select(
                            x => x switch
                            {
                                CIrNativeValue ne => EmitValue(ne.Value),
                                CIrNativeText nt => nt.Text,
                                _ => throw new UnreachableException()
                            }
                        );
                    string final = string.Join("", parts);
                    return final;
                }
        }

        throw new UnreachableException();
    }

    void Emit(string line)
    {
        Ouput
        .Append(' ', _indentSize * _indentLevel)
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
        if (typeSymbol is PointerSymbol pointerSymbol)
        {
            return $"{TypeToCType(pointerSymbol.Inner)}*";
        }
        return typeSymbol.CName;
    }
}
