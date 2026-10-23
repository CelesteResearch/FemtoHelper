// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Graphics.Rendering.Primitives;
using Celeste.Graphics.Rendering.Vertices;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.FemtoHelper.Backdrops.Stars
{
    [RuntimeTypeAlias("PolygonStars")]
    public class PolygonStars : BackdropPresentation
    {
        private struct Stars
        {
            public Vector2 Position;
            public float Speed;
            public float Size;
            public float Rotation;
            public float RotationSpeed;
            public PackedColor Color;
        }

        private readonly ISourceEngineContext engine;
        private readonly ISourceVectorArithmetic vectors;
        private readonly Stars[] stars;
        private readonly Vector2 angle;
        private Vector2 lastCamera = Vector2.Zero;
        private readonly int sideCount;
        private readonly float pointinessMultiplier;
        private readonly float loopBorder;
        private readonly PackedColor[] colors;
        private readonly float alpha;
        private readonly float scroll;
        private readonly bool filled;
        private SourceVertexPositionColor[] vertices;

        private OriginalGameplayBuffers Buffers =>
            ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics).Buffers;

        public int GameplayBufferWidth => Buffers.Gameplay?.Width ?? 320;
        public int GameplayBufferHeight => Buffers.Gameplay?.Height ?? 180;

        public PolygonStars(ISourceEngineContext engine, int sides, float pointiness, float minRotation,
            float maxRotation, float minSize, float maxSize, float border, string color, float angle, float alpha,
            float minSpeed, float maxSpeed, int amount, float scroll, bool filled)
        {
            this.engine = engine;
            sideCount = Math.Max(sides, 0);
            pointinessMultiplier = pointiness;
            loopBorder = border;
            this.alpha = alpha;
            this.scroll = scroll;
            stars = new Stars[amount];
            var values = color.Split(',');
            colors = new PackedColor[values.Length];
            for (var i = 0; i < values.Length; i++) colors[i] = SourceHexColor.HexToColor(values[i].Trim()) * alpha;
            this.angle = new Vector2((float)Math.Sin((double)(angle / 180f) * Math.PI),
                (float)Math.Cos((double)(angle / 180f) * Math.PI));
            vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            var random = ((ISourceRandomEnvironment)engine).Random;
            for (var i = 0; i < stars.Length; i++)
            {
                stars[i].Position = new Vector2(
                    SourceRandomMath.NextFloat(random, GameplayBufferWidth + loopBorder) - loopBorder / 2f,
                    SourceRandomMath.NextFloat(random, GameplayBufferHeight + loopBorder) - loopBorder / 2f);
                stars[i].Speed = SourceRandomMath.Range(random, minSpeed, maxSpeed);
                stars[i].Size = SourceRandomMath.Range(random, minSize, maxSize);
                stars[i].Rotation = SourceRandomMath.NextFloat(random, (float)Math.PI * 2f);
                stars[i].RotationSpeed = SourceRandomMath.Range(random, minRotation, maxRotation);
                stars[i].Color = colors[random.Next(colors.Length)];
            }

            this.filled = filled;
        }

        public override void Update(ISourceBackdropScene scene)
        {
            base.Update(scene);
            var camera = scene.Camera.Position;
            var movement = camera - lastCamera;
            for (var i = 0; i < stars.Length; i++)
            {
                stars[i].Position += angle * stars[i].Speed * engine.DeltaTime - movement * scroll;
                stars[i].Rotation += stars[i].RotationSpeed * engine.DeltaTime;
            }

            lastCamera = camera;
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            if (filled)
            {
                vertices ??= new SourceVertexPositionColor[stars.Length * sideCount * 3];
                var blend = ((ISourceDrawScope)engine).Draw.SpriteBatch.CurrentState.Blend ??
                            SourceBlendState.AlphaBlend;
                var index = 0;
                for (var i = 0; i < stars.Length; i++)
                {
                    var color = stars[i].Color * alpha;
                    var center = Center(i);
                    for (var j = 0; j < sideCount; j++)
                    {
                        var first = j % 2 == 0 ? stars[i].Size : stars[i].Size * pointinessMultiplier;
                        var second = j % 2 == 0 ? stars[i].Size * pointinessMultiplier : stars[i].Size;
                        vertices[index++] = new SourceVertexPositionColor(new Vector3(center, 0f), color);
                        vertices[index++] =
                            new SourceVertexPositionColor(new Vector3(center + Corner(i, j, first), 0f), color);
                        vertices[index++] =
                            new SourceVertexPositionColor(new Vector3(center + Corner(i, j + 1, second), 0f), color);
                    }
                }

                Renderer.EndSpritebatch(scene);
                SourcePrimitiveGraphics.DrawVertices(((ISourcePrimitiveGraphicsEnvironment)engine).PrimitiveGraphics,
                    Renderer.Matrix, vertices, vertices.Length, null, blend);
                Renderer.StartSpritebatch(scene, blend);
                return;
            }

            for (var i = 0; i < stars.Length; i++)
            {
                var color = stars[i].Color * alpha;
                var center = Center(i);
                for (var j = 0; j < sideCount; j++)
                {
                    var first = j % 2 == 0 ? stars[i].Size : stars[i].Size * pointinessMultiplier;
                    var second = j % 2 == 0 ? stars[i].Size * pointinessMultiplier : stars[i].Size;
                    SourcePrimitiveDrawing.Line((ISourceDrawScope)context, center + Corner(i, j, first),
                        center + Corner(i, j + 1, second), color, vectors);
                }
            }
        }

        private Vector2 Center(int index) => new Vector2(
            Mod(stars[index].Position.X, GameplayBufferWidth + loopBorder) - loopBorder / 2f,
            Mod(stars[index].Position.Y, GameplayBufferHeight + loopBorder) - loopBorder / 2f);

        private Vector2 Corner(int star, int side, float size)
        {
            double rotation = (double)stars[star].Rotation + Math.PI / ((float)sideCount / 2f) * side;
            return new Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation)) * new Vector2(size, size);
        }

        private float Mod(float x, float m) => (x % m + m) % m;

        public static PolygonStars Create(ISourceEngineContext engine, BinaryPackerElement child) => new PolygonStars(
            engine, child.ReadInstalledInt("sideCount", 10), child.ReadInstalledFloat("pointinessMultiplier", 2f),
            child.ReadInstalledFloat("minRotationSpeed", 1f), child.ReadInstalledFloat("maxRotationSpeed", 2f),
            child.ReadInstalledFloat("minSize", 2f), child.ReadInstalledFloat("maxSize", 8f),
            child.ReadInstalledFloat("loopBorder", 64f), child.ReadInstalledString("colors", "008080"),
            child.ReadInstalledFloat("angle", 270f), child.ReadInstalledFloat("alpha", 1f),
            child.ReadInstalledFloat("minSpeed", 24f), child.ReadInstalledFloat("maxSpeed", 48f),
            child.ReadInstalledInt("amount", 50), child.ReadInstalledFloat("scroll", .5f),
            child.ReadInstalledBool("filled", false));
    }
}
