using Godot;
using System.Collections.Generic;

public partial class ObjectPool<T> : Node where T : Node2D
{
	private readonly Stack<T> _availableObjects = new();
	private readonly HashSet<T> _activeObjects = new();

	private PackedScene _objectScene;
	private int _maxSize = 10;

	public int AvailableCount => _availableObjects.Count;
	public int ActiveCount => _activeObjects.Count;
	public int TotalCount => _availableObjects.Count + _activeObjects.Count;

	public void Initialize(PackedScene objectScene, int initialSize = 5, int maxSize = 20)
	{
		_objectScene = objectScene;
		_maxSize = maxSize;

		for (int i = 0; i < initialSize; i++)
		{
			CreateNewObject();
		}

		// Ensure the pool does not exceed the maximum size
		while (_availableObjects.Count > _maxSize)
		{
			var obj = _availableObjects.Pop();
			obj.QueueFree();
			_activeObjects.Remove(obj);
		}
	}

	private T CreateNewObject()
	{
		var obj = _objectScene.Instantiate<T>();
		obj.ProcessMode = ProcessModeEnum.Disabled;
		obj.Visible = false;
		AddChild(obj);
		_availableObjects.Push(obj);
		return obj;
	}

	public T Get()
	{
		T obj;

		if (_availableObjects.Count > 0)
		{
			obj = _availableObjects.Pop();
		}
		else
		{
			obj = CreateNewObject();
			_availableObjects.Pop();
		}

		RemoveChild(obj);

		obj.ProcessMode = ProcessModeEnum.Inherit;
		obj.Visible = true;

		_activeObjects.Add(obj);

		return obj;
	}

	public void Return(T obj)
	{
		if (!_activeObjects.Contains(obj))
		{
			GD.PrintErr("Attempted to return an object that does not belong to the pool.");
			return;
		}

		_activeObjects.Remove(obj);

		if (obj.GetParent() != this)
		{
			obj.GetParent()?.RemoveChild(obj);
		}

		obj.ProcessMode = ProcessModeEnum.Disabled;
		obj.Visible = false;

		AddChild(obj);

		if (_availableObjects.Count < _maxSize)
		{
			_availableObjects.Push(obj);
		}
		else
		{
			obj.QueueFree();
		}
	}

	public void ReturnAll()
	{
		foreach (var obj in _activeObjects)
		{
			Return(obj);
		}
	}

	public void Clear()
	{
		ReturnAll();

		while (_availableObjects.Count > 0)
		{
			var obj = _availableObjects.Pop();
			obj.QueueFree();
		}

		_availableObjects.Clear(); 
		_activeObjects.Clear();
	}

	public override void _ExitTree()
	{
		Clear();
		base._ExitTree();
	}
}
