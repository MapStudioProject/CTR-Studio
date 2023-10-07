using GLFrameworkEngine;
using ImGuiNET;
using MapStudio.UI;
using OpenTK.Graphics.OpenGL;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.PICA.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CtrLibrary.Bcres;
using CtrLibrary.Rendering;
using CtrLibrary.Bch;

namespace CtrLibrary
{
    internal class MaterialTextureUI
    {
        private UI.UVViewport UVViewport;
        static int selectedTextureIndex = 0;
        bool open_texture_dialog = false;

        float uvWindowHeight = 150;

        H3DMaterial Material;
        private H3DModel GfxModel;
        private MaterialWrapper UINode;

        TextureSelectionDialog TextureDialog = new TextureSelectionDialog();

        string[] UVSets = new string[]
         {
                "UV Layer 0",
                "UV Layer 1",
                "UV Layer 2",
         };

        public void Init(MaterialWrapper materialNode, H3DModel model, H3DMaterial material)
        {
            UINode = materialNode;
            GfxModel = model;
            Material = material;

            if (UVViewport == null)
            {
                UVViewport = new UI.UVViewport();
            }
            ReloadUVDisplay();

            TextureDialog.Textures = new List<string>();
            foreach (var tex in H3DRender.TextureCache.Values)
                TextureDialog.Textures.Add(tex.Name);
        }

        public void Render()
        {
            var width = ImGui.GetWindowWidth();
            var mparams = Material.MaterialParams;

            if (ImGuiHelper.ComboFromEnum<H3DTexCoordConfig>("Tex Coord IDs", mparams, "TexCoordConfig"))
                UpdateShaders();


            ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeHandler.Theme.FrameBg);
            if (ImGui.BeginChild("textureList", new Vector2(width, 62)))
            {
                DrawTextureUI(0, ref Material.Texture0Name);
                DrawTextureUI(1, ref Material.Texture1Name);
                DrawTextureUI(2, ref Material.Texture2Name);
            }
            ImGui.EndChild();

            ImGui.PopStyleColor();

            if (selectedTextureIndex == 0) DrawTextureInfo(0, ref Material.Texture0Name);
            if (selectedTextureIndex == 1) DrawTextureInfo(1, ref Material.Texture1Name);
            if (selectedTextureIndex == 2) DrawTextureInfo(2, ref Material.Texture2Name);
        }

        void DrawTextureUI(int i, ref string name)
        {
            int ID = IconManager.GetTextureIcon("TEXTURE");

            if (!string.IsNullOrEmpty(name))
            {
                if (IconManager.HasIcon(name))
                    ID = IconManager.GetTextureIcon(name);

                ImGui.Image((IntPtr)ID, new Vector2(18, 18));
                ImGui.SameLine();
            }

            string label = $"{name}";
            if (Material.MaterialParams.BumpMode != H3DBumpMode.NotUsed && Material.MaterialParams.BumpTexture == i)
                label = $"{name} ({Material.MaterialParams.BumpMode})";

            bool select = selectedTextureIndex == i;
            if (ImGui.Selectable(string.IsNullOrEmpty(name) ? $"None##texmap{i}" : $"{label}##texmap{i}", ref select))
            {
                selectedTextureIndex = i;
                ReloadUVDisplay();
            }
        }

        private void ReloadUVDisplay()
        {
            var texMap = Material.TextureMappers[selectedTextureIndex];
            int texLayer = (int)Material.MaterialParams.TextureSources[selectedTextureIndex];

            int ID = IconManager.GetTextureIcon("BLANK");
            string name = Material.Texture0Name;
            if (selectedTextureIndex == 1) name = Material.Texture1Name;
            if (selectedTextureIndex == 2) name = Material.Texture2Name;

            if (!string.IsNullOrEmpty(name) && IconManager.HasIcon(name))
                ID = IconManager.GetTextureIcon(name);

            int width = 1;
            int height = 1;

            if (!string.IsNullOrEmpty(name))
            {
                if (H3DRender.TextureCache.ContainsKey(name))
                {
                    var tex = H3DRender.TextureCache[name];
                    width = tex.Width; height = tex.Height;
                }
            }

            UVViewport.Camera.Zoom = 31.5F;
            UVViewport.ActiveObjects.Clear();
            foreach (var mesh in GfxModel.Meshes)
            {
                if (GfxModel.Materials[mesh.MaterialIndex].Name == Material.Name)
                {
                    //Add mesh to UV list
                    List<OpenTK.Vector2> texCoords = new List<OpenTK.Vector2>();
                    List<int> indices = new List<int>();

                    foreach (var vert in mesh.GetVertices())
                    {
                        //Flip UVs and assign by texture layer
                        if (texLayer == 0)      texCoords.Add(new OpenTK.Vector2(vert.TexCoord0.X, 1 - vert.TexCoord0.Y));
                        else if (texLayer == 1) texCoords.Add(new OpenTK.Vector2(vert.TexCoord1.X, 1 - vert.TexCoord1.Y));
                        else if (texLayer == 2) texCoords.Add(new OpenTK.Vector2(vert.TexCoord2.X, 1 - vert.TexCoord2.Y));
                    }
                    //Use all sub mesh indices
                    foreach (var subMesh in mesh.SubMeshes)
                    {
                        foreach (var ind in subMesh.Indices)
                            indices.Add(ind);
                    }
                    //Add to UV viewer and update it
                    UVViewport.ActiveObjects.Add(new UI.UVMeshObject(texCoords, indices.ToArray()));
                    UVViewport.UpdateVertexBuffer = true;
                }
            }
            UVViewport.ActiveTextureMap = new UI.TextureSamplerMap()
            {
                ID = ID,
                WrapU = GetWrap(texMap.WrapU),
                WrapV = GetWrap(texMap.WrapV),
                MagFilter = GetMagFilter(texMap.MagFilter),
                MinFilter = GetMinFilter(texMap.MinFilter),
                Width = width,
                Height = height,
            };
            var mat = Material.MaterialParams.TextureCoords[selectedTextureIndex].GetTransform().ToMatrix4x4();
            UVViewport.ActiveMatrix = new OpenTK.Matrix4(
              mat.M11, mat.M12, mat.M13, mat.M14,
              mat.M21, mat.M22, mat.M23, mat.M24,
              mat.M31, mat.M32, mat.M33, mat.M34,
              mat.M41, mat.M42, mat.M43, mat.M44);
        }

        private void UpdateUVViewerImage()
        {
            int ID = IconManager.GetTextureIcon("BLANK");
            string name = Material.Texture0Name;
            if (selectedTextureIndex == 1) name = Material.Texture1Name;
            if (selectedTextureIndex == 2) name = Material.Texture2Name;

            if (!string.IsNullOrEmpty(name) && IconManager.HasIcon(name))
                ID = IconManager.GetTextureIcon(name);

            int width = 1;
            int height = 1;

            if (H3DRender.TextureCache.ContainsKey(name))
            {
                var tex = H3DRender.TextureCache[name];
                width = tex.Width; height = tex.Height;
            }

            UVViewport.ActiveTextureMap.ID = ID;
            UVViewport.ActiveTextureMap.Width = width;
            UVViewport.ActiveTextureMap.Height = height;
        }

        private void UpdateUVViewer()
        {
            int CoordIndex = selectedTextureIndex;

            if (CoordIndex == 2 && (
                Material.MaterialParams.TexCoordConfig == H3DTexCoordConfig.Config0110 ||
                Material.MaterialParams.TexCoordConfig == H3DTexCoordConfig.Config0111 ||
                Material.MaterialParams.TexCoordConfig == H3DTexCoordConfig.Config0112))
            {
                CoordIndex = 1;
            }

            var texMap = Material.TextureMappers[selectedTextureIndex];

            UVViewport.ActiveTextureMap.WrapU = GetWrap(texMap.WrapU);
            UVViewport.ActiveTextureMap.WrapV = GetWrap(texMap.WrapV);
            UVViewport.ActiveTextureMap.MagFilter = GetMagFilter(texMap.MagFilter);
            UVViewport.ActiveTextureMap.MinFilter = GetMinFilter(texMap.MinFilter);

            if (CoordIndex == 2 && (
            Material.MaterialParams.TexCoordConfig == H3DTexCoordConfig.Config0110 ||
            Material.MaterialParams.TexCoordConfig == H3DTexCoordConfig.Config0111 ||
            Material.MaterialParams.TexCoordConfig == H3DTexCoordConfig.Config0112))
            {
                CoordIndex = 1;
            }

            var minLOD = (int)texMap.MinLOD;
            UVViewport.ActiveTextureMap.MinLOD = texMap.MinLOD;
            UVViewport.ActiveTextureMap.LODBias = texMap.LODBias;
            var mat = Material.MaterialParams.TextureCoords[CoordIndex].GetTransform().ToMatrix4x4();
            UVViewport.ActiveMatrix = new OpenTK.Matrix4(
                mat.M11, mat.M12, mat.M13, mat.M14,
                mat.M21, mat.M22, mat.M23, mat.M24,
                mat.M31, mat.M32, mat.M33, mat.M34,
                mat.M41, mat.M42, mat.M43, mat.M44);
        }

        private void DrawTextureInfo(int index, ref string name)
        {
            int CoordIndex = index;

            if (CoordIndex == 2 && (
                Material.MaterialParams.TexCoordConfig == H3DTexCoordConfig.Config0110 ||
                Material.MaterialParams.TexCoordConfig == H3DTexCoordConfig.Config0111 ||
                Material.MaterialParams.TexCoordConfig == H3DTexCoordConfig.Config0112))
            {
                CoordIndex = 1;
            }

            if (string.IsNullOrEmpty(name))
            { 
                void CreateNewSlot()
                {
                    Material.MaterialParams.TextureSources[index] = 0;
                    Material.MaterialParams.TextureCoords[CoordIndex] = new H3DTextureCoord()
                    {
                        MappingType = H3DTextureMappingType.UvCoordinateMap,
                        Scale = new Vector2(1, 1),
                        Rotation = 0,
                        Translation = new Vector2(0, 0),
                        TransformType = H3DTextureTransformType.DccMaya,
                        ReferenceCameraIndex = -1,
                    };
                    Material.TextureMappers[index] = new H3DTextureMapper()
                    {
                        WrapU = PICATextureWrap.Repeat,
                        WrapV = PICATextureWrap.Repeat,
                        BorderColor = new SPICA.Math3D.RGBA(0, 0, 0, 255),
                        LODBias = 0,
                        MagFilter = H3DTextureMagFilter.Linear,
                        MinFilter = H3DTextureMinFilter.Linear,
                        MinLOD = 0,
                        SamplerType = (byte)CoordIndex,
                    };
                    Material.EnabledTextures[index] = true;
                    UINode.UpdateUniformBooleans();
                };

                ImGuiHelper.BoldText($"Select Texture:");

                if (ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.Button($"   {IconManager.IMAGE_ICON}   "))
                    {
                        open_texture_dialog = true;
                    }
                    ImGui.SameLine();

                    string texName = name == null ? "" : name;
                    if (ImGui.InputText("Name", ref texName, 0x200))
                    {
                        name = texName;
                        UpdateUVViewerImage();
                        GLContext.ActiveContext.UpdateViewport = true;
                        CreateNewSlot();
                    }
                }

                if (open_texture_dialog)
                {
                    if (TextureDialog.Render(name, ref open_texture_dialog))
                    {
                        name = TextureDialog.OutputName;
                        CreateNewSlot();
                        UpdateUVViewerImage();
                        UINode.ReloadIcon();
                        GLContext.ActiveContext.UpdateViewport = true;
                    }
                }
                return;
            }

            var width = ImGui.GetWindowWidth();
            var texMap = Material.TextureMappers[index];
            var texCoord = Material.MaterialParams.TextureCoords[CoordIndex];
            var texSourceID = (int)Material.MaterialParams.TextureSources[index];
            bool updateUniforms = false;
            var scale = texCoord.Scale;
            var translation = texCoord.Translation;
            var rotation = OpenTK.MathHelper.RadiansToDegrees(texCoord.Rotation);
            var wrapU = texMap.WrapU;
            var wrapV = texMap.WrapV;
            var magFilter = texMap.MagFilter;
            var minFilter = ExtractMinFilterMode(texMap.MinFilter);
            var mipmapFilter = ExtractMipmapMode(texMap.MinFilter);
            var minLOD = (int)texMap.MinLOD;
            var borderColor = texMap.BorderColor;
            int cameraIndex = texCoord.ReferenceCameraIndex;

            var size = ImGui.GetWindowSize();

            if (ImGui.CollapsingHeader("Preview", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginChild("uvWindow", new Vector2(width, uvWindowHeight), false))
                {
                    var pos = ImGui.GetCursorScreenPos();

                    this.UVViewport.Render((int)width, (int)ImGui.GetWindowHeight());

                    ImGui.SetCursorScreenPos(pos);
                    ImGui.Checkbox("Show UVs", ref this.UVViewport.DisplayUVs);
                }
                ImGui.EndChild();

                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.Separator]);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.SeparatorHovered]);
                ImGui.Button("##hsplitter", new Vector2(-1, 2));
                if (ImGui.IsItemActive())
                {
                    var deltaY = -ImGui.GetIO().MouseDelta.Y;
                    if (uvWindowHeight - deltaY < size.Y - 22 && uvWindowHeight - deltaY > 22)
                        uvWindowHeight -= deltaY;
                }
            }

            ImGui.PopStyleColor(2);

            ImGui.BeginChild("propertiesWindow");

            if (ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button($"   {IconManager.IMAGE_ICON}   "))
                {
                    open_texture_dialog = true;
                }
                ImGui.SameLine();

                string texName = name == null ? "" : name;
                if (ImGui.InputText("Name", ref texName, 0x200))
                {   
                    name = texName;
                    if (string.IsNullOrEmpty(name))
                        OnTextureSlotRemoved(index);

                    UpdateUVViewerImage();
                    GLContext.ActiveContext.UpdateViewport = true;
                }

                var mParams = Material.MaterialParams;
                bool isBump = mParams.BumpTexture == index && mParams.BumpMode != H3DBumpMode.NotUsed;
                if (ImGui.Checkbox("Is Bump Map", ref isBump))
                {
                    if (isBump)
                    {
                        mParams.BumpTexture = (byte)index;
                        if (mParams.BumpMode == H3DBumpMode.NotUsed)
                            mParams.BumpMode = H3DBumpMode.AsBump;
                    }
                    else
                    {
                        mParams.BumpMode = H3DBumpMode.NotUsed;
                    }
                    UpdateShaders();
                }
                if (isBump)
                {
                    bool isTangent = mParams.BumpMode == H3DBumpMode.AsTangent;

                    void UpdateMode()
                    {
                        isTangent = !isTangent;

                        if (isTangent) mParams.BumpMode = H3DBumpMode.AsTangent;
                        else
                            mParams.BumpMode = H3DBumpMode.AsBump;
                        UpdateShaders();
                    }

                    ImGui.SameLine();

                    if (ImGui.RadioButton("As Tangent", isTangent))
                        UpdateMode();

                    ImGui.SameLine();
                    if (ImGui.RadioButton("As Bump", !isTangent))
                        UpdateMode();

                    ImGui.SameLine();
                    bool normalize = Material.MaterialParams.FragmentFlags.HasFlag(H3DFragmentFlags.IsBumpRenormalizeEnabled);
                    if (ImGui.Checkbox("Normalize Bump", ref normalize))
                    {
                        if (normalize)
                            Material.MaterialParams.FragmentFlags |= H3DFragmentFlags.IsBumpRenormalizeEnabled;
                        else
                            Material.MaterialParams.FragmentFlags &= ~H3DFragmentFlags.IsBumpRenormalizeEnabled;
                        UpdateShaders();
                    }
                }
            }

            if (open_texture_dialog)
            {
                if (TextureDialog.Render(name, ref open_texture_dialog))
                {
                    name = TextureDialog.OutputName;
                    UpdateUVViewerImage();
                    UINode.ReloadIcon();
                    if (string.IsNullOrEmpty(name))
                        OnTextureSlotRemoved(index);

                    GLContext.ActiveContext.UpdateViewport = true;
                }
            }

            if (ImGui.CollapsingHeader("Tiling", ImGuiTreeNodeFlags.DefaultOpen))
            {
                BcresUIHelper.DrawEnum("Wrap U", ref wrapU, () => { updateUniforms = true; });
                BcresUIHelper.DrawEnum("Wrap V", ref wrapV, () => { updateUniforms = true; });

                var color = borderColor.ToVector4();
                if (ImGui.ColorEdit4("Border Color", ref color))
                {
                    borderColor = new SPICA.Math3D.RGBA(
                      (byte)(color.X * 255), (byte)(color.Y * 255),
                      (byte)(color.Z * 255), (byte)(color.W * 255));
                    updateUniforms = true;
                }
            }
            if (ImGui.CollapsingHeader("UV Settings", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGuiHelper.BoldText($"Coordinate Index #{CoordIndex}");

                BcresUIHelper.DrawEnum("Mapping Type", ref texCoord.MappingType, () => { 
                    //Camera types are the only source > 2 from what I can tell, so reset to 0 if necessary
                    if (texCoord.MappingType != H3DTextureMappingType.CameraCubeEnvMap &&
                        texCoord.MappingType != H3DTextureMappingType.CameraSphereEnvMap && texSourceID > 2)
                    {
                        texSourceID = 0;
                    }
                    //3 for cube env maps
                    if (texCoord.MappingType == H3DTextureMappingType.CameraCubeEnvMap)
                        texSourceID = 3;
                    //4 for camera sphere env maps
                    if (texCoord.MappingType == H3DTextureMappingType.CameraSphereEnvMap)
                        texSourceID = 4;

                    Material.MaterialParams.TextureSources[index] = texSourceID;
                    Material.MaterialParams.TextureCoords[CoordIndex] = texCoord;

                    updateUniforms = true;
                    UINode.UpdateUniformBooleans();
                    UpdateShaders();
                });

                if (texCoord.MappingType == H3DTextureMappingType.UvCoordinateMap && texSourceID < 3)
                {
                    var uvLayer = UVSets[texSourceID];
                    if (ImguiCustomWidgets.ComboScrollable("UV Layer", UVSets[texSourceID], ref uvLayer, UVSets))
                    {
                        texSourceID = UVSets.ToList().IndexOf(uvLayer);
                        updateUniforms = true;
                    }
                }
                if (texCoord.MappingType == H3DTextureMappingType.ProjectionMap)
                {
                    if (ImGui.InputInt("Camera Index", ref cameraIndex))
                    {
                        texCoord.ReferenceCameraIndex = (sbyte)cameraIndex;
                        updateUniforms = true;
                    }
                }
            }
            if (ImGui.CollapsingHeader("Transforming", ImGuiTreeNodeFlags.DefaultOpen))
            {
                BcresUIHelper.DrawEnum("Transform Method", ref texCoord.TransformType, () => { updateUniforms = true; });
                updateUniforms |= ImGui.DragFloat2("Scale", ref scale);
                updateUniforms |= ImGui.DragFloat("Rotation", ref rotation);
                updateUniforms |= ImGui.DragFloat2("Translation", ref translation, 0.05F);
            }
            if (ImGui.CollapsingHeader("Filtering", ImGuiTreeNodeFlags.DefaultOpen))
            {
                BcresUIHelper.DrawEnum("Min Filter", ref minFilter, () => { updateUniforms = true; });
                BcresUIHelper.DrawEnum("Mag Filter", ref magFilter, () => { updateUniforms = true; });
                BcresUIHelper.DrawEnum("Mipmap", ref mipmapFilter, () => { updateUniforms = true; });

                if (texMap.MinFilter.ToString().Contains("Mipmap"))
                {
                    var mipCounter = texMap.MinLOD;
                    if (name != null && H3DRender.TextureCache.ContainsKey(name))
                    {
                        var tex = H3DRender.TextureCache[name];
                        mipCounter = tex.MipmapSize;
                    }

                    ImGui.Text("Mipmap LOD Settings:");
                    updateUniforms |= ImGui.SliderInt("Min LOD", ref minLOD, 0, mipCounter);
                    ImGui.SameLine();
                    ImGui.Text($"{texMap.MinLOD} / {mipCounter}");

                    updateUniforms |= ImGui.SliderFloat("LOD Bias", ref texMap.LODBias, -6, 6);
                }
            }
            if (updateUniforms)
            {
                bool sourceChanged = Material.MaterialParams.TextureSources[index] != texSourceID;

                Material.MaterialParams.TextureSources[index] = texSourceID;
                Material.TextureMappers[index] = new H3DTextureMapper()
                {
                    WrapU = wrapU,
                    WrapV = wrapV,
                    LODBias = texMap.LODBias,
                    MinLOD = (byte)minLOD,
                    MagFilter = magFilter,
                    MinFilter = MergeMinFilterMipmapMode(minFilter, mipmapFilter),
                    BorderColor = borderColor,
                    SamplerType = (byte)index,
                };
                Material.MaterialParams.TextureCoords[CoordIndex] = new H3DTextureCoord()
                {
                    Scale = scale,
                    Translation = translation,
                    Rotation = OpenTK.MathHelper.DegreesToRadians(rotation),
                    TransformType = texCoord.TransformType,
                    Flags = texCoord.Flags,
                    MappingType = texCoord.MappingType,
                    ReferenceCameraIndex = (sbyte)cameraIndex,
                };
                UpdateUniforms();
                UpdateUVViewer();
                if (sourceChanged)
                    ReloadUVDisplay();
            }
            ImGui.EndChild();
        }

        void OnTextureSlotRemoved(int index)
        {
            //Reset source to 0 if removed
            Material.MaterialParams.TextureSources[index] = 0;
            Material.TextureMappers[index] = new H3DTextureMapper();
            Material.EnabledTextures[index] = false;
        }

        void UpdateUniforms()
        {
          //  Material.UpdateShaderUniforms?.Invoke(Material, EventArgs.Empty);
            GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
        }

        void UpdateShaders()
        {
            UINode.UpdateShaders();
            GLFrameworkEngine.GLContext.ActiveContext.UpdateViewport = true;
        }

        private static TextureWrapMode GetWrap(PICATextureWrap Wrap)
        {
            switch (Wrap)
            {
                case PICATextureWrap.ClampToEdge: return TextureWrapMode.ClampToEdge;
                case PICATextureWrap.ClampToBorder: return TextureWrapMode.ClampToBorder;
                case PICATextureWrap.Repeat: return TextureWrapMode.Repeat;
                case PICATextureWrap.Mirror: return TextureWrapMode.MirroredRepeat;

                default: throw new ArgumentException("Invalid wrap mode!");
            }
        }

        //TODO: Change this to use the Mipmaps once Mipmaps are implemented on the loaders
        private static TextureMinFilter GetMinFilter(H3DTextureMinFilter Filter)
        {
            switch (Filter)
            {
                case H3DTextureMinFilter.Nearest: return TextureMinFilter.Nearest;
                case H3DTextureMinFilter.NearestMipmapNearest: return TextureMinFilter.Nearest;
                case H3DTextureMinFilter.NearestMipmapLinear: return TextureMinFilter.Nearest;
                case H3DTextureMinFilter.Linear: return TextureMinFilter.Linear;
                case H3DTextureMinFilter.LinearMipmapNearest: return TextureMinFilter.Linear;
                case H3DTextureMinFilter.LinearMipmapLinear: return TextureMinFilter.Linear;

                default: throw new ArgumentException("Invalid minification filter!");
            }
        }

        private enum MipmapMode
        {
            None,
            Nearest,
            Linear
        };
        private enum MinFilterMode
        {
            Nearest,
            Linear
        };

        private static MipmapMode ExtractMipmapMode(H3DTextureMinFilter Filter)
        {
            switch (Filter)
            {
                case H3DTextureMinFilter.Nearest: return MipmapMode.None;
                case H3DTextureMinFilter.NearestMipmapNearest: return MipmapMode.Nearest;
                case H3DTextureMinFilter.NearestMipmapLinear: return MipmapMode.Linear;
                case H3DTextureMinFilter.Linear: return MipmapMode.None;
                case H3DTextureMinFilter.LinearMipmapNearest: return MipmapMode.Nearest;
                case H3DTextureMinFilter.LinearMipmapLinear: return MipmapMode.Linear;

                default: throw new ArgumentException("Invalid minification filter!");
            }
        }

        private static MinFilterMode ExtractMinFilterMode(H3DTextureMinFilter Filter)
        {
            switch (Filter)
            {
                case H3DTextureMinFilter.Nearest:
                case H3DTextureMinFilter.NearestMipmapNearest:
                case H3DTextureMinFilter.NearestMipmapLinear: 
                    return MinFilterMode.Nearest;
                case H3DTextureMinFilter.Linear:
                case H3DTextureMinFilter.LinearMipmapNearest:
                case H3DTextureMinFilter.LinearMipmapLinear:
                    return MinFilterMode.Linear;

                default: throw new ArgumentException("Invalid minification filter!");
            }
        }

        private static H3DTextureMinFilter MergeMinFilterMipmapMode(MinFilterMode Filter, MipmapMode Mipmap)
        {
            if (Filter == MinFilterMode.Nearest)
            {
                switch (Mipmap)
                {
                    case MipmapMode.None: return H3DTextureMinFilter.Nearest;
                    case MipmapMode.Nearest: return H3DTextureMinFilter.NearestMipmapNearest;
                    case MipmapMode.Linear: return H3DTextureMinFilter.NearestMipmapLinear;
                    default: break;
                }
            } else if (Filter == MinFilterMode.Linear)
            {
                switch (Mipmap)
                {
                    case MipmapMode.None: return H3DTextureMinFilter.Linear;
                    case MipmapMode.Nearest: return H3DTextureMinFilter.LinearMipmapNearest;
                    case MipmapMode.Linear: return H3DTextureMinFilter.LinearMipmapLinear;
                    default: break;
                }
            }
            throw new ArgumentException("Invalid minification filter!");
        }

        private static TextureMagFilter GetMagFilter(H3DTextureMagFilter Filter)
        {
            switch (Filter)
            {
                case H3DTextureMagFilter.Linear: return TextureMagFilter.Linear;
                case H3DTextureMagFilter.Nearest: return TextureMagFilter.Nearest;

                default: throw new ArgumentException("Invalid magnification filter!");
            }
        }
    }
}
