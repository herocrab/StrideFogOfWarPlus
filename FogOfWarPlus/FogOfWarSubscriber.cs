using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Rendering;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace FogOfWarPlus
{
    using Xenko.Engine;

    namespace FogOfWarPlus
    {
        public class FogOfWarSubscriber : SyncScript
        {
            // If you change these in game studio, zero them out here
            public float AlphaFadeOut = .1f;
            public float CameraRange = 30f;
            public float DetectDistance = 11f;
            public float DetectFade = 1.45f;
            public float DetectZeroThreshold = .01f;
            public byte AlphaDelay = 6;

            internal string Name;

            private ModelComponent model;
            private ParameterCollection shaderParams;
            private float alpha;
            private float closestDetectorDistance;
            private float closestLineOfSightDetectorDistance;
            private float detectorDistanceAlpha;
            private Vector3 worldPosRecycler;
            private float distanceRecycler;
            private byte alphaDelayCounter;
            private bool isSubscriberSeen;

            private static Vector3 cameraWorldPos = Vector3.Zero;
            private static readonly (bool, Vector3)[] DetectorWorldPos = new (bool, Vector3)[25];

            public override void Start()
            {
                Name = Entity.Name;
                closestLineOfSightDetectorDistance = float.MaxValue;
                model = Entity.Get<ModelComponent>();
                shaderParams = model?.GetMaterial(0)?.Passes[0]?.Parameters;
                shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, 0f);
                Services.GetService<FogOfWarSystem>().AddSubscriber(this);
            }

            public override void Update()
            {
                UpdateAlphaAndShadow();
            }

            private void UpdateAlphaAndShadow()
            {
                // Delay rendering, cleans up shader artifacts
                if (alphaDelayCounter < AlphaDelay) {
                    alphaDelayCounter++;
                    return;
                }

                // Enable the model, resolves visibility issues at load
                if (!model.Enabled && alpha > DetectZeroThreshold) {
                    model.Enabled = true;
                }

                Entity.Transform.GetWorldTransformation(out worldPosRecycler, out _, out _);
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
                if (isSubscriberSeen) {
                    closestDetectorDistance = closestLineOfSightDetectorDistance;
                    detectorDistanceAlpha = DistanceAlpha(closestDetectorDistance);
                    isSubscriberSeen = false;
                }
                else {
                    closestLineOfSightDetectorDistance = float.MaxValue;
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

                // Default alpha fade out when not detected
                if (Math.Abs(detectorDistanceAlpha) <= DetectZeroThreshold && alpha > DetectZeroThreshold) {
                    detectorDistanceAlpha = alpha - AlphaFadeOut;
                }

                shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, detectorDistanceAlpha);
                alpha = detectorDistanceAlpha;
            }

            internal void UpdateAlphaLineOfSight(Vector3 sourcePos)
            {
                distanceRecycler = Vector3.Distance(worldPosRecycler, sourcePos);
                if (distanceRecycler < closestLineOfSightDetectorDistance) {
                    closestLineOfSightDetectorDistance = distanceRecycler;
                }

                isSubscriberSeen = true;
            }

            internal static void UpdateWorld(Vector3 cameraPos, IReadOnlyList<Vector3> detectorWorldPos)
            {
                cameraWorldPos = cameraPos;
                
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

            public override void Cancel()
            {
                base.Cancel();
                Services.GetService<FogOfWarSystem>().RemoveSubscriber(this);
            }
        }
    }
}