using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

class Emitter
{
    public StringBuilder Ouput { get; } = new();
    public StringBuilder Functions { get; } = new();
    public int IndentLevel { get; set; } = 0;
    Lowerer _lowerer;

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

    List<CIrInstruction> GetInstructionsUntil<T>() where T : CIrInstruction
    {
        List<CIrInstruction> instructions = [];
        while (_ip < _lowerer.Instructions.Count)
        {
            var instruction = _lowerer.Instructions[_ip++];
            if (instruction is T)
                break;
            instructions.Add(instruction);
        }
        return instructions;
    }

    void EmitInstruction(CIrInstruction cIrInstruction)
    {
        switch (cIrInstruction)
        {
            case CIrVariable cIrVariable:
                {
                    string cName = TypeToCType(cIrVariable.Type, GetMangledName(cIrVariable.Name));
                    string value = EmitValue(cIrVariable.Value);
                    Emit($"{cName} = {value};");
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

                    IndentLevel++;

                    EmitInstructionsUntil<CIrEnd>();

                    IndentLevel--;

                    Emit("}");
                    break;
                }

            case CIrBreak:
                Emit("break;");
                break;

            case CIrContinue:
                Emit("continue;");
                break;

            case CIrFunction cIrFunction:
                {
                    string name = TypeToCType(cIrFunction.Function.ReturnType, GetMangledName(cIrFunction.Name));

                    var parameters = cIrFunction.Function.Parameters
                        .Select(x => TypeToCType(x.Type, GetMangledName(x.Name)));

                    var stringParameters = string.Join(", ", parameters);

                    Emit($"{name}({stringParameters});");

                    var instructions = GetInstructionsUntil<CIrFunctionEnd>();

                    Lowerer lowerer = new();
                    lowerer.Instructions.AddRange(instructions);

                    Emitter emitter = new(lowerer);

                    emitter.Emit($"{name}({stringParameters}) {{");

                    emitter.IndentLevel++;

                    emitter.EmitInstructions();

                    emitter.IndentLevel--;

                    emitter.Emit("}");

                    Functions.AppendLine(emitter.Ouput.ToString());

                    break;
                }

            case CIrReturn cIrReturn:
                {
                    if (cIrReturn.Value == null)
                    {
                        Emit("return;");
                        break;
                    }
                    string value = EmitValue(cIrReturn.Value);
                    Emit($"return {value};");
                    break;
                }

            case CIrStruct cIrStruct:
                {
                    Semantics.BoundStructs
                        .Where(x => x.Value.Name == cIrStruct.Name)
                        .Select(x => x.Value)
                        .ToList()
                        .ForEach(x =>
                        {
                            if (x.IsUnion)
                                Emit("typedef union {");
                            else
                                Emit("typedef struct {");

                            IndentLevel++;

                            foreach (var field in x.Fields)
                            {
                                string fieldString = TypeToCType(field.Type, field.Name);
                                Emit($"{fieldString};");
                            }

                            IndentLevel--;

                            Emit($"}} {x.CName};");
                        });
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
                        TokenType.BitwiseNot => $"~({value})",
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

            case CIrCast cIrCast:
                {
                    string type = TypeToCType(cIrCast.Type);
                    string value = EmitValue(cIrCast.Value);
                    return $"(({type}){value})";
                }

            case CIrCall cIrCall:
                {
                    string target = EmitValue(cIrCall.Target);
                    string arguments = string.Join(", ", cIrCall.Arguments.Select(EmitValue));
                    return $"({target})({arguments})";
                }

            case CIrConstructer cIrConstructer:
                {
                    string arguments = string.Join(", ", cIrConstructer.Arguments.Select(EmitValue));
                    return $"({cIrConstructer.StructSymbol.CName}){{{arguments}}}";
                }

            case CIrCName cIrCName:
                {
                    return TypeToCType(cIrCName.Type);
                }

            case CIrFmt cIrFmt:
                {
                    return cIrFmt.Type.FormatSymbol!;
                }

            case CIrIndex cIrIndex:
                {
                    string target = EmitValue(cIrIndex.Target);
                    string index = EmitValue(cIrIndex.Index);
                    return $"{target}[{index}]";
                }

            case CIrIndexSet cIrIndexSet:
                {
                    string target = EmitValue(cIrIndexSet.Target);
                    string index = EmitValue(cIrIndexSet.Index);
                    string value = EmitValue(cIrIndexSet.Value);
                    return $"{target}[{index}] = {value}";
                }

            case CIrMember cIrMember:
                {
                    string target = EmitValue(cIrMember.Target);
                    return $"({target}).{cIrMember.Member}";
                }

            case CIrMemberSet cIrMemberSet:
                {
                    string target = EmitValue(cIrMemberSet.Target);
                    string value = EmitValue(cIrMemberSet.Value);
                    return $"({target}).{cIrMemberSet.Member} = {value}";
                }

            case CIrInit cIrInit:
                {
                    string vlaue = EmitValue(cIrInit.Value);
                    return $"({cIrInit.StructSymbol.CName}){{ .{cIrInit.Member} = {vlaue} }}";
                }
        }

        throw new UnreachableException();
    }

    public void Emit(string line)
    {
        Ouput
        .Append(' ', _indentSize * IndentLevel)
        .AppendLine(line);
    }

    public static string GetMangledName(string name)
        => $"_lenkc_{name}_";

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

    public static string TypeToCType(TypeSymbol typeSymbol, string? name = null)
    {
        if (typeSymbol is PointerSymbol pointerSymbol)
        {
            return TypeToCType(pointerSymbol.Inner, $"*{name}");
        }
        if (typeSymbol is FunctionSymbol functionSymbol)
        {
            var parameters = functionSymbol.ParameterTypes
                .Select(x => TypeToCType(x));

            string parameterList = string.Join(", ", parameters);

            if (parameterList.Length == 0)
                parameterList = "void";

            var returnType = TypeToCType(functionSymbol.ReturnType);

            return $"{returnType} (*{name})({parameterList})";
        }
        return name == null ? typeSymbol.CName : $"{typeSymbol.CName} {name}";
    }
}
