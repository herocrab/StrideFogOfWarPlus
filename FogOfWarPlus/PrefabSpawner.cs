using System.Linq;
using Xenko.Engine;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace FogOfWarPlus
{
    public class PrefabSpawner : StartupScript
    {
        public Prefab Prefab;

        public override void Start()
        {
            Entity.AddChild(Prefab.Instantiate().First());
        }
    }
}
