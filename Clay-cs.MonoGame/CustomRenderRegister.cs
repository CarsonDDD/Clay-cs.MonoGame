using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace Clay_cs.MonoGame;

// I have no clue what to name this. I cannot name it Collection, because 1: its then a very long name, but also 2: it has different functionality than string and texture2d
public class CustomRenderRegister: IDisposable
{
    public unsafe delegate void CustomRenderDelegate(void* userData, Clay_BoundingBox boundingBox, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch);


    private readonly Dictionary<int, CustomRenderDelegate> _customRenderers = new();

    public void RegisterCustomRenderer(int id, CustomRenderDelegate handler) => _customRenderers[id] = handler;

    public void UnregisterCustomRenderer(int id) => _customRenderers.Remove(id);

    public bool TryGetValue(int key, out CustomRenderDelegate? output)
    {
        return _customRenderers.TryGetValue(key, out output);
    }

    public void Dispose()
    {
        _customRenderers.Clear();
    }
}

