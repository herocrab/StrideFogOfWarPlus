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
using System.Collections.Generic;
using System.Linq;
using FogOfWarPlus.FogOfWarPlus;
using Stride.Core.Diagnostics;
using Stride.Engine;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace FogOfWarPlus
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class FogOfWarSystem : SyncScript
    {
        public float FogOpacity;
        public float DetectDistance;
        public float DetectFade;
        public float AlphaFadeOut;
        public float CameraRange;
        public float DetectZeroThreshold;
        public byte AlphaDelay;

        private Dictionary<string, FogOfWarSubscriber> fogSubscribers;
        private Dictionary<string, FogOfWarDetector> fogDetectors;

        public override void Start()
        {
            InitializeFogOfWar();
            RegisterFogOfWar();
        }

        public override void Update()
        {
            UpdateFogOfWarSystem();
        }

        public void AddSubscriber(FogOfWarSubscriber subscriber)
        {
            if (fogSubscribers.ContainsKey(subscriber.Name)) {
                return;
            }

            fogSubscribers.Add(subscriber.Name, subscriber);
            Log.Debug($"Subscriber {subscriber.Name} has been added to the fog of war system.");
        }

        public void RemoveSubscriber(FogOfWarSubscriber subscriber)
        {
            if (fogSubscribers.ContainsKey(subscriber.Name)) {
                fogSubscribers.Remove(subscriber.Name);
                Log.Debug($"Subscriber {subscriber.Name} has been removed from the fog of war system.");
            }
        }

        public void AddDetector(FogOfWarDetector detector)
        {
            if (fogDetectors.ContainsKey(detector.Name)) {
                return;
            }

            fogDetectors.Add(detector.Name, detector);
            Log.Debug($"Detector {detector.Name} has been added to the fog of war system.");
        }

        public void RemoveDetector(FogOfWarDetector detector)
        {
            if (fogDetectors.ContainsKey(detector.Name)) {
                fogDetectors.Remove(detector.Name);
                Log.Debug($"Detector {detector.Name} has been removed from the fog of war system.");
            }
        }

        private void UpdateFogOfWarSystem()
        {
            //Entity.Transform.GetWorldTransformation(out var worldPos, out _, out _);
            var worldPos = Entity.Transform.WorldMatrix.TranslationVector;
            FogOfWarSubscriber.UpdateWorld(worldPos, fogDetectors.Select(a => a.Value.Pos).ToList());
        }

        private void InitializeFogOfWar()
        {
            Log.ActivateLog(LogMessageType.Debug);

            fogDetectors = new Dictionary<string, FogOfWarDetector>();
            fogSubscribers = new Dictionary<string, FogOfWarSubscriber>();

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
