class Local
{
    public Local(string name, TypeSymbol typeSymbol)
    {
        Name = name;
        TypeSymbol = typeSymbol;
    }

    public string Name { get; }
    public TypeSymbol TypeSymbol { get; }
}

static class Scopes
{
    public static Stack<Dictionary<string, Local>> ScopesData { get; set; } = new([new()]);

    public static bool AtGlobalLevel => ScopesData.Count == 1 /* || FunctionLevel > 0*/;
    public static int FunctionLevel = 0;

    public static Local Declare(string name, TypeSymbol typeSymbol, Position position)
    {
        if (ScopesData.Peek().ContainsKey(name))
            throw new Error($"'{name}' has already been declared", position);

        Local local = new Local(name, typeSymbol);
        ScopesData.Peek().Add(name, local);

        return local;
    }

    public static bool TryResolve(string name, out Local? local)
    {
        foreach (var scope in ScopesData)
            if (scope.TryGetValue(name, out local))
                return true;
        local = null;
        return false;
    }

    public static Local Resolve(string name, Position position)
    {
        if (!TryResolve(name, out var local))
            throw new Error($"'{name}' does not exist", position);
        return local!;
    }

    public static void BeginScope() => ScopesData.Push(new());
    public static void EndScope() => ScopesData.Pop();
}