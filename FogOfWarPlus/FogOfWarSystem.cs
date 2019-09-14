using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Rendering;

namespace FogOfWarPlus
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class FogOfWarSystem : SyncScript
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnassignedField.Global
        public float FogOpacity;

        private Dictionary<string, FogSubscriber> fogSubscribers;
        private Dictionary<string, FogDetector> fogDetectors;

        private class FogDetector
        {
            internal readonly string Name;
            internal Vector3 Pos
            {
                get
                {
                    detector.Transform.GetWorldTransformation(out var pos, out _, out _);
                    return pos;
                }
            }

            private readonly Entity detector;

            internal FogDetector(Entity entity)
            {
                Name = entity.Name;
                detector = entity;
            }
        }

        private class FogSubscriber
        {
            internal readonly string Name;

            private readonly Entity subscriber;
            private readonly ModelComponent model;
            private readonly ParameterCollection shaderParams;
            private float alpha;
            private float closestDetectorDistance;
            private float closestLineOfSightDetectorDistance;
            private float detectorDistanceAlpha;
            private Vector3 worldPosRecycler;
            private float distanceRecycler;
            private byte seenByLineOfSightCounter;
            private byte alphaDelayCounter;

            private static Vector3 cameraWorldPos = Vector3.Zero;
            private static readonly (bool, Vector3)[] DetectorWorldPos = new (bool, Vector3)[25];

            private const float CameraRange = 30f;
            private const float DetectDistance = 9.5f;
            private const float DetectFade = 1.45f;
            private const float DetectZeroThreshold = .01f;
            private const byte SeenByLineOfSightCounterReset = 5;
            private const byte AlphaDelay = 6;

            internal FogSubscriber(Entity entity)
            {
                Name = entity.Name;
                closestLineOfSightDetectorDistance = float.MaxValue;
                subscriber = entity;
                model = entity.Get<ModelComponent>();
                shaderParams = model?.GetMaterial(0)?.Passes[0]?.Parameters;
            }

            internal void UpdateAlphaAndShadow()
            {
                // Delay rendering, cleans up shader artifacts
                if (alphaDelayCounter < AlphaDelay) {
                    alphaDelayCounter++;
                    return;
                }

                if (!model.Enabled) {
                    model.Enabled = true;
                }

                subscriber.Transform.GetWorldTransformation(out worldPosRecycler, out _, out _);
                closestDetectorDistance = float.MaxValue;

                // Do not calculate alphas for off screen entities, this *could* use physics on GPU side
                if (Vector3.Distance(worldPosRecycler, cameraWorldPos) > CameraRange)
                {
                    if (alpha >= 0) {
                        shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, 0f);
                        model.IsShadowCaster = false;
                        alpha = 0;
                        return;
                    }
                }

                // Set the closest distance for line of sight detectors
                if (seenByLineOfSightCounter > 0) {
                    closestDetectorDistance = closestLineOfSightDetectorDistance;
                    detectorDistanceAlpha = DistanceAlpha(closestDetectorDistance);
                    seenByLineOfSightCounter -= 1;
                }

                /* Not the most efficient n(n-1)/2; depends on detector distance, uses a shortcut */
                for (var j = 0; j < DetectorWorldPos.Length; j++) {
                    if (!DetectorWorldPos[j].Item1) {
                        continue;
                    }
                    
                    distanceRecycler = Vector3.Distance(worldPosRecycler, DetectorWorldPos[j].Item2);
                    if (distanceRecycler < closestDetectorDistance) {
                        closestDetectorDistance = distanceRecycler;
                        detectorDistanceAlpha = DistanceAlpha(closestDetectorDistance);

                        // Shortcut fully visible units, stop iterating
                        if (Math.Abs(detectorDistanceAlpha - 1) < DetectZeroThreshold) {
                            detectorDistanceAlpha = 1;
                            break;
                        }
                    }
                }

                shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, detectorDistanceAlpha);
                alpha = detectorDistanceAlpha;
            }

            internal void UpdateAlphaLineOfSight(Vector3 sourcePos)
            {
                distanceRecycler = Vector3.Distance(worldPosRecycler, sourcePos);
                if (seenByLineOfSightCounter > 0 && distanceRecycler < closestLineOfSightDetectorDistance) {
                    closestLineOfSightDetectorDistance = distanceRecycler;
                    seenByLineOfSightCounter = SeenByLineOfSightCounterReset;
                }

                if (seenByLineOfSightCounter <= 0) {
                    closestLineOfSightDetectorDistance = distanceRecycler;
                    seenByLineOfSightCounter = SeenByLineOfSightCounterReset;
                }
            }

            internal static void UpdateWorld(Vector3 cameraWorldPos, IReadOnlyList<Vector3> detectorWorldPos)
            {
                FogSubscriber.cameraWorldPos = cameraWorldPos;
                
                // Assign array elements, only up to the maximum length
                for (var i = 0; i < DetectorWorldPos.Length; i++) {
                    if (detectorWorldPos.Count <= i) {
                        DetectorWorldPos[i].Item1 = false;
                        DetectorWorldPos[i].Item2 = Vector3.Zero;
                        continue;
                    }

                    DetectorWorldPos[i].Item1 = true;
                    DetectorWorldPos[i].Item2 = detectorWorldPos[i];
                }
            }

            private float DistanceAlpha(float distance)
            {
                if (distance < DetectDistance) {
                    return 1;
                }

                if (distance < DetectDistance + DetectFade) {
                    return (DetectFade - (distance - DetectDistance)) / DetectFade;
                }

                return 0;
            }
        }

        public override void Start()
        {
            InitializeFogOfWar();
            RegisterFogOfWar();
        }

        public override void Update()
        {
            UpdateFogOfWarSystem();
        }

        public void AddSubscriber(Entity entity)
        {
            var fogSubscriber = new FogSubscriber(entity);
            if (fogSubscribers.ContainsKey(fogSubscriber.Name)) {
                return;
            }

            fogSubscribers.Add(fogSubscriber.Name, fogSubscriber);
        }

        public void RemoveSubscriber(Entity entity)
        {
            if (fogSubscribers.ContainsKey(entity.Name)) {
                fogSubscribers.Remove(entity.Name);
            }
        }

        public void AddDetector(Entity entity)
        {
            var fogDetector = new FogDetector(entity);
            if (fogDetectors.ContainsKey(fogDetector.Name)) {
                return;
            }

            fogDetectors.Add(fogDetector.Name, fogDetector);
        }

        public void RemoveDetector(Entity entity)
        {
            if (fogDetectors.ContainsKey(entity.Name)) {
                fogDetectors.Remove(entity.Name);
            }
        }

        private void UpdateFogOfWarSystem()
        {
            Entity.Transform.GetWorldTransformation(out var worldPos, out _, out _);
            FogSubscriber.UpdateWorld(worldPos, fogDetectors.Select(a => a.Value.Pos).ToList());

            foreach (var fogSubscriber in fogSubscribers.Select(a => a.Value)) {
                fogSubscriber.UpdateAlphaAndShadow();
            }
        }

        private void InitializeFogOfWar()
        {
            fogDetectors = new Dictionary<string, FogDetector>();
            fogSubscribers = new Dictionary<string, FogSubscriber>();

            var modelComponent = Entity.FindChild("FogOfWar").FindChild("FogOfWarLayer1").Get<ModelComponent>();
            modelComponent.Enabled = true;

            Entity.FindChild("Orthographic").Get<CameraComponent>().Enabled = true;

            var perspective = Entity.FindChild("Perspective").Get<CameraComponent>();
            perspective.Enabled = true;
            perspective.Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId();

            modelComponent.GetMaterial(0).Passes[0].Parameters.Set(FogOfWarPlusShaderKeys.FogOpacity, FogOpacity);
        }

        private void RegisterFogOfWar()
        {
            Services.AddService(this);
        }
    }
}
