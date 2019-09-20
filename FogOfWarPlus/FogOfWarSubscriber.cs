/* Copyright (c) 2019 Jeremy Buck (jarmo@netcodez.com) http://github.com/devjarmo 
 
Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.

*/
using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Rendering;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnassignedField.Global

namespace FogOfWarPlus
{
    using Xenko.Engine;

    namespace FogOfWarPlus
    {
        public class FogOfWarSubscriber : SyncScript
        {
            internal string Name;
            private ModelComponent model;
            private ParameterCollection shaderParams;
            private float alpha;
            private float closestDetectorDistance;
            private float detectorDistanceAlpha;
            private Vector3 worldPosRecycler;
            private float distanceRecycler;
            private byte alphaDelayCounter;
            private float detectDistance;
            private float detectFade;
            private float alphaFadeOut;
            private float cameraRange;
            private float detectZeroThreshold;
            private byte alphaDelay;

            private static Vector3 cameraWorldPos = Vector3.Zero;
            private static readonly (bool, Vector3)[] DetectorWorldPos = new (bool, Vector3)[25];

            public override void Start()
            {
                Name = Entity.Name;
                model = Entity.Get<ModelComponent>();
                shaderParams = model?.GetMaterial(0)?.Passes[0]?.Parameters;
                shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, 0f);

                var fogOfWarSystem = Services.GetService<FogOfWarSystem>();
                detectDistance = fogOfWarSystem.DetectDistance;
                detectFade = fogOfWarSystem.DetectFade;
                alphaFadeOut = fogOfWarSystem.AlphaFadeOut;
                cameraRange = fogOfWarSystem.CameraRange;
                detectZeroThreshold = fogOfWarSystem.DetectZeroThreshold;
                alphaDelay = fogOfWarSystem.AlphaDelay;
                fogOfWarSystem.AddSubscriber(this);
            }

            public override void Update()
            {
                UpdateAlphaAndShadow();
            }

            private void UpdateAlphaAndShadow()
            {
                // Delay rendering, cleans up shader artifacts
                if (alphaDelayCounter < alphaDelay) {
                    alphaDelayCounter++;
                    return;
                }

                // Enable the model, resolves visibility issues at load
                if (!model.Enabled && alpha > detectZeroThreshold) {
                    model.Enabled = true;
                }

                Entity.Transform.GetWorldTransformation(out worldPosRecycler, out _, out _);
                closestDetectorDistance = float.MaxValue;

                // Do not calculate alphas for off screen entities, this *could* use physics on GPU side
                if (Vector3.Distance(worldPosRecycler, cameraWorldPos) > cameraRange)
                {
                    if (alpha >= 0) {
                        shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, 0f);
                        model.IsShadowCaster = false;
                        alpha = 0;
                        return;
                    }
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
                        if (Math.Abs(detectorDistanceAlpha - 1) < detectZeroThreshold) {
                            detectorDistanceAlpha = 1;
                            break;
                        }
                    }
                }

                // Default alpha fade out when not detected
                if (Math.Abs(detectorDistanceAlpha) <= detectZeroThreshold && alpha > detectZeroThreshold) {
                    detectorDistanceAlpha = alpha - alphaFadeOut;
                }

                shaderParams?.Set(FogOfWarUnitShaderKeys.Alpha, detectorDistanceAlpha);
                alpha = detectorDistanceAlpha;
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
                if (distance < detectDistance) {
                    return 1;
                }

                if (distance < detectDistance + detectFade) {
                    return (detectFade - (distance - detectDistance)) / detectFade;
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