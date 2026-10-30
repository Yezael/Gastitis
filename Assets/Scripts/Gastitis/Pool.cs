using System.Collections.Generic;
using UnityEngine;

public class Pool<T> where T : MonoBehaviour
{
	private readonly T _prefab;
	private readonly Transform _poolParent;
	private readonly int _growthAmount;

	private readonly Stack<T> _availableItems = new Stack<T>();
	private readonly HashSet<T> _pooledItems = new HashSet<T>();
	private readonly HashSet<T> _checkedOutItems = new HashSet<T>();

	public int AvailableCount => _availableItems.Count;

	public Pool(T prefab, Transform poolParent, int growthAmount)
	{
		if (growthAmount < 1)
		{
			growthAmount = 1;
		}

		_prefab = prefab;
		_poolParent = poolParent;
		_growthAmount = growthAmount;
	}
	public T Get()
	{
		if (_availableItems.Count == 0)
		{
			Grow();
		}

		var item = _availableItems.Pop();
		_checkedOutItems.Add(item);
		item.transform.SetParent(_poolParent, false);
		item.gameObject.SetActive(true);
		return item;
	}

	public void Release(T item)
	{
		if (item == null || !_pooledItems.Contains(item)) return;
		if (!_checkedOutItems.Remove(item)) return;

		item.transform.SetParent(_poolParent, false);
		item.gameObject.SetActive(false);
		_availableItems.Push(item);
	}

	private void Grow()
	{
		for (var i = 0; i < _growthAmount; i++)
		{
			var item = Object.Instantiate(_prefab, _poolParent);
			item.gameObject.SetActive(false);
			_pooledItems.Add(item);
			_availableItems.Push(item);
		}
	}
}
