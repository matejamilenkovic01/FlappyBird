using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnSpawned();
    void OnDespawned();
}

public class ComponentPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Stack<T> _inactive = new();
    private readonly HashSet<T> _active = new();

    public int CountActive => _active.Count;
    public int CountInactive => _inactive.Count;

    public ComponentPool(T prefab, Transform parent, int prewarm)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < prewarm; i++)
        {
            T item = Create();
            item.gameObject.SetActive(false);
            _inactive.Push(item);
        }
    }

    public T Get()
    {
        T item = _inactive.Count > 0 ? _inactive.Pop() : Create();
        _active.Add(item);
        item.gameObject.SetActive(true);

        if (item is IPoolable poolable)
            poolable.OnSpawned();

        return item;
    }

    public void Release(T item)
    {
        if (item == null || !_active.Remove(item))
            return;

        if (item is IPoolable poolable)
            poolable.OnDespawned();

        item.gameObject.SetActive(false);
        _inactive.Push(item);
    }

    private T Create() => Object.Instantiate(_prefab, _parent);
}
