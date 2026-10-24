using System.Diagnostics;

class Lowerer
{
    public List<CIrInstruction> Instructions { get; } = [];

    public void LowererStmts(List<Stmt> stmts)
    {
        foreach (Stmt stmt in stmts)
            LowererStmt(stmt);
    }

    void LowererStmt(Stmt stmt)
    {
        switch (stmt)
        {
            case BoundLetStmt boundLetStmt:
                {
                    var value = LowerExpr(boundLetStmt.Expr);
                    Instructions.Add(new CIrVariable(
                        boundLetStmt.TypeSymbol,
                        boundLetStmt.Name,
                        value
                    ));
                    return;
                }

            case ExprStmt exprStmt:
                {
                    var value = LowerExpr(exprStmt.Expr);
                    Instructions.Add(new CIrValueStmt(value));
                    return;
                }

            case IfStmt ifStmt:
                {
                    for (int i = 0; i < ifStmt.IfBranches.Count; i++)
                    {
                        string name = i == 0 ? "if" : "else if";
                        var branch = ifStmt.IfBranches[i];
                        var condition = LowerExpr(branch.Expr);
                        Instructions.Add(new CIrControlFlowStart(name, condition));
                        LowererStmts(branch.Stmts);
                        Instructions.Add(new CIrEnd());
                    }

                    if (ifStmt.ElseBranch != null)
                    {
                        Instructions.Add(new CIrControlFlowStart("else", null));
                        LowererStmts(ifStmt.ElseBranch);
                        Instructions.Add(new CIrEnd());
                    }

                    return;
                }

            case WhileStmt whileStmt:
                {
                    var branch = whileStmt.Branch;
                    var condition = LowerExpr(branch.Expr);
                    Instructions.Add(new CIrControlFlowStart("while", condition));
                    LowererStmts(branch.Stmts);
                    Instructions.Add(new CIrEnd());
                    return;
                }

            case BreakStmt:
                Instructions.Add(new CIrBreak());
                return;

            case ContinueStmt:
                Instructions.Add(new CIrContinue());
                return;

            case UnboundFunctionStmt unboundFunctionStmt:
                {
                    Semantics.BoundFunctions
                        .Where(x => x.Value.Function.Name == unboundFunctionStmt.Name)
                        .ToList()
                        .ForEach(x =>
                        {
                            Instructions.Add(new CIrFunction(x.Value.Function, x.Key));
                            LowererStmts(x.Value.Function.Body);
                            Instructions.Add(new CIrEnd());
                        });
                    return;
                }

            case ReturnStmt returnStmt:
                {
                    if (returnStmt.Expr == null)
                    {
                        Instructions.Add(new CIrReturn(null));
                        return;
                    }
                    Instructions.Add(new CIrReturn(LowerExpr(returnStmt.Expr)));
                    return;
                }

            case UnboundStructStmt unboundStructStmt:
                {
                    Instructions.Add(new CIrStruct(unboundStructStmt.Name));
                    return;
                }
        }
    }

    CIrValue LowerExpr(Expr expr)
    {
        switch (expr)
        {
            case IntExpr intExpr:
                return new CIrLiteral(intExpr.Value);

            case FloatExpr floatExpr:
                return new CIrLiteral(floatExpr.Value);

            case StringExpr stringExpr:
                return new CIrLiteral(stringExpr.Value);

            case CharExpr charExpr:
                return new CIrLiteral(charExpr.Value);

            case BoolExpr boolExpr:
                return new CIrLiteral(boolExpr.Value);

            case NullExpr:
                return new CIrNull();

            case NameExpr nameExpr:
                return new CIrLocal(nameExpr.Name);

            case UnaryExpr unaryExpr:
                {
                    var right = LowerExpr(unaryExpr.Right);
                    return new CIrUnary(unaryExpr.Op, right);
                }

            case BinaryExpr binaryExpr:
                {
                    var left = LowerExpr(binaryExpr.Left);
                    var right = LowerExpr(binaryExpr.Right);
                    return new CIrBinary(binaryExpr.Op, left, right);
                }

            case AssignExpr assignExpr:
                {
                    var value = LowerExpr(assignExpr.Value);
                    return new CIrAssign(assignExpr.Name, value);
                }

            case PointerAssignExpr pointerAssign:
                {
                    var target = LowerExpr(pointerAssign.Target);
                    var value = LowerExpr(pointerAssign.Expr);
                    return new CIrPointerAssign(target, value);
                }

            case BoundNativeExpr boundNativeExpr:
                {
                    List<CIrNativePart> parts = boundNativeExpr.Parts
                        .Select<NativePart, CIrNativePart>(x =>
                            x switch
                            {
                                NativeExpr ne => new CIrNativeValue(LowerExpr(ne.Expr)),
                                NativeText nt => new CIrNativeText(nt.Text),
                                _ => throw new UnreachableException()
                            }
                        ).ToList();
                    return new CIrNative(parts);
                }

            case BoundCastExpr boundCastExpr:
                {
                    var value = LowerExpr(boundCastExpr.Value);
                    return new CIrCast(boundCastExpr.Symbol, value);
                }

            case CallExpr callExpr:
                {
                    var arguments = callExpr.Arguments
                        .Select(LowerExpr)
                        .ToList();
                    if (callExpr.Struct != null)
                        return new CIrConstructer(callExpr.Struct, arguments);
                    var target = LowerExpr(callExpr.Target);
                    return new CIrCall(target, arguments);
                }

            case BoundCNameExpr boundCNameExpr:
                {
                    return new CIrCName(boundCNameExpr.TypeSymbol);
                }

            case BoundFmtExpr boundFmtExpr:
                {
                    return new CIrFmt(boundFmtExpr.TypeSymbol);
                }

            case IndexExpr indexExpr:
                {
                    return new CIrIndex(
                        LowerExpr(indexExpr.Target),
                        LowerExpr(indexExpr.Index)
                    );
                }

            case IndexAssignExpr indexAssignExpr:
                {
                    return new CIrIndexSet(
                        LowerExpr(indexAssignExpr.IndexExpr.Target),
                        LowerExpr(indexAssignExpr.IndexExpr.Index),
                        LowerExpr(indexAssignExpr.Value)
                    );
                }

            case MemberExpr memberExpr:
                {
                    return new CIrMember(LowerExpr(memberExpr.Target), memberExpr.Member.Name);
                }

            case MemberAssignExpr memberAssignExpr:
                {
                    return new CIrMemberSet(
                        LowerExpr(memberAssignExpr.Target),
                        memberAssignExpr.Member,
                        LowerExpr(memberAssignExpr.Value)
                    );
                }

            case ResolvedExpr resolvedExpr:
                return LowerExpr(resolvedExpr.Expr);
        }

        throw new UnreachableException(expr.GetType().Name);
    }
}