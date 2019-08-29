using Xenko.Engine;

namespace FogOfWarPlus
{
    public class FogOfWarVisionCircle : StartupScript
    {
        public override void Start()
        {
            Entity.Get<SpriteComponent>().Enabled = true;
        }
    }
}
