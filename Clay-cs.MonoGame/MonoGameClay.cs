using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace Clay_cs.MonoGame;

public struct ScissorFrame { public Rectangle Rect; public bool Enabled; }

public class MonoGameClay
{
    public static SpriteFont[] Fonts = new SpriteFont[10]; // I think may always need to be managed here since MeasureText needs it?!?!

    // Very minimal overhead. It would be too ugly to force the user to define and pass these
    private static readonly RasterizerState RsScissorOff = new RasterizerState { ScissorTestEnable = false };
    private static readonly RasterizerState RsScissorOn = new RasterizerState { ScissorTestEnable = true };

    private static Color ToColor(Clay_Color c) => new Color(
       (byte)MathF.Round(c.r),
        (byte)MathF.Round(c.g),
        (byte)MathF.Round(c.b),
        (byte)MathF.Round(c.a)
    );

    public static unsafe Clay_Dimensions MeasureText(Clay_StringSlice slice, Clay_TextElementConfig* config, void* userData)
    {
        //Obviosuly, we store the fonts in userData (to remove the management here), but where is userData set?
        if(config->fontId >= Fonts.Length) return default;
        var font = Fonts[config->fontId];
        if(font == null) return default;

        float scale = config->fontSize;

        // Loop through string tracking newlines
        // measure each line and add that to the total
        float maxWidth = 0f;
        int lines = 0;
        int start = 0;
        for(int i = 0; i <= slice.length; i++)
        {
            bool atEnd = (i == slice.length);
            char c = atEnd ? '\n' : (char)slice.chars[i];// always end with newline
            bool isEol = (c == '\n');

            if(isEol || atEnd)
            {
                int lineLength = i - start;
                if(lineLength > 0)
                {
                    string line = new string(slice.chars, start, lineLength);
                    var size = font.MeasureString(line);
                    if(size.X > maxWidth) maxWidth = size.X;
                    lines++;
                }
                else // empty line, but its empty because its ONLY a newline, thus we still add.
                {
                    lines++;
                }
                start = i + 1;
            }
        }

        return new Clay_Dimensions
        {
            width = maxWidth * scale,
            height = font.LineSpacing * scale * (lines > 0 ? lines : 1)
        };
    }

    public static unsafe void RenderCommands(Clay_RenderCommandArray array, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch,
        Texture2D whitePixel, // needed for rectangle/primative drawing
        Stack<ScissorFrame> scissorStack, // needed to remove state from this class
        CustomRenderCommandCollection? customRenders = null // optional
        )
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RsScissorOff);

        Rectangle viewportRect = graphicsDevice.Viewport.Bounds;

        scissorStack.Clear();

        for(int i = 0; i < array.length; i++)
        {
            var renderCommand = Clay.RenderCommandArrayGet(array, i);
            Clay_BoundingBox boundingBox = renderCommand->boundingBox;

            switch(renderCommand->commandType)
            {
                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
                {
                    spriteBatch.Draw(whitePixel,
                        new Rectangle((int)MathF.Round(boundingBox.x), (int)MathF.Round(boundingBox.y), (int)MathF.Round(boundingBox.width), (int)MathF.Round(boundingBox.height)),
                        ToColor(renderCommand->renderData.rectangle.backgroundColor)
                    );
                    break;
                }

                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
                {
                    var text = renderCommand->renderData.text;
                    var font = Fonts[text.fontId] ?? throw new ArgumentNullException("Font cannot be null");

                    string full = text.stringContents.ToCSharpString();
                    using var spanOwner = text.stringContents.ToSpanOwner();

                    float scale = text.fontSize;
                    float letterSpacing = text.letterSpacing * scale;
                    float x0 = boundingBox.x;
                    float y = boundingBox.y;

                    int lineStart = 0;
                    for(int idx = 0; idx <= full.Length; idx++)
                    {
                        bool atEnd = (idx == full.Length);
                        char ch = atEnd ? '\n' : full[idx];
                        if(ch == '\n')
                        {
                            DrawString(font, full.AsSpan(lineStart, idx - lineStart), x0, y, scale, letterSpacing, text.textColor, spriteBatch);
                            y += font.LineSpacing * scale;
                            lineStart = idx + 1;
                        }
                    }
                    if(lineStart < full.Length)
                    {
                        DrawString(font, full.AsSpan(lineStart), x0, y, scale, letterSpacing, text.textColor, spriteBatch);
                    }
                    break;
                }

                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_IMAGE:
                {
                    var image = renderCommand->renderData.image;

                    var tint = image.backgroundColor;
                    bool zeroTint = (tint.r == 0 && tint.g == 0 && tint.b == 0 && tint.a == 0);
                    var color = zeroTint ? Color.White : ToColor(tint);

                    Texture2D? texture = null;
                    if(image.imageData != null)
                    {
                        //texture = *(Texture2D*)image.imageData;
                        var handle = GCHandle.FromIntPtr((IntPtr)image.imageData);
                        texture = handle.Target as Texture2D;
                    }

                    if(texture == null) throw new ArgumentNullException("Texture cannot be null");

                    spriteBatch.Draw(
                             texture,
                             new Rectangle((int)MathF.Round(boundingBox.x), (int)MathF.Round(boundingBox.y), (int)MathF.Round(boundingBox.width), (int)MathF.Round(boundingBox.height)),
                            color
                        );

                    break;
                }

                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER:
                {
                    var b = renderCommand->renderData.border;

                    int x = (int)MathF.Round(boundingBox.x);
                    int y = (int)MathF.Round(boundingBox.y);
                    int w = (int)MathF.Round(boundingBox.width);
                    int h = (int)MathF.Round(boundingBox.height);

                    int lw = b.width.left;
                    int rw = b.width.right;
                    int tw = b.width.top;
                    int bw = b.width.bottom;

                    // Strange that these need to be floats
                    int tl = (int)MathF.Round(b.cornerRadius.topLeft);
                    int tr = (int)MathF.Round(b.cornerRadius.topRight);
                    int bl = (int)MathF.Round(b.cornerRadius.bottomLeft);
                    int br = (int)MathF.Round(b.cornerRadius.bottomRight);

                    var color = ToColor(b.color);

                    if(tw > 0) spriteBatch.Draw(whitePixel, new Rectangle(x + tl, y, Math.Max(0, w - tl - tr), tw), color);

                    if(bw > 0) spriteBatch.Draw(whitePixel, new Rectangle(x + bl, y + h - bw, Math.Max(0, w - bl - br), bw), color);

                    if(lw > 0) spriteBatch.Draw(whitePixel, new Rectangle(x, y + tl, lw, Math.Max(0, h - tl - bl)), color);

                    if(rw > 0) spriteBatch.Draw(whitePixel, new Rectangle(x + w - rw, y + tr, rw, Math.Max(0, h - tr - br)), color);

                    // todo: corner rendering
                    break;
                }
                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
                {
                    spriteBatch.End();
                    var rect = new Rectangle((int)MathF.Round(boundingBox.x), (int)MathF.Round(boundingBox.y), (int)MathF.Round(boundingBox.width), (int)MathF.Round(boundingBox.height));

                    rect = Rectangle.Intersect(rect, viewportRect);

                    scissorStack.Push(new ScissorFrame
                    {
                        Rect = graphicsDevice.ScissorRectangle,
                        Enabled = graphicsDevice.RasterizerState.ScissorTestEnable
                    });

                    graphicsDevice.ScissorRectangle = rect;
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RsScissorOn);
                    break;
                }

                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
                {
                    spriteBatch.End();

                    ScissorFrame prev = scissorStack.Count > 0 ? scissorStack.Pop() : default;

                    if(prev.Enabled)
                    {
                        graphicsDevice.ScissorRectangle = Rectangle.Intersect(prev.Rect, viewportRect);
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RsScissorOn);
                    }
                    else
                    {
                        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RsScissorOff);
                    }
                    break;
                }

                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_CUSTOM:
                {
                    try
                    {
                        if(customRenders == null) throw new ArgumentNullException("Custom renderers is null, while you are trying to use it");


                        void* customData = renderCommand->renderData.custom.customData;
                        if(customData == null) throw new ArgumentNullException("Custom render data pointer cannot be null");

                        CustomElementData.DecodeData(customData, out var id, out var flags);
                        //bool preserveAspectRatio = (flags & CustomRendererToken.CustomRenderFlags.PreserveAspect) != 0;

                        customRenders.TryGetValue(id, out CustomRenderCommandCollection.CustomRenderDelegate? handler);

                        if(handler == null) throw new ArgumentNullException($"No custom renderer registered for id {id}\n");

                        var renderData = renderCommand->renderData;

                        // todo: make use of these
                        //renderData.clip;
                        //renderData.rectangle
                        //renderData.border
                        //renderData.image
                        //renderData.text;
                        //renderData.custom.cornerRadius
                        //renderData.custom.backgroundColor
                        //renderCommand->boundingBox;
                        //renderCommand->zIndex
                        // border
                        // clip
                        // cornerRadius
                        // floating
                        // userData

                        // renderData.custom.backgroundColor ???????
                        /*if(renderData.rectangle.backgroundColor.a > 0)
                        {
                            spriteBatch.Draw(whitePixel,
                                new Rectangle((int)MathF.Round(boundingBox.x), (int)MathF.Round(boundingBox.y), (int)MathF.Round(boundingBox.width), (int)MathF.Round(boundingBox.height)),
                                ToColor(renderData.rectangle.backgroundColor)
                            );
                        }*/

                        // Image just works out of the box? but not background?
                        
                        // Ok, so now the background color is automatically drawn?!?!?!?!?!
                        // This is strange, because also aspect ratio is properly handled---while the only thing I changed was passing in an int instead of a struct???

                        //bool ratio = renderCommand->renderData.
                        handler(renderCommand->userData, boundingBox, graphicsDevice, spriteBatch);

                    }
                    catch(InvalidCastException ex)
                    {
                        throw new Exception("Error during custom render: Cannot cast void* renderCommand->renderData.custom.customData to a CustomRenderer\nNote: this is not hard inforced, but is supposed to be this struct which contains the Id to the renderer and any relevant information the renderer will need. This is DIFFERENT from renderCommand->userData, userData is whatever", ex);
                    }
                    catch(Exception ex)
                    {
                        throw new Exception("Error during custom render delegate execution", ex);
                    }

                    break;
                }
                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_NONE:
                break;

                default:
                throw new ArgumentOutOfRangeException();
            }
        }

        spriteBatch.End();
    }

    // function for string fx
    private static void DrawString(SpriteFont font, ReadOnlySpan<char> run, float x, float y, float scale, float letterSpacing, Clay_Color clayColor, SpriteBatch sb)
    {
        if(run.Length == 0) return;

        var color = ToColor(clayColor);

        // Basic case
        if(letterSpacing == 0f)
        {
            sb.DrawString(font, run.ToString(), new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            return;
        }

        // Special case1: spacing
        float charX = x;
        for(int i = 0; i < run.Length; i++)
        {
            string s = run[i].ToString();
            sb.DrawString(font, s, new Vector2(charX, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            float adv = font.MeasureString(s).X * scale + letterSpacing;
            charX += adv;
        }

        // Special case...
    }


    public static (Rectangle bounds, Vector2 scale) CalculateGameWindowSizeScale(int designedWidth, int designedHeight, Clay_BoundingBox area)
    {
        float widthRatio = area.width / designedWidth;
        float heightRatio = area.height / designedHeight;

        float x0 = area.x;
        float y0 = area.y;

        float scaleX = widthRatio;
        float scaleY = heightRatio;

        // Clay does his directly?...
       /* if(maintainAspectRatio)
        {
            float scale = MathF.Min(widthRatio, heightRatio);
            scaleX = scale;
            scaleY = scale;

            // center in area
            x0 += (area.width - (designedWidth * scale)) / 2f;
            y0 += (area.height - (designedHeight * scale)) / 2f;
        }*/

        return (new Rectangle((int)MathF.Round(x0), (int)MathF.Round(y0), (int)MathF.Round(designedWidth * scaleX), (int)MathF.Round(designedHeight * scaleY)), new Vector2(scaleX, scaleY));
    }
}