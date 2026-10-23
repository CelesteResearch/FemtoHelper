using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Mods.FemtoHelper.Runtime.Geometry;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FemtoHelper.Entities.Geometry
{
    [CustomEntity("FemtoHelper/PseudoPolyhedron = Load")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.FemtoHelper.PseudoPolyhedron")]
    public sealed class PseudoPolyhedron : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly ISourceVectorArithmetic vectors;
        private readonly PolyhedronEngineState state;
        public float Timer;
        public readonly float RotationSpeed;
        public readonly float BaseWidth;
        public readonly float BaseHeight;
        public Vector2 TipBaseOffset;
        public PackedColor Color;
        public readonly int BaseSides;
        public readonly bool PrismMode;
        public readonly float Alpha;
        public readonly Vector2[,] Positions;
        public int Samples => state.Samples;
        public Vector2 TopBaseSize;
        public Vector2 TopScale;

        public static PseudoPolyhedron Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public PseudoPolyhedron(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            state = PolyhedronEngineState.For(engine);
            RotationSpeed = data.Float("rotationSpeed", 30f);
            Timer = data.Float("rotationOffset", 0f);
            BaseWidth = data.Float("baseWidth", 10f);
            BaseHeight = data.Float("baseHeight", 3f);
            TipBaseOffset = new Vector2(data.Float("tipBaseOffsetX", 0f), data.Float("tipBaseOffsetY", -14f));
            TopBaseSize = new Vector2(data.Float("topBaseWidth", BaseWidth), data.Float("topBaseHeight", BaseHeight));
            Color = new PackedColor(SourceColorParsing.HexToColor(data.Attr("color", "ffffff")));
            BaseSides = data.Int("baseSideCount", 4);
            PrismMode = data.Bool("isPrism", false);
            Alpha = data.Float("alpha", 1);
            state.Samples = Math.Max(data.Int("samples", 128), 2);
            Depth = data.Int("depth", 6000);
            Positions = new Vector2[Samples, BaseSides];
            Vector2 originalScale = vectors.Normalize(new Vector2(BaseWidth, BaseHeight));
            TopScale = originalScale + new Vector2(TopBaseSize.X, TopBaseSize.Y);
            float angle = 0;
            for (int sample = 0; sample < Positions.GetLength(0); sample++)
            {
                angle += (float)Math.PI * 2 / Samples / BaseSides;
                for (float side = 0; side < BaseSides; side++)
                    Positions[sample, (int)side] = new Vector2(
                        (float)Math.Sin(angle + Math.PI / ((float)BaseSides / 2) * side) * BaseWidth,
                        (float)Math.Cos(angle + Math.PI / ((float)BaseSides / 2) * side) * BaseHeight);
            }
        }

        // Calc.Max(params float[]) passes the second value as MathHelper.Max's first operand.
        private static float CalcMaximum(float first, float second) => second > first ? second : first;

        public override void Update()
        {
            Timer += RotationSpeed * engine.DeltaTime * Samples / 15;
            var camera = ((OriginalLevelSceneRoot)Scene).Camera;
            if (Position.X > camera.X + 320 + CalcMaximum(BaseWidth * 2, Math.Abs(TipBaseOffset.X * 2)) ||
                Position.X < camera.X - CalcMaximum(BaseWidth * 2, Math.Abs(TipBaseOffset.X * 2)) ||
                Position.Y > camera.Y + 180 + CalcMaximum(BaseHeight * 2, Math.Abs(TipBaseOffset.Y * 2)) ||
                Position.Y < camera.Y - CalcMaximum(BaseHeight * 2, Math.Abs(TipBaseOffset.Y * 2)))
                Visible = false;
            else Visible = true;
        }

        public override void Render(ISourceRenderContext context)
        {
            int arrayTimer = (int)(Timer % Samples + Samples) % Samples;
            if (PrismMode)
            {
                float xScale = 1 / BaseWidth * TopBaseSize.X;
                float yScale = 1 / BaseHeight * TopBaseSize.Y;
                for (int side = 0; side < BaseSides; side++)
                {
                    SourcePrimitiveDrawing.Line(context, this, Position + Positions[arrayTimer, side],
                        Position + Positions[arrayTimer, (side + 1) % BaseSides], Color * Alpha, vectors);
                    SourcePrimitiveDrawing.Line(context, this,
                        Position + TipBaseOffset + Positions[arrayTimer, side] * new Vector2(xScale, yScale),
                        Position + TipBaseOffset +
                        Positions[arrayTimer, (side + 1) % BaseSides] * new Vector2(xScale, yScale),
                        Color * Alpha, vectors);
                    SourcePrimitiveDrawing.Line(context, this, Position + Positions[arrayTimer, side],
                        Position + TipBaseOffset + Positions[arrayTimer, side] * new Vector2(xScale, yScale),
                        Color * Alpha, vectors);
                }
            }
            else
                for (int side = 0; side < BaseSides; side++)
                {
                    SourcePrimitiveDrawing.Line(context, this, Position + Positions[arrayTimer, side],
                        Position + Positions[arrayTimer, (side + 1) % BaseSides], Color * Alpha, vectors);
                    SourcePrimitiveDrawing.Line(context, this, Position + Positions[arrayTimer, side],
                        Position + TipBaseOffset, Color * Alpha, vectors);
                }
        }
    }
}
