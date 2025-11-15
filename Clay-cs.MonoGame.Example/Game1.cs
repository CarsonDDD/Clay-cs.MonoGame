using Clay_cs.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;

namespace Clay_cs.MonoGame.Example;

public class Game1 : Game, IDisposable
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ClayArenaHandle _arena;

    private int _prevWheel;// Needed for mouse wheel delta calculation

    // --- UI Specific code
    private struct Document { public Clay_String Title; public Clay_String Contents; }
    private Document[] _documents;
    private ClayStringCollection _clayString = new ClayStringCollection();
    private int _selectedDocumentIndex = 0;
    private Clay_Color _contentBackgroundColor = new Clay_Color(90, 90, 90);

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        // Standard MonoGame initialization
        _graphics.PreferredBackBufferWidth = 720;
        _graphics.PreferredBackBufferHeight = 480;
        _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        _graphics.IsFullScreen = false;
        _graphics.SynchronizeWithVerticalRetrace = false;
        _graphics.ApplyChanges();
        IsMouseVisible = true;

        base.Initialize();
    }

    protected override unsafe void LoadContent()// unsafe needed for Clay interop
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

        // Clay.SetDebugModeEnabled(true); // This feature doenst currently "work" (display sanely) because of the spritefont system :/


        // # Actual Content Loading

        // <3
        _documents =[
            new Document
            {
                Title = _clayString.Get("Squirrels"),
                Contents = _clayString.Get(
                    "The Secret Life of Squirrels: Nature's Clever Acrobats\nSquirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n\nMaster Tree Climbers\nAt the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\nBut it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n\nFood Hoarders Extraordinaire\nSquirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\nInterestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n\nThe Great Squirrel Debate: Urban vs. Wild\nWhile squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\nThere is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n\nA Symbol of Resilience\nIn many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\nIn the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.\n")
            },
            new Document
            {
                Title = _clayString.Get("Lorem Ipsum"),
                Contents = _clayString.Get(
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.")
            },
            new Document
            {
                Title = _clayString.Get("Vacuum Instructions"),
                Contents = _clayString.Get(
                    "Chapter 3: Getting Started - Unpacking and Setup\n\nCongratulations on your new SuperClean Pro 5000 vacuum cleaner! In this section, we will guide you through the simple steps to get your vacuum up and running. Before you begin, please ensure that you have all the components listed in the Package Contents\" section on page 2.\n\n1. Unboxing Your Vacuum\nCarefully remove the vacuum cleaner from the box. Avoid using sharp objects that could damage the product. Once removed, place the unit on a flat, stable surface to proceed with the setup. Inside the box, you should find:\n\n    The main vacuum unit\n    A telescoping extension wand\n    A set of specialized cleaning tools (crevice tool, upholstery brush, etc.)\n    A reusable dust bag (if applicable)\n    A power cord with a 3-prong plug\n    A set of quick-start instructions\n\n2. Assembling Your Vacuum\nBegin by attaching the extension wand to the main body of the vacuum cleaner. Line up the connectors and twist the wand into place until you hear a click. Next, select the desired cleaning tool and firmly attach it to the wand's end, ensuring it is securely locked in.\n\nFor models that require a dust bag, slide the bag into the compartment at the back of the vacuum, making sure it is properly aligned with the internal mechanism. If your vacuum uses a bagless system, ensure the dust container is correctly seated and locked in place before use.\n\n3. Powering On\nTo start the vacuum, plug the power cord into a grounded electrical outlet. Once plugged in, locate the power switch, usually positioned on the side of the handle or body of the unit, depending on your model. Press the switch to the \"On\" position, and you should hear the motor begin to hum. If the vacuum does not power on, check that the power cord is securely plugged in, and ensure there are no blockages in the power switch.\n\nNote: Before first use, ensure that the vacuum filter (if your model has one) is properly installed. If unsure, refer to \"Section 5: Maintenance\" for filter installation instructions.")
            },
            new Document { Title = _clayString.Get("Article 4"), Contents = _clayString.Get("Article 4") },
            new Document { Title = _clayString.Get("Article 5"), Contents = _clayString.Get("Article 5") },
        ];

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

        base.Update(gameTime);
    }

    protected override unsafe void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        Clay.SetLayoutDimensions(new Clay_Dimensions(
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height
        ));

        Clay.BeginLayout();

        using(Clay.Element(new Clay_ElementDeclaration
        {
            //id = Clay.Id(_clayString["OuterContainer"]),
            backgroundColor = new Clay_Color(43, 41, 51),
            layout = new Clay_LayoutConfig
            {
                layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                padding = Clay_Padding.All(16),
                childGap = 16
            }
        }))
        {
            // Header bar
            using(Clay.Element(new Clay_ElementDeclaration
            {
                //id = Clay.Id(_clayString["HeaderBar"]),
                backgroundColor = _contentBackgroundColor,
                cornerRadius = Clay_CornerRadius.All(8),
                layout = new Clay_LayoutConfig
                {
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Fixed(60)),
                    padding = Clay_Padding.Hor(16),
                    childGap = 16,
                    childAlignment = new Clay_ChildAlignment(default, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                }
            }))
            {
                var fileButtonStr = _clayString["FileButton"];
                var fileMenuStr = _clayString["FileMenu"];

                // File button
                using(Clay.Element(Clay.Id(fileButtonStr), new()
                {
                    layout = new()
                    {
                        padding = Clay_Padding.HorVer(16, 8)
                    },
                    backgroundColor = new Clay_Color(140, 140, 140),
                    cornerRadius = Clay_CornerRadius.All(5),
                }))
                {
                    Clay.TextElement("File", new Clay_TextElementConfig { 
                        fontId = 0, 
                        fontSize = 1, //Scalar value for SPRITE font
                        textColor = new Clay_Color(255, 255, 255) 
                    });

                    bool isMenuVisible = Clay.IsPointerOver(Clay.GetElementId(fileButtonStr)) || Clay.IsPointerOver(Clay.GetElementId(fileMenuStr));

                    if(isMenuVisible)
                    {
                        using(Clay.Element(Clay.Id(fileMenuStr), new()
                        {
                            floating = new()
                            {
                                attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
                                attachPoints = new Clay_FloatingAttachPoints
                                {
                                    parent = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_BOTTOM,
                                }
                            },
                            layout = new()
                            {
                                padding = Clay_Padding.Ver(8),
                            }
                        }))
                        {
                            using(Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                                    sizing = new Clay_Sizing(Clay_SizingAxis.Fixed(200), default),
                                },
                                backgroundColor = new Clay_Color(40, 40, 40),
                                cornerRadius = Clay_CornerRadius.All(8),
                            }))
                            {
                                RenderDropdownItem(_clayString["New"]);
                                RenderDropdownItem(_clayString["Open"]);
                                RenderDropdownItem(_clayString["Close"]);
                            }
                        }
                    }
                }

                RenderHeaderButton(_clayString["Edit"]);
                using(Clay.Element(new Clay_ElementDeclaration
                {
                    layout = new Clay_LayoutConfig { 
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()) 
                    }
                })) { }

                RenderHeaderButton(_clayString["Upload"]);
                RenderHeaderButton(_clayString["Media"]);
                RenderHeaderButton(_clayString["Support"]);
                RenderHeaderButton(_clayString["Upload"]);
            }

            // sidebar + main
            using(Clay.Element(new Clay_ElementDeclaration
            {
                //id = Clay.Id(_clayString["LowerContent"]),
                layout = new Clay_LayoutConfig
                {
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    childGap = 16
                }
            }))
            {
                // Sidebar
                using(Clay.Element(new Clay_ElementDeclaration
                {
                    //id = Clay.Id(_clayString["Sidebar"]),
                    backgroundColor = _contentBackgroundColor,
                    layout = new Clay_LayoutConfig
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        padding = Clay_Padding.All(16),
                        childGap = 8,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Fixed(250), Clay_SizingAxis.Grow())
                    }
                }))
                {
                    var sidebarButtonLayout = new Clay_LayoutConfig
                    {
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), default),
                        padding = Clay_Padding.All(8)
                    };

                    for(int documentIndex = 0; documentIndex < _documents.Length; documentIndex++)
                    {
                        var document = _documents[documentIndex];
                        if(documentIndex == _selectedDocumentIndex)
                        {
                            using(Clay.Element(new Clay_ElementDeclaration
                            {
                                layout = sidebarButtonLayout,
                                backgroundColor = new Clay_Color(120, 120, 120, 255),
                                cornerRadius = Clay_CornerRadius.All(8)
                            }))
                            {
                                Clay.TextElement(document.Title, new Clay_TextElementConfig 
                                { 
                                    fontId = 0, 
                                    fontSize = 1, 
                                    textColor = new Clay_Color(255, 255, 255) 
                                });
                            }
                        }
                        else
                        {
                            using(var sidebarButton = Clay.OpenElement())
                            {
                                sidebarButton.Configure(new Clay_ElementDeclaration
                                {
                                    layout = sidebarButtonLayout,
                                    backgroundColor = Clay.IsHovered() ? new Clay_Color(120, 120, 120, 255) : default,
                                    cornerRadius = Clay_CornerRadius.All(8)
                                });

                                int index = documentIndex;

                                // Commenting out the hover removes the ExecutionEngineException???
                                Clay.OnHover((_, data, _) =>
                                {
                                    if(data.state == Clay_PointerDataInteractionState.CLAY_POINTER_DATA_PRESSED_THIS_FRAME)
                                        _selectedDocumentIndex = index;
                                });

                                Clay.TextElement(document.Title, new Clay_TextElementConfig
                                { 
                                    fontId = 0, 
                                    fontSize = 1, 
                                    textColor = new Clay_Color(255, 255, 255) 
                                });
                            }
                        }
                    }
                }

                // main
                using(var content = Clay.OpenElement(Clay.Id(_clayString["MainContent"])))
                {
                    content.Configure(new()
                    {
                        clip = new()
                        {
                            vertical = true,
                            childOffset = Clay.GetScrollOffset(),
                        },
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            childGap = 16,
                            padding = Clay_Padding.All(16),
                            sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow())
                        },
                        backgroundColor = _contentBackgroundColor,
                    });

                    var doc = _documents[_selectedDocumentIndex];
                    Clay.TextElement(doc.Title, new()
                    {
                        fontSize = 1,
                        textColor = new Clay_Color(255, 255, 255),
                    });
                    Clay.TextElement(doc.Contents, new()
                    {
                        fontSize = 1,
                        textColor = new Clay_Color(255, 255, 255),
                    });
                }
            }
        }

        var commands = Clay.EndLayout();

        //_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        MonoGameClay.RenderCommands(commands, GraphicsDevice, _spriteBatch);
        //_spriteBatch.End();
        // begin/end now is called inside >:|

        base.Draw(gameTime);
    }


    private void RenderHeaderButton(Clay_String text)
    {
        using(Clay.Element(new Clay_ElementDeclaration
        {
            layout = new Clay_LayoutConfig { 
                padding = Clay_Padding.HorVer(16, 8) 
            },
            backgroundColor = new Clay_Color(140, 140, 140),
            cornerRadius = Clay_CornerRadius.All(5)
        }))
        {
            Clay.TextElement(text, new Clay_TextElementConfig 
            { 
                fontId = 0, 
                fontSize = 1, 
                textColor = new Clay_Color(255, 255, 255) 
            });
        }
    }

    private void RenderDropdownItem(Clay_String text)
    {
        using(Clay.Element(new Clay_ElementDeclaration
        {
            layout = new Clay_LayoutConfig 
            { 
                padding = Clay_Padding.All(16) 
            }
        }))
        {
            Clay.TextElement(text, new Clay_TextElementConfig 
            { 
                fontId = 0, 
                fontSize = 1, 
                textColor = new Clay_Color(255, 255, 255) 
            });
        }
    }

    public void Dispose()
    {
        _clayString.Dispose();
    }
}
