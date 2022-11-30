using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Client.Controls
{
    public class TextBox
    {
        public string CurrentText { get; set; }
        public Vector2 CurrentTextPosition { get; set; }
        public Vector2 CursorPosition { get; set; }
        public int AnimationTime { get; set; }
        public bool Visible { get; set; }
        public float LayerDepth { get; set; }
        public Vector2 Position { get; set; }
        public bool Selected { get; set; }
        public int CellWidth { get; set; }
        public int CellHeight { get; set; }

        private int _cursorWidth;
        private int _cursorHeight;
        private int _length;
        private bool _numericOnly;
        private Texture2D _texture;
        private Texture2D _cursorTexture;
        private Point _cursorDimensions;
        private SpriteFont _font;
        public TextBox(Texture2D texture, Texture2D cursorTexture, Point dimensions, Point cursorDimensions, Vector2 position, int length, bool numericOnly, bool visible, SpriteFont font, string text, float layerDepth)
        {
            _texture = texture;
            CellWidth = dimensions.X;
            CellHeight = dimensions.Y;
            _cursorWidth = cursorDimensions.X;
            _cursorHeight = cursorDimensions.Y;
            _length = length;
            _numericOnly = numericOnly;
            AnimationTime = 0;
            Visible = visible;
            LayerDepth = layerDepth;
            Position = position;
            CursorPosition = new Vector2(position.X + 7, position.Y + 6);
            CurrentTextPosition = new Vector2(position.X + 7, position.Y + 3);
            CurrentText = string.Empty;
            _cursorTexture = cursorTexture;
            _cursorDimensions = cursorDimensions;
            Selected = false;
            _font = font;
            CurrentText = text;
        }

        public void Update() => AnimationTime++;

        public bool IsFlashingCursorVisible()
        {
            int time = AnimationTime % 60;
            if (time >= 0 && time < 31)
                return true;
            else
                return false;
        }

        public void AddMoreText(char text)
        {
            Vector2 spacing = new Vector2();
            KeyboardState keyboardState = OneShotKeyboard.GetState();
            bool lowerThisCharacter = true;

            if (keyboardState.CapsLock || keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                lowerThisCharacter = false;
            if (_numericOnly && (int)Char.GetNumericValue(text) < 0 || (int)Char.GetNumericValue(text) > 9) // Don't allow non-numeric characters if this textbox is numeric only
            {
                if (text != '\b')
                    return;
            }

            if (text != '\b')
            {
                if (CurrentText.Length < _length)
                {
                    if (lowerThisCharacter)
                        text = Char.ToLower(text);

                    CurrentText += text;
                    spacing = _font.MeasureString(text.ToString());
                    CursorPosition = new Vector2(CursorPosition.X + spacing.X, CursorPosition.Y);
                }
            }

            else // If it's a backspace or delete character
            {
                if (CurrentText.Length > 0)
                {
                    spacing = _font.MeasureString(CurrentText.Substring(CurrentText.Length - 1));

                    CurrentText = CurrentText.Remove(CurrentText.Length - 1, 1); // A backspace removes the last character from the string and moves the cursor back
                    CursorPosition = new Vector2(CursorPosition.X - spacing.X, CursorPosition.Y);
                }
            }
        }
        public void Render(SpriteBatch spriteBatch)
        {
            if (Visible)
            {
                spriteBatch.Draw(_texture, Position, Color.White); // Draw the background image
                spriteBatch.DrawString(_font, CurrentText, CurrentTextPosition, Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, LayerDepth); // Draw the current text

                // Draw the flashing cursor only if this textbox is selected and only once a second
                if (Selected && IsFlashingCursorVisible())
                {
                    Rectangle sourceRectangle = new Rectangle(0, 0, _cursorWidth, _cursorHeight);
                    Rectangle destinationRectangle = new Rectangle((int)CursorPosition.X, (int)CursorPosition.Y, _cursorWidth, _cursorHeight);

                    spriteBatch.Draw(_cursorTexture, destinationRectangle, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, LayerDepth);
                }
            }
        }
    }
}
