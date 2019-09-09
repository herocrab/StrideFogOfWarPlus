namespace FogOfWarPlus
{
    using Xenko.Engine;

    namespace FogOfWarPlus
    {
        public class FogOfWarSubscriber : StartupScript
        {
            public override void Start()
            {
                Services.GetService<FogOfWarSystem>().AddSubscriber(Entity);
            }

            public override void Cancel()
            {
                base.Cancel();
                Services.GetService<FogOfWarSystem>().RemoveSubscriber(Entity);
            }
        }
    }
}