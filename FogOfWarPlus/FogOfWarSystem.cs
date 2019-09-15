using System;
using System.Collections.Generic;
using System.Linq;
using FogOfWarPlus.FogOfWarPlus;
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
        }

        public void RemoveSubscriber(FogOfWarSubscriber subscriber)
        {
            if (fogSubscribers.ContainsKey(subscriber.Name)) {
                fogSubscribers.Remove(subscriber.Name);
            }
        }

        public void AddDetector(FogOfWarDetector detector)
        {
            if (fogDetectors.ContainsKey(detector.Name)) {
                return;
            }

            fogDetectors.Add(detector.Name, detector);
        }

        public void RemoveDetector(FogOfWarDetector detector)
        {
            if (fogDetectors.ContainsKey(detector.Name)) {
                fogDetectors.Remove(detector.Name);
            }
        }

        private void UpdateFogOfWarSystem()
        {
            Entity.Transform.GetWorldTransformation(out var worldPos, out _, out _);
            FogOfWarSubscriber.UpdateWorld(worldPos, fogDetectors.Select(a => a.Value.Pos).ToList());
        }

        private void InitializeFogOfWar()
        {
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
