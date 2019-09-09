using Xenko.Input;
using Xenko.Engine;

namespace FogOfWarPlus
{
    public class PlayerController : SyncScript
    {
        private const float Speed = .1f;

        public override void Start()
        {
        }

        public override void Update()
        {
            if (Input.IsKeyDown(Keys.W)) {

                Entity.Transform.Position.Z -= Speed;
            }

            if (Input.IsKeyDown(Keys.A)) {
                Entity.Transform.Position.X -= Speed;
            }

            if (Input.IsKeyDown(Keys.S)) {
                Entity.Transform.Position.Z += Speed;
            }

            if (Input.IsKeyDown(Keys.D)) {
                Entity.Transform.Position.X += Speed;
            }
        }
    }
}
