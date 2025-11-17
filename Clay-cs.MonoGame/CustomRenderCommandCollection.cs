using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace Clay_cs.MonoGame;

public class CustomRenderCommandCollection: IDisposable
{

    // This void pointer is generally supposed to be assumed to be a struct starting with int id;
    public unsafe delegate void CustomRenderDelegate(void* renderData, Clay_BoundingBox boundingBox, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch);


    private readonly Dictionary<int, CustomRenderDelegate> _customRenderers = new();

    public void RegisterCustomRenderer(int id, CustomRenderDelegate handler) => _customRenderers[id] = handler;

    public void UnregisterCustomRenderer(int id) => _customRenderers.Remove(id);

    public bool TryGetValue(int key, out CustomRenderDelegate output)
    {
        return _customRenderers.TryGetValue(key, out output);
    }

    public void Dispose()
    {
        _customRenderers.Clear();
    }
}

