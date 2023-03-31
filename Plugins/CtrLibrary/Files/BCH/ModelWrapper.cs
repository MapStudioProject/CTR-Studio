using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.Model.Mesh;
using SPICA.Formats.CtrGfx.Model.Material;
using SPICA.Formats.CtrH3D;

using ImGuiNET;
using SPICA.Formats.CtrH3D.Model.Material;
using OpenTK;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using MapStudio.UI;
using Newtonsoft.Json;
using System.IO;
using Toolbox.Core;
using CtrLibrary.Rendering;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using System.IO.Compression;
using SPICA.PICA.Commands;
using SPICA.PICA.Converters;
using SPICA.Formats.CtrH3D.Shader;
using SPICA.Formats.Common;
using SPICA.Rendering;

namespace CtrLibrary.Bch
{
    public class ModelFolder : NodeBase
    {
        private H3D H3DFile;

        private BCH ParentBCHNode;

        public override string Header => "Models";

        public ModelFolder(BCH bch, H3D h3D)
        {
            ParentBCHNode = bch;
            H3DFile = h3D;

            ContextMenus.Add(new MenuItemModel("Import", Add));

            foreach (var model in h3D.Models)
                AddChild(new CMDL(bch, h3D, model));
        }

        public void OnSave()
        {
            foreach (CMDL model in this.Children)
                model.OnSave();
        }

        public void Add()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.AddFilter(".dae", "dae");
            dlg.AddFilter(".fbx", "fbx");
            dlg.AddFilter(".smd", "smd");
            dlg.AddFilter(".bcmdl", "bcmdl");

            if (dlg.ShowDialog())
            {
                if (dlg.FilePath.ToLower().EndsWith(".dae") ||
                    dlg.FilePath.ToLower().EndsWith(".fbx") ||
                    dlg.FilePath.ToLower().EndsWith(".smd") ||
                    dlg.FilePath.ToLower().EndsWith(".obj"))
                {
                    CtrModelImportUI importerUI = new CtrModelImportUI();
                    DialogHandler.Show("Importer", 400, 400, () =>
                    {
                        importerUI.Render();
                    }, (o) =>
                    {
                        if (o)
                        {
                            try
                            {
                                H3DModel model = new H3DModel()
                                {
                                    Name = "NewModel",
                                };
                                var modelWrapper = new CMDL(ParentBCHNode, H3DFile, model);
                                modelWrapper.ImportFile(dlg.FilePath, importerUI.Settings);
                                AddChild(modelWrapper);
                            }
                            catch (Exception ex)
                            {
                                DialogHandler.ShowException(ex);
                            }
                        }
                    });
                }
                else if (dlg.FilePath.ToLower().EndsWith(".bcmdl"))
                {
                    H3DModel model = new H3DModel()
                    {
                        Name = "NewModel",
                    };
                    var modelWrapper = new CMDL(ParentBCHNode, H3DFile, model);
                    modelWrapper.ImportFile(dlg.FilePath, new CtrImportSettings());
                    AddChild(modelWrapper);
                }
            }
        }
    }

    public class CMDL : NodeBase, IPropertyUI
    {
        internal H3D H3DFile;

        /// <summary>
        /// The model instance of the bcres file.
        /// </summary>
        public H3DModel Model { get; set; }

        public BCH ParentBCHNode;

        private readonly NodeBase _meshFolder = new NodeBase("Meshes");
        private readonly NodeBase _materialFolder = new NodeBase("Materials");
        private readonly NodeBase _skeletonFolder = new NodeBase("Skeleton");

        public SkeletonRenderer SkeletonRenderer;

        public FSKL Skeleton;

        public Type GetTypeUI() => typeof(BchModelUI);

        public void OnLoadUI(object uiInstance)
        {
            ((BchModelUI)uiInstance).Init(this, Model);
        }

        public void OnRenderUI(object uiInstance)
        {
            ((BchModelUI)uiInstance).Render();
        }

        public CMDL(BCH bch, H3D h3dFile, H3DModel model)
        {
            ParentBCHNode = bch;
            H3DFile = h3dFile;
            Model = model;
            Header = model.Name;
            Icon = MapStudio.UI.IconManager.MODEL_ICON.ToString();
            Tag = Model;
            this.CanRename = true;
            ContextMenus.Add(new MenuItemModel("Export", Export));
            ContextMenus.Add(new MenuItemModel("Replace", Replace));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Rename", () => { this.ActivateRename = true; }));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Delete", Delete));

            HasCheckBox = true;
            OnChecked += delegate
            {
                Model.IsVisible = this.IsChecked;
            };

            AddChild(_meshFolder);
            AddChild(_materialFolder);
            AddChild(_skeletonFolder);

            ReloadModel();
        }

        public void ReloadRender()
        {
            int modelIndex = H3DFile.Models.Find(Model.Name);
            //Get current render, remove then update with new one
            ParentBCHNode.Render.InsertModel(Model, modelIndex);

            GLContext.ActiveContext.UpdateViewport = true;
        }

        private void ReloadModel()
        {
            _meshFolder.Children.Clear();
            _materialFolder.Children.Clear();

            foreach (var mesh in Model.Meshes)
                _meshFolder.AddChild(new SOBJ(Model, mesh));
            foreach (var material in Model.Materials)
                _materialFolder.AddChild(new MaterialWrapper(ParentBCHNode.Render, Model, material));
          

            Skeleton = new FSKL(Model.Skeleton);

            if (SkeletonRenderer != null)
                ParentBCHNode.Render.Skeletons.Remove(SkeletonRenderer);

           SkeletonRenderer = new SkeletonRenderer(Skeleton);
            ParentBCHNode.Render.Skeletons.Add(SkeletonRenderer);

           _skeletonFolder.Children.Clear();
            foreach (var bone in SkeletonRenderer.Bones)
                if (bone.Parent == null)
                    _skeletonFolder.AddChild(bone.UINode);
        }

        public void OnSave()
        {
            //Update the vis names on save so they can be adjusted in editors without conflicts
            Model.MeshNodesTree.Clear();
            foreach (SOBJ mesh in _meshFolder.Children)
            {
                mesh.Mesh.NodeIndex = 0;
                if (!string.IsNullOrEmpty(mesh.MeshVisName))
                {
                    if (!Model.MeshNodesTree.Contains(mesh.MeshVisName))
                        Model.MeshNodesTree.Add(mesh.MeshVisName);
                    mesh.Mesh.NodeIndex = (ushort)Model.MeshNodesTree.Find(mesh.MeshVisName);
                }
            }
            foreach (MaterialWrapper mat in _materialFolder.Children)
                mat.OnSave();
        }

        private void Delete()
        {
            int result = TinyFileDialog.MessageBoxInfoYesNo(String.Format("Are you sure you want to remove {0}? This cannot be undone.", this.Header));
            if (result != 1)
                return;

            //Index of current model
            int modelIndex = H3DFile.Models.Find(Model.Name);

            //Remove from renderer
            ParentBCHNode.Render.Renderer.Models.RemoveAt(modelIndex);
            //Remove from file data
            H3DFile.Models.Remove(Model.Name);
            //Remove from gui
            Parent.Children.Remove(this);
        }

        private void Export()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = true;
            dlg.FileName = $"{Header}";
            dlg.AddFilter(".dae", "dae");
            dlg.AddFilter(".json", "json");
            dlg.AddFilter(".bcmdl", "bcmdl");

            if (dlg.ShowDialog())
            {
                if (dlg.FilePath.EndsWith(".dae"))
                {
                    int modelIndex = H3DFile.Models.Find(Model.Name);
                    var collada = new SPICA.Formats.Generic.COLLADA.DAE(H3DFile, modelIndex);
                    collada.Save(dlg.FilePath);

                    string folder = Path.GetDirectoryName(dlg.FilePath);

                    foreach (var h3dTex in H3DFile.Textures)
                    {
                        //Save image as png
                        h3dTex.ToBitmap().Save(Path.Combine(folder, $"{h3dTex.Name}.png"));
                    }
                }
                else if (dlg.FilePath.EndsWith(".json"))
                    File.WriteAllText(dlg.FilePath, JsonConvert.SerializeObject(Model, Formatting.Indented));
                else if (dlg.FilePath.EndsWith(".bcmdl"))
                {
                    BCH.ExportRaw(dlg.FilePath, Model, BCH.H3DGroupType.Models);
                }
            }
        }

        private void Replace()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.FileName = $"{Header}";
            dlg.AddFilter(".dae", "dae");
            dlg.AddFilter(".fbx", "fbx");
            dlg.AddFilter(".smd", "smd");
            dlg.AddFilter(".bcmdl", "bcmdl");

            if (dlg.ShowDialog())
            {
                if (dlg.FilePath.ToLower().EndsWith(".dae") ||
                             dlg.FilePath.ToLower().EndsWith(".fbx") ||
                             dlg.FilePath.ToLower().EndsWith(".smd") ||
                             dlg.FilePath.ToLower().EndsWith(".obj"))
                {
                    CtrModelImportUI importerUI = new CtrModelImportUI();
                    DialogHandler.Show("Importer", 400, 400, () =>
                    {
                        importerUI.Render();
                    }, (o) =>
                    {
                        if (o)
                        {
                            try
                            {
                                ImportFile(dlg.FilePath, importerUI.Settings);
                            }
                            catch (Exception ex)
                            {
                                DialogHandler.ShowException(ex);
                            }
                        }
                    });
                }
                else if (dlg.FilePath.ToLower().EndsWith(".bcmdl"))
                {
                    try
                    {
                        ImportFile(dlg.FilePath, new CtrImportSettings());
                    }
                    catch (Exception ex)
                    {
                        DialogHandler.ShowException(ex);
                    }
                }
            }
        }

        public void ImportFile(string filePath, CtrImportSettings settings)
        {
            //Index of current model
            int modelIndex = H3DFile.Models.Find(Model.Name);

            if (filePath.EndsWith(".bcmdl"))
                Model = (H3DModel)BCH.ReplaceRaw(filePath, BCH.H3DGroupType.Models);
            else
                Model = BchModelImporter.Import(filePath, ParentBCHNode, Model, settings);
            //Keep the same name
            Model.Name = this.Header;

            //Get current render, remove then update with new one
            ParentBCHNode.Render.InsertModel(Model, modelIndex);

            //Replace existing model. If one is not being replaced, add it
            if (modelIndex != -1)
                H3DFile.Models[modelIndex] = Model;
            else
                H3DFile.Models.Add(Model);

            ReloadModel();
        }

        public void AddMaterial()
        {

        }

        public void ReloadModelRender()
        {
            //Index of current model
            int modelIndex = H3DFile.Models.Find(Model.Name);

            //Get current render, remove then update with new one
            ParentBCHNode.Render.InsertModel(Model, modelIndex);
        }
    }

    public class FSKL : STSkeleton
    {
        public FSKL(H3DDict<H3DBone> bones)
        {
            foreach (var bone in bones)
            {
                STBone bn = new BcresBone(this)
                {
                    BoneData = bone,
                    Name = bone.Name,
                    Position = new OpenTK.Vector3(
                        bone.Translation.X,
                        bone.Translation.Y,
                        bone.Translation.Z),
                    EulerRotation = new OpenTK.Vector3(
                        bone.Rotation.X,
                        bone.Rotation.Y,
                        bone.Rotation.Z),
                    Scale = new OpenTK.Vector3(
                        bone.Scale.X,
                        bone.Scale.Y,
                        bone.Scale.Z),
                };
                Bones.Add(bn);
                bn.ParentIndex = bone.ParentIndex;
            }
            this.Reset();
            this.Update();
        }
    }

    class BcresBone : STBone, IPropertyUI
    {
        public H3DBone BoneData { get; set; }

        public Type GetTypeUI() => typeof(BchBoneUI);

        public void OnLoadUI(object uiInstance)
        {
            ((BchBoneUI)uiInstance).Init(this, BoneData);
        }

        public void OnRenderUI(object uiInstance)
        {
            ((BchBoneUI)uiInstance).Render();
        }

        public BcresBone(STSkeleton skeleton) : base(skeleton)
        {
        }

        /// <summary>
        /// Updates the drawn bone transform back to the bcres file data
        /// </summary>
        public void UpdateBcresTransform()
        {
            BoneData.Translation = new System.Numerics.Vector3(
                this.Position.X,
                this.Position.Y,
                this.Position.Z);
            BoneData.Scale = new System.Numerics.Vector3(
                this.Scale.X,
                this.Scale.Y,
                this.Scale.Z);
            BoneData.Rotation = new System.Numerics.Vector3(
               this.EulerRotation.X,
               this.EulerRotation.Y,
               this.EulerRotation.Z);
            //Update the flags when transform has been adjusted
            UpdateTransformFlags();
        }

        private Matrix4x4 CalculateWorldMatrix()
        {
            Matrix4x4 transform = CalculateLocalMatrix();
            if (Parent != null)
                return transform * ((BcresBone)Parent).CalculateWorldMatrix();
            return transform;
        }

        private Matrix4x4 CalculateLocalMatrix()
        {
            return Matrix4x4.CreateScale(BoneData.Scale) *
                   (Matrix4x4.CreateRotationX(BoneData.Rotation.X) *
                    Matrix4x4.CreateRotationY(BoneData.Rotation.Y) *
                    Matrix4x4.CreateRotationZ(BoneData.Rotation.Z)) *
                    Matrix4x4.CreateTranslation(BoneData.Translation);
        }

        /// <summary>
        /// Updates the current bone transform flags.
        /// These flags determine what matrices can be ignored for matrix updating.
        /// </summary>
        public void UpdateTransformFlags()
        {
            H3DBoneFlags flags = BoneData.Flags;

            //Reset transform flags
            flags &= ~H3DBoneFlags.IsTranslationZero;
            flags &= ~H3DBoneFlags.IsScaleVolumeOne;
            flags &= ~H3DBoneFlags.IsRotationZero;
            flags &= ~H3DBoneFlags.IsScaleUniform;

            //SRT checks to update matrices
            if (this.Position == OpenTK.Vector3.Zero)
                flags |= H3DBoneFlags.IsTranslationZero;
            if (this.Scale == OpenTK.Vector3.One)
                flags |= H3DBoneFlags.IsScaleVolumeOne;
            if (this.Rotation == OpenTK.Quaternion.Identity)
                flags |= H3DBoneFlags.IsRotationZero;
            //Extra scale flags
            if (this.Scale.X == this.Scale.Y && this.Scale.X == this.Scale.Z)
                flags |= H3DBoneFlags.IsScaleUniform;

            BoneData.Flags = flags;
        }
    }

    public class SOBJ : NodeBase, IPropertyUI
    {
        public H3DModel Model { get; set; }
        public H3DMaterial Material { get; set; }
        public H3DMesh Mesh { get; set; }

        public string MeshVisName = "";

        public Type GetTypeUI() => typeof(BchMeshUI);

        public void OnLoadUI(object uiInstance)
        {
            ((BchMeshUI)uiInstance).Init(this, Model, Mesh);
        }

        public void OnRenderUI(object uiInstance)
        {
            ((BchMeshUI)uiInstance).Render();
        }

        public H3DShader TryGetShader()
        {
            var h3d = (this.Parent.Parent as CMDL).H3DFile;
            return h3d.Shaders.FirstOrDefault(x => x.Name == Material.MaterialParams.ShaderReference);
        }

        public SOBJ(H3DModel model, H3DMesh mesh)
        {
            Model = model;
            Mesh = mesh;
            Material = model.Materials[(int)mesh.MaterialIndex];
            Tag = Mesh;

            Icon = IconManager.MESH_ICON.ToString();
            this.Header = $"Mesh{model.Meshes.IndexOf(mesh)}_{Material.Name}";

            if (model.MeshNodesTree?.Count > 0 && mesh.NodeIndex < model.MeshNodesTree.Count)
                MeshVisName = model.MeshNodesTree.Find(mesh.NodeIndex);

            var debugMenu = new MenuItemModel("Debug");
            debugMenu.MenuItems.Add(new MenuItemModel("Export Vertex Data", ExportRawBuffer));
            ContextMenus.Add(debugMenu);

            var normalsMenu = new MenuItemModel("Normals");
            normalsMenu.MenuItems.Add(new MenuItemModel("Smooth", SmoothNormals));
            normalsMenu.MenuItems.Add(new MenuItemModel("Recalculate", RecalculateNormals));
            ContextMenus.Add(normalsMenu);

            MenuItemModel uvsMenu = new MenuItemModel("UVs");
            uvsMenu.MenuItems.Add(new MenuItemModel("Flip Vertical", FlipUVsVerticalAction));
            uvsMenu.MenuItems.Add(new MenuItemModel("Flip Horizontal", FlipUVsHorizontalAction));
            ContextMenus.Add(uvsMenu);

            MenuItemModel colorsMenu = new MenuItemModel("Colors");
            colorsMenu.MenuItems.Add(new MenuItemModel("Set White", SetVertexColorsWhiteAction));
            colorsMenu.MenuItems.Add(new MenuItemModel("Set Color", SetVertexColorsAction));
            ContextMenus.Add(colorsMenu);

            ContextMenus.Add(new MenuItemModel("Recalculate Tangents", RecalculateTangent));

            UpdateNodeName();

            HasCheckBox = true;
            OnChecked += delegate
            {
                Mesh.IsVisible = this.IsChecked;
            };
            this.OnSelected += delegate
            {
                Mesh.IsSelected = this.IsSelected;
            };
        }

        public void FlipUVsVerticalAction() => UpdateVertexData(PicaVertexEditor.FlipUVsVertical);
        public void FlipUVsHorizontalAction() => UpdateVertexData(PicaVertexEditor.FlipUvsHorizontal);
        public void SmoothNormals() => UpdateVertexData(PicaVertexEditor.SmoothNormals);
        public void RecalculateNormals() => UpdateVertexData(PicaVertexEditor.CalculateNormals);
        public void RecalculateTangent() => UpdateVertexData(PicaVertexEditor.CalculateTangent);

        private void SetVertexColorsWhiteAction()
        {
            UpdateVertexData(() =>
            {
                PicaVertexEditor.SetVertexColor(OpenTK.Vector4.One);
            });
        }

        private void SetVertexColorsAction()
        {
            var color = System.Numerics.Vector4.One;

            ImguiCustomWidgets.ColorDialog(color, (outputColor) =>
            {
                UpdateVertexData(() =>
                {
                    PicaVertexEditor.SetVertexColor(new OpenTK.Vector4(outputColor.X, outputColor.Y, outputColor.Z, outputColor.W));
                });
            });
        }

        private void UpdateVertexData(Action action)
        {
            var selected = this.Parent.Children.Where(x => x.IsSelected);
            var meshes = selected.Select(x => ((SOBJ)x).Mesh).ToArray();

            PicaVertexEditor.Start(meshes);
            action();

            for (int i = 0; i < meshes.Length; i++)
            {
                var vertices = PicaVertexEditor.End(meshes[i], i);
                var stride = VerticesConverter.CalculateStride(meshes[i].Attributes);
                var rawBuffer = VerticesConverter.GetBuffer(vertices, meshes[i].Attributes, stride);

                meshes[i].RawBuffer = rawBuffer;
                meshes[i].VertexStride = stride;
            }

            var cmdl = this.Parent.Parent as CMDL;
            cmdl.ReloadRender();
        }

        private void ExportRawBuffer()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = true;
            dlg.FileName = $"{Header}";
            dlg.AddFilter(".bin", "bin");
            if (dlg.ShowDialog())
            {
                File.WriteAllBytes(dlg.FilePath, Mesh.RawBuffer);
            }
        }

        public void UpdateNodeName()
        {
            this.Header = $"Mesh{Model.Meshes.IndexOf(Mesh)}_{Material.Name}";
            if (!string.IsNullOrEmpty(MeshVisName))
                this.Header = MeshVisName;
        }
    }

    public class MaterialWrapper : NodeBase, IPropertyUI
    {
        private H3DModel Model { get; set; }
        public H3DMaterial Material { get; set; }

        public H3DRender Render { get; set; }


        private bool reloadUI = false;

        public Type GetTypeUI() => typeof(MaterialUI);

        public void OnLoadUI(object uiInstance)
        {
            ((MaterialUI)uiInstance).Init(this, Model, Material);
        }

        public void OnRenderUI(object uiInstance)
        {
            if (reloadUI) {
                reloadUI = false;
                OnLoadUI(uiInstance);
            }

            ((MaterialUI)uiInstance).Render();
        }

        public void UpdateModel(H3DModel model)
        {
            Model = model;
        }

        public MaterialWrapper(H3DRender render, H3DModel model, H3DMaterial material)
        {
            Render = render;
            Model = model;
            Material = material;
            Header = material.Name;
            Tag = Material;
            Icon = model.Name + material.Name + "_mat";
            CanRename = true;

            ContextMenus.Add(new MenuItemModel("Export", Export));
            ContextMenus.Add(new MenuItemModel("Replace", Replace));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Copy", Copy));
            ContextMenus.Add(new MenuItemModel("Paste", Paste));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Delete", DeleteBatch));

            this.OnSelected += delegate
            {
                if (this.IsSelected)
                    Material.MaterialParams.SelectionColor = new System.Numerics.Vector4(
                        0.5F, 0.5f, 0, 0.05f);
                else
                    Material.MaterialParams.SelectionColor = new System.Numerics.Vector4(0);
            };

            //Pokemon specific data
            //Here we check the shader name but not the shader data itself as that can be external
            if (Material.MaterialParams.ShaderReference.Contains("PokePack"))
            {
                //Get the first sub mesh assigned by the mesh list
                foreach (var mesh in model.Meshes)
                {
                    //Mesh uses material
                    if (model.Materials[mesh.MaterialIndex] == material)
                    {
                        //First sub mesh
                        var sm = mesh.SubMeshes[0];
                        //4 - 6 bits are used for custom vertex shader boolean settings
                        material.PokemonUserBooleans.IsPhongEnabled = BitUtils.GetBit(sm.BoolUniforms, 3);
                        material.PokemonUserBooleans.IsRimEnabled = BitUtils.GetBit(sm.BoolUniforms, 4);
                        material.PokemonUserBooleans.IsInverseLightEnabled = BitUtils.GetBit(sm.BoolUniforms, 5);
                        material.PokemonUserBooleans.IsLightEnabled = BitUtils.GetBit(sm.BoolUniforms, 6);
                    }
                }
            }

            ReloadIcon();

            if (!IconManager.HasIcon(Icon))
                IconManager.AddIcon(Icon, RenderIcon(21).ID);
        }

        public void OnSave()
        {
            UpdatePokeUserData();
        }

        public void UpdatePokeUserData()
        {
            //Pokemon specific data
            //Here we check the shader name but not the shader data itself as that can be external
            if (Material.MaterialParams.ShaderReference.Contains("PokePack"))
            {
                //Get the first sub mesh assigned by the mesh list
                foreach (var mesh in Model.Meshes)
                {
                    //Mesh uses material
                    if (Model.Materials[mesh.MaterialIndex] == Material)
                    {
                        //Apply to each sub mesh
                        for (int i = 0; i < mesh.SubMeshes.Count; i++)
                        {
                            var sm = mesh.SubMeshes[i];
                            //4 - 6 bits are used for custom vertex shader boolean settings
                            sm.BoolUniforms = (ushort)BitUtils.SetBit(sm.BoolUniforms, Material.PokemonUserBooleans.IsPhongEnabled, 3);
                            sm.BoolUniforms = (ushort)BitUtils.SetBit(sm.BoolUniforms, Material.PokemonUserBooleans.IsRimEnabled, 4);
                            sm.BoolUniforms = (ushort)BitUtils.SetBit(sm.BoolUniforms, Material.PokemonUserBooleans.IsInverseLightEnabled, 5);
                            sm.BoolUniforms = (ushort)BitUtils.SetBit(sm.BoolUniforms, Material.PokemonUserBooleans.IsLightEnabled, 6);
                        }
                    }
                }
            }
        }

        private void Copy()
        {
            var selected = Parent.Children.Where(x => x.IsSelected).ToList();
            if (selected.Count > 1)
            {
                TinyFileDialog.MessageBoxErrorOk("Error! Only one material can be copied, but multiple can be pasted.");
                return;
            }

            float height = MaterialCopyTool.CopyToggles.Count * ImGui.GetFrameHeightWithSpacing();

            DialogHandler.Show("Copy", 250, 80 + height, () =>
            {
                bool anyVisible = MaterialCopyTool.CopyToggles.Any(x => x.Value);
                if  (ImGui.Checkbox("Toggle", ref anyVisible))
                {
                    foreach (var item in MaterialCopyTool.CopyToggles)
                        MaterialCopyTool.CopyToggles[item.Key] = anyVisible;
                }

                foreach (var item in MaterialCopyTool.CopyToggles)
                {
                    ImGuiHelper.IncrementCursorPosX(30);

                    bool enable = item.Value;
                    if (ImGui.Checkbox($"{item.Key}", ref enable))
                        MaterialCopyTool.CopyToggles[item.Key] = enable;
                }

                DialogHandler.DrawCancelOk();
            }, (enter) =>
            {
                if (!enter)
                    return;

                MaterialCopyTool.Copy(Material);
            });
        }

        private void Paste()
        {
            var selected = Parent.Children.Where(x => x.IsSelected).ToList();
            foreach (MaterialWrapper mat in selected)
            {
                MaterialCopyTool.Paste(mat.Material);
                mat.ReloadIcon();
                mat.UpdateUniformBooleans();
            }
            this.UpdateShaders();

            GLContext.ActiveContext.UpdateViewport = true;
            reloadUI = true;
        }

        public virtual void DeleteBatch()
        {
            var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();

            if (Model.Materials.Count == 1 || selected.Count == Model.Materials.Count)
            {
                TinyFileDialog.MessageBoxErrorOk("The model must have atleast one material to work correctly!");
                return;
            }

            string msg = $"Are you sure you want to delete the ({selected.Count}) selected materials? This cannot be undone!";
            if (selected.Count == 1)
                msg = $"Are you sure you want to delete material {Header}? This cannot be undone!";

            int result = TinyFileDialog.MessageBoxInfoYesNo(msg);
            if (result != 1)
                return;

            foreach (MaterialWrapper mat in selected)
            {
                mat.Delete();
            }
            //Update the renderer
            Render.InsertModel(Model, 0);
        }

        public virtual void Delete()
        {
            //List out all references of the material
            List<string> matMeshReferences = new List<string>();
            foreach (var mesh in Model.Meshes)
                matMeshReferences.Add(Model.Materials[mesh.MaterialIndex].Name);

            //Remove the material
            Model.Materials.Remove(this.Material.Name);

            //Update references
            for (int i = 0; i < Model.Meshes.Count; i++)
            {
                var index = Model.Materials.Find(matMeshReferences[i]);
                if (index == -1) //Must have a material. Choose first one by default
                    index = 0;

                Model.Meshes[i].MaterialIndex = (ushort)index;
            }
            //Remove from UI
            var parent = Parent;
            parent.Children.Remove(this);

            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            GLContext.ActiveContext.UpdateViewport = true;
        }

        public void UpdateUniformBooleans()
        {
            foreach (var mesh in Model.Meshes)
            {
                if (Model.Materials[mesh.MaterialIndex].Name != Material.Name)
                    continue;

                mesh.UpdateBoolUniforms(Material);
            }
        }

        public void UpdateAllUniforms() {
            Render.UpdateAllUniforms();
        }

        public void UpdateShaders() {
            Render.UpdateShaders();
        }

        private void Export()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = true;
            dlg.FileName = $"{Header}";
            dlg.AddFilter(".json", "json");
            if (dlg.ShowDialog())
                File.WriteAllText(dlg.FilePath, JsonConvert.SerializeObject(Material, Formatting.Indented));
        }

        private void Replace()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.FileName = $"{Header}";
            dlg.AddFilter(".json", "json");
            if (dlg.ShowDialog())
            {
                int index = Model.Materials.Find(Material.Name);

                Material = JsonConvert.DeserializeObject<H3DMaterial>(File.ReadAllText(dlg.FilePath));
                Material.Name = this.Header;

                //Update render
                Model.Materials[index] = Material;
                this.UpdateShaders();

                ReloadIcon();

                GLContext.ActiveContext.UpdateViewport = true;
                reloadUI = true;
            }
        }
        class TextureMeta
        {
            public int MipCount;
            public PICATextureFormat Format;
        }

        public void ExportPreset(string presetName)
        {
            string dir = Path.Combine(Runtime.ExecutableDir, "Lib", "Presets", "Materials");
            string tempDir = Path.Combine(dir, presetName);

            //Temp directory for packing into a zip
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            //Save the material as json
            File.WriteAllText(Path.Combine(tempDir, $"{presetName}.mat"), JsonConvert.SerializeObject(Material, Formatting.Indented));

            //Save the texture files and meta info
            void SaveTexture(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return;

                foreach (var tex in H3DRender.TextureCache.Values)
                {
                    if (tex.Name != name)
                        continue;

                    tex.ToBitmap().Save(Path.Combine(tempDir, $"{tex.Name}.png"));
                    var json = JsonConvert.SerializeObject(new TextureMeta()
                    {
                        MipCount = tex.MipmapSize,
                        Format = tex.Format,
                    }, Formatting.Indented);
                    File.WriteAllText(Path.Combine(tempDir, $"{tex.Name}.tex"), json);
                }
            };
            SaveTexture(Material.Texture0Name);
            SaveTexture(Material.Texture1Name);
            SaveTexture(Material.Texture2Name);

            //Remove previous preset
            if (File.Exists(Path.Combine(dir, $"{presetName}.zip")))
                File.Delete(Path.Combine(dir, $"{presetName}.zip"));

            //Package preset
            ZipFile.CreateFromDirectory(tempDir, Path.Combine(dir, $"{presetName}.zip"));
            //Remove directory
            foreach (var file in Directory.GetFiles(tempDir))
                File.Delete(file);

            Directory.Delete(tempDir);

            TinyFileDialog.MessageBoxInfoOk($"Preset {presetName} has been saved to {Path.Combine(dir, $"{ presetName}.zip")}!");
        }

        public void ImportPresetBatch(string presetFilePath, bool keepTextures = true)
        {
            var materials = Parent.Children.Where(x => x.IsSelected).ToList();
            foreach (MaterialWrapper mat in materials)
                mat.ImportPreset(presetFilePath, keepTextures);
        }

        //Get the material directly for loading
        public static H3DMaterial GetPresetMaterial(string presetFilePath)
        {
            var zip = ZipFile.OpenRead(presetFilePath);
            foreach (var file in zip.Entries)
            {
                var data = file.Open();
                if (file.Name.EndsWith(".mat"))
                {
                    using var sr = new StreamReader(data);
                    {
                        return JsonConvert.DeserializeObject<H3DMaterial>(sr.ReadToEnd());
                    }
                }
            }
            return null;
        }


        public void ImportPreset(string presetFilePath, bool keepTextures = true)
        {
            var zip = ZipFile.OpenRead(presetFilePath);
            foreach (var file in zip.Entries)
            {
                var data = file.Open();
                if (file.Name.EndsWith(".mat"))
                {
                    using var sr = new StreamReader(data);
                    {
                        string texture0 = Material.Texture0Name;
                        string texture1 = Material.Texture1Name;
                        string texture2 = Material.Texture2Name;

                        int index = Model.Materials.Find(Material.Name);

                        Material = JsonConvert.DeserializeObject<H3DMaterial>(sr.ReadToEnd());
                        Material.Name = this.Header;

                        if (keepTextures)
                        {
                            if (!string.IsNullOrEmpty(texture0))
                                Material.Texture0Name = texture0;
                            if (!string.IsNullOrEmpty(texture1))
                                Material.Texture1Name = texture1;
                            if (!string.IsNullOrEmpty(texture2))
                                Material.Texture2Name = texture2;
                        }

                        //Update render
                        Model.Materials[index] = Material;
                        this.UpdateShaders();

                        ReloadIcon();

                        GLContext.ActiveContext.UpdateViewport = true;
                        reloadUI = true;
                    }
                }
                if (file.Name.EndsWith(".png"))
                {

                }
            }
        }

        public void ReloadIcon()
        {
            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            IconManager.AddIcon(Icon, RenderIcon(21).ID);
        }

        static UVSphereRender MaterialSphere;

        public GLTexture RenderIcon(int size)
        {
            var context = new GLContext();
            context.Camera = new GLFrameworkEngine.Camera();

            var frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, size, size);
            frameBuffer.Bind();

            GL.Viewport(0, 0, size, size);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //Create a simple mvp matrix to render the material data
            Matrix4 modelMatrix = Matrix4.CreateTranslation(0, 0, -12);
            Matrix4 viewMatrix = Matrix4.Identity;
            Matrix4 mtxProj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, 1.0f, 1000f);
            Matrix4 viewProj = mtxProj * viewMatrix;

            var mat = new StandardMaterial();
            mat.HalfLambertShading = true;
            mat.DirectionalLighting = false;
            mat.CameraMatrix = viewProj;
            mat.ModelMatrix = modelMatrix;
            mat.IsSRGB = false;

            if (!string.IsNullOrEmpty(Material.Texture0Name))
            {
                string textureName = Material.Texture0Name;
                if (H3DRender.TextureCache.ContainsKey(textureName))
                {
                    var tex = H3DRender.TextureCache[textureName];
                    mat.DiffuseTextureID = GLTexture2D.FromBitmap(tex.ToBitmap()).ID;
                }
            }

            if (MaterialSphere == null)
                MaterialSphere = new UVSphereRender(8);

            mat.Render(context);
            MaterialSphere.Draw(context);

            var thumbnail = frameBuffer.ReadImagePixels(true);

            //Disable shader and textures
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Disable(EnableCap.FramebufferSrgb);

            frameBuffer.Dispose();

            return GLTexture2D.FromBitmap(thumbnail);
        }
    }

    class BoneWrapper
    {

    }
}
