using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Clay_cs.MonoGame.Examples;

public unsafe class CustomRenderExample : Game, IDisposable
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ClayArenaHandle _arena;
    private ClayStringCollection _clayString = new ClayStringCollection();
    private Texture2D _texturePrimitive;
    Stack<ScissorFrame> _scissorStack = new Stack<ScissorFrame>();

    private CustomRenderCommandCollection _customRenderers = new CustomRenderCommandCollection();
    private UserDataCollection _userDataCollection = new UserDataCollection();


    public CustomRenderExample()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
        _graphics.ApplyChanges();
        IsMouseVisible = true;

        base.Initialize();
    }

    protected override unsafe void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // white pixel and font boilder plate
        _texturePrimitive = new Texture2D(GraphicsDevice, 1, 1);
        _texturePrimitive.SetData(new[] { Color.White });
        MonoGameClay.Fonts[0] = Content.Load<SpriteFont>("myfont");

        uint requiredSize = Clay.MinMemorySize();
        _arena = Clay.CreateArena(requiredSize);
        Clay.Initialize(_arena, new Clay_Dimensions(
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height),
            data => Debug.WriteLine($"{data.errorType}: {data.errorText.ToCSharpString()}")
        );

        Clay.SetMeasureTextFunction(MonoGameClay.MeasureText);

        // # Non boiler plate

        // Define custom renderer
        _customRenderers.RegisterCustomRenderer(1, (void* userData, Clay_BoundingBox bb, GraphicsDevice gd, SpriteBatch sb) =>
        {
            var rect = new Rectangle((int)MathF.Round(bb.x), (int)MathF.Round(bb.y), (int)MathF.Round(bb.width), (int)MathF.Round(bb.height));
            sb.Draw(_texturePrimitive, rect, Color.Magenta * 0.6f);

            int id = (int)(nint)userData;// userData is not enforced to be an int, but it is probably best practice to be used as one referencing an id, so we dont need to deal with pointers for complex objects
            
            var obj = _userDataCollection.Get(id);

            Point? p = obj as Point?;

            var font = MonoGameClay.Fonts[0];
            if (font != null)
            {
                string text = p.HasValue ? $"CUSTOM RENDER: {p.Value.X},{p.Value.Y}" : "CUSTOM RENDER: (no data)";
                var size = font.MeasureString(text);
                var pos = new Vector2(bb.x + (bb.width - size.X) / 2f, bb.y + (bb.height - size.Y) / 2f);
                sb.DrawString(font, text, pos, Color.White);
            }
        });
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // feed mouse to Clay for UI interactions
        var mouse = Mouse.GetState();
        Clay.SetPointerState(new System.Numerics.Vector2(mouse.X, mouse.Y), mouse.LeftButton == ButtonState.Pressed);

        base.Update(gameTime);
    }

    protected override unsafe void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        Clay.SetLayoutDimensions(new Clay_Dimensions(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
        Clay.BeginLayout();

        // spacer top
        using (Clay.Element(new Clay_ElementDeclaration { layout = new Clay_LayoutConfig { sizing = new Clay_Sizing(Clay_SizingAxis.Fixed(100), Clay_SizingAxis.Grow()) } })) { }

        using (Clay.Element(new Clay_ElementDeclaration
        {
            layout = new Clay_LayoutConfig
            {
                sizing = new Clay_Sizing(Clay_SizingAxis.Fixed(400), Clay_SizingAxis.Fixed(200)),
                childAlignment = new Clay_ChildAlignment(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
            },
            custom = new Clay_CustomElementConfig { customData = (void*)1 },// index referring to a custom renderer. This is enforced when rendering
            backgroundColor = new Clay_Color(100, 100, 20, 50),
            userData = (void*)(uint)_userDataCollection.Register(Mouse.GetState().Position) // Registering this, assigns it an ID, so we can reference it in other places
        }))
        {

        }

        var commands = Clay.EndLayout();

        MonoGameClay.RenderCommands(commands, GraphicsDevice, _spriteBatch, _texturePrimitive, _scissorStack, _customRenderers);

        // unregister after rendering
        _userDataCollection.clear();// Either clear all, or unregister individually

        base.Draw(gameTime);
    }

    public void Dispose()
    {
        _customRenderers.Dispose();
        _clayString.Dispose();
        _spriteBatch?.Dispose();
        _texturePrimitive?.Dispose();
        _arena.Dispose();
        _userDataCollection.Dispose();
    }
}
