using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Controls
{
    public class TextBox
    {
        public string CurrentText { get; set; }
        public Vector2 CurrentTextPos { get; set; }
        public Vector2 CursorPos { get; set; }
        public int AnimationTime { get; set; }
        public bool Visible { get; set; }
        public float LayerDepth { get; set; }
        public Vector2 Position { get; set; }
        public bool inFocus { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        private int cursorWidth;
        private int cursorHeight;
        private int length;
        private bool numericOnly;
        private Texture2D texture;
        private Texture2D cursorTexture;
        private Point cursorDim;
        private SpriteFont font;
    }
}
