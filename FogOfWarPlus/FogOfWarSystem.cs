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
            private readonly ParameterCollection shaderParams;
            private float alpha;
            private float closestDetectorDistance;
            private float closestLineOfSightDetectorDistance;
            private float detectorDistanceRecycler;
            private Vector3 worldPosRecycler;
            private float distanceRecycler;
            private float lineOfSightRecycler;
            private byte seenByCounter;

            // ReSharper disable once InconsistentNaming
            private static Vector3 CameraWorldPos = Vector3.Zero;
            private static readonly (bool, Vector3)[] DetectorWorldPos = new (bool, Vector3)[25];
            private const float CameraRange = 25f;
            private const float DetectDistance = 5f;
            private const float DetectFade = 1f;
            private const float DetectZeroThreshold = .01f;
            private const byte SeenByCounterReset = 5;

            internal FogSubscriber(Entity entity)
            {
                Name = entity.Name;
                alpha = 0;
                closestLineOfSightDetectorDistance = float.MaxValue;
                subscriber = entity;
                shaderParams = entity.Get<ModelComponent>()?
                    .GetMaterial(0)?
                    .Passes[0]?
                    .Parameters;
            }

            internal void UpdateAlpha()
            {
                subscriber.Transform.GetWorldTransformation(out worldPosRecycler, out _, out _);

                closestDetectorDistance = float.MaxValue;

                // Do not calculate alphas for off screen entities
                if (Vector3.Distance(worldPosRecycler, CameraWorldPos) > CameraRange)
                {
                    shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, 0f);
                    alpha = 0;
                    return;
                }

                // Set the closest distance for line of sight detectors
                if (seenByCounter > 0) {
                    closestDetectorDistance = closestLineOfSightDetectorDistance;
                    seenByCounter -= 1;
                }

                /* Not the most efficient n(n-1)/2; depends on detector distance, uses a shortcut */
                for (var j = 0; j < DetectorWorldPos.Length; j++) {
                    detectorDistanceRecycler = DetectorDistance(DetectorWorldPos[j].Item2);
                    if (detectorDistanceRecycler < closestDetectorDistance) {
                        closestDetectorDistance = detectorDistanceRecycler;

                        // Shortcut fully visible units, stop iterating
                        if (Math.Abs(detectorDistanceRecycler - 1) < DetectZeroThreshold) {
                            shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, 1f);
                            alpha = 1;
                            break;
                        }
                    }
                }

                // Avoid unnecessarily updating shader parameters
                if (Math.Abs(alpha - closestDetectorDistance) < DetectZeroThreshold) {
                    return;
                }

                shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, closestDetectorDistance);
                alpha = closestDetectorDistance;
            }

            internal void UpdateAlphaLineOfSight(Vector3 sourcePos)
            {
                lineOfSightRecycler = DetectorDistance(sourcePos);
                if (seenByCounter > 0 && lineOfSightRecycler < closestLineOfSightDetectorDistance) {
                    closestLineOfSightDetectorDistance = lineOfSightRecycler;
                    seenByCounter = SeenByCounterReset;
                }

                if (seenByCounter <= 0) {
                    closestLineOfSightDetectorDistance = lineOfSightRecycler;
                    seenByCounter = SeenByCounterReset;
                }
            }

            internal static void UpdateWorld(Vector3 cameraWorldPos, ICollection<Vector3> detectorWorldPos)
            {
                CameraWorldPos = cameraWorldPos;

                // Reset array items to default
                for (var i = 0; i < DetectorWorldPos.Length; i++) {
                    if (detectorWorldPos.Count <= i) {
                        DetectorWorldPos[i].Item1 = false;
                        DetectorWorldPos[i].Item2 = Vector3.Zero;
                        continue;
                    }

                    DetectorWorldPos[i].Item1 = true;
                    DetectorWorldPos[i].Item2 = detectorWorldPos.ElementAt(i);
                }
            }

            private float DetectorDistance(Vector3 playerPos)
            {
                distanceRecycler = Vector3.Distance(worldPosRecycler, playerPos);
                if (distanceRecycler < DetectDistance) {
                    return 1;
                }

                if (distanceRecycler < DetectDistance + DetectFade) {
                    return (DetectFade - (distanceRecycler - DetectDistance)) / DetectFade;
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
            FogSubscriber.UpdateWorld(worldPos, fogDetectors.Select(a => a.Value.Pos));

            foreach (var fogSubscriber in fogSubscribers.Select(a => a.Value)) {
                fogSubscriber.UpdateAlpha();
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
