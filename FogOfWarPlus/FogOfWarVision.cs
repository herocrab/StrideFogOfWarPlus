using Xenko.Engine;

namespace FogOfWarPlus
{
    public class FogOfWarVision : StartupScript
    {
        public override void Start()
        {
            Entity.Get<SpriteComponent>().Enabled = true;
        }
    }
}
