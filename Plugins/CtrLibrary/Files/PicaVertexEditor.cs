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
        static STGenericMesh Mesh;

        public static void Start(H3DMesh mesh)
        {
            //Convert to an in tool generic mesh for manipulating with
            Mesh = new STGenericMesh();

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

        /// <summary>
        /// Recalculates the normals based on vertex positions.
        /// </summary>
        public static void CalculateNormals() => Mesh.CalculateNormals();

        public static void FlipUVsVertical() => Mesh.FlipUvsVertical();
        public static void FlipUvsHorizontal() => Mesh.FlipUvsHorizontal();
        public static void CalculateTangent() => Mesh.CalculateTangentBitangent(0);
        public static void SetVertexColor(Vector4 color) => Mesh.SetVertexColor(color);

        public static void SmoothNormals()
        {
            bool cancel = false;
            Mesh.SmoothNormals(ref cancel);
        }

        public static PICAVertex[] End(H3DMesh mesh)
        {
            //Convert to an in tool generic mesh for manipulating with
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
