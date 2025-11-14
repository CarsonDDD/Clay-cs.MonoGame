using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Clay_cs.MonoGame;

public class MonoGameClay
{
    public static SpriteFont[] Fonts = new SpriteFont[10];// Arbitrary 10 font size limit for demo purposes
    public static Texture2D _whitePixel;// Needed for drawing per pixel. Todo: find a better way

    static readonly RasterizerState RsScissorOff = new RasterizerState { ScissorTestEnable = false };
    static readonly RasterizerState RsScissorOn = new RasterizerState { ScissorTestEnable = true };
    struct ScissorFrame { public Rectangle Rect; public bool Enabled; }
    static readonly Stack<ScissorFrame> _scissorStack = new();

    private static Color ToColor(Clay_Color c) => new Color(
        (byte)MathF.Round(c.r),
        (byte)MathF.Round(c.g),
        (byte)MathF.Round(c.b),
        (byte)MathF.Round(c.a)
    );

    public static unsafe Clay_Dimensions MeasureText(Clay_StringSlice slice, Clay_TextElementConfig* config, void* userData)
    {
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


    public static unsafe void RenderCommands(Clay_RenderCommandArray array, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RsScissorOff);

        Rectangle viewportRect = graphicsDevice.Viewport.Bounds;

        for (int i = 0; i < array.length; i++)
        {
            var renderCommand = Clay.RenderCommandArrayGet(array, i);
            Clay_BoundingBox boundingBox = renderCommand->boundingBox;

            switch (renderCommand->commandType)
            {
                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
                {

                    spriteBatch.Draw(_whitePixel,
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

                    spriteBatch.Draw(
                        *(Texture2D*)image.imageData,
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

                    if(tw > 0) spriteBatch.Draw(_whitePixel, new Rectangle(x + tl, y, Math.Max(0, w - tl - tr), tw), color);

                    if(bw > 0) spriteBatch.Draw(_whitePixel, new Rectangle(x + bl, y + h - bw, Math.Max(0, w - bl - br), bw), color);

                    if(lw > 0) spriteBatch.Draw(_whitePixel, new Rectangle(x, y + tl, lw, Math.Max(0, h - tl - bl)), color);

                    if(rw > 0) spriteBatch.Draw(_whitePixel, new Rectangle(x + w - rw, y + tr, rw, Math.Max(0, h - tr - br)), color);

                    // todo: corner rendering
                    break;
                }
                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
                {
                    spriteBatch.End();
                    var rect = new Rectangle((int)MathF.Round(boundingBox.x),(int)MathF.Round(boundingBox.y),(int)MathF.Round(boundingBox.width),(int)MathF.Round(boundingBox.height));

                    rect = Rectangle.Intersect(rect, viewportRect);

                    _scissorStack.Push(new ScissorFrame
                    {
                        Rect = graphicsDevice.ScissorRectangle,
                        Enabled = graphicsDevice.RasterizerState.ScissorTestEnable
                    });

                    graphicsDevice.ScissorRectangle = rect;
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,DepthStencilState.None, RsScissorOn);
                    break;
                }

                case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
                {
                    spriteBatch.End();

                    ScissorFrame prev = _scissorStack.Count > 0? _scissorStack.Pop(): default;

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

        // Special case 1: spacing
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
}

