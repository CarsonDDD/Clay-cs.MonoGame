using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;

namespace Clay_cs.MonoGame.Examples;


/*
 todo:
- The image scaling over the window size should: clip itself and continue, and not push the other elements
 
 */

public unsafe class ImageViewer : Game, IDisposable
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ClayArenaHandle _arena;
    private int _prevWheel;// Needed for mouse wheel delta calculation
    private ClayStringCollection _clayString = new ClayStringCollection();
    private ClayTexture2DCollection _clayTexture = new ClayTexture2DCollection();


    // --- UI Specific code
    private Texture2D _image;
    private float _zoom = 1f;

    public ImageViewer()
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

        // # boilerplate
        // Create a white pixel texture for drawing rectangles
        MonoGameClay._whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        MonoGameClay._whitePixel.SetData(new[] { Color.White });
        MonoGameClay.Fonts[0] = Content.Load<SpriteFont>("myfont");// DEMO FONT, you may need to add your own font to get it running

        uint requiredSize = Clay.MinMemorySize();
        _arena = Clay.CreateArena(requiredSize);
        Clay.Initialize(_arena, new Clay_Dimensions(
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height),
            data => Debug.WriteLine($"{data.errorType}: {data.errorText.ToCSharpString()}")
        );

        Clay.SetMeasureTextFunction(MonoGameClay.MeasureText);

        // # Actual Content Loading

        _image = Content.Load<Texture2D>("image");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();


        // Feed mouse/input data to Clay
        var mouse = Mouse.GetState();
        int wheelDelta = mouse.ScrollWheelValue - _prevWheel;
        _prevWheel = mouse.ScrollWheelValue;

        Clay.SetPointerState(new System.Numerics.Vector2(mouse.X, mouse.Y), mouse.LeftButton == ButtonState.Pressed);

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Clay.UpdateScrollContainers(true, new System.Numerics.Vector2(0, wheelDelta / 60f), dt);
        Clay.GetScrollOffset();

        base.Update(gameTime);
    }

    private void RenderControlButton(Clay_String text, Action onClick)
    {
        using (var btn = Clay.OpenElement())
        {

            btn.Configure(new Clay_ElementDeclaration
            {
                layout = new Clay_LayoutConfig { padding = Clay_Padding.All(8) },
                backgroundColor = new Clay_Color(120, 120, 120),
                cornerRadius = Clay_CornerRadius.All(4)
            });

            Clay.TextElement(text, new Clay_TextElementConfig
            {
                fontId = 0,
                fontSize = 1,
                textColor = new Clay_Color(255, 255, 255)
            });

            Clay.OnHover((_, data, _) =>
            {
                if (data.state == Clay_PointerDataInteractionState.CLAY_POINTER_DATA_PRESSED_THIS_FRAME)
                {
                    onClick();
                }
            });

        }
    }

    protected override unsafe void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        Clay.SetLayoutDimensions(new Clay_Dimensions(
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height
        ));


        Clay.BeginLayout();

        // Root
        using (Clay.Element(new Clay_ElementDeclaration
        {
            backgroundColor = new Clay_Color(30, 30, 30),
            layout = new Clay_LayoutConfig
            {
                layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                padding = Clay_Padding.All(12),
                childGap = 12
            }
        }))
        {
            // TOP BAR
            using (Clay.Element(new Clay_ElementDeclaration
            {
                layout = new Clay_LayoutConfig
                {
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Fixed(48)),
                    childAlignment = new Clay_ChildAlignment(default, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER),
                    padding = Clay_Padding.HorVer(8, 6),
                    childGap = 8
                },
                backgroundColor = new Clay_Color(60, 60, 60),
                cornerRadius = Clay_CornerRadius.All(6)
            }))
            {
                RenderControlButton(_clayString.Get("-"), () =>
                {
                    _zoom = Math.Max(0.01f, _zoom * 0.9f);
                });

                RenderControlButton(_clayString.Get("+"), () =>
                {
                    _zoom = Math.Min(2f, _zoom * 1.1f);
                });

                RenderControlButton(_clayString.Get("Fit"), () =>
                {

                });


                Clay.TextElement(_clayString.Get("    |    "), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(220, 220, 220) });
                
                Clay.TextElement(_clayString.Get("Zoom:"), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(220, 220, 220) });
                Clay.TextElement(_clayString.Get(((int)(_zoom * 100)).ToString() + "%"), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(255, 255, 255) });
            }

            // main image area
            using (Clay.Element(new Clay_ElementDeclaration
            {
                layout = new Clay_LayoutConfig
                {
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    padding = Clay_Padding.All(8),
                },
                backgroundColor = new Clay_Color(20, 20, 20),
                cornerRadius = Clay_CornerRadius.All(4)
            }))
            {
                using(var imgElem = Clay.Element(new Clay_ElementDeclaration
                {
                    layout = new Clay_LayoutConfig
                    {
                        sizing = new Clay_Sizing(Clay_SizingAxis.Fixed(_image.Width * _zoom), Clay_SizingAxis.Fixed(_image.Height * _zoom))
                    },
                    image = _clayTexture.Get(_image)
                })) { }
            }
        }

        var commands = Clay.EndLayout();

        MonoGameClay.RenderCommands(commands, GraphicsDevice, _spriteBatch);

        base.Draw(gameTime);
    }

    public void Dispose()
    {
        _clayString.Dispose();
        _clayTexture.Dispose();
    }
}
