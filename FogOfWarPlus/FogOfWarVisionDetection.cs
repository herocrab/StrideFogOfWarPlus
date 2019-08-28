using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Input;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Physics;

namespace FogOfWarPlus
{
    public class FogOfWarVisionDetection : SyncScript
    {
        private class FogOfWarSlice
        {
            private readonly Simulation simulation;
            private readonly Entity start;
            private readonly Entity stop;
            private readonly float range;
            private float scale;

            public FogOfWarSlice(Simulation simulation, Entity start, Entity stop, float range)
            {
                this.simulation = simulation;
                this.start = start;
                this.stop = stop;
                this.range = range;
                scale = 1;
            }

            public void Update()
            {
                start.Transform.GetWorldTransformation(out var startPos, out _, out _);
                stop.Transform.GetWorldTransformation(out var stopPos, out _, out _);

                var resultList = simulation.RaycastPenetrating(startPos, stopPos).Where(
                    result => result.Collider.CollisionGroup == CollisionFilterGroups.StaticFilter).ToArray();

                if (!resultList.Any()) {
                    return;
                }

                var distance = Vector3.Distance(startPos, resultList.First().Point);
                scale = distance / range + .2f;
                start.Transform.Scale = new Vector3(scale, scale, scale);
            }
        }

        private FastList<FogOfWarSlice> sliceList;

        public override void Start()
        {
            InitializeFogOfWarSlices();
        }

        public override void Update()
        {
            UpdateFogOfWarSlices();
        }

        private void InitializeFogOfWarSlices()
        {
            sliceList = new FastList<FogOfWarSlice>();
            foreach (var entity in Entity.GetChildren()) {
                var start = entity.FindChild("FogOfWarWedge");
                var stop = start.FindChild("EndPoint");
                var range = Vector3.Distance(start.Transform.Position, stop.Transform.Position);
                var simulation = Entity.Get<RigidbodyComponent>().Simulation;
                sliceList.Add(new FogOfWarSlice(simulation, start, stop, range));
            }
        }

        private void UpdateFogOfWarSlices()
        {
            foreach (var slice in sliceList) {
                slice.Update();
            }
        }
    }
}
