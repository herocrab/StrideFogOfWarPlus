using Xenko.Engine;

namespace FogOfWarPlus
{
    public class FogOfWarDetector : StartupScript
    {
        public override void Start()
        {
            Entity.Get<SpriteComponent>().Enabled = true;
            Services.GetService<FogOfWarSystem>().AddDetector(Entity.GetParent().GetParent());
        }

        public override void Cancel()
        {
            base.Cancel();
            Services.GetService<FogOfWarSystem>().RemoveDetector(Entity.GetParent().GetParent());
        }
    }
}
