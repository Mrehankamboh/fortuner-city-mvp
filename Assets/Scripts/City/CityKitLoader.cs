using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.City
{
    /// <summary>Loads Kenney CC0 city FBX from Resources/City (after Unity import).</summary>
    public static class CityKitLoader
    {
        static GameObject[] _buildings;
        static GameObject[] _trees;
        static GameObject[] _fences;
        static GameObject[] _lights;
        static GameObject _dumpster;
        static GameObject _trafficLight;
        static GameObject _cone;
        static bool _tried;

        public static bool Ready
        {
            get { Ensure(); return _buildings != null && _buildings.Length > 0; }
        }

        public static GameObject Building(System.Random rng)
        {
            Ensure();
            if (_buildings == null || _buildings.Length == 0) return null;
            return _buildings[rng.Next(_buildings.Length)];
        }

        public static GameObject Tree(System.Random rng)
        {
            Ensure();
            if (_trees == null || _trees.Length == 0) return null;
            return _trees[rng.Next(_trees.Length)];
        }

        public static GameObject Fence(System.Random rng)
        {
            Ensure();
            if (_fences == null || _fences.Length == 0) return null;
            return _fences[rng.Next(_fences.Length)];
        }

        public static GameObject StreetLight()
        {
            Ensure();
            if (_lights == null || _lights.Length == 0) return null;
            return _lights[0];
        }

        public static GameObject Dumpster() { Ensure(); return _dumpster; }
        public static GameObject TrafficLight() { Ensure(); return _trafficLight; }
        public static GameObject Cone() { Ensure(); return _cone; }

        static void Ensure()
        {
            if (_tried) return;
            _tried = true;

            _buildings = Resources.LoadAll<GameObject>("City/Buildings");
            if (_buildings == null) _buildings = new GameObject[0];

            var props = Resources.LoadAll<GameObject>("City/Props");
            var roads = Resources.LoadAll<GameObject>("City/Roads");
            var trees = new List<GameObject>();
            var fences = new List<GameObject>();
            if (props != null)
            {
                foreach (var g in props)
                {
                    if (g == null) continue;
                    string n = g.name.ToLowerInvariant();
                    if (n.Contains("tree")) trees.Add(g);
                    else if (n.Contains("fence")) fences.Add(g);
                }
            }
            _trees = trees.ToArray();
            _fences = fences.ToArray();

            var lights = new List<GameObject>();
            if (roads != null)
            {
                foreach (var g in roads)
                {
                    if (g == null) continue;
                    string n = g.name.ToLowerInvariant();
                    if (n.StartsWith("light-")) lights.Add(g);
                    else if (n == "dumpster") _dumpster = g;
                    else if (n == "traffic-light" || n == "trafficlight") _trafficLight = g;
                    else if (n.Contains("cone")) _cone = g;
                }
            }
            _lights = lights.ToArray();

            Debug.Log($"CityKitLoader: buildings={_buildings.Length} trees={_trees.Length} fences={_fences.Length} lights={_lights.Length}");
        }
    }
}
