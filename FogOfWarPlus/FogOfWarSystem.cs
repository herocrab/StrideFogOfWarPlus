using Xenko.Engine;
using Xenko.Graphics;

// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace FogOfWarPlus
{
    public class FogOfWarSystem : StartupScript
    {
        public float FogOpacity;
        public Texture FogOfWarRenderTexture;
        public Entity EnemyUnit;

        public override void Start()
        {
            InitializeFogOfWar();
        }

        private void InitializeFogOfWar()
        {
            var modelComponent = Entity.FindChild("FogOfWar").FindChild("FogOfWarLayer1").Get<ModelComponent>();
            modelComponent.Enabled = true;

            Entity.FindChild("Orthographic").Get<CameraComponent>().Enabled = true;

            var perspective = Entity.FindChild("Perspective").Get<CameraComponent>();
            perspective.Enabled = true;
            perspective.Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId();

            modelComponent.GetMaterial(0).Passes[0].Parameters.Set(FogOfWarPlusShaderKeys.FogOpacity, FogOpacity);

            var enemyShaderParameters = EnemyUnit.Get<ModelComponent>().GetMaterial(0).Passes[0].Parameters;
            enemyShaderParameters.Set(FogOfWarUnitShaderKeys.Fog, FogOfWarRenderTexture);
            enemyShaderParameters.Set(FogOfWarUnitShaderKeys.IsFogLoaded,true);
        }
    }
}
