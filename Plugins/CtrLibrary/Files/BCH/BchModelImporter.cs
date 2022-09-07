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
                var mat = bn.GetWorldTransform(h3dModel.Skeleton);
                Matrix4x4.Invert(mat, out Matrix4x4 inverted);
                skinningMatrices[i] = inverted;
            }
            //Prepare and import materials 
            var meshes = CleanupMeshes(model.Meshes);
            foreach (var mat in scene.Materials)
            {
                //A material does not exist in the file, import a default material to use.
                if (!h3dModel.Materials.Contains(mat.Label))
                {
                    var bcmdlMat = H3DMaterial.GetSimpleMaterial(h3dModel.Name, mat.Label, "");
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
            //Import mesh data
            foreach (var mesh in meshes)
            {
                ConvertMesh(scene, mesh, h3dModel, skinningMatrices, settings);
            }
            h3dModel.MeshNodesCount = h3dModel.MeshNodesTree.Count;

            return h3dModel;
        }
        private static void ConvertMesh(IONET.Core.IOScene scene, IOMesh iomesh, 
            H3DModel h3dModel, Matrix4x4[] skinningMatrices, CtrImportSettings settings)
        {
            string meshName = iomesh.Name;

            if (!string.IsNullOrEmpty(meshName) && !h3dModel.MeshNodesTree.Contains(meshName))
            {
                h3dModel.MeshNodesTree.Add(meshName);
                h3dModel.MeshNodesVisibility.Add(true);
            }

            for (int v = 0; v < iomesh.Vertices.Count; v++)
                iomesh.Vertices[v].Envelope.NormalizeByteType();

            int skinningCount = 0;
            int singleBindIndex = 0;

            //Normalize the weights as byte weights if requried
            if (settings.BoneWeights.Format == PICAAttributeFormat.Ubyte)
            {
                for (int v = 0; v < iomesh.Vertices.Count; v++)
                    iomesh.Vertices[v].Envelope.NormalizeByteType();
            }

            //Calculate skinning amount from max amount of weights used
            skinningCount = iomesh.Vertices.Max(x => x.Envelope.Weights.Count);
            //Check how many bones are used total
            var boneList = iomesh.Vertices.SelectMany(x => x.Envelope.Weights.Select(x => x.BoneName)).Distinct().ToList();
            //If only one bone is used, no skinning requred as a bone can be used as a single binded rigid body.
            if (boneList?.Count == 1)
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
                if (singleBindBone != null)
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

            //Convert attributes into pica attributes for conversion into a buffer
            var vertices = GetPICAVertices(iomesh.Vertices, skinningMatrices, h3dModel, skinningCount == 1).ToArray();

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

            //Create a default color set if one is not present
             if (!mesh.Attributes.Any(x => x.Name == PICAAttributeName.Color))
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

            mesh.UpdateBoolUniforms(h3dModel.Materials[mesh.MaterialIndex], settings.IsPokemon);

            mesh.MetaData = new H3DMetaData();
         //   mesh.MetaData.Add(new H3DMetaDataValue("$BBoxMinMax", CalculateBoundingMinMax(iomesh)));
            mesh.MetaData.Add(new H3DMetaDataValue("OBBox", CalculateBounding(iomesh)));
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
            int skinningCount, int singleBindIndex, ref PICAVertex[] vertices, int max_bones = 16)
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
                    void UpdateVertex(int ind, ref PICAVertex[] vertices)
                    {
                        //Create a new vertex instance, as we need to assign new vertices
                        var v = new PICAVertex()
                        {
                            Position = vertices[ind].Position,
                            Normal = vertices[ind].Normal,
                            Color = vertices[ind].Color,
                            Tangent = vertices[ind].Tangent,
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
                        Indices.Add(remapVertex[v]);
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
                //Check what format to use indices. 255 > is larger than a byte
                bool is16Bit = Indices.Any(x => x > 0xFF);
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
            //Vertex colors
            if (mesh.HasColorSet(0))
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

        //Cleanups the many sub meshes SPICA creates on export by combining same material mapping
        static List<IOMesh> CleanupMeshes(List<IOMesh> meshes)
        {
            return meshes;

            List<string> input = new List<string>();

            List<IOMesh> newList = new List<IOMesh>();
            foreach (var mesh in meshes)
            {
                if (input.Contains(mesh.Polygons[0].MaterialName))
                    continue;

                input.Add(mesh.Polygons[0].MaterialName);
                newList.Add(mesh);

                //Combine meshes by polygons and vertices
                var meshDupes = meshes.Where(x => x.Polygons[0].MaterialName == mesh.Polygons[0].MaterialName).ToList();
                if (meshDupes.Count > 1)
                {
                    foreach (var msh in meshDupes)
                    {
                        if (msh == mesh)
                            continue;

                        //Combine indices and vertex data, remap indices
                        IOPolygon poly = mesh.Polygons[0];
                        foreach (var p in msh.Polygons)
                        {
                            Dictionary<IOVertex, int> remapVertex = new Dictionary<IOVertex, int>();
                            for (int i = 0; i < p.Indicies.Count; i++)
                            {
                                var v = msh.Vertices[p.Indicies[i]];
                                if (!remapVertex.ContainsKey(v))
                                {
                                    remapVertex.Add(v, mesh.Vertices.Count);
                                    mesh.Vertices.Add(v);
                                }
                                poly.Indicies.Add(remapVertex[v]);
                            }
                            remapVertex.Clear();
                        }
                    }
                    mesh.Optimize();
                }
            }
            return newList;
        }
    }
}
