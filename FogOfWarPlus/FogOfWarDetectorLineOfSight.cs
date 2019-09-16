using Xenko.Engine;
using Xenko.Rendering;
using Xenko.Core.Mathematics;
using Xenko.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using FogOfWarPlus.FogOfWarPlus;
// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
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
        private SortedDictionary<float, HitResult> staticResultRecycler;
        private SortedDictionary<float, HitResult> subscriberResultRecycler;
        private Simulation simulation;
        private bool isDetectorMoved;

        public override void Start()
        {
            InitializeVisionField();
        }

        public override void Update()
        {
            UpdateVisionField();
        }

        private void InitializeVisionField()
        {
            Entity.Get<ModelComponent>().Enabled = true;
            shaderParams = Entity.Get<ModelComponent>()?.GetMaterial(0)?.Passes[0]?.Parameters;
            visionSlices = new float[360];
            for (var i = 0; i < visionSlices.Length; i++) {
                visionSlices[i] = VisionRadius;
            }

            simulation = this.GetSimulation();
            staticResultRecycler = new SortedDictionary<float, HitResult>();
            subscriberResultRecycler = new SortedDictionary<float, HitResult>();

            shaderParams?.Set(FogOfWarLineOfSightShaderKeys.VisionRadius, VisionRadius);
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

                isDetectorMoved = prevSourcePosRecycler != sourcePosRecycler;
                staticResultRecycler.Clear();
                subscriberResultRecycler.Clear();
                foreach (var hitResult in simulation.RaycastPenetrating(sourcePosRecycler, targetPosRecycler)
                    .Where(collider => collider.Collider.CollisionGroup == CollisionFilterGroups.StaticFilter ||
                                       collider.Collider.CollisionGroup == CollisionFilterGroups.CustomFilter10)) {

                    switch (hitResult.Collider.CollisionGroup) {
                        case CollisionFilterGroups.StaticFilter:
                            if (isDetectorMoved) {
                                staticResultRecycler.Add(Vector3.Distance(sourcePosRecycler, hitResult.Point), hitResult);
                            }
                            break;
                        case CollisionFilterGroups.CustomFilter10:
                            subscriberResultRecycler.Add(Vector3.Distance(sourcePosRecycler, hitResult.Point), hitResult);
                            break;
                    }
                }

                if (isDetectorMoved) {
                    if (staticResultRecycler.Any()) {
                        visionSlices[i] = staticResultRecycler.First().Key - VisionCounterBloom;
                    }
                    else {
                        visionSlices[i] = VisionRadius;
                    }
                }

                foreach (var hitResult in subscriberResultRecycler) {
                    if (visionSlices[i] > hitResult.Key) {
                        hitResult.Value.Collider.Entity?.Get<FogOfWarSubscriber>().UpdateAlphaLineOfSight(sourcePosRecycler);
                    }
                }
            }

            if (!isDetectorMoved) {
                return;
            }

            shaderParams?.Set(FogOfWarLineOfSightShaderKeys.Slices, visionSlices);
            prevSourcePosRecycler = sourcePosRecycler;
        }
    }
}

