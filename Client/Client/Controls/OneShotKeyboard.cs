using Microsoft.Xna.Framework.Input;
namespace Client.Controls
{
    public class OneShotKeyboard
    {
        static KeyboardState currentKeyState;
        static KeyboardState previousKeyState;
        public OneShotKeyboard()
        {
        }

        public static KeyboardState GetState()

        {
            previousKeyState = currentKeyState;
            currentKeyState = Keyboard.GetState();
            return currentKeyState;
        }

        public static bool IsPressed(Keys key)
        {
            return currentKeyState.IsKeyDown(key);
        }

        public static bool HasNotBeenPressed(Keys key)
        {
            return currentKeyState.IsKeyDown(key) && !previousKeyState.IsKeyDown(key);
        }
    }
}
