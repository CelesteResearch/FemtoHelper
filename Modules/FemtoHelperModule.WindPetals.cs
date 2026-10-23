// SPDX-License-Identifier: MIT

using System;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps;
using Celeste.Mods.FemtoHelper.Backdrops.Weather;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.FemtoHelper.Modules
{
    public sealed partial class FemtoHelperModule
    {
        private void LoadWindPetals() =>
            ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadBackdrop +=
            LoadWindPetalsBackdrop;

        private void UnloadWindPetals() =>
            ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadBackdrop -=
            LoadWindPetalsBackdrop;

        private BackdropPresentation LoadWindPetalsBackdrop(OriginalChapterMapData map, BinaryPackerElement child,
            BinaryPackerElement above) =>
            child.Name.Equals("FemtoHelper/WindPetals", StringComparison.OrdinalIgnoreCase)
                ? WindPetals.Create(SourceEngine, child)
                : null;
    }
}
