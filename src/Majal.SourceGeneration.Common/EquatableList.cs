using System.Collections;

namespace Majal.Common.Abstractions;

public readonly struct EquatableList<T>(IEnumerable<T> list) : IEquatable<EquatableList<T>?>, ICollection<T>
{
    private readonly List<T> _internalList = [..list];

    public bool Equals(EquatableList<T>? other)
    {
        if (_internalList is null && other is null) return true;
        if (_internalList is null || other is null) return false;

        var otherList = other.Value._internalList;
        if (_internalList.Count != otherList.Count) return false;

        for (var index = 0; index < _internalList.Count; index++)
        {
            if (!EqualityComparer<T>.Default.Equals(_internalList[index], otherList[index]))
                return false;
        }

        return true;
    }

    public T this[int index] => _internalList[index];

    public override bool Equals(object? obj) =>
        obj is EquatableList<T> other && Equals(other);

    public IEnumerator<T> GetEnumerator()
    {
        if (_internalList is null) yield break;
        foreach (var item in _internalList)
            yield return item;
    }

    public override int GetHashCode()
    {
        if (_internalList is null) return 0;

        unchecked
        {
            var hash = 17;
            foreach (var item in _internalList)
                hash = hash * 23 + (item?.GetHashCode() ?? 0);

            return hash;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(T item) =>
        _internalList.Add(item);

    public void Clear() =>
        _internalList.Clear();

    public bool Contains(T item) =>
        _internalList.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) =>
        _internalList.CopyTo(array, arrayIndex);

    public bool Remove(T item) =>
        _internalList.Remove(item);

    public int Count =>
        _internalList.Count;

    public bool IsReadOnly => false;
}