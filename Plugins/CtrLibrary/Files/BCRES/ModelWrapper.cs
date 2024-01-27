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
using CtrLibrary.Bch;
using SPICA.PICA.Converters;
using SPICA.Formats.CtrGfx.AnimGroup;
using Toolbox.Core.Animations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using static SPICA.Rendering.Animation.SkeletalAnimation;
using IONET.Collada.Core.Controller;
using IONET.Core.Skeleton;
using static GLFrameworkEngine.SkeletonRenderer;

namespace CtrLibrary.Bcres
{
    public class ModelFolder : NodeBase
    {
        public override string Header => "Models";

        private BCRES ParentBCRESNode;

        private Gfx BcresFile;

        public ModelFolder(BCRES bcres, Gfx bcresFile, H3D h3D)
        {
            ParentBCRESNode = bcres;
            BcresFile = bcresFile;

            ContextMenus.Add(new MenuItemModel("Import", Add));

            foreach (var model in bcresFile.Models)
                AddChild(new CMDL(bcres, bcresFile, model));
        }

        private void Add()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.FileName = $"{Header}";
            dlg.AddFilter(".dae", "dae");
            dlg.AddFilter(".fbx", "fbx");
            if (dlg.ShowDialog())
            {
                if (dlg.FilePath.EndsWith(".dae") || dlg.FilePath.EndsWith(".fbx"))
                    Import(dlg.FilePath);
            }
        }

        public void Import(string filePath)
        {
            CtrModelImportUI importerUI = new CtrModelImportUI();
            DialogHandler.Show("Importer", 400, 500, () =>
            {
                importerUI.Render();
            }, (o) =>
            {
                if (o)
                {
                    GfxModelSkeletal model = new GfxModelSkeletal()
                    {
                        Name = "NewModel",
                        Flags = GfxModelFlags.IsVisible,
                        WorldTransform = new SPICA.Math3D.Matrix3x4(Matrix4x4.Identity),
                        LocalTransform = new SPICA.Math3D.Matrix3x4(Matrix4x4.Identity),
                        FaceCulling = SPICA.PICA.Commands.PICAFaceCulling.BackFace,
                        Skeleton = new GfxSkeleton() { Name = "", },
                    };

                    try
                    {
                        var modelWrapper = new CMDL(ParentBCRESNode, BcresFile, model);
                        modelWrapper.ImportFile(filePath, importerUI.Settings);
                        AddChild(modelWrapper);

                        GLContext.ActiveContext.UpdateViewport = true;
                    }
                    catch (Exception ex)
                    {
                        DialogHandler.ShowException(ex);
                    }
                }
            });
        }
    }

    public class CMDL : NodeBase, IPropertyUI
    {
        private BCRES ParentBCRESNode;

        private Gfx BcresFile;

        /// <summary>
        /// The model instance of the bcres file.
        /// </summary>
        public GfxModel Model { get; set; }

        private readonly NodeBase _meshFolder = new NodeBase("Meshes");
        private readonly NodeBase _materialFolder = new NodeBase("Materials");
        private readonly NodeBase _skeletonFolder = new NodeBase("Skeleton");

        //Settings to configure what groups to generate
        public AnimGroupHelper.AnimationSettings AnimGroupSettings = new AnimGroupHelper.AnimationSettings();

        public Type GetTypeUI() => typeof(BcresModelUI);

        public SkeletonRenderer SkeletonRenderer;

        public FSKL Skeleton;

        public void OnLoadUI(object uiInstance)
        {
            ((BcresModelUI)uiInstance).Init(this, Model);
        }

        public void OnRenderUI(object uiInstance)
        {
            ((BcresModelUI)uiInstance).Render();
        }

        public CMDL(BCRES bcres, Gfx bcresFile, GfxModel model)
        {
            ParentBCRESNode = bcres;
            BcresFile = bcresFile;
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
                Model.H3DModel.IsVisible = this.IsChecked;
            };

            _materialFolder.ContextMenus.Add(new MenuItemModel("Create Material", CreateMaterial));
            _materialFolder.ContextMenus.Add(new MenuItemModel("Import Material", ImportMaterial));

            AddChild(_meshFolder);
            AddChild(_materialFolder);
            AddChild(_skeletonFolder);

            ReloadModel();
        }

        private void Delete()
        {
            int result = TinyFileDialog.MessageBoxInfoYesNo(String.Format("Are you sure you want to remove {0}? This cannot be undone.", this.Header));
            if (result != 1)
                return;

            //Index of current model
            int modelIndex = BcresFile.Models.Find(Model.Name);

            //Remove from renderer
            ParentBCRESNode.Render.Renderer.Models.RemoveAt(modelIndex);

            if (ParentBCRESNode.Render.Skeletons.Contains(this.SkeletonRenderer))
                ParentBCRESNode.Render.Skeletons.Remove(this.SkeletonRenderer);

            //Remove from file data
            BcresFile.Models.Remove(Model);
            //Remove from gui
            Parent.Children.Remove(this);

            GLContext.ActiveContext.UpdateViewport = true;
        }

        private void ReloadModel()
        {
            _meshFolder.Children.Clear();
            _materialFolder.Children.Clear();

            foreach (var mesh in Model.Meshes)
                _meshFolder.AddChild(new SOBJ(Model, mesh));
            foreach (var material in Model.Materials)
                _materialFolder.AddChild(new MTOB(ParentBCRESNode.Render, Model, material));

            if (Model is GfxModelSkeletal)
            {
                var skeleton = ((GfxModelSkeletal)Model).Skeleton;
                this.Skeleton = new FSKL(_skeletonFolder, skeleton);

                if (SkeletonRenderer != null)
                    ParentBCRESNode.Render.Skeletons.Remove(SkeletonRenderer);

                SkeletonRenderer = new SkeletonRenderer(Skeleton);
                ParentBCRESNode.Render.Skeletons.Add(SkeletonRenderer);

                this.Skeleton.InitRender(SkeletonRenderer);

                _skeletonFolder.Children.Clear();
                foreach (var bone in SkeletonRenderer.Bones)
                    if (bone.Parent == null)
                        _skeletonFolder.AddChild(bone.UINode);
            }
            //Settings to toggle what animation groups to use. Checks which values are present
            AnimGroupSettings = AnimGroupHelper.SetupMaterialSettings(Model);
        }

        public void OnSave()
        {
            Model.Name = this.Header;

            Model.Materials.Clear();
            foreach (MTOB material in _materialFolder.Children) {
                material.GfxMaterial.ConvertH3D(material.Material);
                Model.Materials.Add(material.GfxMaterial);
            }

         //   GenerateAnimGroups();

            if (Model.MeshNodeVisibilities.Count > 0)
            {
                foreach (var mesh in Model.Meshes)
                {
                    mesh.MeshNodeIndex = -1;

                    if (string.IsNullOrEmpty(mesh.MeshNodeName))
                        continue;

                    //Check for meshes that are not present in the vis node list
                    if (!Model.MeshNodeVisibilities.Contains(mesh.MeshNodeName))
                        Model.MeshNodeVisibilities.Add(new GfxMeshNodeVisibility() //Add them to the list.
                        {
                            Name = mesh.MeshNodeName,
                            IsVisible = true,
                        });
                    //Set the index of the vis item
                    mesh.MeshNodeIndex = (short)Model.MeshNodeVisibilities.Find(mesh.MeshNodeName);
                }
            }
        }

        public void GenerateMaterialAnimGroups()
        {
            //Generate material animation groups automatically
            var anim = new GfxAnimGroup()
            {
                Name = "MaterialAnimation",
                EvaluationTiming = GfxAnimEvaluationTiming.AfterSceneCull,
                MemberType = 2,
                BlendOperationTypes = new int[4] { 3, 7, 5, 2 }
            };
            if (!Model.AnimationsGroup.Contains("MaterialAnimation"))
                Model.AnimationsGroup.Add(anim);

            var generatedAnimGroups = AnimGroupHelper.GenerateMatAnims(Model.Materials, AnimGroupSettings);
            Model.AnimationsGroup["MaterialAnimation"].Elements.Clear();
            foreach (var elem in generatedAnimGroups.Elements)
                Model.AnimationsGroup["MaterialAnimation"].Elements.Add(elem);
        }

        public void GenerateSkeletalAnimGroups()
        {
            if (!(Model is GfxModelSkeletal)) //model has no bones, skip
                return;

            //Generate material animation groups automatically
            var anim = new GfxAnimGroup()
            {
                Name = "SkeletalAnimation",
                EvaluationTiming = GfxAnimEvaluationTiming.AfterSceneCull,
                MemberType = 1,
                BlendOperationTypes = new int[1] { 8 }
            };
            if (!Model.AnimationsGroup.Contains("SkeletalAnimation"))
                Model.AnimationsGroup.Add(anim);

            var generatedAnimGroups = AnimGroupHelper.GenerateSkeletonAnims((GfxModelSkeletal)Model);
            Model.AnimationsGroup["SkeletalAnimation"].Elements.Clear();
            foreach (var elem in generatedAnimGroups.Elements)
                Model.AnimationsGroup["SkeletalAnimation"].Elements.Add(elem);
        }

        private void Export()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = true;
            dlg.FileName = $"{Header}";
            dlg.AddFilter(".dae", "dae");
            dlg.AddFilter(".json", "json");
            if (dlg.ShowDialog())
            {
                if (dlg.FilePath.EndsWith(".dae"))
                {
                    int modelIndex = BcresFile.Models.Find(Model.Name);
                    var h3d = BcresFile.ToH3D();
                    var collada = new SPICA.Formats.Generic.COLLADA.DAE(h3d, modelIndex);
                    collada.Save(dlg.FilePath);

                    string folder = Path.GetDirectoryName(dlg.FilePath);

                    foreach (var tex in BcresFile.Textures)
                    {
                        //Save image as png
                        var h3dTex = tex.ToH3D();
                        var image = h3dTex.ToBitmap();
                        image.SaveAsPng(Path.Combine(folder, $"{tex.Name}.png"));
                        image.Dispose();
                    }
                }
                if (dlg.FilePath.EndsWith(".json"))
                    File.WriteAllText(dlg.FilePath, JsonConvert.SerializeObject(Model, Formatting.Indented));
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
            if (dlg.ShowDialog())
            {
                if (dlg.FilePath.ToLower().EndsWith(".dae") || 
                    dlg.FilePath.ToLower().EndsWith(".fbx") ||
                    dlg.FilePath.ToLower().EndsWith(".smd") ||
                    dlg.FilePath.ToLower().EndsWith(".obj"))
                {
                    CtrModelImportUI importerUI = new CtrModelImportUI();
                    importerUI.Settings.DisableSkeleton = !(this.Model is GfxModelSkeletal);

                    DialogHandler.Show("Importer", 400, 500, () =>
                    {
                        importerUI.Render();
                    }, (o) =>
                    {
                        if (o)
                        {
                            ImportFile(dlg.FilePath, importerUI.Settings);

                            try
                            {
                            }
                            catch (Exception ex)
                            {
                                DialogHandler.ShowException(ex);
                            }
                        }
                    });
                }
            }
        }

        public void ReloadRender()
        {
            int modelIndex = BcresFile.Models.Find(Model.Name);
            var h3d = Model.ToH3D();
            ParentBCRESNode.Render.InsertModel(h3d, modelIndex);
            GLContext.ActiveContext.UpdateViewport = true;
        }

        public void ImportFile(string filePath, CtrImportSettings settings)
        {
            //Convert H3D materials first (used by gui and render) as gfx materials are applied in importer
            Model.Materials.Clear();
            foreach (MTOB material in _materialFolder.Children)
            {
                material.GfxMaterial.ConvertH3D(material.Material);
                Model.Materials.Add(material.GfxMaterial);
            }

            int modelIndex = BcresFile.Models.Find(Model.Name);

            BcresFile.Models.Remove(Model);

            Model = BcresModelImporter.Import(filePath, ParentBCRESNode, Model, settings);
            Model.Name = this.Header;

            //Generate animation groups with import as there may be additional materials to insert
            GenerateMaterialAnimGroups();
            if (settings.ImportBones)
                GenerateSkeletalAnimGroups();

            if (modelIndex != -1)
                BcresFile.Models.Insert(modelIndex, Model);
            else
                BcresFile.Models.Add(Model);

            var h3d = Model.ToH3D();

            //Get current render, remove then update with new one
            ParentBCRESNode.Render.InsertModel(h3d, modelIndex);

            ReloadModel();

            if (SkeletonRenderer != null)
            {
                if (ParentBCRESNode.Render.Skeletons.Contains(this.SkeletonRenderer))
                    ParentBCRESNode.Render.Skeletons.Remove(this.SkeletonRenderer);

                ParentBCRESNode.Render.Skeletons.Add(this.SkeletonRenderer);
            }

            //Generate and save a .div based on the generated sub mesh bounding boxes
            if (settings.DivideMK7)
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = this.Header;
                dlg.AddFilter(".div", "Divide File");
                if (dlg.ShowDialog())
                {
                    CDAB.Instance.Save(dlg.FilePath);
                }
            }
        }

        public void CreateMaterial()
        {
            int modelIndex = BcresFile.Models.Find(Model.Name);

            List<string> nameList = Model.Materials.Select(x => x.Name).ToList();

            var mat = GfxMaterial.CreateDefault();
            mat.Name = Utils.RenameDuplicateString("NewMaterial", nameList);
            Model.Materials.Add(mat);

            var h3d = Model.ToH3D();
            //Get current render, remove then update with new one
            ParentBCRESNode.Render.InsertModel(h3d, modelIndex);

            ReloadModel();

            GenerateMaterialAnimGroups();
        }

        public void ImportMaterial()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.FileName = $"{Header}";
            dlg.AddFilter(".json", "json");
            if (dlg.ShowDialog())
            {
                int modelIndex = BcresFile.Models.Find(Model.Name);

                var material = JsonConvert.DeserializeObject<H3DMaterial>(File.ReadAllText(dlg.FilePath));
                List<string> nameList = Model.Materials.Select(x => x.Name).ToList();

                var mat = GfxMaterial.CreateDefault();
                mat.ConvertH3D(material);
                mat.Name = Utils.RenameDuplicateString(material.Name, nameList);

                Model.Materials.Add(mat);

                var h3d = Model.ToH3D();
                //Get current render, remove then update with new one
                ParentBCRESNode.Render.InsertModel(h3d, modelIndex);

                ReloadModel();

                GenerateMaterialAnimGroups();
            }
        }
    }

    public class FSKL : STSkeleton
    {
        private NodeBase FolderUI;

        private SkeletonRenderer Renderer;

        private GfxSkeleton GfxSkeleton;

        public FSKL(NodeBase folder, GfxSkeleton skeleton)
        {
            GfxSkeleton = skeleton;
            FolderUI = folder;
            FolderUI.ContextMenus.Add(new MenuItemModel("Add Bone", () =>
            {
                AddNewBoneAction(FolderUI, null);
            }));

            foreach (var bone in skeleton.Bones)
            {
                STBone bn = new BcresBone(skeleton, this)
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
                if (bone.Parent != null)
                    bn.ParentIndex = skeleton.Bones.Find(bone.Parent.Name);
            }
            this.Reset();
            this.Update();
        }

        public void InitRender(SkeletonRenderer renderer)
        {
            Renderer = renderer;
            Renderer.CanSelect = true;
            foreach (var bone in Renderer.Bones)
                PrepareBoneUI(bone.UINode, bone.BoneData);
        }

        private void PrepareBoneUI(NodeBase node, STBone bone)
        {
            node.ContextMenus.Clear();
            node.ContextMenus.Add(new MenuItemModel("Rename", () => { node.ActivateRename = true; }));
            node.ContextMenus.Add(new MenuItemModel(""));
            node.ContextMenus.Add(new MenuItemModel("Add", () =>
            {
                AddNewBoneAction(node, bone);
            }));
            node.ContextMenus.Add(new MenuItemModel("Remove", () =>
            {
                RemoveBoneAction(node, bone);
            }));

            node.CanRename = true;
            node.OnHeaderRenamed += delegate
            {
                //Rename wrapper
                bone.Name = node.Header;
                //Rename raw bfres bone data
                ((BcresBone)bone).BoneData.Name = node.Header;
            };
        }

        private void AddNewBoneAction(NodeBase parentNode, STBone parent)
        {
            var nameList = this.Bones.Select(x => x.Name).ToList();
            string name = Utils.RenameDuplicateString("NewBone", nameList);

            var position = new System.Numerics.Vector3();

            var genericBone = this.AddBone(new GfxBone()
            {
                Name = name,
                Translation = position,
                Scale = new System.Numerics.Vector3(1, 1, 1),
                ParentIndex = parent != null ? (short)parent.Index : (short)-1,
                Flags = GfxBoneFlags.IsNeededRendering | 
                        GfxBoneFlags.IsWorldMtxCalculate |
                        GfxBoneFlags.IsLocalMtxCalculate |
                        GfxBoneFlags.HasSkinningMtx,
            });
            var render = AddBoneRender(genericBone);
            PrepareBoneUI(render.UINode, genericBone);

            if (parent == null)
                FolderUI.AddChild(render.UINode);
            else
                parentNode.AddChild(render.UINode);
        }

        //Todo these will be ported to glframework when library is updated to latest
        public BoneRender AddBoneRender(STBone bone)
        {
            var render = new BoneRender(bone);
            Renderer.Bones.Add(render);

            var parent = Renderer.Bones.FirstOrDefault(x => x.BoneData == bone.Parent);
            render.SetParent(parent);

            return render;
        }

        public void RemoveBoneRender(STBone bone)
        {
            var render = Renderer.Bones.FirstOrDefault(x => x.BoneData == bone);
            if (render != null)
            {
                Renderer.Bones.Remove(render);
            }
        }

        private void RemoveBoneAction(NodeBase node, STBone removedBone)
        {
            List<STBone> bonesToRemove = GetAllChildren(removedBone);
            bonesToRemove.Add(removedBone);
            if (this.Bones.Count == bonesToRemove.Count)
            {
                TinyFileDialog.MessageBoxErrorOk($"Atleast 1 bone is needed to be present!");
                return;
            }

            var result = TinyFileDialog.MessageBoxInfoYesNo(
                string.Format("Are you sure you want to remove {0}? This cannot be undone!", removedBone.Name));

            if (result != 1)
                return;

            //Remove from gui
            if (node.Parent != null)
                node.Parent.Children.Remove(node);

            foreach (var bone in bonesToRemove)
            {
                //Remove from parent
                var parent = bone.Parent;
                if (parent != null)
                    parent.Children.Remove(parent);

                //Remove from bone render
                RemoveBoneRender(bone);

                //Remove from generic skeleton
                this.Bones.Remove(bone);

                //Remove from bcres skeleton data
                if (GfxSkeleton.Bones.Contains(bone.Name))
                    GfxSkeleton.Bones.Remove(GfxSkeleton.Bones[bone.Name]);
            }
        }

        private BcresBone AddBone(GfxBone bone)
        {
            BcresBone genericBone = new BcresBone(GfxSkeleton, this)
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

            Bones.Add(genericBone);

            this.Reset();

            return genericBone;
        }

        private List<STBone> GetAllChildren(STBone bone)
        {
            List<STBone> bones = new List<STBone>();
            foreach (var child in bone.Children)
                bones.AddRange(GetAllChildren(child));
            return bones;
        }
    }

    class BcresBone : STBone, IPropertyUI
    {
        public GfxBone BoneData { get; set; }

        GfxSkeleton GfxSkeleton { get; set; }

        public Type GetTypeUI() => typeof(BcresBoneUI);

        public void OnLoadUI(object uiInstance) {
            ((BcresBoneUI)uiInstance).Init(this, BoneData);
        }

        public void OnRenderUI(object uiInstance) {
            ((BcresBoneUI)uiInstance).Render();
        }

        public BcresBone(GfxSkeleton gfxSkeleton, STSkeleton skeleton) : base(skeleton)
        {
            GfxSkeleton = gfxSkeleton;
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

            foreach (var bone in GfxSkeleton.Bones)
            {
                bone.UpdateMatrices();
            }

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
            GfxBoneFlags flags = BoneData.Flags;

            //Reset transform flags
            flags &= ~GfxBoneFlags.IsTranslationZero;
            flags &= ~GfxBoneFlags.IsScaleVolumeOne;
            flags &= ~GfxBoneFlags.IsRotationZero;
            flags &= ~GfxBoneFlags.IsScaleUniform;

            //SRT checks to update matrices
            if (this.Position == OpenTK.Vector3.Zero)
                flags |= GfxBoneFlags.IsTranslationZero;
            if (this.Scale == OpenTK.Vector3.One)
                flags |= GfxBoneFlags.IsScaleVolumeOne;
            if (this.Rotation == OpenTK.Quaternion.Identity)
                flags |= GfxBoneFlags.IsRotationZero;
            //Extra scale flags
            if (this.Scale.X == this.Scale.Y && this.Scale.X == this.Scale.Z)
                flags |= GfxBoneFlags.IsScaleUniform;

            BoneData.Flags = flags;
        }
    }

    public class SOBJ : NodeBase, MapStudio.UI.IPropertyUI
    {
        public GfxModel Model { get; set; }
        public GfxMaterial Material { get; set; }
        public GfxMesh Mesh { get; set; }

        public Type GetTypeUI() => typeof(BcresMeshUI);

        public void OnLoadUI(object uiInstance)
        {
            ((BcresMeshUI)uiInstance).Init(this, Model, Mesh);
        }

        public void OnRenderUI(object uiInstance)
        {
            ((BcresMeshUI)uiInstance).Render();
        }

        public SOBJ(GfxModel model, GfxMesh mesh)
        {
            Model = model;
            Mesh = mesh;
            Material = model.Materials[(int)mesh.MaterialIndex];
            Tag = Mesh;
            if (Mesh.H3DMesh != null)
                Mesh.H3DMesh.IsVisible = mesh.IsVisible;

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

            Icon = IconManager.MESH_ICON.ToString();
            this.Header = string.IsNullOrEmpty(mesh.Name) ? mesh.MeshNodeName : mesh.Name;
            if (string.IsNullOrEmpty(this.Header))
                this.Header = $"Mesh{model.Meshes.IndexOf(mesh)}";

            this.OnSelected += delegate
            {
                if (Mesh.H3DMesh != null)
                    Mesh.H3DMesh.IsSelected =  this.IsSelected;
            };


            HasCheckBox = true;
            OnChecked += delegate
            {
                if (Mesh.H3DMesh != null)
                    Mesh.H3DMesh.IsVisible = this.IsChecked;
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
            var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();
            var meshes = selected.Select(x => ((SOBJ)x).Mesh.H3DMesh).ToArray();

            PicaVertexEditor.Start(meshes);
            action();

            for (int i = 0; i < meshes.Length; i++)
            {
                var meshNode = selected[i] as SOBJ;

                var vertices = PicaVertexEditor.End(meshes[i], i);
                var stride = VerticesConverter.CalculateStride(meshes[i].Attributes);
                var rawBuffer = VerticesConverter.GetBuffer(vertices, meshes[i].Attributes, stride);
                meshes[i].RawBuffer = rawBuffer;

                foreach (var vertexBuffer in Model.Shapes[meshNode.Mesh.ShapeIndex].VertexBuffers)
                {
                    if (vertexBuffer is GfxVertexBufferInterleaved)
                  {
                        var vbo = vertexBuffer as GfxVertexBufferInterleaved;
                        vbo.VertexStride = stride;
                        vbo.RawBuffer = rawBuffer;
                    }
                }
            }

            var cmdl = this.Parent.Parent as CMDL;
            cmdl.ReloadRender();
        }
    }

    public class MTOB : MaterialWrapper, MapStudio.UI.IPropertyUI
    {
        GfxModel GfxModel;
        public GfxMaterial GfxMaterial;

        public MTOB(H3DRender render, GfxModel model, GfxMaterial material) : base(render, model.H3DModel, material.H3DMaterial)
        {
            GfxMaterial = material;
            GfxModel = model;
            this.OnHeaderRenamed += delegate
            {
                GfxMaterial.Name = this.Header;
            };
        }

        public override void DeleteBatch()
        {
            var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();

            if (GfxModel.Materials.Count == 1 || selected.Count == GfxModel.Materials.Count)
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
            Render.InsertModel(GfxModel.H3DModel, 0);
        }

        public override void Delete()
        {
            //List out all references of the material
            List<string> matMeshReferences = new List<string>();
            foreach (var mesh in GfxModel.Meshes)
                matMeshReferences.Add(GfxModel.Materials[mesh.MaterialIndex].Name);

            //Remove the material
            GfxModel.Materials.Remove(GfxMaterial);
            GfxModel.H3DModel.Materials.Remove(GfxMaterial.Name);

            //Update references
            for (int i = 0; i < GfxModel.Meshes.Count; i++)
            {
                var index = GfxModel.Materials.Find(matMeshReferences[i]);
                if (index == -1) //Must have a material. Choose first one by default
                    index = 0;

                GfxModel.Meshes[i].MaterialIndex = (ushort)index;
                GfxModel.H3DModel.Meshes[i].MaterialIndex =  (ushort)index;
            }

            //Remove from UI
            var parent = Parent;
            parent.Children.Remove(this);

            var modelNode =  parent.Parent as CMDL;
            modelNode.GenerateMaterialAnimGroups();

            if (IconManager.HasIcon(Icon))
                IconManager.RemoveTextureIcon(Icon);

            GLContext.ActiveContext.UpdateViewport = true;
        }
    }

    class BoneWrapper
    {

    }
}
