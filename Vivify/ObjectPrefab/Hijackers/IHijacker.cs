using System.Collections.Generic;

namespace Vivify.ObjectPrefab.Hijackers
{
    internal interface IHijacker
    {
        public void Deactivate();
    }

    internal interface IHijacker<TSpawned> : IHijacker
    {
        public void Activate(List<TSpawned> spawned, bool hideOriginal);
    }
}
