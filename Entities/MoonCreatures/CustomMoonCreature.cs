using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Randomness;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Gameplay.Decoration.MoonCreatures;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Physics.Collision.Shapes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Color = Celeste.Core.Display.PackedColor;

namespace Celeste.Mods.FemtoHelper.Entities.MoonCreatures
{
    [CustomEntity("FemtoHelper/CustomMoonCreature")]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("CustomMoonCreature")]
    public class CustomMoonCreature : SceneEntityState
    {
        public static CustomMoonCreature Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(data, offset, (IOriginalMoonCreatureProcess)level.EngineContext);

        public struct TrailNode : IMoonCreatureTrailNode
        {
            public Vector2 Position;

            public Color Color;

            Vector2 IMoonCreatureTrailNode.TrailPosition
            {
                get => Position;
                set => Position = value;
            }

            Color IMoonCreatureTrailNode.TrailColor => Color;
        }

        private readonly IOriginalMoonCreatureProcess engine;
        private readonly MoonCreatureMotionSettings settings;

        private Color LerpColor(Color a, Color b, float amount) =>
            SourceColorInterpolation.Lerp(a, b, amount, engine.ScalarArithmetic);

        public readonly TrailNode[] Trail;

        public Vector2 Start;

        public Vector2 Target;

        private float targetTimer;

        private Vector2 speed;

        private Vector2 bump;

        private RoomPlayerSession following;

        private Vector2 followingOffset;

        private float followingTime;

        private Color orbColor;

        private readonly Color centerColor;

        private readonly float acceleration = 90f;

        private readonly float followAcceleration = 120f;

        private readonly float maxSpeed = 40f;

        private readonly float maxFollowSpeed = 70f;

        private readonly float maxFollowDistance = 200f;

        private readonly float minFollowTime = 6f;

        private readonly float maxFollowTime = 12f;

        private readonly int spawn;

        private IntRect originLevelBounds;

        private readonly bool tintSprite;

        private readonly int trailCount;

        private readonly float trailBaseSize;

        private readonly float trailCatchupSpeed;

        private readonly float trailSpacing;

        private readonly float targetRangeRadius;

        private readonly float minTargetTime;

        private readonly float maxTargetTime;

        private readonly float trailBaseAlpha;

        private readonly float trailTipAlpha;

        private readonly float trailGravity;

        private readonly BloomPointState bloom;

        private readonly VertexLightState light;

        public CustomMoonCreature(EntityData data, Vector2 offset, IOriginalMoonCreatureProcess engine) : this(
            data.Position + offset, engine)
        {
            Sprite sprite;
            spawn = EntityAttributeReader.Int(data.Values, "number", 1) - 1;
            string spritePath = EntityAttributeReader.Attr(data.Values, "sprite", "moonCreatureTiny");
            tintSprite = EntityAttributeReader.Bool(data.Values, "tintSprite", false);
            trailCount = EntityAttributeReader.Int(data.Values, "trailCount", 10);
            trailBaseSize = EntityAttributeReader.Float(data.Values, "trailBaseSize", 3f);
            acceleration = EntityAttributeReader.Float(data.Values, "acceleration", 90f);
            followAcceleration = EntityAttributeReader.Float(data.Values, "followAcceleration", 120f);
            maxSpeed = EntityAttributeReader.Float(data.Values, "maxSpeed", 40f);
            maxFollowSpeed = EntityAttributeReader.Float(data.Values, "maxFollowSpeed", 70f);
            maxFollowDistance = EntityAttributeReader.Float(data.Values, "maxFollowDistance", 200f);
            minFollowTime = EntityAttributeReader.Float(data.Values, "minFollowTime", 6f);
            maxFollowTime = EntityAttributeReader.Float(data.Values, "maxFollowTime", 12f);
            trailCatchupSpeed = EntityAttributeReader.Float(data.Values, "trailCatchupSpeed", 128f);
            trailSpacing = EntityAttributeReader.Float(data.Values, "trailSpacing", 2f);
            targetRangeRadius = EntityAttributeReader.Float(data.Values, "targetRangeRadius", 32f);
            minTargetTime = EntityAttributeReader.Float(data.Values, "minTargetTime", 0.8f);
            maxTargetTime = EntityAttributeReader.Float(data.Values, "maxTargetTime", 4f);
            trailBaseAlpha = EntityAttributeReader.Float(data.Values, "trailBaseAlpha", 1f);
            trailTipAlpha = EntityAttributeReader.Float(data.Values, "trailTipAlpha", 1f);
            trailGravity = EntityAttributeReader.Float(data.Values, "trailGravity", 0.05f);
            settings = new MoonCreatureMotionSettings(acceleration, followAcceleration, maxSpeed, maxFollowSpeed,
                maxFollowDistance, minFollowTime, maxFollowTime, targetRangeRadius, minTargetTime, maxTargetTime,
                Math.Max(maxTargetTime / 2, minTargetTime));
            List<Color> colors =
            [
                .. EntityAttributeReader.Attr(data.Values, "colors", "c34fc7,4f95c7,53c74f").Split(',')
                    .Select(SourceHexColor.HexToColor)
            ];
            centerColor = LerpColor(colors[engine.Random.Next(colors.Count)], Color.White, 0);
            orbColor = SourceHexColor.HexToColor("b0e6ff");
            //Logger.Log("a", mainColors[0].ToString());
            Color value = LerpColor(centerColor,
                SourceHexColor.HexToColor(EntityAttributeReader.Attr(data.Values, "trailSubcolor1", "bde4ee")),
                EntityAttributeReader.Float(data.Values, "trailSubcolor1LerpAmount", 0.5f));
            Color value2 = LerpColor(centerColor,
                SourceHexColor.HexToColor(EntityAttributeReader.Attr(data.Values, "trailSubcolor2", "2f2941")),
                EntityAttributeReader.Float(data.Values, "trailSubcolor2LerpAmount", 0.5f));
            Trail = new TrailNode[trailCount];
            for (int i = 0; i < trailCount; i++)
            {
                Trail[i] = new TrailNode
                {
                    Position = Position,
                    Color = LerpColor(value, value2, (float)i / (trailCount - 1)) *
                            (trailBaseAlpha - (trailBaseAlpha - trailTipAlpha) * i / trailCount)
                };
            }

            Add(sprite = engine.GameGraphics.SpriteFactory.Create(spritePath));
            if (tintSprite)
            {
                sprite.Color = centerColor;
            }

            Add(bloom = new BloomPointState(EntityAttributeReader.Float(data.Values, "bloomAlpha", 0f),
                EntityAttributeReader.Float(data.Values, "bloomRadius", 0)));
            Add(light = new VertexLightState(engine,
                !EntityAttributeReader.Bool(data.Values, "lightColorIsMainColor")
                    ? SourceHexColor.HexToColor(EntityAttributeReader.Attr(data.Values, "lightColor"))
                    : centerColor, EntityAttributeReader.Float(data.Values, "lightAlpha", 0f),
                EntityAttributeReader.Int(data.Values, "lightStartFade", 12),
                EntityAttributeReader.Int(data.Values, "lightEndFade", 24)));
        }

        private CustomMoonCreature(Vector2 position, IOriginalMoonCreatureProcess engine) : base(
            engine.EntitySourceDataRegistration)
        {
            this.engine = engine;
            Tags = engine.Tags.TransitionUpdate.Value;
            Depth = -13010;
            Collider = new HitboxColliderGeometry(20f, 20f, -10f, -10f);
            Start = position;
            targetTimer = 0f;
            GetRandomTarget();
            Position = Target;
            Add(new PlayerColliderComponentState(OnPlayer));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            for (int i = 0; i < spawn; i++)
            {
                ((OriginalLevelSceneRoot)scene).SourceScene.QueueSourceEntity(
                    new OriginalMoonCreature(
                        Position + new Vector2(SourceRandomMath.Range(engine.Random, -4, 4),
                            SourceRandomMath.Range(engine.Random, -4, 4)), engine));
            }

            originLevelBounds = ((OriginalLevelSceneRoot)scene).Bounds;
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            MoonCreatureSimulation.OnPlayer(engine, Position, Start, player, in settings, ref bump, ref following,
                ref followingTime, ref followingOffset);
        }

        private void GetFollowOffset()
        {
            MoonCreatureSimulation.GetFollowOffset(engine, ref followingOffset);
        }

        private void GetRandomTarget()
        {
            MoonCreatureSimulation.GetRandomTarget(engine, Start, ref Target, targetRangeRadius,
                targetRangeRadius / 4f);
        }

        public override void Update()
        {
            base.Update();
            MoonCreatureSimulation.Update(engine, this, Start, ref Target, ref targetTimer, ref speed, ref bump,
                ref following, ref followingTime, ref followingOffset, in settings);
            MoonCreatureSimulation.UpdateTrail(engine, this, Trail, trailGravity, trailSpacing, trailCatchupSpeed);
            X = ScalarMath.Clamp(X, originLevelBounds.Left + 4, originLevelBounds.Right - 4);
            Y = ScalarMath.Clamp(Y, originLevelBounds.Top + 4, originLevelBounds.Bottom - 4);
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 position = Position;
            Position = Vector2Geometry.Floor(Position);
            MoonCreatureSimulation.RenderTrail(context, this, Trail, trailBaseSize);
            base.Render(context);
            Position = position;
        }

        public override void DebugRender(ISourceRenderContext context, object camera)
        {
            base.DebugRender(context, camera);
            SourcePrimitiveDrawing.Line(context, this, Position, Target, Color.White * .5f, engine.VectorArithmetic);
            SourcePrimitiveDrawing.Rect(context, this, Target - new Vector2(2, 2), 4, 4, centerColor * .5f);
            SourcePrimitiveDrawing.Circle(context, this, Start, targetRangeRadius, centerColor * .5f, 16,
                engine.VectorArithmetic);
        }
    }
}
