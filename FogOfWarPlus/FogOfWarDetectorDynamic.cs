using Xenko.Engine;
using System.Collections.Generic;
using Xenko.Rendering;
using Xenko.Core.Mathematics;
using Xenko.Physics;
using System;
using System.Linq;

namespace FogOfWarPlus
{
    public class FogOfWarDetectorDynamic : SyncScript
    {
        public VisionFidelitySetting VisionFidelity;
        public float VisionRadius;

        public enum VisionFidelitySetting
        {
            One,
            Two,
            Five,
            Ten,
        }

        private readonly Dictionary<VisionFidelitySetting, byte> Degrees =
            new Dictionary<VisionFidelitySetting, byte>
        {
            {VisionFidelitySetting.One, 1},
            {VisionFidelitySetting.Two, 2},
            {VisionFidelitySetting.Five, 5},
            {VisionFidelitySetting.Ten, 10},
        };

        private ParameterCollection shaderParams;
        private byte visionStep;
        private Vector4[] visionSlices;
        private Vector3 sourcePosRecycler;
        private Vector3 targetPosRecycler;
        private HitResult hitResultRecycler;
        private Simulation simulation;

        public override void Start()
        {
            InitializeVisionField();
        }

        public override void Update()
        {
            UpdateVisionField();
        }

        public override void Cancel()
        {
            base.Cancel();
        }

        private void InitializeVisionField()
        {
            Entity.Get<ModelComponent>().Enabled = true;
            shaderParams = Entity.Get<ModelComponent>()?.GetMaterial(0)?.Passes[0]?.Parameters;

            visionStep = Degrees[VisionFidelity];
            visionSlices = new Vector4[360 / visionStep];
            simulation = this.GetSimulation();
        }

        private void UpdateVisionField()
        {
            Entity.Transform.GetWorldTransformation(out sourcePosRecycler, out _, out _);

            float angle = 0;
            for (int i = 0; i < visionSlices.Length; i++)
            {
                angle = i * visionStep;
                targetPosRecycler = new Vector3(
                    sourcePosRecycler.X + VisionRadius * (float)Math.Cos(angle),
                    sourcePosRecycler.Y,
                    sourcePosRecycler.Z + VisionRadius * (float)Math.Sin(angle));

                hitResultRecycler = simulation.RaycastPenetrating(sourcePosRecycler, targetPosRecycler)
                    .FirstOrDefault(collider => collider.Collider.CollisionGroup == CollisionFilterGroups.StaticFilter);

                visionSlices[i].W = angle;
                if (!hitResultRecycler.Succeeded)
                {
                    visionSlices[i].X = VisionRadius;
                } else {
                    visionSlices[i].X = Vector3.Distance(sourcePosRecycler, targetPosRecycler);
                }

                if (i > 0 && i < visionSlices.Length - 1)
                {
                    visionSlices[i - 1].Z = visionSlices[i].X; // prev slice right
                    visionSlices[i].Y = visionSlices[i - 1].X; // curr slice left
                }
            }
            visionSlices[0].Y = visionSlices[visionSlices.Length - 1].X;
            visionSlices[visionSlices.Length - 1].Z = visionSlices[0].X;

            // Shader logics
            // ==============
            // W - angle
            // X - distance
            // Y - left distance
            // Z - right distance
        }
    }
}

