// SPDX-License-Identifier: MIT

using System;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps;
using Celeste.Mods.FemtoHelper.Backdrops.Stars;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.FemtoHelper.Modules
{
    public sealed partial class FemtoHelperModule
    {
        private void LoadStars() =>
            ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadBackdrop += LoadStarsBackdrop;

        private void UnloadStars() =>
            ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadBackdrop -= LoadStarsBackdrop;

        private BackdropPresentation LoadStarsBackdrop(OriginalChapterMapData map, BinaryPackerElement child,
            BinaryPackerElement above) =>
            child.Name.Equals("FemtoHelper/FemtoStars", StringComparison.OrdinalIgnoreCase)
                ? FemtoStars.Create(SourceEngine, child)
                : null;
    }
}
