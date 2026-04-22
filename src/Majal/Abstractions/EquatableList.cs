using System.Collections;

namespace Majal.Abstractions;

public readonly struct EquatableList<T>(IEnumerable<T> list) : IEquatable<EquatableList<T>?>, ICollection<T>
{
    private readonly List<T> _internalList = [..list];

    public bool Equals(EquatableList<T>? other)
    {
        if (_internalList is null && other is null) return true;
        if (_internalList is null || other is null) return false;
        return _internalList.SequenceEqual(other.Value._internalList);
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
            return _internalList.Aggregate(17, (current, item) => current * 23 + (item?.GetHashCode() ?? 0));
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