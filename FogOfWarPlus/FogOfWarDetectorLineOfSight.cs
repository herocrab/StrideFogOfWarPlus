using Xenko.Engine;
using Xenko.Rendering;
using Xenko.Core.Mathematics;
using Xenko.Physics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FogOfWarPlus;
using FogOfWarPlus.FogOfWarPlus;
using Xenko.Core.Collections;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace FogOfWarPlus
{
    public class FogOfWarDetectorLineOfSight : SyncScript
    {
        public float VisionRadius;
        public float VisionCounterBloom;

        private ParameterCollection shaderParams;
        private float[] visionSlices;
        private Vector3 sourcePosRecycler;
        private Vector3 targetPosRecycler;
        private Vector3 prevSourcePosRecycler;
        private float distanceRecycler;
        private IOrderedEnumerable<HitResult> hitResultListRecycler;
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
            visionSlices = new float[360];
            simulation = this.GetSimulation();

            shaderParams?.Set(FogOfWarLineOfSightShaderKeys.VisionCounterBloom, VisionCounterBloom);
        }

        private void UpdateVisionField()
        {
            Entity.Transform.GetWorldTransformation(out sourcePosRecycler, out _, out _);
            if (sourcePosRecycler == Vector3.Zero) {
                return;
            }

            for (var i = 0; i < 360; i++)
            {
                targetPosRecycler = new Vector3(VisionRadius * (float)Math.Cos(i * Math.PI/180), 0, VisionRadius * (float)Math.Sin(i * Math.PI/180));
                targetPosRecycler += sourcePosRecycler;

                hitResultListRecycler = simulation.RaycastPenetrating(sourcePosRecycler, targetPosRecycler).OrderBy(a => Vector3.Distance(sourcePosRecycler, a.Point));
                // ReSharper disable once PossibleMultipleEnumeration
                if (!hitResultListRecycler.Any()) {
                    visionSlices[i] = VisionRadius;
                    continue;
                }

                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var hitResult in hitResultListRecycler) {
                    if (hitResult.Collider.CollisionGroup == CollisionFilterGroups.StaticFilter) {
                        distanceRecycler = Vector3.Distance(sourcePosRecycler, hitResult.Point);
                        visionSlices[i] = distanceRecycler < VisionRadius ? distanceRecycler : VisionRadius;
                        break;
                    }

                    if (hitResult.Collider.CollisionGroup == CollisionFilterGroups.CustomFilter10) {
                        hitResult.Collider.Entity?.Get<FogOfWarSubscriber>().UpdateAlphaLineOfSight(sourcePosRecycler);
                    }
                }
            }

            // Avoid unnecessarily updating shaders, needs to occur after tagging visibility.
            if (sourcePosRecycler == prevSourcePosRecycler) {
                return;
            }

            shaderParams?.Set(FogOfWarLineOfSightShaderKeys.Slices, visionSlices);
            prevSourcePosRecycler = sourcePosRecycler;
        }
    }
}

