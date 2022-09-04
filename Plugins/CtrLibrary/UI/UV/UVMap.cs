using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using GLFrameworkEngine;

namespace CtrLibrary.UI
{
    /// <summary>
    /// Represents a 2D UV drawer object from a given UVMeshObject.
    /// </summary>
    public class UVMap 
    {
        //Mesh drawer
        RenderMesh<Vector2> MeshDrawer;

        //Point list
        private List<Vector2> Points = new List<Vector2>();

        private void Init()
        {
            if (MeshDrawer == null)
                MeshDrawer = new RenderMesh<Vector2>(new Vector2[0], PrimitiveType.Lines);
        }

        public void Reset() {
            Points.Clear();
        }

        public void Draw(UVViewport.Camera2D camera, Matrix4 matrix, Vector2 scale)
        {
            if (MeshDrawer == null)
                return;

            GL.Disable(EnableCap.CullFace);

            var shader = GlobalShaders.GetShader("UV_WINDOW");
            shader.Enable();

            var cameraMtx = camera.ViewMatrix * camera.ProjectionMatrix;

            shader.SetMatrix4x4("mtxCam", ref cameraMtx);
            shader.SetMatrix4x4("mtxMdl", ref matrix);

            shader.SetFloat("brightness", 1.0f);
            shader.SetInt("hasTexture", 0);
            shader.SetVector2("scale", scale);
            shader.SetVector4("uColor", ColorUtility.ToVector4(Runtime.UVEditor.UVColor));

            
            MeshDrawer.Draw(shader);

            GL.Enable(EnableCap.CullFace);
        }

        /// <summary>
        /// Updates the UV display with given UV meshes.
        /// </summary>
        public void UpdateVertexBuffer(List<UVMeshObject> meshes)
        {
            Init();

            Points.Clear();
            //No meshes, skip and draw as empty
            if (meshes.Count == 0)
            {
                MeshDrawer.UpdateVertexData(Points.ToArray());
                return;
            }

            foreach (var mesh in meshes)
            {
                //Check if the texture coordinates and indices are valid
                if (mesh.TexCoords.Count == 0 || mesh.Indices.Length < 3)
                    return;

                for (int v = 0; v < mesh.Indices.Length; v += 3)
                {
                    //Indices must have 3 for each triangle. Skip if it goes over the amount at the end.
                    if (mesh.Indices.Length <= v + 3)
                        break;

                    Vector2 GetCoord(int id) {
                        //Check for valid indices. It cannot index out of the texture coordinate list
                        return mesh.TexCoords.Count > id ? mesh.TexCoords[id] : new Vector2();
                    }

                    //Get and add 3 vertex UV points
                    var v1 = GetCoord(mesh.Indices[v]);
                    var v2 = GetCoord(mesh.Indices[v + 1]);
                    var v3 = GetCoord(mesh.Indices[v + 2]);

                    AddUVTriangle(
                        new Vector2(v1.X, v1.Y),
                        new Vector2(v2.X, v2.Y),
                        new Vector2(v3.X, v3.Y));
                }
            }
            MeshDrawer.UpdateVertexData(Points.ToArray());
        }

        private void AddUVTriangle(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            Vector2 scaleUv = new Vector2(2);
            Vector2 transUv = new Vector2(-1f);
            Points.AddRange(TransformUVTriangle(v1, v2, v3, scaleUv, transUv));
        }

        private static List<Vector2> TransformUVTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 scaleUv, Vector2 transUv)
        {
            List<Vector2> points = new List<Vector2>();
            points.Add(v1 * scaleUv + transUv);
            points.Add(v2 * scaleUv + transUv);

            points.Add(v2 * scaleUv + transUv);
            points.Add(v3 * scaleUv + transUv);

            points.Add(v3 * scaleUv + transUv);
            points.Add(v1 * scaleUv + transUv);
            return points;
        }
    }
}
