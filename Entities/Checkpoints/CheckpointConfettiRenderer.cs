using Celeste.Core.Display;
using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Geometry;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Simulation;
using Celeste.Graphics.Display.Colors;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Core.Reflection.Runtime;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;

namespace Celeste.Mods.FemtoHelper.Entities.Checkpoints
{
    public class CheckpointConfettiRenderer : SceneEntityState
    {
        private struct Particle
        {
            public Vector2 Position;

            public PackedColor Color;

            public Vector2 Speed;

            public float Timer;

            public float Percent;

            public float Duration;

            public float Alpha;

            public float Approach;
        }

        private Particle[] particles = new Particle[30];

        private readonly ISourceEngineContext engine;

        public CheckpointConfettiRenderer(ISourceEngineContext engine, string color, Vector2 position)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            base.Depth = -10010;
            string[] values = color.Split(',');
            var colors = new PackedColor[values.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = SourceHexColor.HexToColor(values[i]);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Position = SourceVector2Math.Add(Position,
                    new Vector2(SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, -3, 3),
                        SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, -3, 3)));
                particles[i].Color =
                    RandomSelection.Choose(((ISourceRandomEnvironment)engine).Random, colors);
                particles[i].Timer = SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random);
                particles[i].Duration = SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, 2, 4);
                particles[i].Alpha = 1f;
                float angleRadians = -(float)Math.PI / 2f +
                                     SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, -0.5f, 0.5f);
                int num = SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, 140, 220);
                particles[i].Speed = Vector2Geometry.AngleToVector(angleRadians, num);
            }
        }

        public override void Update()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Position = SourceVector2Math.Add(particles[i].Position,
                    SourceVector2Math.Multiply(particles[i].Speed, engine.DeltaTime));
                particles[i].Speed.X = ScalarMath.Approach(particles[i].Speed.X, 0f, 80f * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                particles[i].Speed.Y = ScalarMath.Approach(particles[i].Speed.Y, 20f, 500f * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                particles[i].Timer += engine.DeltaTime;
                particles[i].Percent += engine.DeltaTime / particles[i].Duration;
                particles[i].Alpha = ScalarMath.ClampedMap(particles[i].Percent, 0.9f, 1f, 1f, 0f);
                if (particles[i].Speed.Y > 0f)
                {
                    particles[i].Approach = ScalarMath.Approach(particles[i].Approach, 5f, engine.DeltaTime * 16f,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                }
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                float num = 0f;
                Vector2 position = particles[i].Position;
                if (particles[i].Speed.Y < 0f)
                {
                    num = Vector2Geometry.Angle(particles[i].Speed);
                }
                else
                {
                    num = (float)Math.Sin(particles[i].Timer * 4f) * 1f;
                    position = SourceVector2Math.Add(position,
                        Vector2Geometry.AngleToVector((float)Math.PI / 2f + num, particles[i].Approach));
                }

                SourceAtlasDrawing.DrawCentered(context, this,
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["particles/confetti"],
                    SourceVector2Math.Add(position, Vector2.UnitY), PackedColor.Black * (particles[i].Alpha * 0.5f),
                    1f,
                    num);
                SourceAtlasDrawing.DrawCentered(context, this,
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["particles/confetti"], position,
                    particles[i].Color * particles[i].Alpha, 1f, num);
            }
        }
    }
}
