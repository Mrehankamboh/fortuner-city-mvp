using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.Traffic
{
    public class WaypointPath : MonoBehaviour
    {
        public List<Transform> points = new List<Transform>();
        public bool loop = true;
        public float speedScale = 1f;   // motorway loops drive faster

        public Vector3 GetPoint(int index)
        {
            if (points == null || points.Count == 0) return transform.position;
            index = Mathf.Clamp(index, 0, points.Count - 1);
            return points[index].position;
        }

        public int NextIndex(int index)
        {
            if (points == null || points.Count == 0) return 0;
            int n = index + 1;
            if (n >= points.Count) return loop ? 0 : points.Count - 1;
            return n;
        }

        public void BuildFromPositions(IEnumerable<Vector3> worldPositions)
        {
            points.Clear();
            int i = 0;
            foreach (var p in worldPositions)
            {
                var t = new GameObject($"WP_{i++}").transform;
                t.SetParent(transform, false);
                t.position = p;
                points.Add(t);
            }
        }

        void OnDrawGizmos()
        {
            if (points == null || points.Count < 2) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (points[i] && points[i + 1])
                    Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
            if (loop && points[0] && points[points.Count - 1])
                Gizmos.DrawLine(points[points.Count - 1].position, points[0].position);
        }
    }
}
