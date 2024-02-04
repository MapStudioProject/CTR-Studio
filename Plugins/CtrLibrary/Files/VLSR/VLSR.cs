using CtrLibrary;
using GLFrameworkEngine;
using IONET.Helpers;
using IONET;
using OpenTK;
using System.Runtime.InteropServices;
using Toolbox.Core;
using Toolbox.Core.IO;
using UIFramework;
using MapStudio.UI;

namespace FirstPlugin
{
    public class VLSR : MapStudio.UI.FileEditor, IFileFormat
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "VLSR" };
        public string[] Extension { get; set; } = new string[] { "*.pkg" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "vlsr");
            }
        }

        List<Mesh> Meshes = new List<Mesh>();

        STGenericModel Model;

        public void Load(Stream stream)
        {
            using (FileReader reader = new FileReader(stream))
            {
                reader.ReadSignature(4, "vlsr");
                uint numMeshes = reader.ReadUInt32();

                var dataPos = 8 + numMeshes * 4;

                for (int i = 0; i < numMeshes; i++)
                {
                    reader.SeekBegin(8 + i * 4);
                    uint offset = (uint)reader.Position + reader.ReadUInt32();

                    reader.SeekBegin(offset + 52);
                    ushort numTriangles = reader.ReadUInt16();
                    ushort numVertices = reader.ReadUInt16();

                    reader.SeekBegin(dataPos);

                    Mesh mesh = new Mesh();
                    Meshes.Add(mesh);

                    for (int j = 0; j < numVertices; j++)
                    {
                        mesh.Positions.Add(new Vector3(
                            reader.ReadSingle(),
                            reader.ReadSingle(),
                            reader.ReadSingle()));
                    }
                    for (int j = 0; j < numTriangles; j++)
                        mesh.Triangles.Add(reader.ReadStruct<Triangle>());

                    dataPos = (uint)reader.Position;
                    break;
                }

            }

            Model = ToModel();
            var render = new GenericModelRender(Model);
            this.AddRender(render);
            this.Root.AddChild(render.UINode);

            this.Root.ContextMenus.Add(new Toolbox.Core.ViewModels.MenuItemModel("Export Model", Export));
        }

        public class Mesh
        {
            public List<Triangle> Triangles = new List<Triangle>();
            public List<Vector3> Positions = new List<Vector3>();
        }

        public void Save(Stream stream)
        {
        }

        public void Export()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = true;
            dlg.FileName = $"{FileInfo.FileName}.dae";
            dlg.AddFilter(".dae", ".dae");
            if (dlg.ShowDialog())
                Export(dlg.FilePath);
        }

        public void Export(string filePath)
        {
            var genericScene = new STGenericScene();
            genericScene.Models.Add(this.Model);

            var scene = StudioConversion.FromGeneric(genericScene);
            IOManager.ExportScene(scene, filePath, new ExportSettings()
            {
                BlenderMode = true,
                ExportMaterialInfo = true,
                ExportTextureInfo = true,
            });
        }

        public STGenericModel ToModel()
        {
            STGenericModel model = new STGenericModel();

            STGenericMaterial mat = new STGenericMaterial();
            model.Materials.Add(mat);
            mat.Name = "mat";


            uint index = 0;

            foreach (var m in Meshes)
            {
                STGenericMesh mesh = new STGenericMesh();
                model.Meshes.Add(mesh);
                mesh.Name = "col";

                STPolygonGroup poly = new STPolygonGroup();
                mesh.PolygonGroups.Add(poly);

                foreach (var tri in m.Triangles)
                {
                    Vector3 nrm = new Vector3(tri.NormalX, tri.NormalY, tri.NormalZ);
                    nrm = Vector3.Normalize(nrm);

                    mesh.Vertices.Add(new STVertex() { Position = m.Positions[tri.A], Normal = nrm });
                    mesh.Vertices.Add(new STVertex() { Position = m.Positions[tri.B], Normal = nrm });
                    mesh.Vertices.Add(new STVertex() { Position = m.Positions[tri.C], Normal = nrm });

                    poly.Faces.Add(index++);
                    poly.Faces.Add(index++);
                    poly.Faces.Add(index++);
                }

                mesh.CalculateNormals();
            }


            return model;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Triangle
        {
            public ushort A;
            public ushort B;
            public ushort C;
            public sbyte NormalX;
            public sbyte NormalY;
            public sbyte NormalZ;
            public sbyte NormalW;
            public ushort Flag;
        }
    }
}
