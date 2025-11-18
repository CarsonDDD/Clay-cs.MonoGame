using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Clay_cs.MonoGame.Examples;

public unsafe class PongWithHud : Game, IDisposable
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ClayArenaHandle _arena;
    private ClayStringCollection _clayStrings = new ClayStringCollection();
    private Texture2D _texturePrimitive;
    Stack<ScissorFrame> _scissorStack = new Stack<ScissorFrame>();

    // Game state
    private Texture2D _ballTexture;
    private Vector2 _ballPos;
    private Vector2 _ballVel;
    private int _ballSize = 48;

    private Rectangle _leftPaddle;
    private Rectangle _rightPaddle;
    private int _leftScore;
    private int _rightScore;
    private bool _paused;


    public PongWithHud()
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

        // white pixel
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

        // # Actual Content Loading

        _ballTexture = Content.Load<Texture2D>("image");

        ResetGame();
    }

    protected override void Update(GameTime gameTime)
    {
        if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // # Boilerplate
        // Feed mouse into clay
        var mouse = Mouse.GetState();
        Clay.SetPointerState(new System.Numerics.Vector2(mouse.X, mouse.Y), mouse.LeftButton == ButtonState.Pressed);
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        // scroll not needed

        // # Game
        var kb = Keyboard.GetState();

        if(!_paused)
        {
            // Players
            if(kb.IsKeyDown(Keys.W)) _leftPaddle.Y -= (int)(400f * dt);
            if(kb.IsKeyDown(Keys.S)) _leftPaddle.Y += (int)(400f * dt);
            if(kb.IsKeyDown(Keys.Up)) _rightPaddle.Y -= (int)(400f * dt);
            if(kb.IsKeyDown(Keys.Down)) _rightPaddle.Y += (int)(400f * dt);

            _leftPaddle.Y = Math.Clamp(_leftPaddle.Y, 0, GraphicsDevice.Viewport.Height - _leftPaddle.Height);
            _rightPaddle.Y = Math.Clamp(_rightPaddle.Y, 0, GraphicsDevice.Viewport.Height - _rightPaddle.Height);

            // Ball
            _ballPos += _ballVel * dt;

            // Collisions
            if(_ballPos.Y - _ballSize/2 <= 0 || _ballPos.Y + _ballSize / 2 >= GraphicsDevice.Viewport.Height)
            {
                _ballVel.Y = -_ballVel.Y;
            }

            var ballRect = new Rectangle((int)_ballPos.X - _ballSize/2, (int)_ballPos.Y - _ballSize / 2, _ballSize, _ballSize);
            if(ballRect.Intersects(_leftPaddle))
            {
                _ballVel.X = Math.Abs(_ballVel.X);
            }
            if(ballRect.Intersects(_rightPaddle))
            {
                _ballVel.X = -Math.Abs(_ballVel.X);
            }

            // scoring
            if(_ballPos.X < 0)
            {
                _rightScore++;
                _ballPos = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
                _ballVel = new Vector2(200f, 120f);
            }
            else if(_ballPos.X > GraphicsDevice.Viewport.Width)
            {
                _leftScore++;
                _ballPos = new Vector2(GraphicsDevice.Viewport.Width / 2f, GraphicsDevice.Viewport.Height / 2f);
                _ballVel = new Vector2(-200f, 120f);
            }
        }


        base.Update(gameTime);
    }

    private void RenderTopBar()
    {
        using(Clay.Element(new Clay_ElementDeclaration
        {
            layout = new Clay_LayoutConfig
            {
                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Fixed(56)),
                padding = Clay_Padding.HorVer(8, 8),
                childGap = 8,
                childAlignment = new Clay_ChildAlignment(default, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
            },
            
            // Needs to be transparent. TODO: somehow have the game itself be ofset or like a UI component?
            backgroundColor = new Clay_Color(40, 40, 40, 140),
            cornerRadius = Clay_CornerRadius.All(6),

            floating = new Clay_FloatingElementConfig
            {
                expand = new Clay_Dimensions(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
                zIndex = 1,
            }
        }))
        {
            using(var btn = Clay.OpenElement())
            {
                btn.Configure(new Clay_ElementDeclaration
                {
                    layout = new Clay_LayoutConfig { padding = Clay_Padding.All(8) },
                    backgroundColor = new Clay_Color(120, 120, 120, 200),
                    cornerRadius = Clay_CornerRadius.All(4)
                });

                Clay.TextElement(_clayStrings.Get("Restart"), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(255, 255, 255) });
                Clay.OnHover((_, data, _) =>
                {
                    if(data.state == Clay_PointerDataInteractionState.CLAY_POINTER_DATA_PRESSED_THIS_FRAME)
                    {
                        ResetGame();
                    }
                });
            }

            using(Clay.Element(new Clay_ElementDeclaration
            {
                layout = new Clay_LayoutConfig { padding = Clay_Padding.All(8) },
                backgroundColor = new Clay_Color(120, 120, 120, 200),
                cornerRadius = Clay_CornerRadius.All(4)
            }))
            {
                Clay.TextElement(_clayStrings.Get("Pause"), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(255, 255, 255) });
                Clay.OnHover((_, data, _) =>
                {
                    if(data.state == Clay_PointerDataInteractionState.CLAY_POINTER_DATA_PRESSED_THIS_FRAME)
                    {
                        _paused = !_paused;
                    }
                });
            }

            // spacer
            using(Clay.Element(new Clay_ElementDeclaration { layout = new Clay_LayoutConfig { sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), default) } })) { }

            // Padding makes this center
            Clay.TextElement(_clayStrings.Get("Score:"), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(220, 220, 220) });
            Clay.TextElement(_clayStrings.Get(_leftScore.ToString()), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(255, 255, 255) });
            Clay.TextElement(_clayStrings.Get("-"), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(200, 200, 200) });
            Clay.TextElement(_clayStrings.Get(_rightScore.ToString()), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(255, 255, 255) });

            // spacer
            using(Clay.Element(new Clay_ElementDeclaration { layout = new Clay_LayoutConfig { sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), default) } })) { }

            Clay.TextElement(_clayStrings.Get("W/S: Left | Up/Down: Right"), new Clay_TextElementConfig { fontId = 0, fontSize = 1, textColor = new Clay_Color(180, 180, 180) });
        }
    }

    private void RenderPauseOverlay()
    {
        // Overlay + center text
        using(Clay.Element(new Clay_ElementDeclaration
        {
            layout = new Clay_LayoutConfig
            {
                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                childAlignment = new Clay_ChildAlignment(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
            },
            backgroundColor = new Clay_Color(0, 0, 0, 160),
            floating = new Clay_FloatingElementConfig
            {
                //expand = new Clay_Dimensions(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
                zIndex = 1000,
            }
        }))
        {
            string pauseText = "||";
            Clay.TextElement(pauseText, new Clay_TextElementConfig 
            { 
                fontId = 0, 
                fontSize = 8, 
                textColor = new Clay_Color(255, 255, 255),
                //textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,// This is for the chars, the text BOX is centered from parent
            });

            Clay.OnHover((_, data, _) =>
            {
                if(data.state == Clay_PointerDataInteractionState.CLAY_POINTER_DATA_PRESSED_THIS_FRAME)
                {
                    _paused = !_paused;
                }
            });

        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // # Gameplay
        _spriteBatch.Begin();

        _spriteBatch.Draw(_texturePrimitive, _leftPaddle, Color.White);
        _spriteBatch.Draw(_texturePrimitive, _rightPaddle, Color.White);

        _spriteBatch.Draw(_ballTexture, new Rectangle((int)_ballPos.X - _ballSize / 2, (int)_ballPos.Y - _ballSize / 2, _ballSize, _ballSize), Color.Yellow);

        _spriteBatch.End();


        // # UI after
        Clay.SetLayoutDimensions(new Clay_Dimensions(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));

        // main
        Clay.BeginLayout();
        RenderTopBar();
        MonoGameClay.RenderCommands(Clay.EndLayout(), GraphicsDevice, _spriteBatch, _texturePrimitive, _scissorStack);

        // This ui renders on a different layer. Note: the overlay blocks interaction with the main HUD.
        if(_paused)
        {
            // Seperate layout so it renders ontop instead of APART of the existing
            Clay.BeginLayout();
            RenderPauseOverlay();
            MonoGameClay.RenderCommands(Clay.EndLayout(), GraphicsDevice, _spriteBatch, _texturePrimitive, _scissorStack);
        }

        // To solve the issue of the send layout blocking the button, we could do another layer for the button?
        // Unity has UI layers, but the better solution is probably have floating clickthrough to work?
        // No. this is just a limitation to not handle this edge case


        base.Draw(gameTime);
    }

    private void ResetGame()
    {
        int w = GraphicsDevice.Viewport.Width;
        int h = GraphicsDevice.Viewport.Height;

        _ballPos = new Vector2(w / 2f, h / 2f);
        _ballVel = new Vector2(200f, 120f);

        int paddleW = 16;
        int paddleH = 120;
        _leftPaddle = new Rectangle(40, h / 2 - paddleH / 2, paddleW, paddleH);
        _rightPaddle = new Rectangle(w - 40 - paddleW, h / 2 - paddleH / 2, paddleW, paddleH);

        _leftScore = 0;
        _rightScore = 0;
        _paused = false;
    }

    public void Dispose()
    {
        _clayStrings.Dispose();
        _spriteBatch?.Dispose();
        _texturePrimitive.Dispose();
        _arena.Dispose();
        _scissorStack.Clear();
    }
}