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
        public override void Update()
        {
            bounceSfxDelay -= engine.DeltaTime;
            timer += engine.DeltaTime;
            sprite.Position = noWiggle
                ? Vector2.UnitY * 0f * 2f + moveWiggleDir * moveWiggler.Value * -8f
                : FakeHeartHoverMotion.Offset(timer, moveWiggleDir, moveWiggler);
            if (FakeHeartHoverMotion.AdvanceRespawnTimer(ref respawnTimer, engine))
            {
                Collidable = Visible = true;
                if (!nobloom) bloom.Visible = true;
                ScaleWiggler.Start();
                Process.AudioSession.Play("event:/char/badeline/booster_reappear", Position, Process.SaveContext);
                ((OriginalLevelSceneRoot)Scene).Particles.Emit(shineParticle, 25, Center, Vector2.One * 8f);
            }

            base.Update();
            if (Visible && ((OriginalLevelSceneRoot)Scene).OnInterval(0.1f))
            {
                ((OriginalLevelSceneRoot)Scene).Particles.Emit(shineParticle, 1, Center, Vector2.One * 8f);
            }
        }
    }
}
