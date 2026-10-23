using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Randomness;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using MTexture = Celeste.Graphics.Content.Atlas.AtlasEntry;

namespace Celeste.Mods.FemtoHelper.Entities.Environment.Flags
{
    [CustomEntity("FemtoHelper/CustomCliffsideWindFlag")]
    [RuntimeTypeAlias("CustomCliffsideWindFlag")]
    public class CustomCliffsideWindFlag : Entity
    {
        private readonly ISourceEngineContext engine;

        private class Segment
        {
            public MTexture Texture;

            public Vector2 Offset;
        }

        private readonly Segment[] segments;

        private float sine;

        private readonly float random;

        private int sign;

        private readonly float sineFrequency;

        private readonly float sineAmplitude;

        private readonly float naturalDraft;

        private float LevelWind => ScalarMath.ClampedMap(Math.Abs((Scene as Level).Wind.X), 0f, 800f);

        private float NaturalWind => ScalarMath.ClampedMap(Math.Abs(naturalDraft), 0f, 800f);

        public CustomCliffsideWindFlag(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            naturalDraft = data.Float("naturalDraft", 0f);
            sineFrequency = (float)Math.Max(data.Float("sineFrequency", 1f), 0.0001);
            sineAmplitude = data.Float("sineAmplitude", 1f);
            MTexture atlasSubtexturesAt =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtexturesAt(
                    data.Attr("spritesPath", "scenery/cliffside/flag"), data.Int("index"));
            segments = new Segment[atlasSubtexturesAt.FrameWidth];
            for (int i = 0; i < segments.Length; i++)
            {
                Segment segment = new()
                {
                    Texture = atlasSubtexturesAt.GetSubtexture(i, 0, 1, atlasSubtexturesAt.FrameHeight),
                    Offset = new Vector2(i, 0f)
                };
                segments[i] = segment;
            }

            sine = SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, (float)Math.PI * 2f);
            random = SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random);
            Depth = data.Int("depth", 8999);
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate.Value;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            sign = 1;
            float windValue = 0f;
            if (LevelWind != 0f)
            {
                windValue = LevelWind;
                sign = Math.Sign(((Level)Scene).Wind.X);
            }
            else if (naturalDraft != 0f)
            {
                windValue = NaturalWind;
                sign = Math.Sign(naturalDraft);
            }

            for (int i = 0; i < segments.Length; i++)
            {
                SetFlagSegmentPosition(i, windValue, snap: true);
            }
        }

        public override void Update()
        {
            base.Update();
            float windValue = 0f;
            if (LevelWind != 0f)
            {
                windValue = LevelWind;
                sign = Math.Sign(((Level)Scene).Wind.X);
            }
            else if (naturalDraft != 0f)
            {
                windValue = NaturalWind;
                sign = Math.Sign(naturalDraft);
            }

            sine += engine.DeltaTime * (4f + windValue * 4f) * (0.8f + random * 0.2f);
            for (int i = 0; i < segments.Length; i++)
            {
                SetFlagSegmentPosition(i, windValue, snap: false);
            }
        }

        private float Sin(float timer)
        {
            return (float)Math.Sin(0f - timer / sineFrequency) * sineAmplitude;
        }

        private void SetFlagSegmentPosition(int i, float windValue, bool snap)
        {
            Segment segment = segments[i];
            float value = i * sign * (0.2f + windValue * 0.8f * (0.8f + random * 0.2f)) * (0.9f + Sin(sine) * 0.1f);
            float num = ScalarMath.LerpClamp(Sin(sine * 0.5f - i * 0.1f) * (i / (float)segments.Length) * i * 0.2f,
                value, (float)Math.Ceiling(windValue), ((ISourceScalarEnvironment)engine).ScalarArithmetic);
            float num2 = i / (float)segments.Length * Math.Max(0.1f, 1f - windValue) * 16f;
            if (!snap)
            {
                segment.Offset.X = ScalarMath.Approach(segment.Offset.X, num, engine.DeltaTime * 40f,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                segment.Offset.Y = ScalarMath.Approach(segment.Offset.Y, num2, engine.DeltaTime * 40f,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            }
            else
            {
                segment.Offset.X = num;
                segment.Offset.Y = num2;
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            for (int i = 0; i < segments.Length; i++)
            {
                Segment segment = segments[i];
                float scaleFactor = i / (float)segments.Length * Sin(-i * 0.1f + sine) * 2f;
                SourceAtlasDrawing.Draw(context, this, segment.Texture,
                    Position + segment.Offset + Vector2.UnitY * scaleFactor);
            }
        }
    }
}
