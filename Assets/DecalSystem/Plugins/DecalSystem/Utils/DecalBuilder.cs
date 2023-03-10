using System.Linq;
using UnityEngine;

namespace _Decal
{

    public static class DecalBuilder
    {

        private static readonly MeshBuilder builder = new MeshBuilder();

        public static void BuildAndSetDirty(Decal decal, GameObject gameObject)
        {
            Build(builder, decal, gameObject);
        }

        public static GameObject[] BuildAndSetDirty(Decal decal)
        {
            var affectedObjects = Build(builder, decal);

            return affectedObjects;
        }

        private static void Build(MeshBuilder builder, Decal decal, GameObject gameObject)
        {
            MeshFilter filter = decal.GetComponent<MeshFilter>() ?? decal.gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = decal.GetComponent<MeshRenderer>() ?? decal.gameObject.AddComponent<MeshRenderer>();

            if (filter.sharedMesh != null && !filter.sharedMesh.isReadable)
            {
                return;
            }

            if (decal.material == null)
            {
                Object.DestroyImmediate(filter.sharedMesh);
                filter.sharedMesh = null;
                renderer.sharedMaterial = null;
                return;
            }

            Build(builder, decal, gameObject.GetComponent<MeshFilter>());
            builder.Push(decal.pushDistance);

            if (filter.sharedMesh == null)
            {
                filter.sharedMesh = new Mesh();
                filter.sharedMesh.name = "Decal";
            }

            builder.ToMesh(filter.sharedMesh);
            filter.sharedMesh.UploadMeshData(true);

            renderer.sharedMaterial = decal.material;
        }

        private static GameObject[] Build(MeshBuilder builder, Decal decal)
        {
            MeshFilter filter = decal.GetComponent<MeshFilter>() ?? decal.gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = decal.GetComponent<MeshRenderer>() ?? decal.gameObject.AddComponent<MeshRenderer>();


            if (filter.sharedMesh != null && !filter.sharedMesh.isReadable)
            {
                return null;
            }

            if (decal.material == null)
            {
                Object.DestroyImmediate(filter.sharedMesh);
                filter.sharedMesh = null;
                renderer.sharedMaterial = null;
                return null;
            }


            var objects = DecalUtils.GetAffectedObjects(decal);
            foreach (var obj in objects)
            {
                Build(builder, decal, obj);
            }
            builder.Push(decal.pushDistance);


            if (filter.sharedMesh == null)
            {
                filter.sharedMesh = new Mesh();
                filter.sharedMesh.name = "Decal";
            }

            builder.ToMesh(filter.sharedMesh);
            renderer.sharedMaterial = decal.material;

            return objects.Select(i => i.gameObject).ToArray();
        }


        private static void Build(MeshBuilder builder, Decal decal, MeshFilter @object)
        {
            Matrix4x4 objToDecalMatrix = decal.transform.worldToLocalMatrix * @object.transform.localToWorldMatrix;

            Mesh mesh = @object.sharedMesh;
            
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = triangles[i];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                Vector3 v1 = objToDecalMatrix.MultiplyPoint(vertices[i1]);
                Vector3 v2 = objToDecalMatrix.MultiplyPoint(vertices[i2]);
                Vector3 v3 = objToDecalMatrix.MultiplyPoint(vertices[i3]);

                AddTriangle(builder, decal, v1, v2, v3);
            }
        }


        private static void AddTriangle(MeshBuilder builder, Decal decal, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Rect uvRect = new Rect(0, 0, 1, 1);
            Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

            if (Vector3.Angle(Vector3.forward, -normal) <= decal.maxAngle)
            {
                var poly = PolygonClippingUtils.Clip(v1, v2, v3);
                if (poly.Length > 0)
                {
                    builder.AddPolygon(poly, normal, uvRect);
                }
            }
        }

        private static Rect To01(Rect rect, Texture2D texture)
        {
            rect.x /= texture.width;
            rect.y /= texture.height;
            rect.width /= texture.width;
            rect.height /= texture.height;
            return rect;
        }
    }
}
