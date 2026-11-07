using System.Diagnostics;

partial class Semantics
{
    static int s_vacount = 0;
    static bool s_canUseDParam = false;
    static int s_nextLambda = 0;

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
                            .Select(x => ResolveSymbol(x).RejectVoid("Type argument cannot be void", x.Position))
                            .ToList();

                    if (s_functions.TryGetValue((null, nameExpr.Name), out var set))
                    {
                        if (set.Count > 1)
                            throw new Error($"Function '{nameExpr.Name}' is overloaded, thus cannot be stored", nameExpr.Position);
                        var bound = BoundFunction(set[0], typeArguments, nameExpr.Position);
                        return new(nameExpr with { Name = bound.Name }, bound.Symbol);
                    }

                    if (s_externFunctions.TryGetValue((null, nameExpr.Name), out var externFunction))
                    {
                        if (typeArguments.Count > 0)
                            throw new Error("Extern functions cant take type parameters", nameExpr.Position);
                        return new(nameExpr with { Name = externFunction.CName, IsExtern = true }, externFunction.Function);
                    }

                    if (Scopes.TryResolve(nameExpr.Name, out var local))
                    {
                        if (nameExpr.TypeArguments.Count > 0)
                            throw new Error("Variable does not support generics", nameExpr.Position);
                        return new(nameExpr, local!.TypeSymbol);
                    }

                    throw new Error($"'{nameExpr.Name}' does not exist", nameExpr.Position);
                }

            case UnaryExpr unaryExpr:
                {
                    var resolved = CheckExpr(unaryExpr.Right);
                    TypeSymbol type = resolved.TypeSymbol;
                    if (unaryExpr.Op == TokenType.Bang)
                        type = TypeSymbol.Bool;
                    else if (unaryExpr.Op == TokenType.BitwiseAnd)
                        type = new PointerSymbol(type);
                    else if (unaryExpr.Op == TokenType.Mul)
                    {
                        if (type is PointerSymbol pointer)
                        {
                            if (pointer.Inner is VoidSymbol)
                                throw new Error("Cannot deference a void pointer", unaryExpr.Position);
                            type = pointer.Inner;
                        }
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
                    return CheckBinaryExpr(binaryExpr);
                }

            case AssignExpr assignExpr:
                {
                    var target = Scopes.Resolve(assignExpr.Name, assignExpr.Position);

                    var value = CheckAssignment(
                        new NameExpr(assignExpr.Name, [], assignExpr.Position),
                        assignExpr.Value,
                        assignExpr.AssignOperator,
                        assignExpr.Position
                    );

                    var newExpr = new AssignExpr(
                        assignExpr.Name,
                        assignExpr.AssignOperator,
                        value,
                        assignExpr.Position
                    );

                    return new(newExpr, target.TypeSymbol);
                }

            case PointerAssignExpr pointerAssignExpr:
                {
                    var resolved = CheckExpr(pointerAssignExpr.Target);

                    var value = CheckAssignment(
                        resolved.Expr,
                        resolved.Expr,
                        pointerAssignExpr.AssignOperator,
                        pointerAssignExpr.Position
                    );

                    var newExpr = pointerAssignExpr with
                    {
                        Target = (UnaryExpr)resolved.Expr,
                        Expr = value
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
                    var arguments = callExpr.Arguments
                            .Select(CheckExpr)
                            .ToList();

                    ResolvedExpr? resolvedExpr = null;
                    if (callExpr.Target is NameExpr nameExpr)
                    {
                        var typeArguments = nameExpr.TypeArguments
                            .Select(x => ResolveSymbol(x).RejectVoid("Type argument cannot be void", x.Position))
                            .ToList();

                        if (s_structs.ContainsKey(nameExpr.Name))
                        {
                            var symbol = new NamePSymbol(nameExpr.Name, nameExpr.TypeArguments, nameExpr.Position);

                            var structSymbol = (StructSymbol)ResolveSymbol(symbol);

                            if (structSymbol.IsUnion)
                                throw new Error("Union cannot be constructed via call-syntax", callExpr.Position);

                            if (callExpr.Arguments.Count != structSymbol.Fields.Count)
                                throw new Error($"Struct '{structSymbol.Name}' requires {structSymbol.Fields.Count} arguments, got {callExpr.Arguments.Count}", callExpr.Position);

                            return new(new CallExpr(
                                nameExpr,
                                callExpr.Arguments
                                    .Select(x => CheckExpr(x).Expr)
                                    .ToList(),
                                    callExpr.Position,
                                    structSymbol
                                ), structSymbol
                            );
                        }

                        if (s_functions.TryGetValue((null, nameExpr.Name), out var set))
                        {
                            int previousVaCount = s_vacount;

                            var function = ResolveOverload(
                                    null,
                                    nameExpr.Name,
                                    typeArguments,
                                    arguments,
                                    nameExpr.Position
                                );

                            s_vacount = function.IsVariadic
                                ? callExpr.Arguments.Count - function.Parameters.Count(p => !p.IsVariadic)
                                : 0;

                            var bound = BoundFunction(function, typeArguments, nameExpr.Position);

                            resolvedExpr = new(
                                nameExpr with { Name = bound.Name },
                                bound.Symbol
                            );

                            s_vacount = previousVaCount;
                        }
                    }

                    if (callExpr.Target is MemberExpr memberExpr)
                    {
                        if (memberExpr.Target is NameExpr nameExpr1)
                        {
                            if (nameExpr1.TypeArguments.Count > 0)
                                throw new Error("Member target cannot have type parameters in this context", nameExpr1.Position);

                            if (s_functions.TryGetValue((nameExpr1.Name, memberExpr.Member.Name), out var set))
                            {
                                int previousVaCount = s_vacount;

                                var typeArguments = memberExpr.Member.TypeArguments
                                    .Select(x => ResolveSymbol(x).RejectVoid("Type argument cannot be void", x.Position))
                                    .ToList();

                                var function = ResolveOverload(
                                    nameExpr1.Name,
                                    memberExpr.Member.Name,
                                    typeArguments,
                                    arguments,
                                    nameExpr1.Position
                                );

                                s_vacount = function.IsVariadic
                                    ? callExpr.Arguments.Count - function.Parameters.Count(p => !p.IsVariadic)
                                    : 0;

                                var bound = BoundFunction(function, typeArguments, nameExpr1.Position, nameExpr1.Name);

                                resolvedExpr = new(
                                    memberExpr.Member with { Name = bound.Name },
                                    bound.Symbol
                                );

                                s_vacount = previousVaCount;
                            }
                        }
                    }

                    var target = resolvedExpr ?? CheckExpr(callExpr.Target);

                    if (target.TypeSymbol is FunctionSymbol functionSymbol)
                    {
                        if (functionSymbol.IsVariadic)
                        {
                            if (arguments.Count < functionSymbol.ParameterTypes.Count)
                                throw new Error(
                                    $"Function requires atleast {functionSymbol.ParameterTypes.Count} argument(s), got {arguments.Count}",
                                    callExpr.Position
                                );
                        }
                        else
                        {
                            if (arguments.Count != functionSymbol.ParameterTypes.Count)
                                throw new Error(
                                    $"Function expects {functionSymbol.ParameterTypes.Count} argument(s), got {arguments.Count}",
                                    callExpr.Position
                                );
                        }

                        bool addDummy = functionSymbol.IsVariadic && functionSymbol.ParameterTypes.Count == 0;

                        return new(new CallExpr(
                            target.Expr,
                            arguments
                            .Select(x => x.Expr)
                            .ToList(),
                            callExpr.Position,
                            AddDummy: addDummy

                        ), functionSymbol.ReturnType);
                    }

                    throw new Error("Only functions can be called", callExpr.Position);
                }

            case UnboundCNameExpr unboundCNameExpr:
                {
                    var type = ResolveSymbol(unboundCNameExpr.Symbol);
                    return new(new BoundCNameExpr(type, unboundCNameExpr.Position), TypeSymbol.String);
                }

            case UnboundFmtExpr unboundFmtExpr:
                {
                    var type = ResolveSymbol(unboundFmtExpr.Symbol);
                    if (type.FormatSymbol == null)
                        throw new Error("Type cannot be formatted", unboundFmtExpr.Position);
                    return new(new BoundFmtExpr(type, unboundFmtExpr.Position), TypeSymbol.String);
                }

            case IndexExpr indexExpr:
                {
                    var target = CheckExpr(indexExpr.Target);
                    var index = CheckExpr(indexExpr.Index);
                    if (target.TypeSymbol is not PointerSymbol pointer)
                        throw new Error("Cannot index a non-pointer or non-string", indexExpr.Position);
                    if (pointer.Inner is VoidSymbol)
                        throw new Error("Cannot index a void pointer", indexExpr.Position);
                    return new(new IndexExpr(target.Expr, index.Expr, indexExpr.Position), pointer.Inner);
                }

            case IndexAssignExpr indexAssignExpr:
                {
                    var indexExpr = indexAssignExpr.IndexExpr;
                    var target = CheckExpr(indexExpr.Target);
                    var index = CheckExpr(indexExpr.Index);

                    if (target.TypeSymbol is not PointerSymbol pointer)
                        throw new Error("Cannot index a non-pointer", indexExpr.Position);

                    if (pointer.Inner is VoidSymbol)
                        throw new Error("Cannot index a void pointer", indexExpr.Position);

                    IndexExpr newIndexExpr = new(
                        target.Expr,
                        index.Expr,
                        indexExpr.Position
                    );

                    var value = CheckAssignment(
                        newIndexExpr,
                        indexAssignExpr.Value,
                        indexAssignExpr.AssignOperator,
                        indexAssignExpr.Position
                    );

                    return new(new IndexAssignExpr(
                        newIndexExpr,
                        indexAssignExpr.AssignOperator,
                        value
                    ), pointer.Inner);
                }

            case MemberExpr memberExpr:
                {
                    if (memberExpr.Target is NameExpr nameExpr)
                    {
                        if (nameExpr.TypeArguments.Count > 0)
                            throw new Error("Member target cannot have type parameters in this context", nameExpr.Position);

                        if (s_enums.TryGetValue(nameExpr.Name, out var enumSymbol))
                        {
                            if (memberExpr.Member.TypeArguments.Count > 0)
                                throw new Error($"Enums dont take any type arguments", nameExpr.Position);

                            if (!enumSymbol.Values.TryGetValue(memberExpr.Member.Name, out var value))
                                throw new Error($"Enum '{enumSymbol.Name}' does not contain value '{memberExpr.Member.Name}'", memberExpr.Position);

                            return new(new IntExpr(value, memberExpr.Member.Position), enumSymbol.Type);
                        }

                        if (s_functions.TryGetValue((nameExpr.Name, memberExpr.Member.Name), out var set))
                        {
                            if (set.Count > 1)
                                throw new Error($"Function '{memberExpr.Member.Name}' is overloaded, thus cannot be stored", memberExpr.Member.Position);
                            var typeArguments = memberExpr.Member.TypeArguments
                                .Select(x => ResolveSymbol(x).RejectVoid("Type argument cannot be void", x.Position))
                                .ToList();
                            var bound = BoundFunction(set[0], typeArguments, nameExpr.Position, nameExpr.Name);
                            return new(memberExpr.Member with { Name = bound.Name }, bound.Symbol);
                        }

                        if (s_externFunctions.TryGetValue((nameExpr.Name, memberExpr.Member.Name), out var externFunction))
                        {
                            if (memberExpr.Member.TypeArguments.Count > 0)
                                throw new Error("Extern functions cant take type parameters", memberExpr.Member.Position);
                            return new(nameExpr with { Name = externFunction.CName, IsExtern = true }, externFunction.Function);
                        }
                    }

                    var target = CheckExpr(memberExpr.Target);
                    var member = memberExpr.Member;

                    var targetType = target.TypeSymbol;
                    var targetExpr = target.Expr;

                    if (targetType is PointerSymbol pointer)
                    {
                        targetType = pointer.Inner;
                        targetExpr = new UnaryExpr(targetExpr, TokenType.Mul, memberExpr.Position);
                    }

                    if (targetType is StructSymbol structSymbol)
                    {
                        var field = structSymbol.Fields.Find(x => x.Name == member.Name);
                        if (field != null)
                        {
                            if (member.TypeArguments.Count > 0)
                                throw new Error($"Field '{field.Name}' does not take any type arguments", member.Position);
                            return new(memberExpr with { Target = targetExpr }, field.Type);
                        }
                        throw new Error($"Struct '{structSymbol.Name}' has no field '{member.Name}'", memberExpr.Position);
                    }

                    throw new Error($"Member access requires a struct or pointer to a struct", memberExpr.Position);
                }

            case MemberAssignExpr memberAssignExpr:
                {
                    var target = CheckExpr(memberAssignExpr.Target);
                    var member = memberAssignExpr.Member;

                    var targetType = target.TypeSymbol;
                    var targetExpr = target.Expr;

                    if (targetType is PointerSymbol pointer)
                    {
                        targetType = pointer.Inner;
                        targetExpr = new UnaryExpr(targetExpr, TokenType.Mul, memberAssignExpr.Position);
                    }

                    if (targetType is StructSymbol structSymbol)
                    {
                        var field = structSymbol.Fields.Find(x => x.Name == member);
                        if (field != null)
                        {
                            var memberExpr = new MemberExpr(
                                targetExpr,
                                new(member, [], memberAssignExpr.Position),
                                memberAssignExpr.Position
                            );

                            var value = CheckAssignment(
                                memberExpr,
                                memberAssignExpr.Value,
                                memberAssignExpr.AssignOperator,
                                memberAssignExpr.Position
                            );

                            return new(memberAssignExpr with { Target = targetExpr, Value = value }, field.Type);
                        }
                        throw new Error($"Struct '{structSymbol.Name}' has no field '{member}'", memberAssignExpr.Position);
                    }

                    throw new Error($"Member access requires a struct or pointer to a struct", memberAssignExpr.Position);
                }

            case TypeIsExpr typeIsExpr:
                {
                    var left = ResolveSymbol(typeIsExpr.Left);
                    var right = ResolveSymbol(typeIsExpr.Right);
                    return new(
                        new BoolExpr(left.TypeCheck(right), typeIsExpr.Position),
                        TypeSymbol.Bool
                    );
                }

            case UnboundInitExpr unboundInitExpr:
                {
                    if (unboundInitExpr.Target is NameExpr nameExpr)
                    {
                        if (s_structs.ContainsKey(nameExpr.Name))
                        {
                            var symbol = new NamePSymbol(nameExpr.Name, nameExpr.TypeArguments, nameExpr.Position);

                            var structSymbol = (StructSymbol)ResolveSymbol(symbol);

                            if (!structSymbol.IsUnion)
                                throw new Error("Only unions can be constructed by brace-syntax", unboundInitExpr.Position);

                            // if (unboundInitExpr.Arguments.Count != structSymbol.Fields.Count)
                            //     throw new Error($"Struct '{structSymbol.Name}' requires {structSymbol.Fields.Count} arguments, got {callExpr.Arguments.Count}", callExpr.Position);

                            if (structSymbol.Fields.Find(field => field.Name == unboundInitExpr.Member) == null)
                                throw new Error($"'{unboundInitExpr.Member}' does not exist in union '{structSymbol.Name}'", unboundInitExpr.Position);

                            Expr value = CheckExpr(unboundInitExpr.Value);

                            return new(
                                new BoundInitExpr(structSymbol, unboundInitExpr.Member, value, unboundInitExpr.Position),
                                structSymbol
                            );
                        }
                    }

                    throw new Error("Only unions can be constructed by brace-syntax", unboundInitExpr.Position);
                }

            case VacountExpr vacountExpr:
                if (Scopes.FunctionLevel == 0)
                    throw new Error("Keyword 'vacount' can only be used in a function", vacountExpr.Position);
                return new(new IntExpr(s_vacount, vacountExpr.Position), TypeSymbol.Int);

            case UnboundDefaultExpr unboundDefaultExpr:
                {
                    var type = ResolveSymbol(unboundDefaultExpr.Symbol);
                    return new(new BoundDefaultExpr(type, unboundDefaultExpr.Position), type);
                }

            case DParamExpr dParamExpr:
                if (Scopes.FunctionLevel == 0)
                    throw new Error("Keyword 'dparam' can only be used in a function", dParamExpr.Position);
                if (!s_canUseDParam)
                    throw new Error("Keyword 'dparam' can only be used in a zero-parameter, variadic function", dParamExpr.Position);
                return new(dParamExpr, TypeSymbol.Int);

            case UnboundOutExpr unboundOutExpr:
                {
                    var type = ResolveSymbol(unboundOutExpr.Symbol)
                        .RejectVoid("Cannot use void as a variable type", unboundOutExpr.Position);

                    Scopes.Declare(unboundOutExpr.Name, type, unboundOutExpr.Position);

                    return new(
                        new BoundOutExpr(type, unboundOutExpr.Name, unboundOutExpr.Position),
                        type
                    );
                }

            case HasTagExpr hasTagExpr:
                {
                    var type = ResolveSymbol(hasTagExpr.Target);
                    if (!TypeRegistery.TagRegistery.TryGetValue(type, out var tags))
                        return new(new BoolExpr(false, hasTagExpr.Position), TypeSymbol.Bool);
                    if (!tags.Contains(hasTagExpr.Tag))
                        return new(new BoolExpr(false, hasTagExpr.Position), TypeSymbol.Bool);
                    return new(new BoolExpr(true, hasTagExpr.Position), TypeSymbol.Bool);
                }

            case LambdaExpr lambdaExpr:
                {
                    var function = lambdaExpr.UnboundFunctionStmt;
                    function.IsVariadic = CheckParametersAndIsVariadac(function.Parameters, lambdaExpr.Position);
                    function.Name = $"lambda_{s_nextLambda++}";

                    var prevScopes = Scopes
                        .ScopesData.Take(Scopes.ScopesData.Count - 1)
                        .ToList();

                    var globalScope = Scopes.ScopesData.Last();

                    Scopes.ScopesData.Clear();

                    Scopes.ScopesData.Push(globalScope);

                    var bound = BoundFunction(function, [], lambdaExpr.Position);

                    prevScopes.Reverse();

                    foreach (var scope in prevScopes)
                        Scopes.ScopesData.Push(scope);

                    return new(
                        new NameExpr(bound.Name, [], lambdaExpr.Position),
                        bound.Symbol
                    );
                }
        }

        throw new UnreachableException($"Invalid expression: {expr.GetType().Name}");
    }

    ResolvedExpr CheckBinaryExpr(BinaryExpr binaryExpr)
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

        throw new Error("Binary error", binaryExpr.Position);
    }

    Expr CheckAssignment(Expr target, Expr value, TokenType op, Position position)
    {
        var binaryOp = Lexer.CompoundToBinary[op];

        if (binaryOp == TokenType.Equal)
            return CheckExpr(value).Expr;

        var binary = new BinaryExpr(
            target,
            value,
            binaryOp,
            position
        );

        return ((BinaryExpr)CheckExpr(binary).Expr).Right;
    }

    UnboundFunctionStmt ResolveOverload(string? parent, string name, List<TypeSymbol> typeArguments, List<ResolvedExpr> arguments, Position position)
    {
        if (!s_functions.TryGetValue((parent, name), out var set))
            throw new Error("No matching overloads were found", position);

        List<UnboundFunctionStmt> candidates = [];

        foreach (var overload in set)
        {
            if (overload.TypeParameters.Count != typeArguments.Count)
                continue;

            int fixedCount = overload.Parameters
                .Where(x => !x.IsVariadic)
                .ToList().Count;

            if (overload.IsVariadic)
            {
                if (arguments.Count < fixedCount)
                    continue;
            }
            else
            {
                if (arguments.Count != fixedCount)
                    continue;
            }

            candidates.Add(overload);
        }

        if (candidates.Count == 0)
            throw new Error("No matching overloads were found", position);

        List<(UnboundFunctionStmt Function, List<int> Ranks)> matchs = [];

        foreach (var candidate in candidates)
        {
            Dictionary<string, TypeSymbol?> prevTypes = [];
            List<TypeSymbol> parameterTypes = [];

            for (int i = 0; i < candidate.TypeParameters.Count; i++)
            {
                string param = candidate.TypeParameters[i];

                TypeRegistery.Registery.TryGetValue(param, out var prev);
                prevTypes[param] = prev;
                TypeRegistery.Registery[param] = typeArguments[i];
            }

            parameterTypes = candidate.Parameters
                .Where(x => !x.IsVariadic)
                .Select(p => ResolveSymbol(p.Symbol))
                .ToList();

            foreach (var pair in prevTypes)
            {
                if (pair.Value == null)
                    TypeRegistery.Registery.Remove(pair.Key);
                else
                    TypeRegistery.Registery[pair.Key] = pair.Value;
            }

            List<int> ranks = [];
            bool matchesParameters = true;

            for (int i = 0; i < parameterTypes.Count; i++)
            {
                var paramType = parameterTypes[i];
                var arg = arguments[i];
                var argType = arg.TypeSymbol;

                bool exact = paramType.TypeCheck(argType) && argType.TypeCheck(paramType);

                if (exact)
                    ranks.Add(0);
                else if (arg.Expr is IntExpr literal &&
                    paramType is NumericSymbol numeric &&
                    !numeric.IsFloat &&
                    FitsInteger(literal.Value, numeric))
                {
                    ranks.Add(1);
                }
                else if (paramType.TypeCheck(argType))
                {
                    ranks.Add(1);
                }
                else
                {
                    matchesParameters = false;
                    break;
                }
            }

            while (ranks.Count < arguments.Count)
                ranks.Add(2);

            if (matchesParameters)
                matchs.Add((candidate, ranks));
        }

        if (matchs.Count == 0)
            throw new Error("No matching overloads were found", position);

        if (matchs.Count == 1)
            return matchs[0].Function;

        int lowest = int.MaxValue;
        UnboundFunctionStmt? lowestMatch = null;
        bool ambiguous = false;

        foreach (var match in matchs)
        {
            var summedRanks = match.Ranks.Sum();
            if (summedRanks < lowest)
            {
                lowest = summedRanks;
                lowestMatch = match.Function;
                ambiguous = false;
            }
            else if (summedRanks == lowest)
                ambiguous = true;
        }

        if (lowestMatch == null)
            throw new Error("No matching overloads were found", position);

        if (ambiguous)
            throw new Error("Ambiguous overload", position);

        return lowestMatch;
    }
}