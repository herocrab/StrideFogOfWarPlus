using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;

namespace FogOfWarPlus
{
    public class PlayerController : SyncScript
    {
        private CharacterComponent character;
        private const float Speed = 5.5f;

        public override void Start()
        {
            character = Entity.Get<CharacterComponent>();
            ((GameBase) Game).IsDrawDesynchronized = true;
        }

        public override void Update()
        {
            if (!Input.IsKeyDown(Keys.W) &&
                !Input.IsKeyDown(Keys.A) &&
                !Input.IsKeyDown(Keys.S) &&
                !Input.IsKeyDown(Keys.D)) {
                character.SetVelocity(Vector3.Zero);
            }

            var velocity = Vector3.Zero;

            if (Input.IsKeyDown(Keys.W)) {
               velocity += -Vector3.UnitZ * Speed;
            }

            if (Input.IsKeyDown(Keys.A)) {
                velocity += -Vector3.UnitX * Speed;
            }

            if (Input.IsKeyDown(Keys.S)) {
                velocity +=  Vector3.UnitZ * Speed;
            }

            if (Input.IsKeyDown(Keys.D)) {
                velocity += Vector3.UnitX * Speed;
            }

            character.SetVelocity(velocity);
        }
    }
}
