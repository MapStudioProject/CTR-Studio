using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPICA.Formats.CtrGfx.AnimGroup;
using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.Formats.CtrGfx.Model.Mesh;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx;
using ImGuiNET;
using Discord;
using static Toolbox.Core.IO.STFileLoader;

namespace CtrLibrary.Bcres
{
    /// <summary>
    /// An animation helper for generating groups used for binding data to animations used by BCRES files.
    /// </summary>
    public class AnimGroupHelper
    {
        /// <summary>
        /// Settings to configure what type to generate on save.
        /// </summary>
        public class AnimationSettings
        {
            /// <summary>
            /// A list of material anim element types that can be generated.
            /// </summary>
            public Dictionary<string, bool> MaterialTypes = new Dictionary<string, bool>();

            public AnimationSettings()
            {
                //Prepare default settings
                foreach (var item in MaterialAnimElements)
                    MaterialTypes.Add(item.Key, true);
            }
        }

        /// <summary>
        /// Generates settings provided by the GfxModel anim group data.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AnimationSettings SetupMaterialSettings(GfxModel model)
        {
            AnimationSettings setting = new AnimationSettings();

            if (model.AnimationsGroup.Contains("MaterialAnimation"))
            {
                var anim = model.AnimationsGroup["MaterialAnimation"];
                //All mat animation types
                var animTypes = AnimGroupHelper.MaterialAnimElements;
                var mat = model.Materials[0];

                setting.MaterialTypes.Clear();
                int index = 0;
                foreach (var item in animTypes)
                {
                    //Name of element to display in UI
                    string name = AnimGroupHelper.MatAnimTypes[index];
                    //Check if the first material has the binded set of data
                    bool hasType = anim.Elements.Any(x => x.Name == string.Format(item.Key, mat.Name, "0"));
                    setting.MaterialTypes.Add(item.Key, hasType);
                    index++;
                }
            }
            return setting;
        }

        /// <summary>
        /// A list of bindable material animation elements.
        /// </summary>
        public static string[] MatAnimTypes = new string[]
        {
            "Emission Color",
            "Ambient Color",
            "Diffuse Color",
            "Specular0 Color",
            "Specular1 Color",
            "Constant0 Color",
            "Constant1 Color",
            "Constant2 Color",
            "Constant3 Color",
            "Constant4 Color",
            "Constant5 Color",
            "Texture Border Color",
            "Texture Pattern",
            "Blend Color",
            "Texture Scale",
            "Texture Rotate",
            "Texture Translate",
        };

        /// <summary>
        /// A lookup of bindable material elements by binding key.
        /// </summary>
        public static Dictionary<string, ElemetConfig> MaterialAnimElements = new Dictionary<string, ElemetConfig>()
        {
            //Elements use offsets (where to find data in bcres), member types (the element type depending on the group type used)
            //The type of group to create, then the operation blend index (not sure what that does)

            //Colors
            { "Materials[\"{0}\"].MaterialColor.Emission",  new ElemetConfig(0,  0, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Ambient",   new ElemetConfig(16, 1, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Diffuse",   new ElemetConfig(32, 2, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Specular0", new ElemetConfig(48, 3, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Specular1", new ElemetConfig(64, 4, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Constant0", new ElemetConfig(80, 5, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Constant1", new ElemetConfig(96, 6, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Constant2", new ElemetConfig(112, 7, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Constant3", new ElemetConfig(128, 8, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Constant4", new ElemetConfig(144, 9, typeof(GfxAnimGroupMaterialColor)) },
            { "Materials[\"{0}\"].MaterialColor.Constant5", new ElemetConfig(160, 10, typeof(GfxAnimGroupMaterialColor)) },
            //Sampler
            { "Materials[\"{0}\"].TextureMappers[{1}].Sampler.BorderColor", new ElemetConfig(12, 0, typeof(GfxAnimGroupTexSampler)) },
            //Texture Pattern
            { "Materials[\"{0}\"].TextureMappers[{1}].Texture", new ElemetConfig(8, 0, typeof(GfxAnimGroupTexMapper), 1) },
            //Blend color
            { "Materials[\"{0}\"].FragmentOperation.BlendOperation.BlendColor", new ElemetConfig(4, 0, typeof(GfxAnimGroupBlendOp)) },
            //Texture Scale
            { "Materials[\"{0}\"].TextureCoordinators[{1}].Scale", new ElemetConfig(16, 0, typeof(GfxAnimGroupTexCoord), 2) },
            //Texture Rotate
            { "Materials[\"{0}\"].TextureCoordinators[{1}].Rotate", new ElemetConfig(24, 1, typeof(GfxAnimGroupTexCoord), 3) },
            //Texture Translate
            { "Materials[\"{0}\"].TextureCoordinators[{1}].Translate", new ElemetConfig(28, 2, typeof(GfxAnimGroupTexCoord), 2) },
        };

        public class ElemetConfig
        {
            public int Offset;
            public int MemberType;
            public Type AnimType;
            public int OpIndex = 0;

            public ElemetConfig(int offset, int type, Type animGroupType, int opIdx = 0)
            {
                Offset = offset;
                MemberType = type;
                AnimType = animGroupType;
                OpIndex = opIdx;
            }

            public GfxAnimGroupElement Generate(string name)
            {
                GfxAnimGroupElement element = (GfxAnimGroupElement)Activator.CreateInstance(AnimType);
                element.MemberOffset = Offset;
                element.Name = name;
                element.BlendOpIndex = OpIndex;
                element.MemberType = (uint)MemberType;
                return element;
            }
        }
        public static List<GfxMeshNodeVisibility> GenerateMeshVisGroups(GfxModel model)
        {
            List<GfxMeshNodeVisibility> meshNodeVis = new List<GfxMeshNodeVisibility>();
            foreach (var mesh in model.Meshes)
            {
                if (string.IsNullOrEmpty(mesh.MeshNodeName))
                    continue;

                meshNodeVis.Add(new GfxMeshNodeVisibility()
                {
                    Name = mesh.MeshNodeName,
                    IsVisible = true,
                });
            }
            return meshNodeVis;
        }

        public static List<GfxAnimGroup> GenerateAnimGroups(GfxModel model, AnimationSettings settings)
        {
            List<GfxAnimGroup> animations = new List<GfxAnimGroup>();
            animations.Add(GenerateMatAnims(model.Materials, settings));
            animations.Add(GenerateVisAnims(model, model.Meshes));
            return animations;
        }

        public static GfxAnimGroup GenerateSkeletonAnims(GfxModelSkeletal model)
        {
            var anim = new GfxAnimGroup()
            {
                Name = "SkeletalAnimation",
                EvaluationTiming = GfxAnimEvaluationTiming.AfterSceneCull,
                MemberType = 1,
                BlendOperationTypes = new int[1] { 8 }
            };

            foreach (var bone in model.Skeleton.Bones)
            {
                anim.Elements.Add(new GfxAnimGroupBone()
                {
                    MemberOffset = 0, MemberType = 0, BlendOpIndex = 0,
                    ObjType = GfxAnimGroupObjType.Bone,
                    BoneName = bone.Name,
                    Name = bone.Name,
                });
            }
            return anim;
        }

        public static GfxAnimGroup GenerateMatAnims(GfxDict<GfxMaterial> materials, AnimationSettings settings)
        {
            var anim = new GfxAnimGroup()
            {
                Name = "MaterialAnimation",
                EvaluationTiming = GfxAnimEvaluationTiming.AfterSceneCull,
                MemberType = 2,
                BlendOperationTypes = new int[4] { 3, 7, 5, 2 }
            };

            foreach (var elem in MaterialAnimElements)
            {
                if (settings.MaterialTypes.ContainsKey(elem.Key) && !settings.MaterialTypes[elem.Key])
                    continue;

                foreach (var mat in materials)
                {
                    //The element name (with included material name)
                    string elementName = string.Format(elem.Key, mat.Name, "0");
                    //The element group type to generate
                    Type elementType = elem.Value.AnimType;

                    //Determine how to apply each type
                    if (elementType == typeof(GfxAnimGroupTexCoord))
                    {
                        //Set per texture coordinate
                        for (int j = 0; j < mat.UsedTextureCoordsCount; j++)
                        {
                            elementName = string.Format(elem.Key, mat.Name, j);

                            //Create the element
                            var element = (GfxAnimGroupTexCoord)elem.Value.Generate(elementName);
                            element.TexCoordIndex = j;
                            element.MaterialName = mat.Name;

                            Console.WriteLine("Generating element " + element.Name);

                            anim.Elements.Add(element);
                        }
                    }
                    else if (elementType == typeof(GfxAnimGroupTexSampler) ||
                             elementType == typeof(GfxAnimGroupTexMapper))
                    {
                        //Set per used texture map
                        for (int j = 0; j < 3; j++)
                        {
                            bool hasTextures = mat.TextureMappers[j] != null && !string.IsNullOrEmpty(mat.TextureMappers[j].Texture.Path);
                            if (!hasTextures)
                                continue;

                            elementName = string.Format(elem.Key, mat.Name, j);

                            //Create the element
                            var element = elem.Value.Generate(elementName);
                            if (element is GfxAnimGroupTexMapper)
                            {
                                ((GfxAnimGroupTexMapper)element).TexMapperIndex = j;
                                ((GfxAnimGroupTexMapper)element).MaterialName = mat.Name;
                            }
                            if (element is GfxAnimGroupTexSampler)
                            {
                                ((GfxAnimGroupTexSampler)element).TexSamplerIndex = j;
                                ((GfxAnimGroupTexSampler)element).MaterialName = mat.Name;
                            }

                            Console.WriteLine("Generating element " + element.Name);

                            anim.Elements.Add(element);
                        }
                    }
                    else
                    {
                        //Create the element
                        var element = elem.Value.Generate(elementName);
                        if (elementType == typeof(GfxAnimGroupMaterialColor))
                            ((GfxAnimGroupMaterialColor)element).MaterialName = mat.Name;
                        if (elementType == typeof(GfxAnimGroupBlendOp))
                            ((GfxAnimGroupBlendOp)element).MaterialName = mat.Name;

                        Console.WriteLine("Generating element " + element.Name);

                        anim.Elements.Add(element);
                    }
                }
            }
            return anim;
        }

        public static GfxAnimGroup GenerateLightAnims()
        {
            var anim = new GfxAnimGroup()
            {
                Name = "LightAnimation",
                EvaluationTiming = GfxAnimEvaluationTiming.BeforeWorldUpdate,
                MemberType = 4,
                BlendOperationTypes = new int[5] { 4, 3, 6, 2, 0 }
            };
            anim.Elements.Add(CreateElement<GfxAnimGroupElement>("Transform", 48, 0, 0, (GfxAnimGroupObjType)9));
            anim.Elements.Add(CreateElement<GfxAnimGroupElement>("Ambient", 188, 0, 1, (GfxAnimGroupObjType)13));
            anim.Elements.Add(CreateElement<GfxAnimGroupElement>("Diffuse", 204, 1, 1, (GfxAnimGroupObjType)13));
            anim.Elements.Add(CreateElement<GfxAnimGroupElement>("Specular0", 220, 2, 1, (GfxAnimGroupObjType)13));
            anim.Elements.Add(CreateElement<GfxAnimGroupElement>("Specular1", 236, 3, 1, (GfxAnimGroupObjType)13));
            anim.Elements.Add(CreateElement<GfxAnimGroupElement>("Direction", 268, 4, 2, (GfxAnimGroupObjType)13));
            anim.Elements.Add(CreateElement<GfxAnimGroupElement>("DistanceAttenuationStart", 288, 5, 3, (GfxAnimGroupObjType)13));
            anim.Elements.Add(CreateElement<GfxAnimGroupElement>("DistanceAttenuationEnd", 292, 6, 3, (GfxAnimGroupObjType)13));
            anim.Elements.Add(CreateElement<GfxAnimGroupElement>("IsLightEnabled", 180, 0, 4, (GfxAnimGroupObjType)0));
            return anim;
        }

        static GfxAnimGroup GenerateVisAnims(GfxModel model, List<GfxMesh> meshes)
        {
            var anim = new GfxAnimGroup()
            {
                Name = "VisibilityAnimation",
                EvaluationTiming = GfxAnimEvaluationTiming.BeforeWorldUpdate,
                MemberType = 3,
                BlendOperationTypes = new int[1] { 0 }
            };
            anim.Elements.Add(CreateElement<GfxAnimGroupModel>("IsBranchVisible", 28, 0, 0));
            anim.Elements.Add(CreateElement<GfxAnimGroupModel>("IsVisible", 212, 1, 0));

            for (int i = 0; i < meshes.Count; i++)
            {
                var meshIndNode = CreateElement<GfxAnimGroupMesh>($"Meshes[{i}].IsVisible", 36, 0, 0);
                meshIndNode.MeshIndex = i;
                anim.Elements.Add(meshIndNode);
            }
            for (int i = 0; i < meshes.Count; i++)
            {
                if (string.IsNullOrEmpty(meshes[i].MeshNodeName))
                    continue;

                var meshVisNode = CreateElement<GfxAnimGroupMeshNodeVis>($"MeshNodeVisibilities[\"{meshes[i].MeshNodeName}\"].IsVisible", 4, 0, 0);
                meshVisNode.NodeName = meshes[i].MeshNodeName;
                anim.Elements.Add(meshVisNode);
            }
            return anim;
        }

        static T CreateElement<T>(string name, int offset, int type, int opIdx) where T : GfxAnimGroupElement
        {
            GfxAnimGroupElement element = Activator.CreateInstance<T>();
            element.MemberOffset = offset;
            element.Name = name;
            element.BlendOpIndex = opIdx;
            element.MemberType = (uint)type;

            return (T)element;
        }

        static T CreateElement<T>(string name, int offset, int type, int opIdx, GfxAnimGroupObjType obj) where T : GfxAnimGroupElement
        {
            GfxAnimGroupElement element = Activator.CreateInstance<T>();
            element.MemberOffset = offset;
            element.Name = name;
            element.BlendOpIndex = opIdx;
            element.MemberType = (uint)type;
            element.ObjType = obj;

            return (T)element;
        }
    }
}
