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
            var spawnedEntity = Prefab.Instantiate().First();
            spawnedEntity.Name = Entity.GetParent().Name;
            Entity.AddChild(spawnedEntity);
        }
    }
}
