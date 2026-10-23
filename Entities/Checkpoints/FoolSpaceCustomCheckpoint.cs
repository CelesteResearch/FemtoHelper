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
    [CustomEntity("FemtoHelper/FoolSpaceCustomCheckpoint = Load")]
    [RuntimeTypeAlias("FoolSpaceCustomCheckpoint")]
    public class FoolSpaceCustomCheckpoint : SceneEntityState, ISourceEngineContextEntity
    {
        private readonly ISourceEngineContext engine;
        ISourceEngineContext ISourceEngineContextEntity.EngineContext => engine;
        public bool Activated;
        public readonly string Number;
        public readonly string NumberB;
        public readonly string SpriteRoot;
        public readonly string Colors;
        private Vector2 respawn;
        private readonly AtlasEntry baseEmpty, baseToggle, baseActive;
        private readonly AtlasEntry numbersEmpty, numbersEmptyB, numbersActive, numbersActiveB;

        public static FoolSpaceCustomCheckpoint Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public FoolSpaceCustomCheckpoint(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            Colors = data.Attr("colors", "fe2074,205efe,cefe20");
            Number = data.Attr("digitA", "");
            NumberB = data.Attr("digitB", "");
            SpriteRoot = data.Attr("spriteRoot", "scenery/foolspace_checkpoint/");
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            baseEmpty = atlas[SpriteRoot + "base00"];
            baseToggle = atlas[SpriteRoot + "base01"];
            baseActive = atlas[SpriteRoot + "base02"];
            numbersEmpty = atlas[SpriteRoot + "numberbg0" + Number];
            numbersEmptyB = atlas[SpriteRoot + "numberbg0" + NumberB];
            numbersActive = atlas[SpriteRoot + "number0" + Number];
            numbersActiveB = atlas[SpriteRoot + "number0" + NumberB];
            Collider = new HitboxColliderGeometry(32f, 32f, -16f, -8f);
            Depth = 8999;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if ((scene as OriginalLevelSceneRoot).Session.GetFlag("summit_checkpoint_" + Number + NumberB))
                Activated = true;
            respawn = (Scene as OriginalLevelSceneRoot).GetSpawnPoint(Position);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (!Activated && CollideCheck(typeof(RoomPlayerSession).FullName))
            {
                Activated = true;
                var level = Scene as OriginalLevelSceneRoot;
                level.Session.SetFlag("summit_checkpoint_" + Number + NumberB);
                level.Session.RespawnPoint = respawn;
            }
        }

        public override void Update()
        {
            if (!Activated)
            {
                RoomPlayerSession player = CollideFirst<RoomPlayerSession>(typeof(RoomPlayerSession).FullName);
                if (player != null && player.OnGround() && player.Data.Motion.Normal.Speed.Y >= 0f)
                {
                    var level = Scene as OriginalLevelSceneRoot;
                    Activated = true;
                    level.Session.SetFlag("summit_checkpoint_" + Number + NumberB);
                    level.Session.RespawnPoint = respawn;
                    level.Session.UpdateLevelStartDashes();
                    level.Session.HitCheckpoint = true;
                    level.Displacement.AddBurst(Position, .5f, 4f, 24f, .5f);
                    level.SourceScene.QueueSourceEntity(new CheckpointConfettiRenderer(engine, Colors, Position));
                    ((IOriginalGameplayProcess)engine).AudioSession.Play("event:/game/07_summit/checkpoint_confetti",
                        Position, ((IOriginalGameplayProcess)engine).SaveContext);
                }
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            AtlasEntry first = Activated ? numbersActive : numbersEmpty;
            AtlasEntry second = Activated ? numbersActiveB : numbersEmptyB;
            AtlasEntry texture = baseActive;
            if (!Activated)
                texture = ((ISourceScene)Scene).SourceScene.Clock.BetweenInterval(.25f) ? baseEmpty : baseToggle;
            SourceAtlasDrawing.Draw(context, this, texture,
                Position - new Vector2(texture.FrameWidth / 2 + 1, texture.FrameHeight / 2));
            SourceAtlasDrawing.DrawJustified(context, this, first, Position + new Vector2(-1f, 1f),
                new Vector2(1f, 0f), PackedColor.White);
            SourceAtlasDrawing.DrawJustified(context, this, second, Position + new Vector2(0f, 1f),
                new Vector2(0f, 0f), PackedColor.White);
        }
    }
}
