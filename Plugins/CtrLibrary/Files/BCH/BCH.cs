using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using MapStudio.UI;
using SPICA.Rendering;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrGfx;
using UIFramework;
using System.Numerics;
using Toolbox.Core.ViewModels;
using Newtonsoft.Json;
using CtrLibrary.Rendering;
using GLFrameworkEngine;
using CtrLibrary.UI;
using SPICA.Formats.ModelBinary;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Shader;
using SPICA.Formats.CtrH3D.Scene;
using SPICA.Formats.CtrH3D.Light;
using SPICA.Formats.CtrH3D.Fog;
using SPICA.Formats.CtrH3D.LUT;
using SPICA.Formats.Common;
using static System.Collections.Specialized.BitVector32;
using System.Runtime.ConstrainedExecution;
using static CtrLibrary.Bch.BCH;
using SPICA.Formats.CtrGfx.Animation;
using static MapStudio.UI.AnimationTree;
using SPICA.Formats.CtrH3D.Camera;
using Discord;
using IONET.Collada.FX.Rendering;
using System.Transactions;

namespace CtrLibrary.Bch
{
    /// <summary>
    /// Represents a plugin for loading/editing/saving BCH binary files.
    /// </summary>
    public class BCH : FileEditor, IFileFormat, IPropertyUI, IDisposable
    {
        /// <summary>
        /// The description of the file extension of the plugin.
        /// </summary>
        public string[] Description => new string[] { "Bch" };

        /// <summary>
        /// The extension of the plugin.
        /// </summary>
        public string[] Extension => new string[] { "*.bch" };

        /// <summary>
        /// Determines if the plugin can save or not.
        /// </summary>
        public bool CanSave { get; set; } = true;

        /// <summary>
        /// File info of the loaded file format.
        /// </summary>
        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(3, "BCH");
            }
        }

        //Draw header UI for BCH file data to edit version info
        //Activated by IPropertyUI

        #region IPropertyUI

        public Type GetTypeUI() => typeof(HeaderUI);

        public void OnLoadUI(object uiInstance)
        {
            ((HeaderUI)uiInstance).Init(H3DData);
        }

        public void OnRenderUI(object uiInstance)
        {
            ((HeaderUI)uiInstance).Render();
        }

        #endregion

        /// <summary>
        /// Creates a new bcres instance for the new file menu UI.
        /// Returns false if not supported.
        /// </summary>
        /// <returns></returns>
        public override bool CreateNew()
        {
            FileInfo = new File_Info();
            FileInfo.FilePath = "NewFile";
            FileInfo.FileName = "NewFile";

            H3D h3d = new H3D();
            Load(h3d);

            this.Root.Header = "NewFile.bch";
            this.Root.Tag = this;

            return true;
        }

        /// <summary>
        /// The render instance used to display the model in 3D view.
        /// </summary>
        public H3DRender Render;

        /// <summary>
        /// The file instance for handling bch data.
        /// </summary>
        public H3D H3DData;

        //Shader window for debugging and viewing how shader code is generated
        ShaderWindow ShaderWindow;

        //Optional .mbn used by smash 3ds
        private MBn ModelBinary;

        //Folder for model data
        private ModelFolder<H3DModel> ModelFolder;

        //Folder for texture data
        private TextureFolder<H3DTexture> TextureFolder;

        public override bool DisplayViewport => ModelFolder.Children.Count > 0;

        public BCH() { }

        public BCH(H3D h3D, string name)
        {
            this.FileInfo = new File_Info() { FileName = name };
            Load(h3D);
        }

        public void Load(Stream stream)
        {
            Load(H3D.Open(new MemoryStream(stream.ToArray())));
        }

        public void Load(H3D h3d)
        {
            H3DData = h3d;

            Root.TagUI.Tag = h3d;

            //Check for optional .mbn file binary which is used in Smash 3DS to store mesh buffers
            if (!string.IsNullOrEmpty(FileInfo.FilePath))
            {
                if (FileInfo.FilePath.EndsWith(".bch") && File.Exists(FileInfo.FilePath.Replace(".bch", ".mbn")))
                {
                    ModelBinary = new MBn(FileInfo.FilePath.Replace(".bch", ".mbn"), H3DData);
                    H3DData = ModelBinary.ToH3D();
                }
            }
            //Create a renderer and add to the editor
            Render = new H3DRender(H3DData, null);
            AddRender(Render);

            //Display for optional shader window, switch by selected material
            this.Workspace.Outliner.SelectionChanged += delegate
            {
                var node = this.Workspace.Outliner.SelectedNode;
                if (ShaderWindow != null && node is MaterialWrapper) //material tree node
                {
                    var mat = ((MaterialWrapper)node).Material;
                    ShaderWindow.Material = mat;
                }
            };

            //Prepare the global scene lighting to configure in the viewer
            var light = Render.Renderer.Lights[0];

            //Smash 3DS lighting setup
            if (ModelBinary != null)
            {
                light.Directional = true;
                light.TwoSidedDiffuse = false;

                Renderer.GlobalHsLGCol = new Vector3(1, 1, 1);
                Renderer.GlobalHsLSCol = new Vector3(1, 1, 1);

                light.Direction = new OpenTK.Vector3(-0.681f, -0.096f, -3.139f);
                light.Position = light.Direction;

                light.Diffuse = new OpenTK.Graphics.Color4(1, 1, 1, 1f);
                light.Ambient = new OpenTK.Graphics.Color4(1, 1, 1, 1f);

                light.Specular0 = new OpenTK.Graphics.Color4(0.27f, 0.27f, 0.27f, 1f);
                light.Specular1 = new OpenTK.Graphics.Color4(0.23f, 0.23f, 0.23f, 1f);

                Render.Renderer.UpdateAllUniforms();
            }

            // AddRender(new SceneLightingUI.LightPreview(light));
            foreach (var lightNode in SceneLightingUI.Setup(Render, Render.Renderer.Lights))
            {
                Root.AddChild(lightNode);
                //only load one scene light for global usage
                break;
            }

            //Prepare tree nodes to visualize in the gui
            ModelFolder = new ModelFolder<H3DModel>(this, H3DData, H3DData.Models);
            TextureFolder = new TextureFolder<H3DTexture>(Render, H3DData.Textures);

            Root.AddChild(ModelFolder);
            Root.AddChild(TextureFolder);
            Root.AddChild(new LUTFolder<H3DLUT>(Render, H3DData.LUTs));

            AddNodeGroup(H3DData.Shaders, H3DGroupType.Shaders);
            AddNodeGroup(H3DData.Cameras, H3DGroupType.Cameras);
            AddNodeGroup(H3DData.Lights, H3DGroupType.Lights);
            AddNodeGroup(H3DData.Fogs, H3DGroupType.Fogs);
            AddNodeGroup(H3DData.Scenes, H3DGroupType.Scenes);
            AddNodeGroup(H3DData.SkeletalAnimations, H3DGroupType.SkeletalAnim);
            AddNodeGroup(H3DData.MaterialAnimations, H3DGroupType.MaterialAnim);
            AddNodeGroup(H3DData.VisibilityAnimations, H3DGroupType.VisibiltyAnim);
            AddNodeGroup(H3DData.CameraAnimations, H3DGroupType.CameraAnim);
            AddNodeGroup(H3DData.LightAnimations, H3DGroupType.LightAnim);
            AddNodeGroup(H3DData.FogAnimations, H3DGroupType.FogAnim);
        }

     /*   public void FrameCamera()
        {
            if (Render.Renderer.Models.Count == 0)
                return;

            var AABB = Render.Renderer.Models[0].GetModelAABB();
            var MdlCenter = AABB.Center;

            float Dimension = 1;

            Dimension = Math.Max(Dimension, Math.Abs(AABB.Size.X));
            Dimension = Math.Max(Dimension, Math.Abs(AABB.Size.Y));
            Dimension = Math.Max(Dimension, Math.Abs(AABB.Size.Z));
            Dimension *= 2;

            var Translation = new OpenTK.Vector3(0, 0, Dimension);
            GLContext.ActiveContext.Camera.SetPosition(MdlCenter + Translation);
            GLContext.ActiveContext.Camera.RotationX = 0;
            GLContext.ActiveContext.Camera.RotationY = 0;
            GLContext.ActiveContext.Camera.RotationZ = 0;

            GLContext.ActiveContext.Camera.UpdateMatrices();
        }*/


        /// <summary>
        /// Imports a texture into the file with a given path.
        /// The texture is updated to the UI folder directly.
        /// </summary>
        public void ImportTexture(string filePath)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            if (TextureFolder.Children.Any(x => x.Header == name))
                return;

            TextureFolder.ImportTextureDirect(filePath);
        }

        /// <summary>
        /// Saves the binary file and the editor contents to a stream.
        /// </summary>
        public void Save(Stream stream)
        {
            if (ModelBinary != null)
                SaveMbn();

            foreach (CMDL<H3DModel> model in ModelFolder.Children)
                model.OnSave();

            H3DData.Textures = TextureFolder.GetTextures();

            foreach (var folder in this.Root.Children)
            {
                foreach (var c in folder.Children)
                {
                    if (c is AnimationNode<H3DAnimation>)
                        ((AnimationNode<H3DAnimation>)c).OnSave();
                    else if (c is AnimationNode<H3DMaterialAnim>)
                        ((AnimationNode<H3DMaterialAnim>)c).OnSave();
                    else if (c is AnimationNode<H3DCameraAnim>)
                        ((AnimationNode<H3DCameraAnim>)c).OnSave();
                    else if (c is AnimationNode<H3DLightAnim>)
                        ((AnimationNode<H3DLightAnim>)c).OnSave();
                }
            }

            H3D.Save(stream, H3DData);

            //Reload raw data from binary if needed
            if (ModelBinary != null)
                ModelBinary.ToH3D();
        }

        private void SaveMbn()
        {
            if (H3DData.Models.Count == 0)
                return;

            ModelBinary.FromH3D(H3DData.Models[0]);
            ModelBinary.Save($"{FileInfo.FilePath.Replace(".bch", ".mbn")}");
            //Blank out data as mbn stores the vertex/index data
            foreach (var mesh in H3DData.Models[0].Meshes)
            {
                mesh.RawBuffer = new byte[0];
                foreach (var sm in mesh.SubMeshes)
                    sm.Indices = new ushort[0];
            }
        }

        /// <summary>
        /// Disposes the render data during a workspace close.
        /// </summary>
        public void Dispose() {
            Render.Dispose();
        }

        /// <summary>
        /// Prepares the dock layouts to be used for the file format.
        /// </summary>
        public override List<DockWindow> PrepareDocks()
        {
            if (ShaderWindow == null)
            {
                ShaderWindow = new ShaderWindow(this.Workspace);
                ShaderWindow.DockDirection = ImGuiNET.ImGuiDir.Down;
            }

            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            windows.Add(Workspace.ConsoleWindow);
            windows.Add(Workspace.ViewportWindow);
            windows.Add(Workspace.TimelineWindow);
            windows.Add(Workspace.GraphWindow);
            windows.Add(ShaderWindow);
            return windows;
        }


        private void AddNodeGroup<T>(H3DDict<T> subSections, H3DGroupType type)
   where T : SPICA.Formats.Common.INamed
        {
            var folder = new H3DGroupNode<T>(type, subSections);

            foreach (var item in subSections)
            {
                if (typeof(T) == typeof(H3DShader))
                    folder.AddChild(new ShaderNode<T>(subSections, item));
                else if (item is H3DAnimation)
                    folder.AddChild(new AnimationNode<T>(subSections, item));
                else if (item is H3DFog)
                    folder.AddChild(new FogNode<T>(subSections, item));
                else if (item is H3DLight)
                    folder.AddChild(new LightNode<T>(subSections, item));
                else if (item is H3DCamera)
                    folder.AddChild(new CameraNode<T>(subSections, item));
                else if (item is H3DScene)
                    folder.AddChild(new SceneNode<T>(subSections, item));
                else
                    folder.AddChild(new NodeSection<T>(subSections, item));
            }

            if (folder.Children.Count > 0)
                Root.AddChild(folder);

            var addMenu = Root.ContextMenus.FirstOrDefault(x => x.Header == "Add Folder");
            if (addMenu == null)
            {
                addMenu = new MenuItemModel($"Add Folder", () =>
                {
                    if (!Root.Children.Contains(folder))
                        Root.AddChild(folder);
                });
                Root.ContextMenus.Add(addMenu);
            }
            addMenu.MenuItems.Add(new MenuItemModel($"{type}", () =>
            {
                if (!Root.Children.Contains(folder))
                    Root.AddChild(folder);
            }));
        }

        public enum H3DGroupType
        {
            Models,
            Textures,
            Lookups,
            Materials,
            Shaders,
            Cameras,
            Lights,
            Fogs,
            Scenes,
            SkeletalAnim,
            MaterialAnim,
            VisibiltyAnim,
            CameraAnim,
            LightAnim,
            FogAnim,
            Particles,
        }

        public class H3DGroupNode<T> : NodeBase where T : SPICA.Formats.Common.INamed
        {
            //Folder group type
            public H3DGroupType Type;

            //Raw section list to load/save
            internal H3DDict<T> SectionList;

            //Tree node type to add when creating new UI nodes
            public virtual Type ChildNodeType => typeof(NodeSection<T>);

            public H3DGroupNode(H3DGroupType type, H3DDict<T> subSections)
            {
                Type = type;
                Header = GetName();
                this.ContextMenus.Add(new MenuItemModel("Add", Add));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Import", Import));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Export All", ExportAll));
                this.ContextMenus.Add(new MenuItemModel("Replace All", ReplaceAll));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Clear", Clear));
                SectionList = subSections;
            }

            public virtual void Add()
            {
                //Create section instance
                var item = Activator.CreateInstance(typeof(T)) as SPICA.Formats.Common.INamed;
                //Default name
                item.Name = $"New{this.Type}";
                //Auto rename possible dupes
                item.Name = Utils.RenameDuplicateString(item.Name, SectionList.Select(x => x.Name).ToList());
                AddNewSection(item);
            }

            public virtual void Import()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.FileName = $"{Header}.json";
                dlg.MultiSelect = true;
                dlg.AddFilter("json", "json");

                if (dlg.ShowDialog())
                {
                    foreach (var f in dlg.FilePaths)
                    {
                        //Create section instance
                        var item = (T)Activator.CreateInstance(typeof(T));
                        item.Name = Path.GetFileNameWithoutExtension(f);
                        //Auto rename possible dupes
                        item.Name = Utils.RenameDuplicateString(item.Name, SectionList.Select(x => x.Name).ToList());
                        //Add section list
                        AddNewSection(item);
                    }
                }
            }

            private void AddNewSection(INamed item)
            {
                //Add section list
                SectionList.Add((T)item);
                //Add to UI
                if (item is H3DAnimation)
                {
                    switch (this.Type)
                    {
                        case H3DGroupType.MaterialAnim:
                            ((H3DAnimation)item).AnimationType = H3DAnimationType.Material;
                            break;
                        case H3DGroupType.VisibiltyAnim:
                            ((H3DAnimation)item).AnimationType = H3DAnimationType.Visibility;
                            break;
                        case H3DGroupType.CameraAnim:
                            ((H3DAnimation)item).AnimationType = H3DAnimationType.Camera;
                            break;
                        case H3DGroupType.LightAnim:
                            ((H3DAnimation)item).AnimationType = H3DAnimationType.Light;
                            break;
                        case H3DGroupType.Fogs:
                            ((H3DAnimation)item).AnimationType = H3DAnimationType.Fog;
                            break;
                    }

                    var node = new AnimationNode<T>(SectionList, item);
                    AddChild(node);
                }
                else if (item is H3DFog)
                    AddChild(new FogNode<T>(SectionList, item));
                else if (item is H3DLight)
                    AddChild(new LightNode<T>(SectionList, item));
                else if (item is H3DCamera)
                    AddChild(new CameraNode<T>(SectionList, item));
                else if (item is H3DScene)
                    AddChild(new SceneNode<T>(SectionList, item));
                else
                {
                    var node = (NodeSection<T>)Activator.CreateInstance(ChildNodeType, SectionList, item);
                    AddChild(node);
                }
            }

            public virtual void ExportAll()
            {
                ImguiFolderDialog dlg = new ImguiFolderDialog();
                if (dlg.ShowDialog())
                {
                    foreach (NodeSection<T> node in this.Children)
                        node.Export(Path.Combine(dlg.SelectedPath, $"{node.Header}.json"));
                }
            }

            public virtual void ReplaceAll()
            {
                ImguiFolderDialog dlg = new ImguiFolderDialog();
                if (dlg.ShowDialog())
                {
                    foreach (var file in Directory.GetFiles(dlg.SelectedPath))
                    {
                        foreach (NodeSection<T> node in this.Children)
                        {
                            if (node.Header == Path.GetFileNameWithoutExtension(file))
                                node.Replace(file);
                        }
                    }
                }
            }

            public virtual void Clear()
            {
                string msg = $"Are you sure you want to clear ({this.Type})? This cannot be undone!";

                int result = TinyFileDialog.MessageBoxInfoYesNo(msg);
                if (result != 1)
                    return;

                SectionList.Clear();
                this.Children.Clear();
            }

            //Get folder name via type
            private string GetName()
            {
                switch (Type)
                {
                    case H3DGroupType.Models: return "Models";
                    case H3DGroupType.Textures: return "Textures";
                    case H3DGroupType.Lookups: return "Lookups";
                    case H3DGroupType.Materials: return "Materials";
                    case H3DGroupType.Shaders: return "Shaders";
                    case H3DGroupType.Cameras: return "Cameras";
                    case H3DGroupType.Lights: return "Lights";
                    case H3DGroupType.Fogs: return "Fogs";
                    case H3DGroupType.Scenes: return "Scenes";
                    case H3DGroupType.SkeletalAnim: return "Skeletal Animations";
                    case H3DGroupType.MaterialAnim: return "Material Animations";
                    case H3DGroupType.VisibiltyAnim: return "Visibilty Animations";
                    case H3DGroupType.CameraAnim: return "Camera Animations";
                    case H3DGroupType.LightAnim: return "Light Animations";
                    case H3DGroupType.FogAnim: return "Fog Animations";
                    case H3DGroupType.Particles: return "Particles";
                    default:
                        throw new System.Exception("Unknown type? " + Type);
                }
            }
        }

        class AnimationNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            public override string DefaultExtension => ".json";
            public override string[] ExportFilters => new string[] { ".bch", ".json", ".anim" };
            public override string[] ReplaceFilters => new string[] { ".bch", ".json", ".gltf", ".glb", ".dae", ".anim" };

            public AnimationNode(H3DDict<T> subSections, object section) : base(subSections, section)
            {
                //Create an animation wrapper for animation playback if node is an animation type
                var wrapper = new AnimationWrapper((H3DAnimation)section);
                Tag = wrapper;
                this.OnHeaderRenamed += delegate
                {
                    wrapper.Root.Header = this.Header;
                };
                wrapper.Root.OnHeaderRenamed += delegate
                {
                    this.Header = wrapper.Root.Header;
                };

                BchAnimPropertyUI propertyUI = new BchAnimPropertyUI();
                this.TagUI.UIDrawer += delegate
                {
                    propertyUI.Render(wrapper, null);
                };

                this.OnSelected += delegate
                {
                    //Check if the current node selected was an animation and apply playback
                    if (Tag is AnimationWrapper)
                        ((AnimationWrapper)Tag).AnimationSet();
                };
            }

            public override void Export(string filePath)
            {
                OnSave();

                string ext = Path.GetExtension(filePath.ToLower());
                switch (ext)
                {
                    case ".json":
                        File.WriteAllText(filePath, JsonConvert.SerializeObject(Section, Formatting.Indented));
                        break;
                    case ".bch":
                         var type = ((H3DGroupNode<T>)this.Parent).Type;
                        ExportRaw(filePath, Section, type);
                        break;
                    case ".anim":
                    case ".dae":
                    case ".glb":
                    case ".gltf":
                        BchSkelAnimationImporter.Export(((H3DAnimation)Section), GetModel(), filePath);
                        break;
                    default:
                        throw new Exception($"Unsupported file extension {ext}!"); 
                }
            }

            public override void Replace(string filePath)
            {
                //Replace as raw binary or json text formats
                string ext = Path.GetExtension(filePath.ToLower());
                switch (ext)
                {
                    case ".json":
                        base.Replace(filePath);
                        break;
                    case ".bch":
                        base.Replace(filePath);
                        break;
                    case ".anim":
                    case ".dae":
                    case ".glb":
                    case ".gltf":
                        BchSkelAnimationImporter.Import(filePath, ((H3DAnimation)Section), GetModel());
                        ReloadName();
                        break;
                    default:
                        throw new Exception($"Unsupported file extension {ext}!");
                }
                ((AnimationWrapper)Tag).Reload((H3DAnimation)Section);
                ((AnimationWrapper)Tag).AnimationSet();
            }

            private H3DModel GetModel()
            {
                return H3DRender.GetFirstVisibleModel();
            }

            public override void OnSave()
            {
                //check for possible edits
                bool isEdited = ((AnimationWrapper)Tag).IsEdited();
                //Convert the gui to H3D animation
                if (isEdited)
                    ((AnimationWrapper)Tag).ToH3D((H3DAnimation)Section);
                else
                {
                    //only transfer loop and frame count property
                    if (((AnimationWrapper)Tag).Loop)
                        ((H3DAnimation)Section).AnimationFlags |= H3DAnimationFlags.IsLooping;
                    else
                        ((H3DAnimation)Section).AnimationFlags &= H3DAnimationFlags.IsLooping;

                    ((H3DAnimation)Section).FramesCount = ((AnimationWrapper)Tag).FrameCount;
                }
                //Apply any wrapper data on save
                ((AnimationWrapper)Tag).OnSave();
            }
        }

        class ShaderNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            H3DShader Shader => (H3DShader)Section;

            ShaderUI ShaderUI;

            public ShaderNode(H3DDict<T> subSections, object section) : base(subSections, section)
            {
                ShaderUI = new ShaderUI(Shader.ToBinary(), Shader.VtxShaderIndex, Shader.GeoShaderIndex);
                this.TagUI.UIDrawer += delegate
                {
                    ShaderUI.Render();
                };
            }
        }

        class FogNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            H3DFog Fog => (H3DFog)Section;

            public FogNode(H3DDict<T> subSections, object section) : base(subSections, section)
            {
                BchFogUI fogUI = new BchFogUI();
                fogUI.Init(Fog);

                this.TagUI.UIDrawer += delegate
                {
                    fogUI.Render();
                };
            }
        }

        class LightNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            H3DLight Light => (H3DLight)Section;

            public LightNode(H3DDict<T> subSections, object section) : base(subSections, section)
            {
                BchLightUI lightUI = new BchLightUI();
                lightUI.Init(Light);

                this.TagUI.UIDrawer += delegate
                {
                    lightUI.Render();
                };
            }
        }

        class CameraNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            H3DCamera Camera => (H3DCamera)Section;

            //public CameraHandler CameraDisplay;

            public CameraNode(H3DDict<T> subSections, object section) : base(subSections, section)
            {
                BchCameraUI cameraUI = new BchCameraUI();
                cameraUI.Init(Camera);

                //Create a camera handle instance for previewing
            //    CameraDisplay = new CameraHandler(Camera);

                this.TagUI.UIDrawer += delegate
                {
                    cameraUI.Render();
                };
                this.OnSelected += delegate
                {
                  //  CameraDisplay.Activate();
                };
            }
        }

        class SceneNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            H3DScene Scene => (H3DScene)Section;

            public SceneNode(H3DDict<T> subSections, object section) : base(subSections, section)
            {
                BchSceneUI sceneUI = new BchSceneUI();
                sceneUI.Init(Scene);

                this.TagUI.UIDrawer += delegate
                {
                    sceneUI.Render();
                };
            }
        }

        public class NodeSection<T> : NodeBase where T : SPICA.Formats.Common.INamed
        {
            internal object Section;
            private H3DDict<T> Dict;

            public virtual string DefaultExtension => ".json";

            public virtual string[] ExportFilters => new string[] { ".bch", ".json" };
            public virtual string[] ReplaceFilters => new string[] { ".bch", ".json" };

            public virtual MenuItemModel[] ExtraMenuItems => new MenuItemModel[0];

            public NodeSection(H3DDict<T> subSections,  object section)
            {
                Header = ((T)section).Name;
                Section = section;
                Dict = subSections;
                CanRename = true;
                Icon = IconManager.FILE_ICON.ToString();
                Tag = section;

                this.ContextMenus.Add(new MenuItemModel("Export", ExportDialog));
                this.ContextMenus.Add(new MenuItemModel("Replace", ReplaceDialog));
                this.ContextMenus.Add(new MenuItemModel(""));
                if (ExtraMenuItems.Length > 0) 
                {
                    this.ContextMenus.AddRange(ExtraMenuItems);
                    this.ContextMenus.Add(new MenuItemModel(""));
                }
                this.ContextMenus.Add(new MenuItemModel("Rename", () => { ActivateRename = true; }));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Delete", () => Delete()));

                this.OnHeaderRenamed += delegate
                {
                    //Update binary name on tree node rename
                    ReloadName();
                };
            }

            public virtual void OnSave()
            {

            }

            public virtual bool Delete()
            {
                var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();

                string msg = $"Are you sure you want to delete the ({selected.Count}) selected textures? This cannot be undone!";
                if (selected.Count == 1)
                    msg = $"Are you sure you want to delete {Header}? This cannot be undone!";

                int result = TinyFileDialog.MessageBoxInfoYesNo(msg);
                if (result != 1)
                    return false;

                foreach (NodeSection<T> node in selected)
                {
                    //Remove from section
                    Dict.Remove((T)node.Section);
                    //Remove from UI
                    this.Parent.Children.Remove(node);
                }
                return true;
            }

            void ReplaceDialog()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.FileName = $"{Header}{DefaultExtension}";

                foreach (var ext in ReplaceFilters)
                    dlg.AddFilter(ext, ext);

                if (dlg.ShowDialog())
                {
                    Replace(dlg.FilePath);
                }
            }

            public virtual void Replace(string filePath)
            {
                //Replace as raw binary or json text formats
                if (filePath.ToLower().EndsWith(".bch") || filePath.ToLower().EndsWith(".bmdl"))
                {
                    var type = ((H3DGroupNode<T>)this.Parent).Type;
                    Dict[this.Header] = (T)ReplaceRaw(filePath, type);
                }
                else
                {
                    Section = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                    Dict[this.Header] = (T)Section;
                }
                ReloadName();
            }

            void ExportDialog()
            {
                var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();
                //Batch export
                if (selected.Count > 1)
                {
                    ImguiFolderDialog dlg = new ImguiFolderDialog();
                    if (dlg.ShowDialog())
                    {
                        foreach (NodeSection<T> node in selected)
                        {
                            node.Export(Path.Combine(dlg.SelectedPath, $"{node.Header}{DefaultExtension}"));
                        }
                    }
                }
                else //Normal export
                {
                    ImguiFileDialog dlg = new ImguiFileDialog();
                    dlg.SaveDialog = true;
                    dlg.FileName = $"{Header}{DefaultExtension}";
                    foreach (var ext in ExportFilters)
                        dlg.AddFilter(ext, ext);

                    if (dlg.ShowDialog())
                    {
                        Export(dlg.FilePath);
                    }
                }
            }

            public virtual void Export(string filePath)
            {
                //Export as raw binary or json text formats
                if (filePath.ToLower().EndsWith(".bch"))
                {
                    var type = ((H3DGroupNode<T>)this.Parent).Type;
                    ExportRaw(filePath, Section, type);
                }
                else
                {
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(Section, Formatting.Indented));
                }
            }

            //Applies the current UI tree node name to the section used by the binary file.
            public virtual void ReloadName()
            {
                //check if name was changed or not
                if (((INamed)Section).Name == this.Header)
                    return;

                //update the lookup with name
                if (Dict.Contains(((INamed)Section).Name))
                    Dict.Remove(((INamed)Section).Name);

                ((INamed)Section).Name = this.Header;

                Dict.Add((T)Section);
            }
        }
        public static object ReplaceRaw(string filePath, H3DGroupType type)
        {
            object Section = null;

            H3D h3d = H3D.Open(File.ReadAllBytes(filePath));
            switch (type)
            {
                case H3DGroupType.Models: Section = h3d.Models[0]; break;
                case H3DGroupType.Textures: Section = h3d.Textures[0]; break;
                case H3DGroupType.SkeletalAnim: Section = h3d.SkeletalAnimations[0]; break;
                case H3DGroupType.MaterialAnim: Section = h3d.MaterialAnimations[0]; break;
                case H3DGroupType.Lookups: Section = h3d.LUTs[0]; break;
                case H3DGroupType.Lights: Section = h3d.Lights[0]; break;
                case H3DGroupType.Fogs: Section = h3d.Fogs[0]; break;
                case H3DGroupType.Scenes: Section = h3d.Scenes[0]; break;
                case H3DGroupType.Shaders: Section = h3d.Shaders[0]; break;
                case H3DGroupType.VisibiltyAnim: Section = h3d.VisibilityAnimations[0]; break;
                case H3DGroupType.CameraAnim: Section = h3d.CameraAnimations[0]; break;
                case H3DGroupType.LightAnim: Section = h3d.LightAnimations[0]; break;
                default:
                    throw new Exception($"Unsupported section! {type}");
            }
            return Section;
        }

        public static void ExportRaw(string filePath, object Section, H3DGroupType type)
        {
            H3D h3d = new H3D();
            switch (type)
            {
                case H3DGroupType.Models: h3d.Models.Add((H3DModel)Section); break;
                case H3DGroupType.Textures: h3d.Textures.Add((H3DTexture)Section); break;
                case H3DGroupType.SkeletalAnim: h3d.SkeletalAnimations.Add((H3DAnimation)Section); break;
                case H3DGroupType.MaterialAnim: h3d.MaterialAnimations.Add((H3DMaterialAnim)Section); break;
                case H3DGroupType.Lookups: h3d.LUTs.Add((H3DLUT)Section); break;
                case H3DGroupType.Lights: h3d.Lights.Add((H3DLight)Section); break;
                case H3DGroupType.Fogs: h3d.Fogs.Add((H3DFog)Section); break;
                case H3DGroupType.Scenes: h3d.Scenes.Add((H3DScene)Section); break;
                case H3DGroupType.Shaders: h3d.Shaders.Add((H3DShader)Section); break;
                case H3DGroupType.VisibiltyAnim: h3d.VisibilityAnimations.Add((H3DAnimation)Section); break;
                case H3DGroupType.CameraAnim: h3d.CameraAnimations.Add((H3DCameraAnim)Section); break;
                case H3DGroupType.LightAnim: h3d.LightAnimations.Add((H3DLightAnim)Section); break;
                default:
                    throw new Exception($"Unsupported section! {type}");
            }
            H3D.Save(filePath, h3d);
        }
    }
}
