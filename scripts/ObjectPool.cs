using System.Collections.Generic;
using Godot;

public partial class ObjectPool<T> : Node where T : Node
{
    private readonly Stack<T> _available = new();
    private readonly HashSet<T> _active = new();

    private PackedScene _scene;
    private int _maxSize = 10;

    public void Initialize(PackedScene scene, int initialSize = 5, int maxSize = 20)
    {
        _scene = scene;
        _maxSize = maxSize;

        for (int i = 0; i < initialSize; i++)
            CreateNew();
    }

    private T CreateNew()
    {
        if (_scene == null)
        {
            GD.PrintErr("ObjectPool: Initialize() not called.");
            return null;
        }

        var obj = _scene.Instantiate<T>();

        obj.ProcessMode = ProcessModeEnum.Disabled;

        if (obj is CanvasItem ci)
            ci.Visible = false;

        AddChild(obj);
        _available.Push(obj);
        return obj;
    }

    public T Get()
    {
        T obj;

        if (_available.Count > 0)
        {
            obj = _available.Pop();
        }
        else
        {
            obj = CreateNew();
            if (obj == null)
                return null;

            // CreateNew already pushed it to available
            _available.Pop();
        }

        if (obj.GetParent() == this)
            RemoveChild(obj);

        obj.ProcessMode = ProcessModeEnum.Inherit;

        if (obj is CanvasItem ci)
            ci.Visible = true;

        _active.Add(obj);
        return obj;
    }

    public void Return(T obj)
    {
        if (obj == null)
            return;

        if (!_active.Contains(obj))
        {
            GD.PrintErr("ObjectPool: Returning object not owned by pool.");
            return;
        }

        _active.Remove(obj);

        var parent = obj.GetParent();
        if (parent != null && parent != this)
            parent.RemoveChild(obj);

        obj.ProcessMode = ProcessModeEnum.Disabled;

        if (obj is CanvasItem ci)
            ci.Visible = false;

        if (obj.GetParent() != this)
            AddChild(obj);

        if (_available.Count < _maxSize)
            _available.Push(obj);
        else
            obj.QueueFree();
    }

    public void ReturnAll()
    {
        var list = new List<T>(_active);
        for (int i = 0; i < list.Count; i++)
            Return(list[i]);
    }

    public void Clear()
    {
        ReturnAll();

        while (_available.Count > 0)
            _available.Pop().QueueFree();

        _available.Clear();
        _active.Clear();
    }

    public override void _ExitTree()
    {
        Clear();
        base._ExitTree();
    }
}
