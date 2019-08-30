using Xenko.Engine;
using Xenko.Graphics;
// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace FogOfWarPlus
{
    public class FogOfWarSystem : StartupScript
    {
        public Entity FogOfWar;

        public override void Start()
        {
            InitializeFogOfWar();
        }

        private void InitializeFogOfWar()
        {
            var modelComponent = FogOfWar.Get<ModelComponent>();
            modelComponent.Enabled = true;
        }
    }
}
