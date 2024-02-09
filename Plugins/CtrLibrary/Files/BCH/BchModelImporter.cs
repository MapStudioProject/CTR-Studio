using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using IONET;
using IONET.Core.Model;
using IONET.Core.Skeleton;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using SPICA.PICA;
using Newtonsoft.Json;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Formats.CtrGfx.Model.Mesh;
using IONET.Core.IOMath;

namespace CtrLibrary.Bch
{
    internal class BchModelImporter
    {
        public static H3DModel Import(string filePath, BCH bchWrapper, H3DModel parent, CtrImportSettings settings)
        {
            //Load the .dae/.fbx/.obj into a scene object for importing data.
            var scene = IOManager.LoadScene(filePath, new IONET.ImportSettings() {
                Optimize = settings.Optimize,
                FlipUVs = settings.FlipUVs,
                WeightNormalize = false,
                GenerateTangentsAndBinormals = settings.ImportTangents,
            });
            var model = scene.Models[0];
            var bones = model.Skeleton.BreathFirstOrder();
            //Import textures into file/gui
            foreach (var mat in scene.Materials)
            {
                if (mat.DiffuseMap == null)
                    continue;

                string tex = mat.DiffuseMap.FilePath;
                if (File.Exists(tex))
                    bchWrapper.ImportTexture(tex);
            }
            //Copy model data into a new model 
            H3DModel h3dModel = new H3DModel()
            {
                Flags = H3DModelFlags.HasSkeleton | H3DModelFlags.IsDrawingEnabled,
                BoneScaling = H3DBoneScaling.Maya,
                WorldTransform = new SPICA.Math3D.Matrix3x4(Matrix4x4.Identity),
                Skeleton = new H3DDict<H3DBone>() { new H3DBone("Root"), },
                SilhouetteMaterialsCount = 0,
                MetaData = new H3DMetaData(),
            };
            if (parent != null)
            {
                h3dModel = new H3DModel()
                {
                    Flags = parent.Flags,
                    BoneScaling = parent.BoneScaling,
                    SilhouetteMaterialsCount = parent.SilhouetteMaterialsCount,
                    WorldTransform = parent.WorldTransform,
                    Materials = settings.UseOriginalMaterials ? parent.Materials : new H3DDict<H3DMaterial>(),
                    MetaData = parent.MetaData,
                    Skeleton = parent.Skeleton,
                    Name = parent.Name,
                    MeshNodesVisibility = parent.MeshNodesVisibility,
                    MeshNodesTree = parent.MeshNodesTree,
                    MeshNodesCount = parent.MeshNodesCount,
                };
            }
            if (settings.ImportBones)
            {
                h3dModel.Skeleton = new H3DDict<H3DBone>();

                var boneList = model.Skeleton.BreathFirstOrder();
                foreach (var bone in boneList)
                {
                    var bn = new H3DBone();
                    bn.Name = bone.Name;
                    bn.Translation = bone.Translation;
                    bn.Rotation = bone.RotationEuler;
                    bn.Scale = bone.Scale;
                    bn.BillboardMode = H3DBillboardMode.Off;
                    bn.Flags = H3DBoneFlags.IsWorldMatrixUpdated;
                    bn.UpdateTransformFlags();
                    bn.ParentIndex = -1;
                    h3dModel.Skeleton.Add(bn);
                }
                //Setup references
                for (int i = 0; i < boneList.Count; i++)
                {
                    var bn = h3dModel.Skeleton[boneList[i].Name];
                    //Setup parent
                    if (boneList[i].Parent != null)
                        bn.ParentIndex = (short)h3dModel.Skeleton.Find(boneList[i].Parent.Name);
                }

                foreach (var bone in h3dModel.Skeleton)
                    bone.CalculateTransform(h3dModel.Skeleton);
            }

            //If no bones are present, then make default root bone. This is required for transforming in worldspace with a map editor
            if (h3dModel.Skeleton.Count == 0) {
                h3dModel.Skeleton.Add(new H3DBone("Root"));
            }

            if (h3dModel.Skeleton?.Count > 0)
                h3dModel.Flags |= H3DModelFlags.HasSkeleton;

            //Force skeleton to be disabled
            if (settings.DisableSkeleton)
            {
                h3dModel.Skeleton = new H3DDict<H3DBone>();
                h3dModel.Flags &= ~H3DModelFlags.HasSkeleton;
            }

            h3dModel.Flags &= ~H3DModelFlags.HasSubMeshCulling;

            //Todo maybe include sub mesh generating (need to figure out rest of the struct)
            h3dModel.SubMeshCullings = new List<H3DSubMeshCulling>();
            //Match the file name as the imported name
            h3dModel.Name = Path.GetFileNameWithoutExtension(filePath);
            Console.WriteLine($"Importing model {model.Name}");

            //Create a skinning list for inverted matrices to convert vertex data into local space later
            Matrix4x4[] skinningMatrices = new Matrix4x4[h3dModel.Skeleton.Count];
            for (int i = 0; i < h3dModel.Skeleton.Count; i++)
            {
                //Set the world matrix as inverted
                var bn = h3dModel.Skeleton[i];
                //Important. Update the inverse matrix aswell so the calculated matrix has the same precision
                bn.CalculateTransform(h3dModel.Skeleton);

                var mat = bn.GetWorldTransform(h3dModel.Skeleton);
                Matrix4x4.Invert(mat, out Matrix4x4 inverted);
                skinningMatrices[i] = inverted;
            }
            //Prepare and import materials 
            var meshes = model.Meshes;
            foreach (var mat in scene.Materials)
            {
                //A material does not exist in the file, import a default material to use.
                if (!h3dModel.Materials.Contains(mat.Label))
                {
                    //Assign texture to new material if used
                    string texture = "";
                    if (!string.IsNullOrEmpty(mat.DiffuseMap?.FilePath) && !settings.UseOriginalMaterials)
                    {
                        string ext = mat.DiffuseMap.FilePath.Split(".").LastOrDefault();
                        //Map out the texture. Swap out the extension, files can have multiple dots which GetFileWithoutExtension can break.
                         texture = Path.GetFileName(mat.DiffuseMap.FilePath).Replace($".{ext}", texture);
                    }

                    var bcmdlMat = H3DMaterial.GetSimpleMaterial(h3dModel.Name, mat.Label, texture);
                    bcmdlMat.Name = mat.Label;
                    h3dModel.Materials.Add(bcmdlMat);
                }
            }

            //Make sure there is atleast one material present
            if (h3dModel.Materials.Count == 0)
                h3dModel.Materials.Add(H3DMaterial.GetSimpleMaterial(h3dModel.Name, "Default", ""));

            //Check the materials and map out diffuse textures
            foreach (var mat in h3dModel.Materials)
            {
                //Check for a match and that the material has diffuse used
                var iomaterial = scene.Materials.FirstOrDefault(x => x.Label == mat.Name);
                if (iomaterial != null && iomaterial.DiffuseMap != null)
                {
                    string ext = iomaterial.DiffuseMap.FilePath.Split(".").LastOrDefault();

                    //Map out the texture. Swap out the extension, files can have multiple dots which GetFileWithoutExtension can break.
                    var texture = Path.GetFileName(iomaterial.DiffuseMap.FilePath).Replace($".{ext}", "");
                    if (!string.IsNullOrEmpty(texture))
                    {
                        Console.WriteLine($"Mapping {texture} to diffuse at slot 0");
                        //Map them out into the first texturemap slot for diffuse.
                        //Todo might work better to map based on combiner data?
                        mat.Texture0Name = texture;
                    }
                }
            }

            //Prepare mesh skinning settings
            foreach (var iomesh in model.Meshes)
            {
                for (int v = 0; v < iomesh.Vertices.Count; v++)
                {
                    if (settings.LimitSkinCount || iomesh.Vertices[v].Envelope.Weights.Count > 4)
                        iomesh.Vertices[v].Envelope.LimtSkinCount(settings.SkinCountLimit);
                    iomesh.Vertices[v].Envelope.NormalizeByteType(settings.BoneWeights.Scale);

                    iomesh.Vertices[v].Envelope.Weights = iomesh.Vertices[v].Envelope.Weights.OrderByDescending(x => x.Weight).ToList();
                }
            }

            if (!settings.UseSingleAttributeBuffer)
            {
                //Import mesh data
                foreach (var mesh in meshes)
                    ConvertMesh(scene, mesh, h3dModel, skinningMatrices, settings);
            }
            else
            {
                //Setup meshes with vertices into one buffer
                ConvertMeshesSingleBuffer(scene, meshes, h3dModel, skinningMatrices, settings);
            }

            h3dModel.MeshNodesCount = h3dModel.MeshNodesTree.Count;

            return h3dModel;
        }

        //Single buffer used by smash 3ds
        private static void ConvertMeshesSingleBuffer(IONET.Core.IOScene scene, List<IOMesh> meshes,
    H3DModel h3dModel, Matrix4x4[] skinningMatrices, CtrImportSettings settings)
        {
            var skinningCount = GetMaxSkinCount(meshes);
            var attributes = CreateSingleBufferAttributes(meshes, skinningCount, settings);

            //Total vertex buffer in single list
            List<PICAVertex> verts = new List<PICAVertex>();

            var vertexStride = VerticesConverter.CalculateStride(attributes);
            int vertexID = 0;

            foreach (var iomesh in meshes)
            {
                string meshName = iomesh.Name;

                if (!string.IsNullOrEmpty(meshName) && !h3dModel.MeshNodesTree.Contains(meshName))
                {
                    h3dModel.MeshNodesTree.Add(meshName);
                    h3dModel.MeshNodesVisibility.Add(true);
                }

                int singleBindIndex = 0;

                //Check how many bones are used total
                var boneList = iomesh.Vertices.SelectMany(x => x.Envelope.Weights.Select(x => x.BoneName)).Distinct().ToList();

                if (skinningCount == 0)
                {
                    //Bind bone node from mesh
                    var singleBindBone = h3dModel.Skeleton.FirstOrDefault(x => x.Name == meshName);
                    //Bind bone node from single skinned bone
                    if (boneList?.Count == 1)
                        singleBindBone = h3dModel.Skeleton.FirstOrDefault(x => x.Name == boneList[0]);
                    //Get bind matrix for single binds
                    if (singleBindBone != null && h3dModel.Skeleton.Find(singleBindBone.Name) != -1)
                        singleBindIndex = h3dModel.Skeleton.Find(singleBindBone.Name);

                    //Convert the positions into local space for single binds
                    foreach (var vertex in iomesh.Vertices)
                    {
                        //Rigid binds to local space
                        if (skinningMatrices.Length > singleBindIndex)
                        {
                            vertex.Position = Vector3.Transform(vertex.Position, skinningMatrices[singleBindIndex]);
                            vertex.Normal = Vector3.TransformNormal(vertex.Normal, skinningMatrices[singleBindIndex]);
                        }
                    }
                }

                H3DMesh mesh = new H3DMesh()
                {
                    Type = H3DMeshType.Normal,
                };
                h3dModel.AddMesh(mesh);

                //Skinning
                mesh.Skinning = H3DMeshSkinning.Mixed;

                CalculatePositionScaleOffset(attributes.FirstOrDefault(), mesh, iomesh);

                //Attributes
                mesh.Attributes = attributes;
                mesh.Layer = 0;
                mesh.MaterialIndex = 0;
                mesh.VertexStride = vertexStride;

                PICAVertex[] vertices = GetPICAVertices(iomesh.Vertices, skinningMatrices, h3dModel, skinningCount == 1).ToArray();

                //Generate sub meshes.
                //Very important that this is called before the raw buffer is created
                foreach (var poly in iomesh.Polygons)
                {
                    //Generates the sub meshes representing the face data
                    var subMeshes = GenerateSubMeshes(h3dModel, iomesh, poly, skinningCount, singleBindIndex, ref vertices, 16, vertexID);
                    mesh.SubMeshes.AddRange(subMeshes);
                    //Map the material if one matches from the .dae. 
                    var mat = scene.Materials.FirstOrDefault(x => x.Name == poly.MaterialName);
                    if (mat != null)
                    {
                        //Searh for the material. This should never be -1 as all the materials are added from the file if used.
                        var index = h3dModel.Materials.Find(mat.Label);
                        if (index != -1)
                            mesh.MaterialIndex = (ushort)index;
                    }
                    else
                        Console.WriteLine($"Cannot find material {poly.MaterialName}!");
                }

                verts.AddRange(vertices);

                //Increase vertex ID used for the index buffer as it uses one global list
                vertexID += vertices.Length;

                //Check what material gets used and set the mesh layer to what is ideal
                mesh.Layer = h3dModel.Materials[mesh.MaterialIndex].MaterialParams.RenderLayer;

                mesh.NodeIndex = 0;
                for (int i = 0; i < h3dModel.MeshNodesTree.Count; i++)
                {
                    if (h3dModel.MeshNodesTree.Find(i) == meshName)
                        mesh.NodeIndex = (ushort)i;
                }
                mesh.MetaData = new H3DMetaData();
                mesh.UpdateBoolUniforms(h3dModel.Materials[mesh.MaterialIndex], settings.IsPokemon, settings.IsSmash3DS);
            }
            //Lastly go through each mesh and make sure the indices use the single vertex list
            var vertexBuffer = VerticesConverter.GetBuffer(verts, attributes, vertexStride);

            for (int i = 0; i < h3dModel.Meshes.Count; i++)
                h3dModel.Meshes[i].RawBuffer = vertexBuffer;
        }

        private static void ConvertMesh(IONET.Core.IOScene scene, IOMesh iomesh, 
            H3DModel h3dModel, Matrix4x4[] skinningMatrices, CtrImportSettings settings) 
        {
            if (iomesh.Vertices.Count == 0)
                return;

            string meshName = iomesh.Name;

            if (settings.ImportTangents)
                GenerateTangentsAndBitangents(iomesh);

            if (!string.IsNullOrEmpty(meshName) && !h3dModel.MeshNodesTree.Contains(meshName))
            {
                h3dModel.MeshNodesTree.Add(meshName);
                h3dModel.MeshNodesVisibility.Add(true);
            }

            int skinningCount = 0;
            int singleBindIndex = 0;

            //Calculate skinning amount from max amount of weights used
            skinningCount = iomesh.Vertices.Max(x => x.Envelope.Weights.Count);
            //Check how many bones are used total
            var boneList = iomesh.Vertices.SelectMany(x => x.Envelope.Weights.Select(x => x.BoneName)).Distinct().ToList();
            //If only one bone is used, no skinning requred as a bone can be used as a single binded rigid body.
            //if (boneList?.Count == 1 || settings.DisableSkeleton)

            if (settings.DisableSkeleton)
                skinningCount = 0;

            Console.WriteLine($"skinningCount {skinningCount}");

            if (skinningCount == 0)
            {
                //Bind bone node from mesh
                var singleBindBone = h3dModel.Skeleton.FirstOrDefault(x => x.Name == meshName);
                //Bind bone node from single skinned bone
                if (boneList?.Count == 1)
                    singleBindBone = h3dModel.Skeleton.FirstOrDefault(x => x.Name == boneList[0]);
                //Get bind matrix for single binds
                if (singleBindBone != null && h3dModel.Skeleton.Find(singleBindBone.Name) != -1)
                    singleBindIndex = h3dModel.Skeleton.Find(singleBindBone.Name);

                //Convert the positions into local space for single binds
                foreach (var vertex in iomesh.Vertices)
                {
                    //Rigid binds to local space
                    if (skinningMatrices.Length > singleBindIndex)
                    {
                        vertex.Position = Vector3.Transform(vertex.Position, skinningMatrices[singleBindIndex]);
                        vertex.Normal = Vector3.TransformNormal(vertex.Normal, skinningMatrices[singleBindIndex]);
                    }
                }
            }

            H3DMesh mesh = new H3DMesh()
            {
                Type = H3DMeshType.Normal,
            };
            h3dModel.AddMesh(mesh);

            //Pica attributes from vertex data
            var attributes = CreateAttributes(iomesh, skinningCount, settings);

            CalculatePositionScaleOffset(attributes.FirstOrDefault(), mesh, iomesh);

            //Convert attributes into pica attributes for conversion into a buffer
            PICAVertex[] vertices = GetPICAVertices(iomesh.Vertices, skinningMatrices, h3dModel, skinningCount == 1).ToArray();

            //Skinning
            mesh.Skinning = H3DMeshSkinning.Mixed;
            if (skinningCount == 1)
                mesh.Skinning = H3DMeshSkinning.Rigid;
            if (skinningCount > 1)
                mesh.Skinning = H3DMeshSkinning.Smooth;
            //Attributes
            mesh.Attributes = attributes;
            mesh.Layer = 0;
            mesh.MaterialIndex = 0;
            //Generate sub meshes.
            //Very important that this is called before the raw buffer is created
            foreach (var poly in iomesh.Polygons)
            {
                //Generates the sub meshes representing the face data
                var subMeshes = GenerateSubMeshes(h3dModel, iomesh, poly, skinningCount, singleBindIndex, ref vertices);
                mesh.SubMeshes.AddRange(subMeshes);
                //Map the material if one matches from the .dae. 
                var mat = scene.Materials.FirstOrDefault(x => x.Name == poly.MaterialName);
                if (mat != null)
                {
                    //Searh for the material. This should never be -1 as all the materials are added from the file if used.
                    var index = h3dModel.Materials.Find(mat.Label);
                    if (index != -1)
                        mesh.MaterialIndex = (ushort)index;
                }
                else
                    Console.WriteLine($"Cannot find material {poly.MaterialName}!");
            }

            //Check what material gets used and set the mesh layer to what is ideal
            mesh.Layer = h3dModel.Materials[mesh.MaterialIndex].MaterialParams.RenderLayer;

            mesh.VertexStride = VerticesConverter.CalculateStride(attributes);
            mesh.RawBuffer = VerticesConverter.GetBuffer(vertices, attributes, mesh.VertexStride);

            Console.WriteLine($"stride {mesh.VertexStride}");

            mesh.NodeIndex = 0;
            for (int i = 0; i < h3dModel.MeshNodesTree.Count; i++)
            {
                if (h3dModel.MeshNodesTree.Find(i) == meshName)
                    mesh.NodeIndex = (ushort)i;
            }

            if (!settings.IsSmash3DS) //Smash 3DS does not use fixed attributes
            {
                //Create a default color set if one is not present
                if (settings.ImportVertexColors && !mesh.Attributes.Any(x => x.Name == PICAAttributeName.Color))
                {
                    mesh.FixedAttributes.Add(new PICAFixedAttribute()
                    {
                        Name = PICAAttributeName.Color,
                        Value = new PICAVectorFloat24(1, 1, 1, 1),
                    });
                }

                if (!mesh.Attributes.Any(x => x.Name == PICAAttributeName.BoneWeight))
                {
                    mesh.FixedAttributes.Add(new PICAFixedAttribute()
                    {
                        Name = PICAAttributeName.BoneWeight,
                        Value = new PICAVectorFloat24(1, 0, 0, 1),
                    });
                }

                if (!mesh.Attributes.Any(x => x.Name == PICAAttributeName.BoneIndex))
                {
                    mesh.FixedAttributes.Add(new PICAFixedAttribute()
                    {
                        Name = PICAAttributeName.BoneIndex,
                        Value = new PICAVectorFloat24(0, 0, 0, 1),
                    });
                }
            }

            mesh.UpdateBoolUniforms(h3dModel.Materials[mesh.MaterialIndex], settings.IsPokemon, settings.IsSmash3DS);

            mesh.MetaData = new H3DMetaData();
            if (settings.IsPokemon)
                mesh.MetaData.Add(new H3DMetaDataValue("$BBoxMinMax", CalculateBoundingMinMax(iomesh)));
            if (!settings.IsSmash3DS)
                mesh.MetaData.Add(new H3DMetaDataValue("OBBox", CalculateBounding(iomesh)));
        }


        static void CalculatePositionScaleOffset(PICAAttribute attributePos, H3DMesh shape, IOMesh iomesh)
        {
            //Only calculate scale/offset when not a float
            if (attributePos.Format == PICAAttributeFormat.Float)
                return;

            //Calculate AABB
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < iomesh.Vertices.Count; i++)
            {
                minX = Math.Min(minX, iomesh.Vertices[i].Position.X);
                minY = Math.Min(minY, iomesh.Vertices[i].Position.Y);
                minZ = Math.Min(minZ, iomesh.Vertices[i].Position.Z);
                maxX = Math.Max(maxX, iomesh.Vertices[i].Position.X);
                maxY = Math.Max(maxY, iomesh.Vertices[i].Position.Y);
                maxZ = Math.Max(maxZ, iomesh.Vertices[i].Position.Z);
            }

            //Min/max coordinates
            Vector3 max = new Vector3(maxX, maxY, maxZ);
            Vector3 min = new Vector3(minX, minY, minZ);
            //Get smallest/largest value
            float smallest = MathF.Min(MathF.Min(min.X, min.Z), min.Y);
            float largest = MathF.Max(MathF.Max(max.X, max.Z), max.Y);
            //Precision value by data type
            float precision = 0.001f;

            //Get the scale by smallest/largest value
            float GetScale(float minV, float maxV)
            {
                var nvalues = 1 + MathF.Ceiling((maxV - minV) / (2 * precision));
                var n = MathF.Ceiling(MathF.Log2(nvalues));
                return (maxV - minV) / (MathF.Pow(2, n) - 1);
            }

            shape.PositionOffset = new Vector4();
            attributePos.Scale = GetScale(smallest, largest);
        }

        static float[] CalculateBoundingMinMax(IOMesh iomesh)
        {
            //Calculate AABB
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < iomesh.Vertices.Count; i++)
            {
                minX = Math.Min(minX, iomesh.Vertices[i].Position.X);
                minY = Math.Min(minY, iomesh.Vertices[i].Position.Y);
                minZ = Math.Min(minZ, iomesh.Vertices[i].Position.Z);
                maxX = Math.Max(maxX, iomesh.Vertices[i].Position.X);
                maxY = Math.Max(maxY, iomesh.Vertices[i].Position.Y);
                maxZ = Math.Max(maxZ, iomesh.Vertices[i].Position.Z);
            }
            return new float[6] { minX, minY, minZ, maxX, maxY, maxZ };
        }

        static H3DBoundingBox CalculateBounding(IOMesh iomesh)
        {
            //Calculate AABB
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < iomesh.Vertices.Count; i++)
            {
                minX = Math.Min(minX, iomesh.Vertices[i].Position.X);
                minY = Math.Min(minY, iomesh.Vertices[i].Position.Y);
                minZ = Math.Min(minZ, iomesh.Vertices[i].Position.Z);
                maxX = Math.Max(maxX, iomesh.Vertices[i].Position.X);
                maxY = Math.Max(maxY, iomesh.Vertices[i].Position.Y);
                maxZ = Math.Max(maxZ, iomesh.Vertices[i].Position.Z);
            }

            Vector3 max = new Vector3(maxX, maxY, maxZ);
            Vector3 min = new Vector3(minX, minY, minZ);

            //Extend + center
            float xxMax = GetExtent(max.X, min.X);
            float yyMax = GetExtent(max.Y, min.Y);
            float zzMax = GetExtent(max.Z, min.Z);
            Vector3 extend = new Vector3(xxMax, yyMax, zzMax);

            return new H3DBoundingBox()
            {
                Center = min + ((max - min) / 2),
                Size = extend, 
                Orientation = new SPICA.Math3D.Matrix3x3(Matrix4x4.Identity),
            };
        }
        private static float GetExtent(float max, float min)
        {
            return (float)Math.Max(Math.Sqrt(max * max), Math.Sqrt(min * min));
        }

        static List<H3DSubMesh> GenerateSubMeshes(H3DModel gfxModel, IOMesh mesh, IOPolygon poly,
            int skinningCount, int singleBindIndex, ref PICAVertex[] vertices, int max_bones = 16, int indexStart = 0)
        {
            Dictionary<PICAVertex, ushort> remapVertex = new Dictionary<PICAVertex, ushort>();
            List<PICAVertex> newVertices = new List<PICAVertex>();

            List<ushort> faces = new List<ushort>();
            foreach (var index in poly.Indicies)
                faces.Add((ushort)index);

            Queue<ushort> IndicesQueue = new Queue<ushort>(faces);
            List<H3DSubMesh> subMeshes = new List<H3DSubMesh>();

            //Split the mesh into sub meshes based on the max amount of bones used
            while (IndicesQueue.Count > 0)
            {
                int Count = IndicesQueue.Count / 3;

                List<ushort> Indices = new List<ushort>();
                List<int> Bones = new List<int>();

                while (Count-- > 0)
                {
                    //Split by triangle
                    ushort i0 = IndicesQueue.Dequeue();
                    ushort i1 = IndicesQueue.Dequeue();
                    ushort i2 = IndicesQueue.Dequeue();

                    //Add to the bone stack and check if the index list must be split
                    List<int> TempBones = new List<int>(12);

                    //Check each individual vertex on the triangle and add to the full bone list
                    void AddIndices(int ind, ref PICAVertex[] vertices)
                    {
                        for (int j = 0; j < mesh.Vertices[ind].Envelope.Weights.Count; j++)
                        {
                            var b0 = vertices[ind].Indices[j];
                            if (b0 != -1 && (!(Bones.Contains(b0) || TempBones.Contains(b0)))) TempBones.Add(b0);
                        }
                    }

                    Vector4 GetNormalValues(Vector4 vec)
                    {
                        return new Vector4(
                            !float.IsNaN(vec.X) ? vec.X : 0,
                            !float.IsNaN(vec.Y) ? vec.Y : 1,
                            !float.IsNaN(vec.Z) ? vec.Z : 0,
                            !float.IsNaN(vec.W) ? vec.W : 0);
                    }

                    void UpdateVertex(int ind, ref PICAVertex[] vertices)
                    {
                        //Create a new vertex instance, as we need to assign new vertices
                        var v = new PICAVertex()
                        {
                            Position = vertices[ind].Position,
                            Normal = GetNormalValues(vertices[ind].Normal),
                            Color = vertices[ind].Color,
                            Tangent = GetNormalValues(vertices[ind].Tangent),
                            TexCoord0 = vertices[ind].TexCoord0,
                            TexCoord1 = vertices[ind].TexCoord1,
                            TexCoord2 = vertices[ind].TexCoord2,
                            Indices = new BoneIndices()
                            {
                                b0 = vertices[ind].Indices.b0,
                                b1 = vertices[ind].Indices.b1,
                                b2 = vertices[ind].Indices.b2,
                                b3 = vertices[ind].Indices.b3,
                            },
                            Weights = new BoneWeights()
                            {
                                w0 = vertices[ind].Weights.w0,
                                w1 = vertices[ind].Weights.w1,
                                w2 = vertices[ind].Weights.w2,
                                w3 = vertices[ind].Weights.w3,
                            },
                        };
                        //NOTE important we set the indices first before adding to index list to compare changes

                        //Correct bone index from the sub mesh bone table
                        for (int j = 0; j < 4; j++)
                        {
                            var id = Bones.IndexOf(vertices[ind].Indices[j]);
                            if (id != -1)
                                v.Indices[j] = id;
                        }
                        if (!remapVertex.ContainsKey(v))
                        {
                            remapVertex.Add(v, (ushort)newVertices.Count);
                            newVertices.Add(v);
                        }
                        //Link the index with the remapped vertex placement
                        Indices.Add((ushort)(indexStart + remapVertex[v]));
                    }

                    //Add each triangle index to the bone stack for checking if it reached the bone counter
                    AddIndices(i0, ref vertices);
                    AddIndices(i1, ref vertices);
                    AddIndices(i2, ref vertices);

                    //Bone stack has reached the limit, then split into a new index list
                    if (Bones.Count + TempBones.Count > max_bones)
                    {
                        IndicesQueue.Enqueue(i0);
                        IndicesQueue.Enqueue(i1);
                        IndicesQueue.Enqueue(i2);
                    }
                    else //bone limit not reached, continue as normal by adding the indices and bones to the sub mesh
                    {
                        Bones.AddRange(TempBones);
                        //Update the vertex bone instance to get the correct index
                        UpdateVertex(i0, ref vertices);
                        UpdateVertex(i1, ref vertices);
                        UpdateVertex(i2, ref vertices);
                    }
                }
                //Add the sub mesh to the list for the shape

                //Sub mesh
                H3DSubMesh SM = new H3DSubMesh();
                //Determine the kind of skinning to use
                SM.Skinning = H3DSubMeshSkinning.None;
                if (skinningCount == 1)
                    SM.Skinning = H3DSubMeshSkinning.Rigid;
                if (skinningCount > 1)
                    SM.Skinning = H3DSubMeshSkinning.Smooth;
                //Face data
                SM.PrimitiveMode = PICAPrimitiveMode.Triangles;
                SM.Indices = Indices.ToArray();
                //Add the bone indices to the sub mesh
                SM.BoneIndices = new ushort[Bones.Count];
                for (int i = 0; i < Bones.Count; i++)
                    SM.BoneIndices[i] = (ushort)Bones[i];

                //Need to atleast bind to a single bone.
                //If no bones are binded, the full model cannot be moved within a map editor if required.
                if (SM.BoneIndices.Length == 0 && gfxModel.Skeleton.Count > 0)
                    SM.BoneIndices = new ushort[1];

                //Add the sub mesh to the list for the shape
                subMeshes.Add(SM);
            }

            vertices = newVertices.ToArray();

            return subMeshes;
        }

        //Generates a single skin count for single buffer attributes
        static int GetMaxSkinCount(List<IOMesh> meshes)
        {
            return meshes.Max(x => x.Vertices.Max(x => x.Envelope.Weights.Count));
        }

        //Generates a single list of attributes for all meshes
        //This is required for smash 3ds with mbn generating
        static List<PICAAttribute> CreateSingleBufferAttributes(List<IOMesh> meshes, int skinningCount, CtrImportSettings settings)
        {
            List<PICAAttribute> attributes = new List<PICAAttribute>();
            //Vertex positions
            attributes.Add(new PICAAttribute()
            {
                Elements = 3,
                Format = settings.Position.Format,
                Name = PICAAttributeName.Position,
                Scale = settings.Position.Scale,
            });
            //Vertex normals
            if (meshes.Any(x => x.HasNormals))
            {
                attributes.Add(new PICAAttribute()
                {
                    Elements = 3,
                    Format = settings.Normal.Format,
                    Name = PICAAttributeName.Normal,
                    Scale = settings.Normal.Scale,
                });
            }
            //Texture coordinates (supports up to 3)
            for (int i = 0; i < 3; i++)
            {
                if (meshes.Any(x => x.HasUVSet(i)))
                {
                    attributes.Add(new PICAAttribute()
                    {
                        Elements = 2,
                        Format = settings.TexCoord.Format,
                        Name = (PICAAttributeName)((int)PICAAttributeName.TexCoord0 + i),
                        Scale = settings.TexCoord.Scale,
                    });
                }
            }
            //Tangents for lighting
            if (settings.ImportTangents)
            {
                attributes.Add(new PICAAttribute()
                {
                    Elements = 3,
                    Format = settings.Tangents.Format,
                    Name = PICAAttributeName.Tangent,
                    Scale = settings.Tangents.Scale,
                });
            }
            //Vertex colors
            if (settings.ImportVertexColors && meshes.Any(x => x.HasColorSet(0)))
            {
                var format = settings.BoneWeights.Format;
                if (format == PICAAttributeFormat.Byte)
                    format = PICAAttributeFormat.Ubyte;

                attributes.Add(new PICAAttribute()
                {
                    Elements = 4,
                    Format = format,
                    Name = PICAAttributeName.Color,
                    Scale = settings.Colors.Scale,
                });
            }
            //Use bone indices for rigging
            if (meshes.Any(x => x.HasEnvelopes()) && skinningCount > 0)
            {
                var indformat = settings.BoneIndices.Format;
                if (indformat == PICAAttributeFormat.Byte)
                    indformat = PICAAttributeFormat.Ubyte;

                attributes.Add(new PICAAttribute()
                {
                    Elements = skinningCount,
                    Format = indformat,
                    Name = PICAAttributeName.BoneIndex,
                    Scale = settings.BoneIndices.Scale,
                });
                //Skinning over 1 uses weights for blending
                if (skinningCount > 1)
                {
                    var format = settings.BoneWeights.Format;
                    if (format == PICAAttributeFormat.Byte)
                        format = PICAAttributeFormat.Ubyte;

                    attributes.Add(new PICAAttribute()
                    {
                        Elements = skinningCount,
                        Format = format,
                        Name = PICAAttributeName.BoneWeight,
                        Scale = settings.BoneWeights.Scale,
                    });
                }
            }
            return attributes;
        }

        static List<PICAAttribute> CreateAttributes(IOMesh mesh, int skinningCount, CtrImportSettings settings)
        {
            List<PICAAttribute> attributes = new List<PICAAttribute>();
            //Vertex positions
            attributes.Add(new PICAAttribute()
            {
                Elements = 3,
                Format = settings.Position.Format,
                Name = PICAAttributeName.Position,
                Scale = settings.Position.Scale,
            });
            //Vertex normals
            if (mesh.HasNormals)
            {
                attributes.Add(new PICAAttribute()
                {
                    Elements = 3,
                    Format = settings.Normal.Format,
                    Name = PICAAttributeName.Normal,
                    Scale = settings.Normal.Scale,
                });
            }
            //Texture coordinates (supports up to 3)
            for (int i = 0; i < 3; i++)
            {
                if (mesh.HasUVSet(i))
                {
                    attributes.Add(new PICAAttribute()
                    {
                        Elements = 2,
                        Format = settings.TexCoord.Format,
                        Name = (PICAAttributeName)((int)PICAAttributeName.TexCoord0 + i),
                        Scale = settings.TexCoord.Scale,
                    });
                }
            }
            //Tangents for lighting
            if (settings.ImportTangents)
            {
                attributes.Add(new PICAAttribute()
                {
                    Elements = 3,
                    Format = settings.Tangents.Format,
                    Name = PICAAttributeName.Tangent,
                    Scale = settings.Tangents.Scale,
                });
            }
            //Vertex colors
            if (settings.ImportVertexColors && mesh.HasColorSet(0))
            {
                var format = settings.BoneWeights.Format;
                if (format == PICAAttributeFormat.Byte)
                    format = PICAAttributeFormat.Ubyte;

                attributes.Add(new PICAAttribute()
                {
                    Elements = 4,
                    Format = format,
                    Name = PICAAttributeName.Color,
                    Scale = settings.Colors.Scale,
                });
            }
            //Use bone indices for rigging
            if (mesh.HasEnvelopes() && skinningCount > 0)
            {
                var indformat = settings.BoneIndices.Format;
                if (indformat == PICAAttributeFormat.Byte)
                    indformat = PICAAttributeFormat.Ubyte;

                attributes.Add(new PICAAttribute()
                {
                    Elements = skinningCount,
                    Format = indformat,
                    Name = PICAAttributeName.BoneIndex,
                    Scale = settings.BoneIndices.Scale,
                });
                //Skinning over 1 uses weights for blending
                if (skinningCount > 1)
                {
                    var format = settings.BoneWeights.Format;
                    if (format == PICAAttributeFormat.Byte)
                        format = PICAAttributeFormat.Ubyte;

                    attributes.Add(new PICAAttribute()
                    {
                        Elements = skinningCount,
                        Format = format,
                        Name = PICAAttributeName.BoneWeight,
                        Scale = settings.BoneWeights.Scale,
                    });
                }
            }
            return attributes;
        }

        static List<PICAVertex> GetPICAVertices(List<IOVertex> vertices, Matrix4x4[] skinningMatrices, H3DModel model, bool rigid)
        {
            int index = 0;
            List<PICAVertex> verts = new List<PICAVertex>();
            foreach (var vertex in vertices)
            {
                var picaVertex = new PICAVertex();
                picaVertex.Position = new Vector4(vertex.Position.X, vertex.Position.Y, vertex.Position.Z, 1.0f);
                picaVertex.Normal = new Vector4(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z, 1.0f);
                picaVertex.Color = new Vector4(1, 1, 1, 1);

                if (vertex.Colors?.Count > 0)
                    picaVertex.Color = new Vector4(vertex.Colors[0].X, vertex.Colors[0].Y, vertex.Colors[0].Z, vertex.Colors[0].W);
                if (vertex.UVs?.Count > 0)
                    picaVertex.TexCoord0 = new Vector4(vertex.UVs[0].X, vertex.UVs[0].Y, 0, 0);
                if (vertex.UVs?.Count > 1)
                    picaVertex.TexCoord1 = new Vector4(vertex.UVs[1].X, vertex.UVs[1].Y, 0, 0);
                if (vertex.UVs?.Count > 2)
                    picaVertex.TexCoord2 = new Vector4(vertex.UVs[2].X, vertex.UVs[2].Y, 0, 0);
                picaVertex.Tangent = new Vector4(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z, 1.0f);

                //Default weight values in the event these are forced to be used
                if (vertex.Envelope.Weights.Count == 0)
                {
                    picaVertex.Indices.b0 = 0;
                    picaVertex.Weights.w0 = 1;
                }

                for (int j = 0; j < vertex.Envelope.Weights.Count; j++)
                {
                    var boneWeight = vertex.Envelope.Weights[j];
                    if (!model.Skeleton.Contains(boneWeight.BoneName))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Missing bone {boneWeight.BoneName}!");
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    var bone = model.Skeleton[boneWeight.BoneName];
                    int bindex = model.Skeleton.Find(bone.Name);

                    picaVertex.Weights[j] = boneWeight.Weight;
                    picaVertex.Indices[j] = bindex;

                    if (rigid)
                    {
                        picaVertex.Weights[j] = 1.0f;

                        picaVertex.Position = new Vector4(Vector3.Transform(vertex.Position, skinningMatrices[bindex]), 1.0f);
                        picaVertex.Normal = new Vector4(Vector3.TransformNormal(vertex.Normal, skinningMatrices[bindex]), 1.0f);
                    }
                }

                verts.Add(picaVertex);
                index++;
            }
            return verts;
        }

        /// <summary>
        /// Generates Tangents and Bitangents for the vertices
        /// </summary>
        public static void GenerateTangentsAndBitangents(IOMesh mesh)
        {
            List<int> indices = new List<int>();

            foreach (var v in mesh.Polygons)
            {
                v.ToTriangles(mesh);

                if (v.PrimitiveType != IOPrimitive.TRIANGLE)
                    continue;

                indices.AddRange(v.Indicies);
            }

            var positions = mesh.Vertices.Select(e => e.Position).ToList();
            var normals = mesh.Vertices.Select(e => e.Normal).ToList();
            var uvs = mesh.HasUVSet(0) ? mesh.Vertices.Select(e => e.UVs[0]).ToList() : mesh.Vertices.Select(e => Vector2.Zero).ToList();

            //Flip for BCH/BCRES
            for (int i = 0; i < uvs.Count; i++)
                uvs[i] = new Vector2(uvs[i].X, 1f - uvs[i].Y);

            TriangleListUtils.CalculateTangentsBitangents(positions, normals, uvs, indices, out Vector3[] tangents, out Vector3[] bitangents);

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var vertex = mesh.Vertices[i];
                vertex.Tangent = tangents[i];
                vertex.Binormal = bitangents[i];
                mesh.Vertices[i] = vertex;
            }
        }
    }
}
