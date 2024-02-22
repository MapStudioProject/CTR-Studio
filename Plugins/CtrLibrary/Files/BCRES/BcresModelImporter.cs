using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using IONET;
using IONET.Core.Model;
using IONET.Core.Skeleton;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.Model.Mesh;
using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using SPICA.PICA;
using Newtonsoft.Json;
using System.IO;
using static OpenTK.Graphics.OpenGL.GL;

namespace CtrLibrary.Bcres
{
    internal class BcresModelImporter
    {
        public static GfxModel Import(string filePath, BCRES bcresWrapper, GfxModel parent, CtrImportSettings settings)
        {
            //Load the .dae/.fbx/.obj into a scene object for importing data.
            var scene = IOManager.LoadScene(filePath, new IONET.ImportSettings()
            {
                Optimize = false,
                FlipUVs = settings.FlipUVs,
                WeightNormalize = true,
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
                    bcresWrapper.ImportTexture(tex);
            }

            //We currently always want to use bones for map editor purposes
            bool hasBones = true;
            //Copy model data into a new model for both static and normal bone types
            GfxModel gfxModel = settings.DisableSkeleton ? new GfxModel() : new GfxModelSkeletal();
            gfxModel.Materials = settings.UseOriginalMaterials ? parent.Materials : new GfxDict<GfxMaterial>();
            gfxModel.MetaData = parent.MetaData;
            gfxModel.AnimationsGroup = parent.AnimationsGroup;
            gfxModel.Childs = parent.Childs;
            gfxModel.FaceCulling = parent.FaceCulling;
            gfxModel.Flags = parent.Flags;
            gfxModel.LayerId = parent.LayerId;
            gfxModel.IsBranchVisible = parent.IsBranchVisible;
            gfxModel.TransformScale = parent.TransformScale;
            gfxModel.TransformRotation = parent.TransformRotation;
            gfxModel.TransformTranslation = parent.TransformTranslation;
            gfxModel.LocalTransform = parent.LocalTransform;
            gfxModel.WorldTransform = parent.WorldTransform;
            gfxModel.MeshNodeVisibilities = parent.MeshNodeVisibilities;
            gfxModel.Name = parent.Name;

            //Match the file name as the imported name
            gfxModel.Name = Path.GetFileNameWithoutExtension(filePath);
            Console.WriteLine($"Importing model {model.Name}");

            if (gfxModel is GfxModelSkeletal)
            {
                //Todo
                if (settings.ImportBones)
                {    //Create a skeleton
                    GfxSkeleton skeleton = new GfxSkeleton();
                    skeleton.Name = "";
                    skeleton.ScalingRule = GfxSkeletonScalingRule.Maya;
                    skeleton.MetaData = new GfxDict<GfxMetaData>();
                    ((GfxModelSkeletal)gfxModel).Skeleton = skeleton;

                    var boneList = model.Skeleton.BreathFirstOrder();
                    //Remove "Armature" bone blender makes as it is not needed
                    var armatures = boneList.Where(x => x.Name == "Armature").ToList();
                 //   foreach (var armature in armatures)
                  //      boneList.Remove(armature);

                    foreach (var bone in boneList)
                    {
                        var bn = new GfxBone();
                        bn.Name = bone.Name;
                        bn.Translation = bone.Translation;
                        bn.Rotation = bone.RotationEuler;
                        bn.Scale = bone.Scale;
                        bn.BillboardMode = GfxBillboardMode.Off;
                        bn.Flags = GfxBoneFlags.IsNeededRendering | GfxBoneFlags.IsLocalMtxCalculate | GfxBoneFlags.IsWorldMtxCalculate;
                        bn.Flags |= GfxBoneFlags.HasSkinningMtx;
                        bn.UpdateTransformFlags();
                        bn.LocalTransform = new SPICA.Math3D.Matrix3x4(bn.CalculateLocalMatrix());
                        bn.MetaData = new GfxDict<GfxMetaData>();
                        bn.ParentIndex = -1;
                        skeleton.Bones.Add(bn);
                    }
                    //Setup references
                    for (int i = 0; i < boneList.Count; i++)
                    {
                        var bn = skeleton.Bones[boneList[i].Name];
                        bn.Index = i;
                        //Setup parent
                        if (boneList[i].Parent != null)
                        {
                            //The bone parent
                            bn.Parent = skeleton.Bones[boneList[i].Parent.Name];
                            //The child bone of parent
                            if (skeleton.Bones[boneList[i].Parent.Name].Child == null)
                                skeleton.Bones[boneList[i].Parent.Name].Child = bn;
                            bn.ParentIndex = skeleton.Bones.Find(boneList[i].Parent.Name);
                        }
                    }
                    for (int i = 0; i < boneList.Count; i++)
                    {
                        var bn = skeleton.Bones[boneList[i].Name];

                        //Siblings
                        var siblings = skeleton.Bones.Where(x => x.Parent == bn.Parent).ToList();
                        for (int j = 0; j < siblings.Count; j++)
                        {
                            //Check if bone is after this bone
                            if (siblings.Count > j + 1 && siblings[j + 1] == bn)
                                bn.PrevSibling = siblings[j];
                            //Check if bone is behind this bone
                            if (j > 0 && siblings[j - 1] == bn)
                                bn.NextSibling = siblings[j];
                        }

                        //Calculate world space matrix
                        bn.UpdateMatrices();
                    }
                }
                else
                {
                    //Create a skeleton
                    GfxSkeleton skeleton = new GfxSkeleton();
                    skeleton.Name = "";
                    skeleton.ScalingRule = GfxSkeletonScalingRule.Maya;
                    //Set into the model
                    ((GfxModelSkeletal)gfxModel).Skeleton = skeleton;

                    //Copy the parent skeleton data if used
                    if (parent is GfxModelSkeletal)
                    {
                        skeleton = ((GfxModelSkeletal)parent).Skeleton;
                        ((GfxModelSkeletal)gfxModel).Skeleton = skeleton;
                    }
                }
                //If no bones are present, then make default root bone. This is required for transforming in worldspace with a map editor
                if (((GfxModelSkeletal)gfxModel).Skeleton.Bones.Count == 0)
                {
                    ((GfxModelSkeletal)gfxModel).Skeleton.Bones.Add(new GfxBone()
                    {
                        Name = gfxModel.Name,
                        MetaData = new GfxDict<GfxMetaData>(),
                        Translation = new Vector3(),
                        Scale = new Vector3(1, 1, 1),
                        Rotation = new Vector3(),
                        BillboardMode = GfxBillboardMode.Off,
                        Flags = (GfxBoneFlags)415 | GfxBoneFlags.IsNeededRendering,
                        Index = 0,
                        ParentIndex = -1,
                        WorldTransform = new SPICA.Math3D.Matrix3x4(Matrix4x4.Identity),
                        InvWorldTransform = new SPICA.Math3D.Matrix3x4(Matrix4x4.Identity),
                        LocalTransform = new SPICA.Math3D.Matrix3x4(Matrix4x4.Identity),
                    });
                }
            }

            //Create a skinning list for inverted matrices to convert vertex data into local space later
            Matrix4x4[] skinningMatrices = new Matrix4x4[0];
            if (gfxModel is GfxModelSkeletal)
            {
                var skeleton = ((GfxModelSkeletal)gfxModel).Skeleton;

                skinningMatrices = new Matrix4x4[skeleton.Bones.Count];
                for (int i = 0; i < skeleton.Bones.Count; i++)
                {
                    //Set the world matrix as inverted
                    var bn = skeleton.Bones[i];
                    bn.UpdateMatrices();

                    var mat = GetWorldTransform(skeleton.Bones, bn);
                    Matrix4x4.Invert(mat, out Matrix4x4 inverted);
                    skinningMatrices[i] = inverted;
                }
            }
            //Prepare and import materials 
            foreach (var mat in scene.Materials)
            {
                string name = !string.IsNullOrEmpty(mat.Label) ? mat.Label : mat.Name;

                //A material does not exist in the file, import a default material to use.
                if (!gfxModel.Materials.Contains(name))
                {
                    var bcmdlMat = GfxMaterial.CreateDefault();
                    //Apply presets
                    if (File.Exists(settings.MaterialPresetFile))
                    {
                        var h3dMat = Bch.MaterialWrapper.GetPresetMaterial(settings.MaterialPresetFile);
                        if (h3dMat != null)
                            bcmdlMat.ConvertH3D(h3dMat);
                    }
                    bcmdlMat.Name = name;
                    gfxModel.Materials.Add(bcmdlMat);
                }
            }
            //Ensure there is atleast one material present
            if (gfxModel.Materials.Count == 0)
                gfxModel.Materials.Add(GfxMaterial.CreateDefault());

            //Check the materials and map out diffuse textures
            foreach (var mat in gfxModel.Materials)
            {
                if (settings.DisplayBothFaces)
                    mat.Rasterization.FaceCulling = GfxFaceCulling.Never;

                //Check for a match and that the material has diffuse used
                var iomaterial = scene.Materials.FirstOrDefault(x => x.Label == mat.Name);
                if (iomaterial != null && iomaterial.DiffuseMap != null && !settings.UseOriginalMaterials)
                {
                    string ext = iomaterial.DiffuseMap.FilePath.Split(".").LastOrDefault();

                    //Map out the texture. Swap out the extension, files can have multiple dots which GetFileWithoutExtension can break.
                    var texture = Path.GetFileName(iomaterial.DiffuseMap.FilePath).Replace($".{ext}", "");
                    if (!string.IsNullOrEmpty(texture))
                    {
                        Console.WriteLine($"Mapping {texture} to diffuse at slot 0");
                        //Map them out into the first texturemap slot for diffuse.
                        if (mat.TextureMappers[0] == null)
                            mat.TextureMappers[0] = new GfxTextureMapper();

                        mat.TextureMappers[0].Texture.Name = texture;
                        mat.TextureMappers[0].Texture.Path = texture;
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
                }
            }

            //Import mesh data
            foreach (var mesh in model.Meshes)
                ConvertMesh(scene, mesh, gfxModel, skinningMatrices, settings);

            gfxModel.ToH3D();
            return gfxModel;
        }

        //Gets a bone in world space
        static Matrix4x4 GetWorldTransform(GfxDict<GfxBone> skeleton, GfxBone bn)
        {
            if (bn.Parent != null && skeleton.Contains(bn.Parent.Name))
                return GetLocalTransform(bn) * GetWorldTransform(skeleton, skeleton[bn.Parent.Name]);
            return GetLocalTransform(bn);
        }

        //Gets a bone in local space
        static Matrix4x4 GetLocalTransform(GfxBone bn)
        {
            var trans = Matrix4x4.CreateTranslation(bn.Translation);
            var sca = Matrix4x4.CreateScale(bn.Scale);
            var rot = Matrix4x4.CreateRotationX(bn.Rotation.X) *
                      Matrix4x4.CreateRotationY(bn.Rotation.Y) *
                      Matrix4x4.CreateRotationZ(bn.Rotation.Z);
            return sca * rot * trans;
        }

        private static void ConvertMesh(IONET.Core.IOScene scene, IOMesh iomesh,
            GfxModel gfxModel, Matrix4x4[] skinningMatrices, CtrImportSettings settings)
        {
            if (iomesh.Vertices.Count == 0 || iomesh.Polygons.Sum(x => x.Indicies.Count) == 0)
                return;

            string meshName = iomesh.Name;
            //Mesh data
            GfxShape gfxShape = new GfxShape();
            gfxShape.BlendShape = new GfxBlendShape();
            gfxShape.Name = "";

            GfxMesh gfxMesh = new GfxMesh();
            gfxMesh.Name = "";
            gfxMesh.ShapeIndex = gfxModel.Shapes.Count;
            gfxMesh.PrimitiveIndex = 0;
            gfxMesh.Parent = gfxModel;
            gfxMesh.MeshNodeName = meshName;
            gfxMesh.IsVisible = true;
            gfxMesh.RenderPriority = 0;
            gfxMesh.MeshNodeIndex = (short)gfxModel.MeshNodeVisibilities.Find(meshName);

            int skinningCount = 0;
            int singleBindIndex = 0;

            //Calculate skinning amount from max amount of weights used
            skinningCount = iomesh.Vertices.Max(x => x.Envelope.Weights.Count);

            //Check how many bones are used total
            var boneList = iomesh.Vertices.SelectMany(x => x.Envelope.Weights.Select(x => x.BoneName)).Distinct().ToList();
            //If only one bone is used, no skinning requred as a bone can be used as a single binded rigid body.
          //  if (boneList?.Count == 1 || !(gfxModel is GfxModelSkeletal))
             //   skinningCount = 0;

            bool isRigid = skinningCount == 1;

            if (gfxModel is GfxModelSkeletal)
            {
                var skeleton = ((GfxModelSkeletal)gfxModel).Skeleton;
                if (skinningCount == 0)
                {
                    //Bind bone node from mesh
                    var singleBindBone = skeleton.Bones.FirstOrDefault(x => x.Name == meshName);
                    //Bind bone node from single skinned bone
                    if (boneList?.Count == 1)
                        singleBindBone = skeleton.Bones.FirstOrDefault(x => x.Name == boneList[0]);
                    //Get bind matrix for single binds
                    if (singleBindBone != null && skeleton.Bones.Find(singleBindBone.Name) != -1)
                        singleBindIndex = skeleton.Bones.Find(singleBindBone.Name);

                    //Convert the positions into local space for single binds
                    foreach (var vertex in iomesh.Vertices)
                    {
                        //Rigid binds to local space
                        vertex.Position = Vector3.Transform(vertex.Position, skinningMatrices[singleBindIndex]);
                        vertex.Normal = Vector3.TransformNormal(vertex.Normal, skinningMatrices[singleBindIndex]);
                    }
                }
            }

            //Pica attributes from vertex data
            var attributes = CreateAttributes(iomesh, skinningCount, settings);

            //Convert attributes into pica attributes for conversion into a buffer
            var vertices = GetPICAVertices(iomesh.Vertices, skinningMatrices, gfxModel,isRigid).ToArray();
            gfxMesh.MaterialIndex = 0;

            Console.WriteLine($"{iomesh.Polygons.Count}!");

            foreach (var poly in iomesh.Polygons)
            {
                //Generates the sub meshes representing the face data
                var subMeshes = GenerateSubMeshes(gfxModel, iomesh, poly, skinningCount, singleBindIndex, settings, ref vertices);
                gfxShape.SubMeshes.AddRange(subMeshes);
                //Map the material if one matches from the .dae. 
                var mat = scene.Materials.FirstOrDefault(x => x.Name == poly.MaterialName);
                if (mat != null)
                {
                    //Searh for the material. This should never be -1 as all the materials are added from the file if used.
                    var index = gfxModel.Materials.Find(mat.Label);
                    if (index != -1)
                        gfxMesh.MaterialIndex = index;
                }
                else
                    Console.WriteLine($"Cannot find material {poly.MaterialName}!");
            }

            //Create a vertex buffer to store the whole vertex data
            var vertexBuffer = new GfxVertexBufferInterleaved();
            vertexBuffer.AttrName = PICAAttributeName.Interleave;
            vertexBuffer.Type = GfxVertexBufferType.Interleaved;
            //Total stride for all the attributes

            CalculatePositionScaleOffset(attributes.FirstOrDefault(), gfxShape, iomesh);

            vertexBuffer.Attributes.AddRange(attributes);
            vertexBuffer.VertexStride = VerticesConverter.CalculateStride(attributes.Select(x => x.ToPICAAttribute()), true);
            vertexBuffer.RawBuffer = VerticesConverter.GetBuffer(vertices, attributes.Select(x => x.ToPICAAttribute()),    vertexBuffer.VertexStride);
            gfxShape.VertexBuffers.Add(vertexBuffer);

            foreach (var att in attributes)
                Console.WriteLine(att.AttrName + "Offset " + att.Offset);

            Console.WriteLine("VertexStride " + vertexBuffer.VertexStride);

            //Calculate bounding box
            CalculateBounding(ref gfxShape, iomesh);

            //Create a default color set if one is not present
            if (settings.ImportVertexColors && !vertexBuffer.Attributes.Any(x => x.AttrName == PICAAttributeName.Color))
            {
                gfxShape.VertexBuffers.Add(new GfxVertexBufferFixed()
                {
                    AttrName = PICAAttributeName.Color,
                    Elements = 4,
                    Format = GfxGLDataType.GL_FLOAT,
                    Scale = 1.0f,
                    Type = GfxVertexBufferType.Fixed,
                    Vector = new float[4] { 1, 1, 1, 1 }
                });
            }
            gfxModel.Meshes.Add(gfxMesh);
            gfxModel.Shapes.Add(gfxShape);
        }

        static void CalculatePositionScaleOffset(GfxAttribute attributePos, GfxShape shape, IOMesh iomesh)
        {
            //Only calculate scale/offset when not a float
            if (attributePos.Format == GfxGLDataType.GL_FLOAT)
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

            shape.PositionOffset = new Vector3();
            attributePos.Scale = GetScale(smallest, largest);
        }

        static void CalculateBounding(ref GfxShape gfxShape, IOMesh iomesh)
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

            gfxShape.BoundingBox.Center = min + ((max - min) / 2);
            gfxShape.BoundingBox.Size = extend;
            gfxShape.BoundingBox.Orientation = new SPICA.Math3D.Matrix3x3(Matrix4x4.CreateTranslation(gfxShape.BoundingBox.Center));
        }

        private static float GetExtent(float max, float min)
        {
            return (float)Math.Max(Math.Sqrt(max * max), Math.Sqrt(min * min));
        }

        static List<GfxSubMesh> GenerateSubMeshes(GfxModel gfxModel, IOMesh mesh, IOPolygon poly,
            int skinningCount, int singleBindIndex, CtrImportSettings settings, ref PICAVertex[] vertices, int max_bones = 16)
        {
            Dictionary<PICAVertex, ushort> remapVertex = new Dictionary<PICAVertex, ushort>();
            List<PICAVertex> newVertices = new List<PICAVertex>();

            List<ushort> faces = new List<ushort>();
            foreach (var index in poly.Indicies)
                faces.Add((ushort)index);

            Queue<ushort> IndicesQueue = new Queue<ushort>(faces);
            List<GfxSubMesh> subMeshes = new List<GfxSubMesh>();

            if (settings.DivideMK7)
            {
                return SplitByDiv(mesh, poly);
            }

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
                        Vector4 GetNormalValues(Vector4 vec)
                        {
                            return new Vector4(
                                !float.IsNaN(vec.X) ? vec.X : 0,
                                !float.IsNaN(vec.Y) ? vec.Y : 1,
                                !float.IsNaN(vec.Z) ? vec.Z : 0,
                                !float.IsNaN(vec.W) ? vec.W : 0);
                        }

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
                        float weightMax = 0;
                        for (int j = 0; j < 4; j++)
                        {
                            var id = Bones.IndexOf(vertices[ind].Indices[j]);
                            if (id != -1)
                                v.Indices[j] = id;

                            weightMax += vertices[ind].Weights[j];
                        }

                        if (weightMax == 0 && Bones.Count > 0)
                            vertices[ind].Weights[0] = 1.0f;

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
                subMeshes.Add(CreateSubMesh(Indices, singleBindIndex, skinningCount, Bones, settings));
            }

            vertices = newVertices.ToArray();

            return subMeshes;
        }

        //Splits sub meshes by .div
        static List<GfxSubMesh> SplitByDiv(IOMesh mesh, IOPolygon poly)
        {
            //Sub meshes
            List<GfxSubMesh> subMeshes = new List<GfxSubMesh>();
            //Only need to create one sub mesh. Split by face desc
            var subMesh = new GfxSubMesh();
            subMeshes.Add(subMesh);

            //Face data
            GfxFace face = new GfxFace();
            subMesh.Faces.Add(face);

            //Clip data (.div file)
            var clipData = CDAB.Instance;

            clipData.Shapes.Clear();
            clipData.Shapes.Add(new CDAB.Shape());

            //Stream per mesh
            CDAB.MeshStream streamData = new CDAB.MeshStream();
            streamData.VertexCount = (ushort)mesh.Vertices.Count;
            clipData.Shapes[0].Streams.Add(streamData);

            //Generate a triangle list to make checks easier.
            List<Triangle> triangles = new List<Triangle>();
            for (int i = 0; i < poly.Indicies.Count / 3; i++)
            {
                int ind = i * 3;

                var tri = new Triangle();
                triangles.Add(tri);
                for (int j = 0; j < 3; j++)
                {
                    tri.Indices.Add(poly.Indicies[ind + j]);
                    tri.Vertices.Add(mesh.Vertices[poly.Indicies[ind + j]].Position);
                }
            }
            triangles = triangles.OrderBy(tri => tri.GetMinZ()).ToList();

            //Generates a sub mesh division
            void GenerateDiv(List<Triangle> xstrip)
            {
                List<ushort> faces = new List<ushort>();
                foreach (var tri in xstrip)
                {
                    foreach (var index in tri.Indices)
                        faces.Add((ushort)index);
                }
                //Check what format to use indices. 255 > is larger than a byte
                bool is16Bit = faces.Any(x => x > 0xFF);
                //Add face desc for divided indices
                face.FaceDescriptors.Add(new GfxFaceDescriptor()
                {
                    PrimitiveMode = PICAPrimitiveMode.Triangles,
                    Indices = faces.ToArray(),
                    Format = is16Bit ? GfxGLDataType.GL_UNSIGNED_SHORT : GfxGLDataType.GL_UNSIGNED_BYTE,
                });
                //Prepare the buffer
                face.Setup();

                //Add stream to clip data (.div)
                streamData.Boundings.Add(CreateStream(xstrip));
            }

            float distMax = 1000;

            List<Triangle> zstrip = new List<Triangle>();
            Triangle zseltri = triangles[0];
            int zindex = 0;
            while (true)
            {
                //Check triangles in the Z direction

                //Distance between current triangle set
                while (triangles[zindex].GetMinZ() - zseltri.GetMinZ() <= distMax) //Add triangles within 1000 units to be in the same group
                {
                    zstrip.Add(triangles[zindex]);
                    zindex++;
                    //Last triangle, break the loop
                    if (zindex == triangles.Count) break;
                }
                zstrip = zstrip.OrderBy(face => face.GetMinX()).ToList();
                //Check triangles in the X direction
                List<Triangle> xstrip = new List<Triangle>();
                var xseltri = zstrip[0];
                int xindex = 0;
                while (true)
                {
                    //Distance between current triangle set
                    while (zstrip[xindex].GetMinX() - xseltri.GetMinX() <= distMax) //Add triangles within 1000 units to be in the same group
                    {
                        xstrip.Add(zstrip[xindex]);
                        xindex++;
                        //Last triangle, break the loop
                        if (xindex == zstrip.Count) break;
                    }
                    GenerateDiv(xstrip);

                    if (xindex == zstrip.Count) break;
                    xseltri = zstrip[xindex];
                    xstrip.Clear();
                }
                if (zindex == triangles.Count) break;
                zseltri = triangles[zindex];
                zstrip.Clear();
            }

            Console.WriteLine($"Mesh {mesh.Name} split {face.FaceDescriptors.Count} times.");

            return subMeshes;
        }

        static CDAB.BoundingBox CreateStream(List<Triangle> tris)
        {
            float minX = tris.Min(x => x.GetMinX());
            float minZ = tris.Min(x => x.GetMinZ());
            float maxX = tris.Max(x => x.GetMaxX());
            float maxZ = tris.Max(x => x.GetMaxZ());
            return new CDAB.BoundingBox()
            {
                MinX = minX,
                MinZ = minZ,
                MaxX = maxX,
                MaxZ = maxZ,
            };
        }

        class Triangle
        {
            public List<int> Indices = new List<int>();
            public List<Vector3> Vertices = new List<Vector3>();

            public float GetMinX() => Vertices.Min(x => x.X);
            public float GetMinZ() => Vertices.Min(x => x.Z);

            public float GetMaxX() => Vertices.Max(x => x.X);
            public float GetMaxZ() => Vertices.Max(x => x.Z);
        }

        static GfxSubMesh CreateSubMesh(List<ushort> Indices, int singleBindIndex,
            int skinningCount, List<int> Bones, CtrImportSettings settings)
        {
            //Sub mesh
            GfxSubMesh SM = new GfxSubMesh();
            //Determine the kind of skinning to use
            SM.Skinning = GfxSubMeshSkinning.None;
            if (skinningCount == 1)
                SM.Skinning = GfxSubMeshSkinning.Rigid;
            if (skinningCount > 1)
                SM.Skinning = GfxSubMeshSkinning.Smooth;
            //Check what format to use indices. 255 > is larger than a byte
            bool is16Bit = Indices.Any(x => x > 0xFF);
            //Face data
            GfxFace face = new GfxFace();
            face.FaceDescriptors.Add(new GfxFaceDescriptor()
            {
                PrimitiveMode = PICAPrimitiveMode.Triangles,
                Indices = Indices.ToArray(),
                Format = is16Bit ? GfxGLDataType.GL_UNSIGNED_SHORT : GfxGLDataType.GL_UNSIGNED_BYTE,
            });
            //Prepare the buffer and add to submesh
            face.Setup();
            SM.Faces.Add(face);
            //Add the bone indices to the sub mesh
            for (int i = 0; i < Bones.Count; i++)
                SM.BoneIndices.Add((byte)Bones[i]);

            //Need to atleast bind to a single bone.
            //If no bones are binded, the full model cannot be moved within a map editor if required.
            if (SM.BoneIndices.Count == 0 && !settings.DisableSkeleton)
                SM.BoneIndices.Add(singleBindIndex);

            return SM;
        }

        static List<GfxAttribute> CreateAttributes(IOMesh mesh, int skinningCount, CtrImportSettings settings)
        {
            List<GfxAttribute> attributes = new List<GfxAttribute>();
            //Vertex positions
            attributes.Add(new GfxAttribute()
            {
                Elements = 3,
                Format = FormatList[settings.Position.Format],
                AttrName = PICAAttributeName.Position,
                Scale = settings.Position.Scale,
            });
            //Vertex normals
            if (mesh.HasNormals)
            {
                attributes.Add(new GfxAttribute()
                {
                    Elements = 3,
                    Format = FormatList[settings.Normal.Format],
                    AttrName = PICAAttributeName.Normal,
                    Scale = settings.Normal.Scale,
                });
            }
            //Texture coordinates (supports up to 3)
            for (int i = 0; i < 3; i++)
            {
                if (mesh.HasUVSet(i))
                {
                    attributes.Add(new GfxAttribute()
                    {
                        Elements = 2,
                        Format = FormatList[settings.TexCoord.Format],
                        AttrName = (PICAAttributeName)((int)PICAAttributeName.TexCoord0 + i),
                        Scale = settings.TexCoord.Scale,
                    });
                }
            }
            //Vertex colors
            if (settings.ImportVertexColors && mesh.HasColorSet(0))
            {
                var colorFormat = FormatList[settings.Colors.Format];
                if (colorFormat == GfxGLDataType.GL_BYTE)
                    colorFormat = GfxGLDataType.GL_UNSIGNED_BYTE;

                attributes.Add(new GfxAttribute()
                {
                    Elements = 4,
                    Format = colorFormat,
                    AttrName = PICAAttributeName.Color,
                    Scale = settings.Colors.Scale,
                });
            }
            //Use bone indices for rigging
            if (mesh.HasEnvelopes() && skinningCount > 0)
            {
                var boneIndexFormat = FormatList[settings.BoneIndices.Format];
                var boneWeightFormat = FormatList[settings.BoneWeights.Format];

                if (boneIndexFormat == GfxGLDataType.GL_BYTE)
                    boneIndexFormat = GfxGLDataType.GL_UNSIGNED_BYTE;
                if (boneWeightFormat == GfxGLDataType.GL_BYTE)
                    boneWeightFormat = GfxGLDataType.GL_UNSIGNED_BYTE;

                attributes.Add(new GfxAttribute()
                {
                    Elements = skinningCount,
                    Format = boneIndexFormat,
                    AttrName = PICAAttributeName.BoneIndex,
                    Scale = settings.BoneIndices.Scale,
                });
                //Skinning over 1 uses weights for blending
                if (skinningCount > 1)
                {
                    attributes.Add(new GfxAttribute()
                    {
                        Elements = skinningCount,
                        Format = boneWeightFormat,
                        AttrName = PICAAttributeName.BoneWeight,
                        Scale = settings.BoneWeights.Scale,
                    });
                }
            }
            //Tangents for lighting
            if (mesh.HasTangents)
            {
                attributes.Add(new GfxAttribute()
                {
                    Elements = 3,
                    Format = GfxGLDataType.GL_FLOAT,
                    AttrName = PICAAttributeName.Tangent,
                    Scale = 1.0f,
                });
            }

            //Interleaved attribute offset
            int offset = 0;
            foreach (var att in attributes)
            {
                att.Offset = offset;
                offset += (int)(att.Elements * GetStride(att.Format));
            }

            return attributes;
        }

        static Dictionary<PICAAttributeFormat, GfxGLDataType> FormatList = new Dictionary<PICAAttributeFormat, GfxGLDataType>()
        {
            { PICAAttributeFormat.Float, GfxGLDataType.GL_FLOAT },
            { PICAAttributeFormat.Short, GfxGLDataType.GL_SHORT },
            { PICAAttributeFormat.Byte, GfxGLDataType.GL_BYTE },
        };
        
        static List<PICAVertex> GetPICAVertices(List<IOVertex> vertices, Matrix4x4[] skinningMatrices, GfxModel model, bool rigid)
        {
            GfxSkeleton skeleton = new GfxSkeleton();
            if (model is GfxModelSkeletal)
                skeleton = ((GfxModelSkeletal)model).Skeleton;

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
                    if (boneWeight == null)
                        continue;

                    if (!skeleton.Bones.Contains(boneWeight.BoneName))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Missing bone {boneWeight.BoneName}!");
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }


                    var bone = skeleton.Bones[boneWeight.BoneName];
                    picaVertex.Weights[j] = boneWeight.Weight;
                    picaVertex.Indices[j] = bone.Index;

                    //Smooth
                    if (!rigid)
                    {
                        if (!bone.Flags.HasFlag(GfxBoneFlags.HasSkinningMtx))
                            bone.Flags |= GfxBoneFlags.HasSkinningMtx;
                    }

                    if (rigid)
                    {
                        picaVertex.Weights[j] = 1.0f;

                        picaVertex.Position = new Vector4(Vector3.Transform(vertex.Position, skinningMatrices[bone.Index]), 1.0f);
                        picaVertex.Normal = new Vector4(Vector3.TransformNormal(vertex.Normal, skinningMatrices[bone.Index]), 1.0f);
                    }
                }

                verts.Add(picaVertex);
                index++;
            }
            return verts;
        }

        static int GetStride(GfxGLDataType format)
        {
            switch (format)
            {
                case GfxGLDataType.GL_BYTE:
                case GfxGLDataType.GL_UNSIGNED_BYTE:
                    return 1;
                case GfxGLDataType.GL_SHORT:
                case GfxGLDataType.GL_UNSIGNED_SHORT:
                    return 2;
                case GfxGLDataType.GL_FLOAT:
                    return 4;
            }
            return 4;
        }

    }
}
