using System;
using System.Linq;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Physics;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace FogOfWarPlus
{
    public class FogOfWarVisionDetection : SyncScript
    {
        // set in game studio
        public Prefab Slice;
        public short StartDegree;
        public short StopDegree;

        private class FogOfWarSlice
        {
            private readonly Simulation simulation;
            private readonly Entity start;
            private readonly Entity stop;
            private Vector3 prevStart;
            private Vector3 prevStop;
            private float range;
            private float scale;

            public FogOfWarSlice(Simulation simulation, Entity start, Entity stop)
            {
                this.simulation = simulation;
                this.start = start;
                this.stop = stop;
                prevStart = Vector3.Zero;
                prevStop = Vector3.Zero;
                scale = 1;
            }

            public void Update()
            {
                start.Transform.GetWorldTransformation(out var startPos, out _, out _);
                stop.Transform.GetWorldTransformation(out var stopPos, out _, out _);

                // set the range--this must be done one tick after being added to the scene
                if (Math.Abs(range) < .01f) {
                    range = Vector3.Distance(startPos, stopPos);
                }

                if (prevStart == startPos) {
                    return;
                }

                var result = simulation.Raycast(startPos, stopPos);

                if (!result.Succeeded) {
                    scale = 1;
                    start.Transform.Scale = Vector3.One;
                    return;
                }

                if (prevStop == result.Point) {
                    return;
                }
                prevStart = startPos;
                prevStop = result.Point;

                var distance = Vector3.Distance(startPos, result.Point);
                scale = distance / range; 
                if (scale > 1) {
                    scale = 1;
                }

                if (Math.Abs(start.Transform.Scale.X - scale) < .01f) {
                    return;
                }

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
            for (int i = StartDegree; i < StopDegree; i++) {
                var slice = Slice.Instantiate().First();
                var start = slice.FindChild("StartPoint");
                start.Get<SpriteComponent>().Enabled = true;
                var stop = slice.FindChild("EndPoint");
                var simulation = Entity.Get<RigidbodyComponent>().Simulation;
                slice.Transform.Rotation *= Quaternion.RotationAxis(Vector3.UnitY, i * 0.0174533f);
                sliceList.Add(new FogOfWarSlice(simulation, start, stop));
                Entity.AddChild(slice);
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
