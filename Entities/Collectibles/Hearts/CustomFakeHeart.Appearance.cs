using System;
using Celeste.Core.Components;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Engine.Actions;
using Celeste.Game.Gameplay.Collectibles.Hearts;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Simulation.Gameplay.Collectibles.Hearts;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FemtoHelper.Entities.Collectibles.Hearts
{
    public partial class CustomFakeHeart
    {
        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (heartColor == -1)
            {
                // Preserve the authored idle replacement after the spin loop.
                OwnedComponents.Add(sprite = new Sprite(engine, Graphics.GameAtlas, spriteDirectory + "/"));
                sprite.AddLoop("idle", "", 0f, 0);
                sprite.AddLoop("spin", "", 0.1f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
                    13);
                sprite.AddLoop("idle", "", 0.1f);
                sprite.CenterOrigin();
            }
            else
            {
                OwnedComponents.Add(sprite = Graphics.SpriteFactory.Create("heartgem" + heartColor));
            }

            sprite.Play("spin");
            sprite.OnLoop = delegate(string anim)
            {
                if (!Visible || anim != "spin") return;
                if (!doesPulse) return;
                if (!silentPulse)
                {
                    if (fakeSounds)
                    {
                        Process.AudioSession.Play("event:/new_content/game/10_farewell/fakeheart_pulse", Position,
                            Process.SaveContext);
                    }
                    else
                    {
                        Process.AudioSession.Play(
                            miniMode ? "event:/game/general/crystalheart_pulse" : "event:/SC2020_heartShard_pulse",
                            Position, Process.SaveContext);
                    }
                }

                ScaleWiggler.Start();
                (Scene as OriginalLevelSceneRoot)?.Displacement.AddBurst(Position, 0.35f, 8f, 48f, 0.25f);
            };
            PackedColor value;
            switch ((OriginalAreaMode)heartColor)
            {
                case OriginalAreaMode.Normal:
                    value = PackedColor.Aqua;
                    shineParticle = Process.ParticleTypes.HeartGem.P_BlueShine;
                    break;
                case OriginalAreaMode.BSide:
                    value = PackedColor.Red;
                    shineParticle = Process.ParticleTypes.HeartGem.P_RedShine;
                    break;
                case OriginalAreaMode.CSide:
                    value = PackedColor.Gold;
                    shineParticle = Process.ParticleTypes.HeartGem.P_GoldShine;
                    break;
                default:
                    value = PackedColor.FromHex(shineParticleColor);
                    shineParticle = new ParticleTypePresentation(Process.ParticleTypes.HeartGem.P_BlueShine)
                    {
                        Color = value
                    };
                    break;
            }

            value = SourceColorInterpolation.Lerp(value, PackedColor.White, 0.5f,
                ((ISourceScalarEnvironment)engine).ScalarArithmetic);

            if (miniMode)
            {
                Collider = new HitboxColliderGeometry(12f, 12f, -6f, -6f);
                OwnedComponents.Add(bloom = new BloomPointState(0.75f, 12f));
                OwnedComponents.Add(new VertexLightState(engine, value, 1f, 24, 48));
            }
            else
            {
                Collider = new HitboxColliderGeometry(16f, 16f, -8f, -8f);
                OwnedComponents.Add(bloom = new BloomPointState(0.75f, 16f));
                OwnedComponents.Add(new VertexLightState(engine, value, 1f, 32, 64));
            }

            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayer));
            OwnedComponents.Add(ScaleWiggler = WigglerPool.Create(0.5f, 4f,
                delegate(float f) { sprite.Scale = Vector2.One * (1f + f * 0.25f); }));
            moveWiggler = WigglerPool.Create(0.8f, 2f);
            moveWiggler.StartZero = true;
            OwnedComponents.Add(moveWiggler);
            if (nobloom)
            {
                bloom.Visible = false;
            }
        }
    }
}
