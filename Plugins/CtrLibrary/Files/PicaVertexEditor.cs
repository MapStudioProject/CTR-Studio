using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPICA.PICA.Converters;
using SPICA;
using SPICA.Formats.CtrH3D.Model.Mesh;
using OpenTK;
using Toolbox.Core;

namespace CtrLibrary
{
    internal class PicaVertexEditor
    {
        static List<STGenericMesh> Meshes;

        public static void Start(params H3DMesh[] meshes)
        {
            //Convert to an in tool generic mesh for manipulating with
            Meshes = new List<STGenericMesh>();

            foreach (var mesh in meshes)
            {
                var Mesh = new STGenericMesh();
                Meshes.Add(Mesh);

                var vertices = mesh.GetVertices();
                for (int i = 0; i < vertices.Length; i++)
                {
                    Mesh.Vertices.Add(new STVertex()
                    {
                        Position = new Vector3(
                             vertices[i].Position.X,
                             vertices[i].Position.Y,
                             vertices[i].Position.Z),
                        Normal = new Vector3(
                             vertices[i].Normal.X,
                             vertices[i].Normal.Y,
                             vertices[i].Normal.Z),
                        TexCoords = new Vector2[1]
                        {
                        new Vector2(
                         vertices[i].TexCoord0.X,
                         vertices[i].TexCoord0.Y),
                        },
                        Colors = new Vector4[1]
                        {
                        new Vector4(
                         vertices[i].Color.X,
                         vertices[i].Color.Y,
                         vertices[i].Color.Z,
                         vertices[i].Color.W),
                        },
                    });
                }
                STPolygonGroup poly = new STPolygonGroup();
                Mesh.PolygonGroups.Add(poly);
                foreach (var sm in mesh.SubMeshes)
                {
                    foreach (var ind in sm.Indices)
                        poly.Faces.Add(ind);
                }
            }
        }

        /// <summary>
        /// Recalculates the normals based on vertex positions.
        /// </summary>
        public static void CalculateNormals() => Meshes.ForEach(x => x.CalculateNormals());

        /// <summary>
        /// Flips all UVs vertical.
        /// </summary>
        public static void FlipUVsVertical() => Meshes.ForEach(x => x.FlipUvsVertical());

        /// <summary>
        /// Flips all UVs horizontal.
        /// </summary>
        public static void FlipUvsHorizontal() => Meshes.ForEach(x => x.FlipUvsHorizontal());

        /// <summary>
        /// Calculates tangent data.
        /// </summary>
        public static void CalculateTangent() => Meshes.ForEach(x => x.CalculateTangentBitangent(0));

        /// <summary>
        /// Sets a single constant vertex color.
        /// </summary>
        public static void SetVertexColor(Vector4 color) => Meshes.ForEach(x => x.SetVertexColor(color));

        /// <summary>
        /// Smooths normals from the current set of meshes.
        /// </summary>
        public static void SmoothNormals()
        {
            bool cancel = false;
            STGenericMesh.SmoothNormals(ref cancel, Meshes);
        }

        public static PICAVertex[] End(H3DMesh mesh, int index = 0)
        {
            //Convert to an in tool generic mesh for manipulating with
            var Mesh = Meshes[index];
            var vertices = mesh.GetVertices();
            for (int i = 0; i < Mesh.Vertices.Count; i++)
            {
                vertices[i].Position = new System.Numerics.Vector4(
                         Mesh.Vertices[i].Position.X,
                         Mesh.Vertices[i].Position.Y,
                         Mesh.Vertices[i].Position.Z, 1);

                vertices[i].Normal = new System.Numerics.Vector4(
                         Mesh.Vertices[i].Normal.X,
                         Mesh.Vertices[i].Normal.Y,
                         Mesh.Vertices[i].Normal.Z, 1);

                vertices[i].TexCoord0 = new System.Numerics.Vector4(
                         Mesh.Vertices[i].TexCoords[0].X,
                         Mesh.Vertices[i].TexCoords[0].Y,
                         0, 0);

                vertices[i].Color = new System.Numerics.Vector4(
                         Mesh.Vertices[i].Colors[0].X,
                         Mesh.Vertices[i].Colors[0].Y,
                         Mesh.Vertices[i].Colors[0].Z,
                         Mesh.Vertices[i].Colors[0].Z);
            }
            return vertices;
        }
    }
}
