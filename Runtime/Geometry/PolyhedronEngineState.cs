using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.FemtoHelper.Runtime.Geometry
{
    internal sealed class PolyhedronEngineState
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, PolyhedronEngineState> States = new();
        internal int Samples;
        internal static PolyhedronEngineState For(ISourceEngineContext engine) => States.GetValue(engine, _ => new());
    }
}
