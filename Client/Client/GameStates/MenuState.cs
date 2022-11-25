using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Client.GameStates
{
    public class MenuState : GameState
    {
        private List<Controls.Component> components;
        public MenuState(ClientCode.Client clientGame, GraphicsDevice graphicsDevice, ContentManager content) : base(clientGame, graphicsDevice, content)
        {
            var buttonTexture = content.Load<Texture2D>("button");
            var buttonFont = content.Load<SpriteFont>("font");

            var playButton = new Controls.Button(buttonTexture, buttonFont)
            {
                Position = new Vector2(50, 50),
                Text = "Играть"
            };
            playButton.Click += PlayButton_Click;

            var settingsButton = new Controls.Button(buttonTexture, buttonFont)
            {
                Position = new Vector2(50, 150),
                Text = "Настройки"
            };

            components = new List<Controls.Component>()
            {

            };
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            throw new NotImplementedException();
        }

        public override void PostUpdate(GameTime gameTime)
        {
            throw new NotImplementedException();
        }

        public override void Update(GameTime gameTime)
        {
            throw new NotImplementedException();
        }
    }
}
