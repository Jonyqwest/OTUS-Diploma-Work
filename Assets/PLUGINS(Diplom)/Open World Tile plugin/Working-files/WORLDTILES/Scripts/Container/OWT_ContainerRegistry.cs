namespace WorldTile
{
    using System.Collections.Generic;

    public static class OWT_ContainerRegistry
    {
        private static readonly Dictionary<(byte z, int x, int y), List<OWT_Container>> _byTile = new();

        public static void Register(OWT_Container container)
        {
            var key = container.worldPosition.Key();
            if (!_byTile.TryGetValue(key, out var list))
            {
                list = new List<OWT_Container>();
                _byTile[key] = list;
            }
            list.Add(container);
        }

        public static void Unregister(OWT_Container container)
        {
            var key = container.worldPosition.Key();
            if (_byTile.TryGetValue(key, out var list))
            {
                list.Remove(container);
                if (list.Count == 0) _byTile.Remove(key);
            }
        }

        public static List<OWT_Container> GetContainersOnTile(byte z, int x, int y)
        {
            _byTile.TryGetValue((z, x, y), out var list);
            return list;
        }

        public static IEnumerable<OWT_Container> GetAll()
        {
            foreach (var kv in _byTile)
                foreach (var c in kv.Value)
                    yield return c;
        }

        public static void Clear() => _byTile.Clear();
    }
}
