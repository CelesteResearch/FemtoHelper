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
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.FemtoHelper.CustomFakeHeart")]
    [CustomEntity("FemtoHelper/CustomFakeHeart")]
    public partial class CustomFakeHeart : SceneEntityState
    {
        public static CustomFakeHeart Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        private readonly ISourceEngineContext engine;
        private IOriginalGameplayProcess Process => (IOriginalGameplayProcess)engine;
        private ISourceGameGraphicsResources Graphics => ((ISourceGameGraphicsEnvironment)engine).GameGraphics;
        private WigglerComponentPool WigglerPool => ((ISourceWigglerPoolScope)engine).WigglerPool;

        private readonly float respawnTime = 3f;

        private readonly int heartBehavior = 0;

        private readonly bool flagSet = true;

        private readonly string customFlag = "custom_flag";

        private readonly bool fakeSounds = false;

        private readonly bool doesPulse = true;

        private readonly float freezeTime = 0.1f;

        private readonly string audioEvent = "event:/game/06_reflection/boss_spikes_burst";

        private readonly float flashOpacity = 1f;

        private readonly string flashColor = "ffffff";

        private readonly int particleCount = 12;

        private readonly float particleSpawnSpread = 8f;

        private readonly float burstDuration = 1.5f;

        private readonly float burstRadiusFrom = 4f;

        private readonly float burstRadiusTo = 40f;

        private readonly float burstAlpha = 0.5f;

        private Sprite sprite;

        private ParticleTypePresentation shineParticle;

        private readonly ParticleTypePresentation customParticle;

        public WigglerComponent ScaleWiggler;

        private WigglerComponent moveWiggler;

        private Vector2 moveWiggleDir;

        private BloomPointState bloom;

        private readonly HoldableColliderState crystalCollider;

        private float timer;

        private float bounceSfxDelay;

        private float respawnTimer;

        private readonly bool miniMode;

        private readonly string shineParticleColor;

        private readonly string spriteDirectory;

        private readonly bool removeBloomOnShatter;

        private readonly bool nobloom;

        private readonly bool breakOnContact;

        private readonly bool noWiggle;

        private readonly bool silentPulse;

        public readonly bool CollectFlash;

        public readonly bool FlashOverPlayer;

        // Legacy
        private readonly int heartColor = 0;

        public CustomFakeHeart(ISourceEngineContext engine, Vector2 position)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            OwnedComponents.Add(crystalCollider = new HoldableColliderState(OnHoldable));
            OwnedComponents.Add(new MirrorReflectionState());
        }

        public CustomFakeHeart(EntityData data, Vector2 offset, ISourceEngineContext engine) : this(engine,
            data.Position + offset)
        {
            respawnTime = EntityAttributeReader.Float(data.Values, "respawnTime");
            heartBehavior = EntityAttributeReader.Int(data.Values, "heartBehavior");
            flagSet = EntityAttributeReader.Bool(data.Values, "setFlag");
            customFlag = EntityAttributeReader.Attr(data.Values, "flag");
            fakeSounds = EntityAttributeReader.Bool(data.Values, "useFakeSounds");
            doesPulse = EntityAttributeReader.Bool(data.Values, "pulse");
            freezeTime = EntityAttributeReader.Float(data.Values, "freezeTime");
            audioEvent = EntityAttributeReader.Attr(data.Values, "audioEvent");
            flashOpacity = EntityAttributeReader.Float(data.Values, "flashOpacity");
            flashColor = EntityAttributeReader.Attr(data.Values, "flashColor");
            particleCount = EntityAttributeReader.Int(data.Values, "particleCount");
            particleSpawnSpread = EntityAttributeReader.Float(data.Values, "particleSpawnSpread");
            burstDuration = EntityAttributeReader.Float(data.Values, "burstDuration");
            burstRadiusFrom = EntityAttributeReader.Float(data.Values, "burstRadiusFrom");
            burstRadiusTo = EntityAttributeReader.Float(data.Values, "burstRadiusTo");
            burstAlpha = EntityAttributeReader.Float(data.Values, "burstAlpha");
            miniMode = EntityAttributeReader.Bool(data.Values, "miniHeartMode", false);
            nobloom = EntityAttributeReader.Bool(data.Values, "noBloom", false);
            breakOnContact = EntityAttributeReader.Bool(data.Values, "breakOnContact", false);
            noWiggle = EntityAttributeReader.Bool(data.Values, "noWiggle", false);
            silentPulse = EntityAttributeReader.Bool(data.Values, "silentPulse", false);
            CollectFlash = EntityAttributeReader.Bool(data.Values, "collectFlash", true);
            FlashOverPlayer = EntityAttributeReader.Bool(data.Values, "flashOverPlayer", false);

            customParticle = new ParticleTypePresentation
            {
                Source = Graphics.GameAtlas[EntityAttributeReader.Attr(data.Values, "particleTexture")],
                Color = PackedColor.FromHex(EntityAttributeReader.Attr(data.Values, "particleColor")),
                Color2 = PackedColor.FromHex(EntityAttributeReader.Attr(data.Values, "particleColor2")),
                FadeMode =
                    EntityAttributeReader.Int(data.Values, "particleFadeMode") == 0
                        ? ParticleTypePresentation.FadeModes.None
                        : EntityAttributeReader.Int(data.Values, "particleFadeMode") == 1
                            ? ParticleTypePresentation.FadeModes.Linear
                            : EntityAttributeReader.Int(data.Values, "particleFadeMode") == 2
                                ? ParticleTypePresentation.FadeModes.Late
                                : ParticleTypePresentation.FadeModes.InAndOut,
                ColorMode =
                    EntityAttributeReader.Int(data.Values, "particleColorMode") == 0
                        ? ParticleTypePresentation.ColorModes.Static
                        : EntityAttributeReader.Int(data.Values, "particleColorMode") == 1
                            ? ParticleTypePresentation.ColorModes.Choose
                            : EntityAttributeReader.Int(data.Values, "particleColorMode") == 2
                                ? ParticleTypePresentation.ColorModes.Blink
                                : ParticleTypePresentation.ColorModes.Fade,
                RotationMode =
                    EntityAttributeReader.Int(data.Values, "particleRotationMode") == 0
                        ? ParticleTypePresentation.RotationModes.None
                        : EntityAttributeReader.Int(data.Values, "particleRotationMode") == 1
                            ? ParticleTypePresentation.RotationModes.Random
                            : ParticleTypePresentation.RotationModes.SameAsDirection,
                LifeMin = EntityAttributeReader.Float(data.Values, "particleLifespanMin"),
                LifeMax = EntityAttributeReader.Float(data.Values, "particleLifespanMax"),
                Size = EntityAttributeReader.Float(data.Values, "particleSize"),
                SizeRange = EntityAttributeReader.Float(data.Values, "particleSizeRange"),
                ScaleOut = EntityAttributeReader.Bool(data.Values, "particleScaleOut"),
                SpeedMin = EntityAttributeReader.Float(data.Values, "particleSpeedMin"),
                SpeedMax = EntityAttributeReader.Float(data.Values, "particleSpeedMax"),
                Friction = EntityAttributeReader.Float(data.Values, "particleFriction"),
                Acceleration = new Vector2(EntityAttributeReader.Float(data.Values, "particleAccelX"),
                    EntityAttributeReader.Float(data.Values, "particleAccelY")),
                SpinFlippedChance = EntityAttributeReader.Bool(data.Values, "particleFlipChance"),
                SpinMin = EntityAttributeReader.Float(data.Values, "particleSpinSpeedMin"),
                SpinMax = EntityAttributeReader.Float(data.Values, "particleSpinSpeedMax")
            };

            // New
            removeBloomOnShatter = EntityAttributeReader.Bool(data.Values, "removeBloomOnShatter", false);
            spriteDirectory = EntityAttributeReader.Attr(data.Values, "spriteDirectory", "collectables/heartGem/0");
            shineParticleColor = EntityAttributeReader.Attr(data.Values, "shineParticleColor", "5caefa");

            // Legacy
            heartColor = EntityAttributeReader.Int(data.Values, "heartColor", -1);
            if (heartColor != -1)
            {
                heartColor = ScalarMath.Clamp(heartColor, 0, 2);
            }
        }
    }
}
