using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Clay_cs.MonoGame.Examples;

public class QuickStart : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // clay boilerplate
    private ClayArenaHandle _arena;
    //private ClayStringCollection _clayStrings = new ClayStringCollection();
    //private ClayTexture2DCollection _clayTextures = new ClayTexture2DCollection();
    //private CustomRenderRegister _customRenderers = new CustomRenderRegister();
    private Texture2D _texturePrimitive;
    Stack<ScissorFrame> _scissorStack = new Stack<ScissorFrame>();
    private int _prevWheel;// Needed for mouse wheel delta calculation

    public QuickStart()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
        _graphics.ApplyChanges();
        IsMouseVisible = true;
        base.Initialize();
    }

    protected override unsafe void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // white pixel needed for rendering
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
    }

    protected override void Update(GameTime gameTime)
    {
        if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // # Boilerplate Feed mouse into clay
        var mouse = Mouse.GetState();
        int wheelDelta = mouse.ScrollWheelValue - _prevWheel;
        _prevWheel = mouse.ScrollWheelValue;
        Clay.SetPointerState(new System.Numerics.Vector2(mouse.X, mouse.Y), mouse.LeftButton == ButtonState.Pressed);
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Clay.UpdateScrollContainers(true, new System.Numerics.Vector2(0, wheelDelta / 60f), dt);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        Clay.SetLayoutDimensions(new Clay_Dimensions(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

        Clay.BeginLayout();
        using(Clay.Element(new()
        {
            backgroundColor = new Clay_Color(255, 0, 0),
            layout = new()
            {
                sizing = new Clay_Sizing(Clay_SizingAxis.Fixed(200), Clay_SizingAxis.Fixed(200))
            }
        }));
        var commands = Clay.EndLayout();
        MonoGameClay.RenderCommands(commands, GraphicsDevice, _spriteBatch, _texturePrimitive, _scissorStack);

        base.Draw(gameTime);
    }
}