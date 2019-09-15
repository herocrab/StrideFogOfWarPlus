using Xenko.Core.Mathematics;
using Xenko.Engine;
// ReSharper disable ClassNeverInstantiated.Global

namespace FogOfWarPlus
{
    public class FogOfWarDetector : StartupScript
    {
        internal string Name;
        internal Vector3 Pos
        {
            get
            {
                Entity.Transform.GetWorldTransformation(out var pos, out _, out _);
                return pos;
            }
        }

        public override void Start()
        {
            Name = Entity.GetParent().GetParent().Name;
            Entity.Get<SpriteComponent>().Enabled = true;
            Services.GetService<FogOfWarSystem>().AddDetector(this);
        }

        public override void Cancel()
        {
            base.Cancel();
            Services.GetService<FogOfWarSystem>().RemoveDetector(this);
        }
    }
}
