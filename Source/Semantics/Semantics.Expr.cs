using System.Diagnostics;

partial class Semantics
{
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

                    if (s_functions.TryGetValue(nameExpr.Name, out var function))
                    {
                        var bound = BoundFunction(function, typeArguments, nameExpr.Position);
                        return new(nameExpr with { Name = bound.Name }, bound.Symbol);
                    }

                    if (s_externFunctions.TryGetValue(nameExpr.Name, out var externFunction))
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
                    if (callExpr.Target is NameExpr nameExpr)
                    {
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
                    }

                    var target = CheckExpr(callExpr.Target);
                    var arguments = callExpr.Arguments
                        .Select(x => CheckExpr(x).Expr)
                        .ToList();

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

                        return new(new CallExpr(
                            target.Expr,
                            arguments,
                            callExpr.Position
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
                        throw new Error("Cannot index a non-pointer", indexExpr.Position);
                    if (pointer.Inner is VoidSymbol)
                        throw new Error("Cannot index a void pointer", indexExpr.Position);
                    return new(new IndexExpr(target.Expr, index.Expr, indexExpr.Position), pointer.Inner);
                }

            case IndexAssignExpr indexAssignExpr:
                {
                    var indexExpr = indexAssignExpr.IndexExpr;
                    var target = CheckExpr(indexExpr.Target);
                    var index = CheckExpr(indexExpr.Index);
                    var value = CheckExpr(indexAssignExpr.Value);

                    if (target.TypeSymbol is not PointerSymbol pointer)
                        throw new Error("Cannot index a non-pointer", indexExpr.Position);

                    if (pointer.Inner is VoidSymbol)
                        throw new Error("Cannot index a void pointer", indexExpr.Position);

                    return new(new IndexAssignExpr(
                        new(
                            target.Expr,
                            index.Expr,
                            indexExpr.Position
                        ),
                        value.Expr
                    ), value.TypeSymbol);
                }

            case MemberExpr memberExpr:
                {
                    if (memberExpr.Target is NameExpr nameExpr)
                    {
                        if (s_enums.TryGetValue(nameExpr.Name, out var enumSymbol))
                        {
                            if (memberExpr.Member.TypeArguments.Count > 0)
                                throw new Error($"Enums dont take any type arguments", nameExpr.Position);

                            if (!enumSymbol.Values.TryGetValue(memberExpr.Member.Name, out var value))
                                throw new Error($"Enum '{enumSymbol.Name}' does not contain value '{memberExpr.Member.Name}'", memberExpr.Position);

                            return new(new IntExpr(value, memberExpr.Member.Position), enumSymbol.Type);
                        }
                    }

                    var target = CheckExpr(memberExpr.Target);
                    var member = memberExpr.Member;

                    var targetType = target.TypeSymbol;
                    var targetExpr = target.Expr;

                    if (targetType is PointerSymbol pointer)
                    {
                        targetType = pointer.Inner;
                        targetExpr = new UnaryExpr(targetExpr, TokenType.BackTick, memberExpr.Position);
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
                    var value = CheckExpr(memberAssignExpr.Value);

                    var targetType = target.TypeSymbol;
                    var targetExpr = target.Expr;

                    if (targetType is PointerSymbol pointer)
                    {
                        targetType = pointer.Inner;
                        targetExpr = new UnaryExpr(targetExpr, TokenType.BackTick, memberAssignExpr.Position);
                    }

                    if (targetType is StructSymbol structSymbol)
                    {
                        var field = structSymbol.Fields.Find(x => x.Name == member);
                        if (field != null)
                            return new(memberAssignExpr with { Target = targetExpr, Value = value.Expr }, value.TypeSymbol);
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
        }

        throw new UnreachableException($"Invalid expression: {expr.GetType().Name}");
    }
}