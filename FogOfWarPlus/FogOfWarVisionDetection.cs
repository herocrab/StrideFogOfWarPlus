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
    public class FogOfWarVisionDetection : StartupScript
    {
        // set in game studio
        public Prefab Slice;
        public short StartDegree;
        public short StopDegree;

        private class FogOfWarSlice : SyncScript
        {
            internal Simulation Simulation { private get; set; }
            internal Entity StartPoint { private get; set; }
            internal Entity StopPoint { private get; set; }

            private Vector3 prevStart;
            private Vector3 prevStop;
            private float range;
            private float scale;

            public FogOfWarSlice()
            {
                prevStart = Vector3.Zero;
                prevStop = Vector3.Zero;
                scale = 1;
            }

            public override void Update()
            {
                StartPoint.Transform.GetWorldTransformation(out var startPos, out _, out _);
                StopPoint.Transform.GetWorldTransformation(out var stopPos, out _, out _);

                // set the range--this must be done one tick after being added to the scene
                if (Math.Abs(range) < .01f) {
                    range = Vector3.Distance(startPos, stopPos);
                }

                if (prevStart == startPos) {
                    return;
                }

                var resultList =  Simulation.RaycastPenetrating(startPos, stopPos);
                if (!resultList.Any()) {
                    scale = 1;
                    StartPoint.Transform.Scale = Vector3.One;
                    return;
                }

                var closestPoint = resultList.First().Point;
                var closestDistance = Vector3.Distance(startPos, closestPoint);
                foreach (var hitResult in resultList) {
                    if (Vector3.Distance(startPos, hitResult.Point) < closestDistance) {
                        closestPoint = hitResult.Point;
                    }
                }

                if (prevStop == closestPoint) {
                    return;
                }
                prevStart = startPos;
                prevStop = closestPoint;

                var distance = Vector3.Distance(startPos, closestPoint);
                scale = distance / range; 
                if (scale > 1) {
                    scale = 1;
                }

                if (Math.Abs(StartPoint.Transform.Scale.X - scale) < .01f) {
                    return;
                }

                StartPoint.Transform.Scale = new Vector3(scale, scale, scale);
            }
        }

        public override void Start()
        {
            InitializeFogOfWarSlices();
        }

        private void InitializeFogOfWarSlices()
        {
            for (int i = StartDegree; i < StopDegree; i++) {
                var slice = Slice.Instantiate().First();
                var start = slice.FindChild("StartPoint");
                start.Get<SpriteComponent>().Enabled = true;
                var stop = slice.FindChild("EndPoint");
                slice.Transform.Rotation *= Quaternion.RotationAxis(Vector3.UnitY, i * 0.0174533f);

                var fogOfWarSlice = new FogOfWarSlice
                {
                    StartPoint = start,
                    StopPoint = stop,
                    Simulation = this.GetSimulation()
                };

                slice.Add(fogOfWarSlice);
                Entity.AddChild(slice);
            }
        }
    }
}
