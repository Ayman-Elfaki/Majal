using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Models;

internal enum DtoNodeState
{
    Visiting,
    Completed,
    Failed
}

internal sealed class DtoGraph
{
    private readonly Dictionary<INamedTypeSymbol, DtoGraphNode> nodes =
        new(SymbolEqualityComparer.Default);
    private readonly Dictionary<string, INamedTypeSymbol> names = new(StringComparer.Ordinal);

    public bool Register(INamedTypeSymbol sourceSymbol, string dtoName)
    {
        if (nodes.ContainsKey(sourceSymbol)) return false;

        nodes[sourceSymbol] = new DtoGraphNode(sourceSymbol, dtoName, DtoNodeState.Visiting);
        if (!names.ContainsKey(dtoName)) names.Add(dtoName, sourceSymbol);
        return true;
    }

    public bool TryGetNode(INamedTypeSymbol sourceSymbol, out DtoGraphNode node) =>
        nodes.TryGetValue(sourceSymbol, out node!);

    public bool TryGetNode(string dtoName, out DtoGraphNode node)
    {
        if (names.TryGetValue(dtoName, out var sourceSymbol))
            return nodes.TryGetValue(sourceSymbol, out node!);

        node = null!;
        return false;
    }

    public void Complete(INamedTypeSymbol sourceSymbol, DtoData data)
    {
        if (nodes.TryGetValue(sourceSymbol, out var node))
            node.Complete(data);
    }

    public void Fail(INamedTypeSymbol sourceSymbol)
    {
        if (nodes.TryGetValue(sourceSymbol, out var node))
            node.State = DtoNodeState.Failed;
    }

    public IEnumerable<DtoData> GetCompletedDtos(INamedTypeSymbol root)
    {
        return nodes.Values
            .Where(node => node.State == DtoNodeState.Completed &&
                           !SymbolEqualityComparer.Default.Equals(node.SourceSymbol, root))
            .Select(node => node.Data!.Value);
    }
}

internal sealed class DtoGraphNode(
    INamedTypeSymbol sourceSymbol,
    string dtoName,
    DtoNodeState state)
{
    public INamedTypeSymbol SourceSymbol { get; } = sourceSymbol;
    public string DtoName { get; } = dtoName;
    public DtoNodeState State { get; set; } = state;
    public DtoData? Data { get; private set; }

    public void Complete(DtoData data)
    {
        Data = data;
        State = DtoNodeState.Completed;
    }
}
