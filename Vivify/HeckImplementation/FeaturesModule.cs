using System.Linq;
using Heck;
using static Vivify.VivifyController;

namespace Vivify
{
    [Module(ID, 2, LoadType.Active, new[] { "Heck" })]
    [ModulePatcher(HARMONY_ID + "Features", PatchType.Features)]
    [ModuleDataDeserializer(ID, typeof(CustomDataDeserializer))]
    internal class FeaturesModule : IModule
    {
        internal bool Active { get; private set; }

        [ModuleCondition]
        private static bool Condition(
            Capabilities capabilities)
        {
            return capabilities.Requirements.Contains(CAPABILITY);
        }

        [ModuleCallback]
        private void Callback(bool value)
        {
            Active = value;
        }
    }
}
