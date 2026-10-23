using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FemtoHelper.Particles
{
    [CustomEntity("FemtoHelper/ParticleEmitter")]
    [SceneTracked]
    public sealed class ParticleEmitter : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        public readonly ParticleTypePresentation EmitterParticle;
        public readonly float ParticleSpawnSpread;
        public float SpawnTimer;
        public readonly int ParticleCount;
        public readonly float SpawnInterval;
        public readonly float SpawnChance;
        public readonly float ParticleAngle;
        public readonly float ParticleAngleRange;
        public readonly bool IsFg;
        public readonly string Flag;
        public readonly string Tag;
        public readonly bool AttachToPlayer;
        public readonly Vector2 AttachToPlayerOffset;

        public ParticleEmitter(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            OwnedComponents.Add(new BloomPointState(data.Float("BloomAlpha"), data.Float("BloomRadius", 6)));
            ParticleSpawnSpread = data.Float("particleSpawnSpread");
            ParticleCount = data.Int("particleCount");
            SpawnInterval = data.Float("spawnInterval");
            SpawnChance = data.Float("spawnChance");
            ParticleAngle = data.Float("particleAngle");
            ParticleAngleRange = data.Float("particleAngleRange");
            AttachToPlayer = data.Bool("attachToPlayer");
            AttachToPlayerOffset =
                new Vector2(data.Float("attachToPlayerOffsetX"), data.Float("attachToPlayerOffsetY"));
            IsFg = data.Bool("foreground");
            Flag = data.Attr("flag", "");
            Tag = data.Attr("tag", "");
            // The source resolves the chooser even when noTexture is true.
            var chooser = new Chooser<AtlasEntry>();
            foreach (var path in data.Attr("particleTexture", "").Split(','))
                chooser.Add(((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[path], 1);
            EmitterParticle = new ParticleTypePresentation
            {
                SourceChooser = data.Bool("noTexture") ? null : chooser,
                Color = SourceColorParsing.HexToPackedColorWithAlpha(data.Attr("particleColor", "40FF9080")) *
                        data.Float("particleAlpha"),
                Color2 = SourceColorParsing.HexToPackedColorWithAlpha(data.Attr("particleColor2", "20408000")) *
                         data.Float("particleAlpha"),
                FadeMode = (ParticleTypePresentation.FadeModes)Mode(data.Int("particleFadeMode"), 3),
                ColorMode = (ParticleTypePresentation.ColorModes)Mode(data.Int("particleColorMode"), 3),
                RotationMode = (ParticleTypePresentation.RotationModes)Mode(data.Int("particleRotationMode"), 2),
                LifeMin = data.Float("particleLifespanMin"), LifeMax = data.Float("particleLifespanMax"),
                Size = data.Float("particleSize"), SizeRange = data.Float("particleSizeRange"),
                ScaleOut = data.Bool("particleScaleOut"), SpeedMin = data.Float("particleSpeedMin"),
                SpeedMax = data.Float("particleSpeedMax"), Friction = data.Float("particleFriction"),
                Acceleration = new Vector2(data.Float("particleAccelX"), data.Float("particleAccelY")),
                SpinFlippedChance = data.Bool("particleFlipChance"), SpinMin = data.Float("particleSpinSpeedMin"),
                SpinMax = data.Float("particleSpinSpeedMax")
            };
        }

        private static int Mode(int value, int fallback) =>
            value == 0 ? 0 : value == 1 ? 1 : fallback == 3 && value == 2 ? 2 : fallback;

        public override void Update()
        {
            base.Update();
            var level = (OriginalLevelSceneRoot)Scene;
            if (AttachToPlayer)
            {
                var player =
                    level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>();
                if (player != null) Position = player.Position + AttachToPlayerOffset;
            }

            SpawnTimer += 1;
            if (!Visible || !level.OnInterval(SpawnInterval)) return;
            if (Flag.StartsWith("!", StringComparison.Ordinal)
                    ? level.Session.GetFlag(Flag.Substring(1))
                    : !string.IsNullOrEmpty(Flag) && !level.Session.GetFlag(Flag)) return;
            var random = ((ISourceRandomEnvironment)engine).Random;
            for (var i = 0; i < ParticleCount; i++)
            {
                if (!(SourceRandomMath.NextFloat(random) < SpawnChance / 100)) continue;
                var range = ParticleAngleRange / 360 * ((float)Math.PI * 2);
                var angle = ParticleAngle / 360 * ((float)Math.PI * 2) +
                            (SourceRandomMath.NextFloat(random) * range - range / 2);
                (IsFg ? level.ParticlesFG : level.ParticlesBG).Emit(EmitterParticle, 1, Center,
                    Vector2.One * ParticleSpawnSpread, angle);
            }
        }

        public static bool CheckFlag(string flag, Func<string, bool> getFlag) =>
            flag.StartsWith("!", StringComparison.Ordinal)
                ? !getFlag(flag.Substring(1))
                : string.IsNullOrEmpty(flag) || getFlag(flag);

        public override void DebugRender(ISourceRenderContext context, object camera)
        {
            base.DebugRender(context, camera);
            SourcePrimitiveDrawing.HollowRect(context, this, Position.X, Position.Y, 4f, 4f, PackedColor.Purple);
        }
    }
}
