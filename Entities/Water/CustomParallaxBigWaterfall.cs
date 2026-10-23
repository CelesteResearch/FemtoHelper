using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Visibility;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Presentation.PostProcessing.Displacement;
using Celeste.Simulation.Rendering.Waterfalls;

namespace Celeste.Mods.FemtoHelper.Entities.Water
{
    [CustomEntity("FemtoHelper/CustomParallaxBigWaterfall")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("CustomParallaxBigWaterfall")]
    public class CustomParallaxBigWaterfall : SceneEntityState, ISourceEngineContextEntity
    {
        private enum Layers
        {
            Fg,
            Bg
        }

        private enum DisplacementType
        {
            None,
            Vanilla,
            Custom
        }

        private readonly ISourceEngineContext engine;
        ISourceEngineContext ISourceEngineContextEntity.EngineContext => engine;
        private readonly Layers layer;
        private readonly DisplacementType displacementType;
        private readonly float width;
        private readonly float height;
        private readonly float parallax;
        private readonly float fallSpeedMultiplier;
        private readonly List<float> lines = new();
        private readonly PackedColor surfaceColor;
        private readonly PackedColor fillColor;
        private float sine;
        private readonly SoundSourceComponentState loopingSfx;
        private float fade;
        private readonly bool smooth;
        public bool UseDisplacement => displacementType != DisplacementType.None;

        private Vector2 RenderPosition =>
            RenderPositionAtCamera(((OriginalLevelSceneRoot)Scene).Camera.Position + new Vector2(160f, 90f));

        public CustomParallaxBigWaterfall(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate.Value;
            layer = EntityAttributeReader.Enum(data.Values, "layer", Layers.Bg);
            width = data.Width;
            height = data.Height;
            fallSpeedMultiplier = data.Float("fallSpeedMultiplier");
            float surfaceOpacity = data.Float("surfaceOpacity");
            float fillOpacity = data.Float("fillOpacity");
            Depth = data.Int("depth", layer == Layers.Fg ? -49900 : 10010);
            smooth = data.Bool("smooth", layer == Layers.Fg);
            if (!data.Bool("silent", layer == Layers.Bg))
            {
                Add(loopingSfx = ((ISourceSoundSourceEnvironment)engine).SoundSources.Create());
                loopingSfx.Play("event:/env/local/waterfall_big_main");
            }

            displacementType = EntityAttributeReader.Enum(data.Values, "displacementType",
                layer == Layers.Fg ? DisplacementType.Vanilla : DisplacementType.None);
            if (UseDisplacement) Add(new DisplacementRenderHookState(RenderDisplacement));
            if (displacementType == DisplacementType.Vanilla)
            {
                lines.Add(3f);
                lines.Add(width - 4f);
            }
            else
            {
                lines.Add(6f);
                lines.Add(width - 7f);
            }

            parallax = data.Float("parallax");
            surfaceColor = SourceHexColor.HexToColor(data.Attr("surfaceColor")) * surfaceOpacity;
            fillColor = SourceHexColor.HexToColor(data.Attr("fillColor")) * fillOpacity;
            fade = 1f;
            Add(new RoomTransitionListener { OnIn = value => fade = value, OnOut = value => fade = 1f - value });
            if (!(width > 16f)) return;
            ISourceRandom random = ((ISourceRandomEnvironment)engine).Random;
            int count = random.Next((int)(width / 16f));
            for (int index = 0; index < count; index++) lines.Add(8f + SourceRandomMath.NextFloat(random, width - 16f));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (((OriginalLevelSceneRoot)Scene).Transitioning) fade = 0f;
        }

        public Vector2 RenderPositionAtCamera(Vector2 camera)
        {
            Vector2 value = Position + new Vector2(width, height) / 2f - camera;
            Vector2 zero = Vector2.Zero;
            zero -= value * (1f - parallax);
            return Position + zero;
        }

        public void RenderDisplacement()
        {
            float x = RenderPosition.X;
            if (!EverestCullHelper.IsRectangleVisible(engine, x, Y, width, height)) return;
            var scope = (ISourceDrawScope)((ISourceRenderEnvironment)engine).CurrentContext;
            if (displacementType == DisplacementType.Vanilla)
            {
                SourcePrimitiveDrawing.Rect(scope, x, Y, width, height, new PackedColor(.5f, .5f, 1f, 1f));
                return;
            }

            if (displacementType == DisplacementType.None) return;
            Vector2 camera = ((OriginalLevelSceneRoot)Scene).Camera.Position;
            int segmentLength = smooth ? 1 : 3;
            float startY = Math.Max(Y, (float)Math.Floor(camera.Y / segmentLength) * segmentLength);
            float endY = Math.Min(Y + height, camera.Y + 180f);
            WaterfallSurfaceDrawing.WavyDisplacement(scope, x, width, startY, endY, segmentLength, sine);
        }

        public override void Update()
        {
            sine += engine.DeltaTime * fallSpeedMultiplier;
            if (loopingSfx != null)
            {
                Vector2 camera = ((OriginalLevelSceneRoot)Scene).Camera.Position;
                loopingSfx.Position = new Vector2(RenderPosition.X - X,
                    ScalarMath.Clamp(camera.Y + 90f, Y, height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic) -
                    Y);
            }

            base.Update();
        }

        public override void Render(ISourceRenderContext context)
        {
            float x = RenderPosition.X;
            if (!EverestCullHelper.IsRectangleVisible(engine, x, Y, width, height)) return;
            var scope = (ISourceDrawScope)context;
            PackedColor fill = fillColor * fade;
            PackedColor surface = surfaceColor * fade;
            SourcePrimitiveDrawing.Rect(scope, x, Y, width, height, fill);
            if (UseDisplacement)
            {
                float overdraw = displacementType == DisplacementType.Custom ? 0f : 1f;
                WaterfallSurfaceDrawing.Straight(scope, x, x - overdraw, x + (width - 3f) + overdraw, Y, height, lines,
                    surface);
                return;
            }

            Vector2 camera = ((OriginalLevelSceneRoot)Scene).Camera.Position;
            int segmentLength = smooth ? 1 : 3;
            float startY = Math.Max(Y, (float)Math.Floor(camera.Y / segmentLength) * segmentLength);
            float endY = Math.Min(Y + height, camera.Y + 180f);
            WaterfallSurfaceDrawing.Wavy(scope, x, width, startY, endY, segmentLength, sine, lines, surface);
        }
    }
}
