using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace CtrLibrary.UI
{
    /// <summary>
    /// Represents a mesh with a list of texture coordinates and indices for displaying UVs.
    /// </summary>
    public class UVMeshObject
    {
        /// <summary>
        /// List of texture coordinates to display from the indices.
        /// </summary>
        public List<Vector2> TexCoords = new List<Vector2>();

        /// <summary>
        /// List of triangle indices used to index and display the texture coordinates.
        /// These must be from a triangulated mesh to display correctly.
        /// </summary>
        public int[] Indices = new int[0];

        public UVMeshObject() { }

        public UVMeshObject(List<Vector2> texCoords, int[] indices)
        {
            TexCoords = texCoords;
            Indices = indices;
        }
    }
}
