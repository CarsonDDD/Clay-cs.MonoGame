namespace Clay_cs.MonoGame;


// This class leaves memory management of the objects to the user
// This is only for associating ids with void* UserData, so the data can be passed around (through passing its id)
// Yes, GC go brrr
public class UserDataRegister : IDisposable
{
    private readonly Dictionary<int, object?> _map = new();
    private readonly Stack<int> _free = new();
    private int _next = 1;

    public int Register(object? data)
    {
        int id = _free.Count > 0 ? _free.Pop() : _next++;
        _map[id] = data;
        return id;
    }

    public object? Get(int id)
    {
        return _map.TryGetValue(id, out var obj) ? obj : null;
    }

    public void Unregister(int id)
    {
        if (_map.Remove(id))
        {
            _free.Push(id);
        }
    }

    public void clear()
    {
        _map.Clear();
        _free.Clear();
        _next = 1;
    }

    public void Dispose()
    {
        clear();
    }
}

