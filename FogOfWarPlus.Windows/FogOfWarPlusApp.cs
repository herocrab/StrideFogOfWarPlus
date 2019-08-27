using Xenko.Engine;

namespace FogOfWarPlus.Windows
{
    class FogOfWarPlusApp
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
