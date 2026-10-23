using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;

namespace Celeste.Mods.FemtoHelper.Modules
{
    public sealed partial class FemtoHelperModule(ISourceEngineContext engine) : SourceEverestModule(engine)
    {
        // CustomEntity discovery registers this subset, including custom moon creatures; no process hooks are needed.
        public override void Load()
        {
            LoadWindPetals();
            LoadStars();
            LoadPolygonStars();
        }

        public override void Unload()
        {
            UnloadWindPetals();
            UnloadStars();
            UnloadPolygonStars();
        }
    }
}
