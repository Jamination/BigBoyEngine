namespace BigBoyEngine;

public readonly struct NodeId {
    public readonly int Index;
    public readonly int Generation;

    public NodeId(int index, int generation) {
        Index = index;
        Generation = generation;
    }

    public Node Get() => Core.Nodes[Index];
}