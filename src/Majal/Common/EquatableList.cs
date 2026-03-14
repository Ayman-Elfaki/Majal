using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Majal.Common;

public readonly struct EquatableList<T>(IEnumerable<T> list) : IEquatable<EquatableList<T>?>, ICollection<T>
{
    private readonly List<T> _list = [..list];

    public bool Equals(EquatableList<T>? other)
    {
        if (_list is null && other is null) return true;
        if (_list is null || other is null) return false;
        return _list.SequenceEqual(other.Value._list);
    }

    public override bool Equals(object? obj) => 
        obj is EquatableList<T> other && Equals(other);

    public IEnumerator<T> GetEnumerator()
    {
        if (_list is null) yield break;
        foreach (var item in _list)
        {
            yield return item;
        }
    }

    public override int GetHashCode()
    {
        if (_list is null) return 0;
        unchecked
        {
            return _list.Aggregate(17, (current, item) => current * 23 + (item?.GetHashCode() ?? 0));
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item) => _list.Add(item);

    public void Clear() => _list.Clear();

    public bool Contains(T item) => _list.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

    public bool Remove(T item) => _list.Remove(item);

    public int Count => _list.Count;

    public bool IsReadOnly => false;
}