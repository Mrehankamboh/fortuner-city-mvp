using System.Collections.Generic;
using UnityEngine;

namespace FortunerCity.City
{
    /// <summary>
    /// Runtime meshes with real-world UVs so textures (windows, lane lines, paving) keep their size
    /// on any box. Submesh 0 = walls, submesh 1 = top.
    /// </summary>
    public static class CityMesh
    {
        static readonly Dictionary<string, Mesh> QuadCache = new Dictionary<string, Mesh>();

        /// <summary>
        /// Box with its bottom at y=0, centred on XZ.
        /// Walls: u = metres along the face / tileU, v = (y + vOffset) / tileV so floors line up on stacked boxes.
        /// Top: u,v = metres / topTile.
        /// </summary>
        public static Mesh Box(Vector3 size, float tileU, float tileV, float vOffset, float topTile)
        {
            float hx = size.x * 0.5f, hz = size.z * 0.5f, h = size.y;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var walls = new List<int>();
            var top = new List<int>();

            void Wall(Vector3 lb, Vector3 rb, Vector3 n)
            {
                float w = Vector3.Distance(lb, rb);
                int i = verts.Count;
                verts.Add(lb); verts.Add(lb + Vector3.up * h); verts.Add(rb + Vector3.up * h); verts.Add(rb);
                for (int k = 0; k < 4; k++) norms.Add(n);
                float v0 = vOffset / tileV, v1 = (vOffset + h) / tileV;
                uvs.Add(new Vector2(0f, v0)); uvs.Add(new Vector2(0f, v1));
                uvs.Add(new Vector2(w / tileU, v1)); uvs.Add(new Vector2(w / tileU, v0));
                walls.Add(i); walls.Add(i + 1); walls.Add(i + 2);
                walls.Add(i); walls.Add(i + 2); walls.Add(i + 3);
            }

            Wall(new Vector3(hx, 0f, hz), new Vector3(-hx, 0f, hz), Vector3.forward);
            Wall(new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz), Vector3.back);
            Wall(new Vector3(hx, 0f, -hz), new Vector3(hx, 0f, hz), Vector3.right);
            Wall(new Vector3(-hx, 0f, hz), new Vector3(-hx, 0f, -hz), Vector3.left);

            int t = verts.Count;
            verts.Add(new Vector3(-hx, h, -hz)); verts.Add(new Vector3(-hx, h, hz));
            verts.Add(new Vector3(hx, h, hz)); verts.Add(new Vector3(hx, h, -hz));
            for (int k = 0; k < 4; k++)
            {
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(verts[t + k].x / topTile, verts[t + k].z / topTile));
            }
            top.Add(t); top.Add(t + 1); top.Add(t + 2);
            top.Add(t); top.Add(t + 2); top.Add(t + 3);

            var mesh = new Mesh { name = "CityBox" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(walls, 0);
            mesh.SetTriangles(top, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Flat quad in XZ, centred, length along local Z. u across (width), v along (length).</summary>
        public static Mesh Quad(float width, float length, float uTile, float vTile, bool flipU = false)
        {
            string key = $"{width:F2}_{length:F2}_{uTile:F2}_{vTile:F2}_{flipU}";
            if (QuadCache.TryGetValue(key, out var cached)) return cached;

            float hw = width * 0.5f, hl = length * 0.5f;
            var verts = new[]
            {
                new Vector3(-hw, 0f, -hl), new Vector3(-hw, 0f, hl),
                new Vector3(hw, 0f, hl), new Vector3(hw, 0f, -hl)
            };
            var uvs = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                float u = (flipU ? hw - verts[i].x : verts[i].x + hw) / uTile;
                uvs[i] = new Vector2(u, (verts[i].z + hl) / vTile);
            }
            var mesh = new Mesh { name = "CityQuad" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            QuadCache[key] = mesh;
            return mesh;
        }

        public static GameObject Spawn(string name, Transform parent, Mesh mesh, Material[] mats,
            Vector3 localPos, Quaternion localRot, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            if (collider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size;
            }
            return go;
        }

        /// <summary>Box helper: walls material + top material, bottom at localPos.y.</summary>
        public static GameObject SpawnBox(string name, Transform parent, Vector3 size, Vector3 localPos,
            Material walls, Material top, bool collider, float vOffset = 0f, float topTile = 4f)
        {
            var mesh = Box(size, CityArt.FacadeTileU, CityArt.FacadeTileV, vOffset, topTile);
            return Spawn(name, parent, mesh, new[] { walls, top }, localPos, Quaternion.identity, collider);
        }

        public static GameObject SpawnQuad(string name, Transform parent, Material mat, float width, float length,
            float uTile, float vTile, Vector3 localPos, Quaternion localRot, bool flipU = false)
        {
            return Spawn(name, parent, Quad(width, length, uTile, vTile, flipU), new[] { mat }, localPos, localRot, false);
        }
    }
}
