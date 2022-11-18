namespace Server
{
    public class Ball
    {
        public void Initialize() { }
        
        public void ServerSideUpdate(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}
