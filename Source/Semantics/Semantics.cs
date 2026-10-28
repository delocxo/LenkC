using System.Diagnostics;
using System.Numerics;

partial class Semantics
{
    public static Dictionary<string, (BoundFunctionStmt Function, FunctionSymbol Type)> BoundFunctions { get; } = [];
    public static Dictionary<string, StructSymbol> BoundStructs { get; } = [];
    static Dictionary<string, UnboundFunctionStmt> s_functions = [];
    static Dictionary<string, UnboundStructStmt> s_structs = [];
    static Dictionary<string, EnumSymbol> s_enums = [];

    int _loopDepth = 0;

    public List<Stmt> Check(List<Stmt> stmts)
    {
        List<Stmt> newStmts = [];

        foreach (Stmt stmt in stmts)
            if (stmt is UnboundFunctionStmt unboundFunction)
            {
                if (!s_functions.TryAdd(unboundFunction.Name, unboundFunction))
                    throw new Error($"'{unboundFunction.Name}' is an already existing function", unboundFunction.Position);

                HashSet<string> typeParameters = [];
                foreach (var parameter in unboundFunction.TypeParameters)
                    if (!typeParameters.Add(parameter))
                        throw new Error($"'{parameter}' is a duplicate function type parameter", unboundFunction.Position);

                HashSet<string> parameters = [];
                foreach (var parameter in unboundFunction.Parameters)
                    if (!parameters.Add(parameter.Name))
                        throw new Error($"'{parameter.Name}' is a duplicate function parameter", parameter.Position);

            }
            else if (stmt is UnboundStructStmt structStmt)
            {
                VerfiyUnboundStruct(structStmt);
            }
            else if (stmt is TypeStmt unboundTypeStmt)
            {
                TypeExist(unboundTypeStmt.Name, unboundTypeStmt.Position);

                if (unboundTypeStmt.TypeofType == TypeofType.Numeric)
                    TypeRegistery.Register(unboundTypeStmt.NumericSymbol!);
                else if (unboundTypeStmt.TypeofType == TypeofType.Struct)
                {
                    var structStmt2 = unboundTypeStmt.StructStmt!;
                    VerfiyUnboundStruct(structStmt2);
                }
                else
                    TypeRegistery.Register(new TypeSymbol(unboundTypeStmt.Name, unboundTypeStmt.CName, null));
            }
            else if (stmt is UnboundEnumStmt unboundEnumStmt)
            {
                TypeExist(unboundEnumStmt.Name, unboundEnumStmt.Position);

                var type = Promote(ResolveSymbol(unboundEnumStmt.Symbol));

                if (type is not NumericSymbol numericSymbol || numericSymbol.IsFloat)
                    throw new Error($"Enum type must be a integer", unboundEnumStmt.Position);

                TypeRegistery.Register(unboundEnumStmt.Name, type);

                BigInteger currentInteger = 0;

                Dictionary<string, BigInteger> values = [];

                foreach (var value in unboundEnumStmt.Values)
                {
                    if (values.ContainsKey(value.Name))
                        throw new Error($"'{value.Name}' is a duplicate value of enum '{unboundEnumStmt.Name}'", value.Position);

                    if (value.Expr is null)
                    {
                        BigInteger newValue = currentInteger++;
                        if (!FitsInteger(newValue, numericSymbol))
                            throw new Error($"Enum value no longer fits within type '{numericSymbol}'", value.Position);
                        values[value.Name] = newValue;
                    }
                    else
                    {
                        BigInteger newValue = CheckEnumValue(value.Expr, values);
                        if (!FitsInteger(newValue, numericSymbol))
                            throw new Error($"Enum value no longer fits within type '{numericSymbol}'", value.Position);
                        values[value.Name] = newValue;
                    }
                }

                s_enums.Add(unboundEnumStmt.Name, new(numericSymbol, unboundEnumStmt.Name, values));
            }

        foreach (var stmt in stmts)
            newStmts.Add(CheckStmt(stmt));

        return newStmts;
    }

    void VerfiyUnboundStruct(UnboundStructStmt structStmt)
    {
        if (!s_structs.TryAdd(structStmt.Name, structStmt))
            throw new Error($"'{structStmt.Name}' is an already existing struct", structStmt.Position);

        TypeExist(structStmt.Name, structStmt.Position);

        HashSet<string> typeParameters = [];
        foreach (var parameter in structStmt.TypeParameters)
            if (!typeParameters.Add(parameter))
                throw new Error($"'{parameter}' is a duplicate struct type parameter", structStmt.Position);

        HashSet<string> parameters = [];
        foreach (var fields in structStmt.Fields)
            if (!parameters.Add(fields.Name))
                throw new Error($"'{fields.Name}' is a duplicate struct field", fields.Position);
    }


    public (string, int) CheckMain()
    {
        if (!s_functions.TryGetValue("main", out var mainfunction))
            throw new Error("Program must declare a main function", null);

        if (mainfunction.TypeParameters.Count > 0)
            throw new Error("Main cannot be generic", mainfunction.Position);

        if (mainfunction.Parameters.Count is not (0 or 2))
            throw new Error("Main can only take 0 or 2 parameters", mainfunction.Position);

        var returnType = ResolveSymbol(mainfunction.ReturnType);

        if (returnType != TypeSymbol.Int)
            throw new Error("Main must return an int", mainfunction.Position);

        var bound = BoundFunction(mainfunction, [], mainfunction.Position);

        return (bound.Name, mainfunction.Parameters.Count);
    }

    public BigInteger CheckEnumValue(Expr expr, Dictionary<string, BigInteger> enums)
    {
        if (expr is IntExpr intExpr)
            return intExpr.Value;

        if (expr is NameExpr nameExpr)
        {
            if (nameExpr.TypeArguments.Count > 0)
                throw new Error("Cannot use type arguments in this context", expr.Position);

            if (!enums.TryGetValue(nameExpr.Name, out var bigInteger))
                throw new Error($"''{nameExpr.Name} does not exist in this context", expr.Position);

            return bigInteger;
        }

        if (expr is UnaryExpr unaryExpr)
        {
            if (unaryExpr.Op is not (TokenType.Sub or TokenType.BitwiseNot))
                throw new Error($"Only unary '-' and '~' is allowed in enums values", expr.Position);

            if (unaryExpr.Op is TokenType.Sub)
                return -CheckEnumValue(unaryExpr.Right, enums);

            return ~CheckEnumValue(unaryExpr.Right, enums);
        }

        if (expr is BinaryExpr binaryExpr)
        {
            var left = CheckEnumValue(binaryExpr.Left, enums);
            var right = CheckEnumValue(binaryExpr.Right, enums);

            if (binaryExpr.Op is
                TokenType.And or TokenType.Or or
                TokenType.IsEqual or TokenType.NotEqual or
                TokenType.Less or TokenType.Greater or
                TokenType.LessEq or TokenType.GreaterEq)
                throw new Error("Comparison and equality operators cannot be used in enum values", expr.Position);

            if (binaryExpr.Op is TokenType.BitwiseLeftShift or TokenType.BitwiseRightShift)
            {
                if (right < 0 || right >= 32)
                    throw new Error("Shift count must be bewtween 0 and 31", expr.Position);
                if (binaryExpr.Op is TokenType.BitwiseLeftShift)
                    return left << (int)right;
                return left >> (int)right;
            }

            return binaryExpr.Op switch
            {
                TokenType.Add => left + right,
                TokenType.Sub => left - right,
                TokenType.Mul => left * right,
                TokenType.Div => left / right,
                TokenType.Mod => left % right,
                TokenType.BitwiseAnd => left & right,
                TokenType.BitwiseXor => left ^ right,
                TokenType.BitwiseOr => left | right,
                _ => throw new UnreachableException(binaryExpr.Op.ToString())
            };
        }

        throw new Error("Enum values must be integer literals or negated integer literals", expr.Position);
    }

    public void TypeExist(string name, Position position)
    {
        if (TypeRegistery.Registery.ContainsKey(name))
            throw new Error($"'{name}' is already an existing type", position);

        if (s_structs.ContainsKey(name))
            throw new Error($"'{name}' is already an existing struct", position);

        if (s_enums.ContainsKey(name))
            throw new Error($"'{name}' is already an existing enum", position);
    }
}