using Xenko.Engine;
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedParameter.Local

namespace FogOfWarPlus.Windows
{
    internal class FogOfWarPlusApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
