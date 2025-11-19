using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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


    // ui code
    struct CustomRenderData
    {
        public string text;
    }

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

        // white pixel and font
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

        // Define custom renderer
        _customRenderers.RegisterCustomRenderer(1, (void* userData, Clay_BoundingBox bb, GraphicsDevice gd, SpriteBatch sb) =>
        {
            var rect = new Rectangle((int)MathF.Round(bb.x), (int)MathF.Round(bb.y), (int)MathF.Round(bb.width), (int)MathF.Round(bb.height));
            sb.Draw(_texturePrimitive, rect, Color.Magenta * 0.6f);

            CustomRenderData data = *(CustomRenderData*)userData;

            var font = MonoGameClay.Fonts[0];
            if (font != null)
            {
                string text = $"CUSTOM RENDER: " + data.text;
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

        CustomRenderData userData = new CustomRenderData { text = "Hello, World!!!!" };

        using (Clay.Element(new Clay_ElementDeclaration
        {
            layout = new Clay_LayoutConfig
            {
                sizing = new Clay_Sizing(Clay_SizingAxis.Fixed(400), Clay_SizingAxis.Fixed(200)),
                childAlignment = new Clay_ChildAlignment(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
            },
            custom = new Clay_CustomElementConfig { customData = CustomElementData.SetData(1) },
            backgroundColor = new Clay_Color(100, 100, 20, 50),
            userData = (void*)&userData
        }))
        {
            
        }

        var commands = Clay.EndLayout();

        MonoGameClay.RenderCommands(commands, GraphicsDevice, _spriteBatch, _texturePrimitive, _scissorStack,_customRenderers);

        base.Draw(gameTime);
    }

    public void Dispose()
    {
        _customRenderers.Dispose();
        _clayString.Dispose();
        _spriteBatch?.Dispose();
        _texturePrimitive?.Dispose();
        _arena.Dispose();
    }
}
