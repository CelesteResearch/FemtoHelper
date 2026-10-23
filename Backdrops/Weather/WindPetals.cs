// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display;
using Celeste.Graphics.Display.Colors;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Simulation.Presentation.Backdrops.Weather.Petals;

namespace Celeste.Mods.FemtoHelper.Backdrops.Weather
{
    [RuntimeTypeAlias("WindPetals")]
    public class WindPetals : BackdropPresentation
    {
        private struct Particle
        {
            public Vector2 Position;
            public float Speed, SpeedX, Spin, MaxRotate, RotationCounter;
            public PackedColor Color;
        }

        private readonly ISourceEngineContext engine;
        private readonly Particle[] particles;
        public string PetalColor = "66cc22";
        public readonly PackedColor[] Colors;

        public readonly float FallSpeedMin,
            FallSpeedMax,
            XDriftSpeedMin,
            XDriftSpeedMax,
            BlurDensity,
            Parallax,
            SpinSpeedMultiplier,
            SpinAmount,
            Alpha,
            Scale,
            WindXMultiplier,
            WindYMultiplier,
            ExtraLoopBorder;

        public readonly int BlurCount;
        public readonly string Sprite;
        private float fade;

        private OriginalGameplayBuffers Buffers =>
            ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics).Buffers;

        public int GameplayBufferWidth => Buffers.Gameplay?.Width ?? 320;
        public int GameplayBufferHeight => Buffers.Gameplay?.Height ?? 180;

        public WindPetals(ISourceEngineContext engine, string colors, float fallingSpeedMin, float fallingSpeedMax,
            int blurCount, float blurDensity, string texture, int count, float scroll, float spinFrequency,
            float spinAmplitude, float transparency, float size, float xDriftingSpeedMin, float xDriftingSpeedMax,
            float windXMultiplier, float windYMultiplier, float extraLoopBorder)
        {
            this.engine = engine;
            var values = colors.Split(',');
            Colors = new PackedColor[values.Length];
            for (int i = 0; i < values.Length; i++)
                Colors[i] = SourceColorParsing.HexToPackedColorWithAlpha(values[i].Trim());
            FallSpeedMin = fallingSpeedMin;
            FallSpeedMax = fallingSpeedMax;
            XDriftSpeedMin = xDriftingSpeedMin;
            XDriftSpeedMax = xDriftingSpeedMax;
            BlurCount = blurCount;
            BlurDensity = blurDensity;
            Sprite = texture;
            particles = new Particle[count];
            Parallax = (float)Math.Max(scroll, 1E-05);
            SpinSpeedMultiplier = spinFrequency;
            SpinAmount = spinAmplitude;
            Alpha = transparency;
            Scale = size;
            WindXMultiplier = windXMultiplier;
            WindYMultiplier = windYMultiplier;
            ExtraLoopBorder = extraLoopBorder;
            for (int i = 0; i < particles.Length; i++) Reset(i);
        }

        private void Reset(int i)
        {
            var random = ((ISourceRandomEnvironment)engine).Random;
            particles[i].Position =
                new Vector2(SourceRandomMath.Range(random, 0f, GameplayBufferWidth + ExtraLoopBorder) / Parallax,
                    SourceRandomMath.Range(random, 0f, GameplayBufferHeight + ExtraLoopBorder) / Parallax);
            particles[i].Speed = SourceRandomMath.Range(random, FallSpeedMin, FallSpeedMax);
            particles[i].SpeedX = SourceRandomMath.Range(random, XDriftSpeedMin, XDriftSpeedMax);
            particles[i].Spin = SourceRandomMath.Range(random, 8f, 12f) * .2f;
            PetalParticleMotion.InitializeRotation(random, out particles[i].RotationCounter,
                out particles[i].MaxRotate);
            particles[i].Color = Colors[random.Next(Colors.Length)];
        }

        public override void Update(ISourceBackdropScene scene)
        {
            base.Update(scene);
            for (int i = 0; i < particles.Length; i++)
            {
                PetalParticleMotion.AdvancePosition(ref particles[i].Position, particles[i].Speed, engine.DeltaTime);
                particles[i].Position.X += particles[i].SpeedX * engine.DeltaTime;
                PetalParticleMotion.AdvanceRotation(ref particles[i].RotationCounter, particles[i].Spin,
                    engine.DeltaTime);
                particles[i].Position.Y += scene.Wind.Y * WindYMultiplier * engine.DeltaTime;
                particles[i].Position.X += scene.Wind.X * WindXMultiplier * engine.DeltaTime;
            }

            fade = ScalarMath.Approach(fade, Visible ? 1f : 0f, engine.DeltaTime);
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            if (fade <= 0f) return;
            float eased = ((ISourceEasingEnvironment)engine).EasingArithmetic.SineInOut(fade);
            var camera = scene.Camera;
            var texture = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[Sprite];
            for (int i = 0; i < particles.Length; i++)
            {
                var position = new Vector2(
                    -16f + Mod(particles[i].Position.X - camera.X,
                        (GameplayBufferWidth + ExtraLoopBorder * Parallax) / Parallax),
                    -16f + Mod(particles[i].Position.Y - camera.Y,
                        (GameplayBufferHeight + ExtraLoopBorder * Parallax) / Parallax));
                float rotation = (float)Math.PI / 2f +
                                 MathF.Sin(particles[i].RotationCounter * SpinSpeedMultiplier *
                                           particles[i].MaxRotate) * 1f;
                position += Vector2Geometry.AngleToVector(rotation, 4f);
                float windX = scene.Wind.X * WindXMultiplier, windY = scene.Wind.Y * WindYMultiplier;
                for (int j = 1; j < BlurCount; j++)
                    Draw(context, texture,
                        (position - new Vector2(windX / 300f * (j / BlurDensity), windY / 300f * (j / BlurDensity))) *
                        Parallax,
                        particles[i].Color * ScalarMath.Map(j, 1f, BlurCount - 1, .5f, 0f) * fade *
                        Math.Max(Math.Min(Math.Abs(windX) / 300f, 1f), Math.Min(Math.Abs(windY) / 300f, 1f)) * Alpha,
                        (rotation - .8f) * SpinAmount);
                Draw(context, texture, position * Parallax, particles[i].Color * eased * Alpha,
                    (rotation - .8f) * SpinAmount);
            }
        }

        private void Draw(ISourceRenderContext context, global::Celeste.Graphics.Content.Atlas.AtlasEntry texture,
            Vector2 position, PackedColor color, float rotation)
        {
            var draw = new AtlasTextureDraw(texture, position, texture.Center, color, new Vector2(1f * Scale),
                rotation);
            ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, this);
        }

        private float Mod(float x, float m) => (x % m + m) % m;

        public static WindPetals Create(ISourceEngineContext engine, BinaryPackerElement child) => new WindPetals(
            engine, child.ReadInstalledString("colors", "66cc33"), child.ReadInstalledFloat("fallingSpeedMin", 8f),
            child.ReadInstalledFloat("fallingSpeedMax", 16f), child.ReadInstalledInt("blurCount", 15),
            child.ReadInstalledFloat("blurDensity", 3f), child.ReadInstalledString("texture", "particles/petal"),
            child.ReadInstalledInt("particleCount", 40), child.ReadInstalledFloat("parallax", 1f),
            child.ReadInstalledFloat("spinSpeedMultiplier", 1f), child.ReadInstalledFloat("spinAmountMultiplier", 1f),
            child.ReadInstalledFloat("alpha", 1f), child.ReadInstalledFloat("scale", 1f),
            child.ReadInstalledFloat("minXDriftSpeed", 0f), child.ReadInstalledFloat("maxXDriftSpeed", 0f),
            child.ReadInstalledFloat("windX_Multiplier", 1f), child.ReadInstalledFloat("windY_Multiplier", 1f),
            child.ReadInstalledFloat("extraLoopBorder", 32f));
    }
}
