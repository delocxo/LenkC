using System.Diagnostics;
using System.Numerics;

class Semantics
{
    public static Dictionary<string, (BoundFunctionStmt Function, FunctionSymbol Type)> BoundFunctions { get; } = [];
    static Dictionary<string, UnboundFunctionStmt> s_functions = [];

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

        foreach (var stmt in stmts)
            newStmts.Add(CheckStmt(stmt));

        return newStmts;
    }

    Stmt CheckStmt(Stmt stmt)
    {
        if (stmt is UnboundLetStmt unboundLetStmt)
        {
            var type = ResolveSymbol(unboundLetStmt.PSymbol)
                .RejectVoid("Cannot use void as a variable type", unboundLetStmt.Position); ;
            var resolvedExpr = CheckExpr(unboundLetStmt.Expr);
            if (resolvedExpr.TypeSymbol is NullSymbol && type is not PointerSymbol)
                throw new Error("Cannont assign null to a non-pointer variable", unboundLetStmt.Position);
            resolvedExpr.TypeSymbol.RejectVoid("Expression cannot be void", unboundLetStmt.Position);
            Scopes.Declare(unboundLetStmt.Name, type, unboundLetStmt.Position);
            return new BoundLetStmt(
                type,
                unboundLetStmt.Name,
                resolvedExpr,
                unboundLetStmt.Position
            );
        }
        else if (stmt is InferStmt inferStmt)
        {
            var resolvedExpr = CheckExpr(inferStmt.Expr);
            resolvedExpr.TypeSymbol.RejectVoid("Expression cannot be void", inferStmt.Position);
            if (resolvedExpr.TypeSymbol is NullSymbol)
                throw new Error("Cannot infer a variable from null", inferStmt.Position);
            Scopes.Declare(inferStmt.Name, resolvedExpr.TypeSymbol, inferStmt.Position);
            return new BoundLetStmt(
                resolvedExpr.TypeSymbol,
                inferStmt.Name,
                resolvedExpr,
                inferStmt.Position
            );
        }
        else if (stmt is ExprStmt exprStmt)
        {
            if (exprStmt.Expr is not (AssignExpr or PointerAssignExpr or UnboundNativeExpr))
                throw new Error($"Expression statement can only be an assignment or call: {exprStmt.Expr.GetType().Name}", exprStmt.Position);
            return new ExprStmt(
                CheckExpr(exprStmt.Expr).Expr,
                exprStmt.Position
            );
        }
        else if (stmt is IfStmt ifStmt)
        {
            List<IfBranch> branches = [];
            foreach (var branch in ifStmt.IfBranches)
            {
                Scopes.BeginScope();
                var expr = CheckExpr(branch.Expr).Expr;
                var stmts = Check(branch.Stmts);
                Scopes.EndScope();
                branches.Add(new IfBranch(expr, stmts));
            }

            List<Stmt>? elseBranch = null;
            if (ifStmt.ElseBranch != null)
            {
                Scopes.BeginScope();
                elseBranch = Check(ifStmt.ElseBranch);
                Scopes.EndScope();
            }

            return new IfStmt(branches, elseBranch, ifStmt.Position);
        }
        else if (stmt is WhileStmt whileStmt)
        {
            var branch = whileStmt.Branch;
            var expr = CheckExpr(branch.Expr).Expr;
            Scopes.BeginScope();
            _loopDepth++;
            var stmts = Check(branch.Stmts);
            _loopDepth--;
            Scopes.EndScope();
            return new WhileStmt(new IfBranch(expr, stmts), whileStmt.Position);
        }
        else if (stmt is BreakStmt breakStmt)
        {
            if (_loopDepth <= 0)
                throw new Error("Cannont use break outside a loop", breakStmt.Position);
            return breakStmt;
        }
        else if (stmt is ContinueStmt continueStmt)
        {
            if (_loopDepth <= 0)
                throw new Error("Cannot use continue outside a loop", continueStmt.Position);
            return continueStmt;
        }
        else if (stmt is UnboundFunctionStmt unboundFunctionStmt)
        {
            if (!Scopes.AtGlobalLevel)
                throw new Error($"Function '{unboundFunctionStmt.Name}' can only be declared at the top level", unboundFunctionStmt.Position);
            return unboundFunctionStmt;
        }
        else if (stmt is ReturnStmt returnStmt)
        {
            if (Scopes.FunctionLevel == 0)
                throw new Error("Cannot use a return outside a function", returnStmt.Position);
            if (returnStmt.Expr == null)
                return returnStmt;
            return new ReturnStmt(
                CheckExpr(returnStmt.Expr),
                returnStmt.Position
            );
        }

        throw new UnreachableException($"Invalid statement: {stmt.GetType().Name}");
    }

    (FunctionSymbol Symbol, string Name) BoundFunction(UnboundFunctionStmt unboundFunctionStmt, List<TypeSymbol> typeArguments, Position position)
    {
        if (typeArguments.Count != unboundFunctionStmt.TypeParameters.Count)
            throw new Error($"'{unboundFunctionStmt.Name}' expects {unboundFunctionStmt.TypeParameters.Count} argument(s), got {typeArguments.Count}", unboundFunctionStmt.Position);

        string key = $"function_{unboundFunctionStmt.Name}_" + string.Join("_", typeArguments.Select(TypeKey));

        if (BoundFunctions.TryGetValue(key, out var bound))
            return (bound.Type, key);

        Dictionary<string, TypeSymbol> pairedTypeArguments = unboundFunctionStmt.TypeParameters
            .Zip(typeArguments)
            .ToDictionary(x => x.First, x => x.Second);

        Dictionary<string, TypeSymbol?> previousTypes = [];

        foreach (var pair in pairedTypeArguments)
        {
            TypeRegistery.Registery.TryGetValue(pair.Key, out var previous);
            previousTypes.Add(pair.Key, previous);
            TypeRegistery.Registery[pair.Key] = pair.Value;
        }

        Scopes.BeginScope();

        Semantics semantics = new Semantics();

        var returnType = semantics.ResolveSymbol(unboundFunctionStmt.ReturnType);

        if (returnType is not VoidSymbol &&
            unboundFunctionStmt.Body.Count == 0 ||
            unboundFunctionStmt.Body[^1] is not ReturnStmt returnStmt ||
            returnStmt.Expr is null)
        {
            throw new Error(
                $"Non-void function '{unboundFunctionStmt.Name}' must have a final return with a value",
                unboundFunctionStmt.Position
            );
        }

        var boundParameters = unboundFunctionStmt.Parameters
            .Select(x =>
            {
                var type = semantics.ResolveSymbol(x.Symbol);
                return new BoundField(type, x.Name);
            }).ToList();

        foreach (var boundParam in boundParameters)
            Scopes.Declare(boundParam.Name, boundParam.Type, unboundFunctionStmt.Position);

        FunctionSymbol functionSymbol = new(returnType, boundParameters.Select(x => x.Type).ToList());

        List<Stmt> newBody = [];

        BoundFunctions[key] = (new(
            returnType,
            unboundFunctionStmt.Name,
            boundParameters,
            newBody,
            unboundFunctionStmt.Position
        ), functionSymbol);

        Scopes.FunctionLevel++;

        newBody.AddRange(semantics.Check(unboundFunctionStmt.Body));

        Scopes.FunctionLevel--;

        Scopes.EndScope();

        foreach (var pair in previousTypes)
            if (pair.Value == null)
                TypeRegistery.Registery.Remove(pair.Key);
            else
                TypeRegistery.Registery[pair.Key] = pair.Value;

        return (functionSymbol, key);
    }

    ResolvedExpr CheckExpr(Expr expr)
    {
        switch (expr)
        {
            case IntExpr intExpr:
                return new(intExpr, ResolveInteger(intExpr.Value, intExpr.Position));

            case FloatExpr floatExpr:
                return new(floatExpr, ResolveFloat(floatExpr.Value));

            case StringExpr stringExpr:
                return new(stringExpr, TypeSymbol.String);

            case CharExpr charExpr:
                return new(charExpr, TypeSymbol.Char);

            case BoolExpr boolExpr:
                return new(boolExpr, TypeSymbol.Bool);

            case NullExpr nullExpr:
                return new(nullExpr, TypeSymbol.Null);

            case NameExpr nameExpr:
                {
                    var typeArguments = nameExpr.TypeArguments
                            .Select(ResolveSymbol)
                            .ToList();

                    if (s_functions.TryGetValue(nameExpr.Name, out var function))
                    {
                        var bound = BoundFunction(function, typeArguments, nameExpr.Position);
                        return new(nameExpr with { Name = bound.Name }, bound.Symbol);
                    }

                    if (Scopes.TryResolve(nameExpr.Name, out var local))
                    {
                        if (nameExpr.TypeArguments.Count > 0)
                            throw new Error("Variable does not support generics", nameExpr.Position);
                        return new(nameExpr, local!.TypeSymbol);
                    }

                    break;
                }

            case UnaryExpr unaryExpr:
                {
                    var resolved = CheckExpr(unaryExpr.Right);
                    TypeSymbol type = resolved.TypeSymbol;
                    if (unaryExpr.Op == TokenType.Bang)
                        type = TypeSymbol.Bool;
                    else if (unaryExpr.Op == TokenType.BitwiseAnd)
                        type = new PointerSymbol(type);
                    else if (unaryExpr.Op == TokenType.BackTick)
                    {
                        if (type is PointerSymbol pointer)
                        {
                            if (pointer.Inner is VoidSymbol)
                                throw new Error("Cannot deference a void pointer", unaryExpr.Position);
                            type = pointer.Inner;
                        }
                        else if (type is StringSymbol)
                            type = TypeSymbol.Char;
                        else
                            throw new Error("Cannot deference a string or non-pointer", unaryExpr.Position);
                    }
                    else if (unaryExpr.Op == TokenType.BitwiseNot)
                    {
                        if (type == TypeSymbol.Char || type == TypeSymbol.Bool)
                            type = TypeSymbol.Int;
                        else if (type is NumericSymbol n && !n.IsFloat)
                            type = n.Bits < 32 ? TypeSymbol.Int : n;
                        else
                            throw new Error("Bitwise NOT requires an integer", unaryExpr.Position);
                    }
                    else if (type is NumericSymbol n && !n.IsFloat && n.Bits < 32)
                        type = TypeSymbol.Int;
                    return new(unaryExpr with { Right = resolved.Expr }, type);
                }

            case BinaryExpr binaryExpr:
                {
                    var left = CheckExpr(binaryExpr.Left);
                    var right = CheckExpr(binaryExpr.Right);

                    if (left.TypeSymbol == TypeSymbol.Char || left.TypeSymbol == TypeSymbol.Bool)
                        left = left with { TypeSymbol = TypeSymbol.Int };

                    if (right.TypeSymbol == TypeSymbol.Char || right.TypeSymbol == TypeSymbol.Bool)
                        right = right with { TypeSymbol = TypeSymbol.Int };

                    var newExpr = binaryExpr with
                    {
                        Left = left.Expr,
                        Right = right.Expr
                    };

                    if (binaryExpr.Op is
                        TokenType.And or TokenType.Or or
                        TokenType.IsEqual or TokenType.NotEqual or
                        TokenType.Less or TokenType.Greater or
                        TokenType.LessEq or TokenType.GreaterEq)
                    {
                        return new(newExpr, TypeSymbol.Bool);
                    }

                    if (left.TypeSymbol is PointerSymbol pointer &&
                        right.TypeSymbol is NumericSymbol number &&
                        !number.IsFloat &&
                        binaryExpr.Op is TokenType.Add or TokenType.Sub)
                    {
                        return new(newExpr, pointer);
                    }

                    if (right.TypeSymbol is PointerSymbol pointerRight &&
                        left.TypeSymbol is NumericSymbol numberLeft &&
                        !numberLeft.IsFloat &&
                        binaryExpr.Op == TokenType.Add)
                    {
                        return new(newExpr, pointerRight);
                    }

                    if (left.TypeSymbol is PointerSymbol &&
                        right.TypeSymbol is PointerSymbol &&
                        binaryExpr.Op == TokenType.Sub)
                    {
                        return new(newExpr, TypeSymbol.Long);
                    }

                    if (left.TypeSymbol is NumericSymbol l && right.TypeSymbol is NumericSymbol r)
                    {
                        if (binaryExpr.Op is TokenType.BitwiseLeftShift or TokenType.BitwiseRightShift)
                        {
                            if (!l.IsFloat && l.Bits < 32)
                                l = TypeSymbol.Int;
                            return new ResolvedExpr(newExpr, l);
                        }
                        return new(newExpr, PromoteArithmetic(l, r));
                    }

                    throw new Error("Binary error", expr.Position);
                }

            case AssignExpr assignExpr:
                {
                    Scopes.Resolve(assignExpr.Name, assignExpr.Position);
                    var resolved = CheckExpr(assignExpr.Value);
                    var newExpr = new AssignExpr(assignExpr.Name, resolved.Expr, assignExpr.Position);
                    return new(newExpr, resolved.TypeSymbol);
                }

            case PointerAssignExpr pointerAssignExpr:
                {
                    var resolved = CheckExpr(pointerAssignExpr.Target);
                    var value = CheckExpr(pointerAssignExpr.Expr);
                    var newExpr = pointerAssignExpr with
                    {
                        Target = (UnaryExpr)resolved.Expr,
                        Expr = value.Expr
                    };
                    return new(newExpr, resolved.TypeSymbol);
                }

            case UnboundNativeExpr unboundNativeExpr:
                {
                    var type = ResolveSymbol(unboundNativeExpr.Symbol);
                    string source = unboundNativeExpr.Source;
                    Position position = unboundNativeExpr.Position;

                    List<NativePart> parts = [];
                    int i = 0;

                    while (i < source.Length)
                    {
                        int start = source.IndexOf("${", i, StringComparison.Ordinal);

                        if (start == -1)
                        {
                            parts.Add(new NativeText(source[i..]));
                            break;
                        }

                        if (start > i)
                            parts.Add(new NativeText(source[i..start]));

                        int end = source.IndexOf('}', start + 2);

                        if (end == -1)
                            throw new Error("Unterminated native expression", position);

                        string text = source[(start + 2)..end];

                        var nativeExpr = Parser.ParseExprText(text, position.Source);
                        var resolved = CheckExpr(nativeExpr);

                        parts.Add(new NativeExpr(resolved.Expr));

                        i = end + 1;
                    }

                    return new(
                        new BoundNativeExpr(type, parts, position),
                        type
                    );
                }

            case UnboundCastExpr unboundCastExpr:
                {
                    var symbol = ResolveSymbol(unboundCastExpr.Symbol);
                    var value = CheckExpr(unboundCastExpr.Value).Expr;
                    return new(new BoundCastExpr(symbol, value, unboundCastExpr.Position), symbol);
                }

            case CallExpr callExpr:
                {
                    var target = CheckExpr(callExpr.Target);
                    var arguments = callExpr.Arguments
                        .Select(x => CheckExpr(x).Expr)
                        .ToList();

                    if (target.TypeSymbol is FunctionSymbol functionSymbol)
                    {
                        if (arguments.Count != functionSymbol.ParameterTypes.Count)
                            throw new Error(
                                $"Function expects {functionSymbol.ParameterTypes.Count} argument(s), got {arguments.Count}",
                                callExpr.Position
                            );

                        return new(new CallExpr(
                            target.Expr,
                            arguments,
                            callExpr.Position
                        ), functionSymbol.ReturnType);
                    }

                    throw new Error("Only functions can be called", callExpr.Position);
                }
        }

        throw new UnreachableException($"Invalid expression: {expr.GetType().Name}");
    }

    public TypeSymbol ResolveSymbol(PSymbol pSymbol)
    {
        switch (pSymbol)
        {
            case NamePSymbol namePSymbol:
                {
                    if (!TypeRegistery.Registery.TryGetValue(namePSymbol.Name, out var value))
                        throw new Error($"Type '{namePSymbol.Name}' does not exist", pSymbol.Position);
                    return value;
                }

            case PointerPSymbol pointerPSymbol:
                return new PointerSymbol(ResolveSymbol(pointerPSymbol.InnerSymbol));

            case FunctionPSymbol functionPSymbol:
                {
                    var parameterTypes = functionPSymbol.Parameters
                        .Select(ResolveSymbol)
                        .ToList();

                    var returnType = ResolveSymbol(functionPSymbol.ReturnType);

                    return new FunctionSymbol(returnType, parameterTypes);
                }

            default:
                throw new NotImplementedException();
        }
    }

    TypeSymbol ResolveInteger(BigInteger value, Position position)
    {
        if (value <= int.MaxValue)
            return TypeSymbol.Int;
        else if (value <= long.MaxValue)
            return TypeSymbol.Long;
        else if (value <= ulong.MaxValue)
            return TypeSymbol.ULong;
        throw new Error("Integer literal to larger", position);
    }

    TypeSymbol ResolveFloat(double value)
    {
        return value <= float.MaxValue ? TypeSymbol.Float : TypeSymbol.Double;
    }

    NumericSymbol PromoteArithmetic(NumericSymbol left, NumericSymbol right)
    {
        if (!left.IsFloat && left.Bits < 32)
            left = TypeSymbol.Int;

        if (!right.IsFloat && right.Bits < 32)
            right = TypeSymbol.Int;

        return left.Promote(right);
    }

    string TypeKey(TypeSymbol type) => type switch
    {
        PointerSymbol pointer => $"ptr_{TypeKey(pointer.Inner)}_end",
        FunctionSymbol function =>
            $"fn_{function.ParameterTypes.Count}_" +
            string.Join("_", function.ParameterTypes.Select(TypeKey)) +
            $"_ret_{TypeKey(function.ReturnType)}_end",
        _ => type.Name
    };

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
}