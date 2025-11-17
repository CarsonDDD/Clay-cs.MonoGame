using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace Clay_cs.MonoGame;


// Same idea as Clay-cs.ClayStringCollection.
// In order to convert Texture2D to void* we need to mess with Alloc and thus manage it ourself.
// In order to avoid allocating every frame, manage the allocs here
// This implicitly is then required in every usage of the api.
// In the future this will probably be converted to handle all monogame assets?
// to avoid the meme of having ClayStringCollection, ClayTexture2DCollection, ClayXYZCollection etc.
// However this counts as adding state,...but to the user, and not, "Clay"?
public readonly struct ClayTexture2DCollection() : IDisposable
{
    private readonly Dictionary<Texture2D, GCHandle> _dictionary = new();

    public unsafe Clay_ImageElementConfig Get(Texture2D texture)
    {
        if (_dictionary.TryGetValue(texture, out GCHandle data))
        {
            return new Clay_ImageElementConfig { imageData = (void*)GCHandle.ToIntPtr(data) };
        }

        var handle = GCHandle.Alloc(texture, GCHandleType.Normal);
        var imageData = (void*)GCHandle.ToIntPtr(handle);
        _dictionary[texture] = handle;

        return new Clay_ImageElementConfig { imageData = imageData };
    }

    public void Clear()
    {
        foreach (var pair in _dictionary)
        {
            var handle = pair.Value;
            if (handle.IsAllocated) handle.Free();
        }
        _dictionary.Clear();
    }

    public void Dispose()
    {
        Clear();
    }
}

