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
        public void OnHoldable(HoldableState h)
        {
            RoomPlayerSession entity =
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession)
                    .FullName);
            if (Visible && h.Dangerous(crystalCollider))
            {
                Collect(entity, Vector2Geometry.Angle(h.GetSpeed()));
            }
        }

        public void OnPlayer(RoomPlayerSession player)
        {
            if (!Visible || (Scene as OriginalLevelSceneRoot).Frozen)
            {
                return;
            }

            if (player.DashAttacking || breakOnContact)
            {
                if (heartBehavior == 0)
                {
                    (Scene as OriginalLevelSceneRoot).Session.SetFlag(customFlag, flagSet);
                    ((OriginalLevelSceneRoot)Scene).Displacement.AddBurst(Position, burstDuration, burstRadiusFrom,
                        burstRadiusTo, burstAlpha);
                    Process.AudioSession.Play(audioEvent, Position, Process.SaveContext);
                    OriginalCelesteFreeze.Freeze(engine, freezeTime);
                    if (CollectFlash)
                    {
                        ((OriginalLevelSceneRoot)Scene).Flash(PackedColor.FromHex(flashColor) * flashOpacity,
                            !FlashOverPlayer);
                    }

                    for (int i = 0; i < particleCount; i++)
                    {
                        ((OriginalLevelSceneRoot)Scene).Particles.Emit(customParticle, 1, Center,
                            Vector2.One * particleSpawnSpread,
                            SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random) *
                            ((float)Math.PI * 2f));
                    }

                    ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(1, 1);
                    Collect(player, Vector2Geometry.Angle(player.Speed));
                    if (removeBloomOnShatter)
                    {
                        bloom.Visible = false;
                    }

                    return;
                }
                else if (heartBehavior == 1)
                {
                    (Scene as OriginalLevelSceneRoot).Session.SetFlag(customFlag, flagSet);
                    player.Die(player.Position - Position, false, true);
                    ((OriginalLevelSceneRoot)Scene).Displacement.AddBurst(Position, burstDuration, burstRadiusFrom,
                        burstRadiusTo, burstAlpha);
                    Process.AudioSession.Play(audioEvent, Position, Process.SaveContext);
                    OriginalCelesteFreeze.Freeze(engine, freezeTime);
                    if (CollectFlash)
                    {
                        ((OriginalLevelSceneRoot)Scene).Flash(PackedColor.FromHex(flashColor) * flashOpacity,
                            !FlashOverPlayer);
                    }

                    for (int i = 0; i < particleCount; i++)
                    {
                        ((OriginalLevelSceneRoot)Scene).Particles.Emit(customParticle, 1, Center,
                            Vector2.One * particleSpawnSpread,
                            SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random) *
                            ((float)Math.PI * 2f));
                    }

                    ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(1, 1);
                    Collect(player, Vector2Geometry.Angle(player.Speed));
                    if (removeBloomOnShatter)
                    {
                        bloom.Visible = false;
                    }

                    return;
                }
                else if (heartBehavior is 2 or 3)
                {
                    (Scene as OriginalLevelSceneRoot).Session.SetFlag(customFlag, flagSet);
                    ((OriginalLevelSceneRoot)Scene).Displacement.AddBurst(Position, burstDuration, burstRadiusFrom,
                        burstRadiusTo, burstAlpha);
                    Process.AudioSession.Play(audioEvent, Position, Process.SaveContext);
                    OriginalCelesteFreeze.Freeze(engine, freezeTime);
                    if (CollectFlash)
                    {
                        ((OriginalLevelSceneRoot)Scene).Flash(PackedColor.FromHex(flashColor) * flashOpacity,
                            !FlashOverPlayer);
                    }

                    for (int i = 0; i < particleCount; i++)
                    {
                        ((OriginalLevelSceneRoot)Scene).Particles.Emit(customParticle, 1, Center,
                            Vector2.One * particleSpawnSpread,
                            SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random) *
                            ((float)Math.PI * 2f));
                    }
                }
            }

            if (bounceSfxDelay <= 0f)
            {
                Process.AudioSession.Play(
                    fakeSounds
                        ? "event:/new_content/game/10_farewell/fakeheart_bounce"
                        : "event:/game/general/crystalheart_bounce", Position, Process.SaveContext);
                bounceSfxDelay = 0.1f;
            }

            if (heartBehavior != 3)
            {
                player.PointBounce(Center);
            }
            else if (player.DashAttacking == false && !breakOnContact)
            {
                player.PointBounce(Center);
            }

            moveWiggler.Start();
            ScaleWiggler.Start();
            moveWiggleDir =
                ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(Center - player.Center,
                    Vector2.UnitY);
            ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(1, 1);
        }

        private void Collect(RoomPlayerSession player, float angle)
        {
            FakeHeartShatter.Process(engine, this, player, angle, respawnTime, ref respawnTimer);
        }
    }
}
