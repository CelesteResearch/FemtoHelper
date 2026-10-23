// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.FemtoHelper.Backdrops.Stars
{
    [RuntimeTypeAlias("FemtoStars")]
    public class FemtoStars : BackdropPresentation
    {
        private struct Star
        {
            public Vector2 Position;
            public int TextureSet;
            public float Timer;
            public float Rate;
            public PackedColor[] Colors;
            public PackedColor MainColor;
            public Vector2 Speed;
        }

        private readonly ISourceEngineContext engine;
        private readonly ISourceVectorArithmetic vectors;
        private readonly Star[] stars;
        private readonly PackedColor[] colors2;
        private readonly List<AtlasEntry>[] textures;
        private readonly Vector2 center;
        private readonly int trailCount;
        private readonly float extraLoopBorderX;
        private readonly float extraLoopBorderY;
        private readonly PackedColor backdropColor;
        private readonly float backdropAlpha;
        private readonly float parallaxX;
        private readonly float parallaxY;
        public float AlphaLegacy;
        public readonly string Alpha;
        private readonly float separation;

        private OriginalGameplayBuffers Buffers =>
            ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics).Buffers;

        public int GameplayBufferWidth => Buffers.Gameplay?.Width ?? 320;
        public int GameplayBufferHeight => Buffers.Gameplay?.Height ?? 180;

        public FemtoStars(ISourceEngineContext engine, int blurCount, string colors3, float minXSpeed, float maxXSpeed,
            float minYSpeed, float maxYSpeed, float loopBorderX, float loopBorderY, int count, string backgroundColor,
            float backgroundAlpha, string sprite, float scrollX, float scrollY, float transparency,
            float trailSeparation, float animationRate, string transp2, float animationRateRandom)
        {
            this.engine = engine;
            trailCount = blurCount;
            extraLoopBorderX = loopBorderX;
            extraLoopBorderY = loopBorderY;
            backdropColor = SourceHexColor.HexToColor(backgroundColor);
            backdropAlpha = backgroundAlpha;
            parallaxX = scrollX;
            parallaxY = scrollY;
            AlphaLegacy = transparency;
            Alpha = transp2;
            separation = trailSeparation;
            var alphas = Array.ConvertAll(Alpha.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                double.Parse);
            var colorStrings = colors3.Split(',');
            colors2 = new PackedColor[colorStrings.Length];
            for (var i = 0; i < colors2.Length; i++)
                colors2[i] = SourceColorParsing.HexToPackedColorWithAlpha(colorStrings[i].Trim());
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            textures = new[]
            {
                atlas.GetAtlasSubtextures(sprite + "/a"),
                atlas.GetAtlasSubtextures(sprite + "/b"),
                atlas.GetAtlasSubtextures(sprite + "/c")
            };
            center = new Vector2(textures[0][0].FrameWidth, textures[0][0].FrameHeight) / 2f;
            stars = new Star[count];
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            var random = ((ISourceRandomEnvironment)engine).Random;
            for (var i = 0; i < stars.Length; i++)
            {
                stars[i] = new Star
                {
                    Position = new Vector2(
                        SourceRandomMath.NextFloat(random, GameplayBufferWidth + extraLoopBorderX) -
                        extraLoopBorderX / 2f,
                        SourceRandomMath.NextFloat(random, GameplayBufferHeight + extraLoopBorderY) -
                        extraLoopBorderY / 2f),
                    Timer = SourceRandomMath.NextFloat(random, (float)Math.PI * 2f),
                    Rate = animationRate + SourceRandomMath.NextFloat(random, animationRateRandom),
                    TextureSet = random.Next(textures.Length),
                    Colors = new PackedColor[trailCount]
                };
                var colorIndex = random.Next(colors2.Length);
                var alphaIndex = random.Next(alphas.Length);
                for (var j = 0; j < stars[i].Colors.Length; j++)
                    stars[i].Colors[j] = colors2[colorIndex] * (float)alphas[alphaIndex] *
                                         (1f - (float)j / stars[i].Colors.Length);
                stars[i].MainColor = colors2[colorIndex] * (float)alphas[alphaIndex];
                stars[i].Speed = new Vector2(SourceRandomMath.Range(random, minXSpeed, maxXSpeed),
                    SourceRandomMath.Range(random, minYSpeed, maxYSpeed));
            }
        }

        public override void Update(ISourceBackdropScene scene)
        {
            base.Update(scene);
            if (Visible)
                for (var i = 0; i < stars.Length; i++)
                {
                    stars[i].Timer += engine.DeltaTime * stars[i].Rate;
                    stars[i].Position += stars[i].Speed * engine.DeltaTime / 60f * 100f;
                }
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            SourcePrimitiveDrawing.Rect(context, this, 0f, 0f, GameplayBufferWidth, GameplayBufferHeight,
                backdropColor * backdropAlpha);
            for (var i = 0; i < stars.Length; i++)
            {
                var frames = textures[stars[i].TextureSet];
                var frame = (int)((Math.Sin(stars[i].Timer) + 1.0) / 2.0 * frames.Count);
                frame %= frames.Count;
                var position = stars[i].Position;
                var texture = frames[frame];
                position.Y -= scene.Camera.Y * parallaxY;
                position.X -= scene.Camera.X * parallaxX;
                position.Y %= GameplayBufferHeight + extraLoopBorderY;
                position.X %= GameplayBufferWidth + extraLoopBorderX;
                if (position.Y < 0f) position.Y += GameplayBufferHeight + extraLoopBorderY;
                if (position.X < 0f) position.X += GameplayBufferWidth + extraLoopBorderX;
                var direction = vectors.Normalize(stars[i].Speed);
                for (var j = 0; j < stars[i].Colors.Length; j++)
                    Draw(context, texture, position - new Vector2(extraLoopBorderX / 2f, extraLoopBorderY / 2f) -
                                           direction * ((float)j * separation),
                        stars[i].Colors[j] * FadeAlphaMultiplier);
                Draw(context, texture, position - new Vector2(extraLoopBorderX / 2f, extraLoopBorderY / 2f),
                    stars[i].MainColor * FadeAlphaMultiplier);
            }
        }

        private void Draw(ISourceRenderContext context, AtlasEntry texture, Vector2 position, PackedColor color)
        {
            var draw = new AtlasTextureDraw(texture, position, center, color, Vector2.One, 0f);
            ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, this);
        }

        public static FemtoStars Create(ISourceEngineContext engine, BinaryPackerElement child) => new FemtoStars(
            engine,
            child.ReadInstalledInt("trailCount", 8), child.ReadInstalledString("colors", "ffffff"),
            child.ReadInstalledFloat("minXSpeed", 0f), child.ReadInstalledFloat("maxXSpeed", 0f),
            child.ReadInstalledFloat("minYSpeed", 6f), child.ReadInstalledFloat("maxYSpeed", 12f),
            child.ReadInstalledFloat("extraLoopBorderX", 0f), child.ReadInstalledFloat("extraLoopBorderY", 0f),
            child.ReadInstalledInt("starCount", 100), child.ReadInstalledString("backgroundColor", "000000"),
            child.ReadInstalledFloat("backgroundAlpha", 1f), child.ReadInstalledString("sprite", "bgs/02/stars"),
            child.ReadInstalledFloat("scrollX", 0f), child.ReadInstalledFloat("scrollY", 0f),
            child.ReadInstalledFloat("alpha", 1f), child.ReadInstalledFloat("trailSeparation", 1f),
            child.ReadInstalledFloat("animationRate", 2f), child.ReadInstalledString("alphas", "1"),
            child.ReadInstalledFloat("animationRateRange", 2f));
    }
}
