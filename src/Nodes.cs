using System;
using System.Buffers;
using Dcrew;

namespace BigBoyEngine;

public static class Nodes<T> where T : Node {
    private static FreeList<NodeId> _store = new(1024);

    public static void Add(NodeId id) {
        if (!_store.Has(id.Index))
            _store.Add(id);
        else
            _store[id.Index] = id;
    }

    public static bool TryRemove(NodeId id) {
        if (!_store.Has(id.Index) || _store[id.Index].Generation != id.Generation) return false;
        _store.Remove(id.Index);
        return true;
    }

    public static T Get(int index) {
        if (_store.Has(index) && _store[index].Generation == Core.LatestGeneration[index])
            return (T)Core.Nodes[_store[index].Index];
        return null;
    }
    public static ReadOnlySpan<T> All() {
        var all = _store.All;
        var arr = ArrayPool<T>.Shared.Rent(all.Length);
        var c = 0;
        foreach (var t in all)
            if (_store[t].Generation == Core.LatestGeneration[_store[t].Index])
                arr[c++] = (T)Core.Nodes[_store[t].Index];
        return new ReadOnlySpan<T>(arr, 0, c);
    }
    public static bool Has(NodeId id) => _store.Has(id.Index) && _store[id.Index].Generation == id.Generation;
    public static void Clear() => _store.Clear();
}