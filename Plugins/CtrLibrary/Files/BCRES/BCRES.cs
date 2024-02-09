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
using CtrLibrary.UI;
using SPICA.Formats.CtrGfx.Texture;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrGfx.Animation;
using GLFrameworkEngine;
using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Fog;
using SPICA.Formats.CtrH3D.Light;
using SPICA.Formats.CtrH3D.LUT;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Scene;
using SPICA.Formats.CtrH3D.Shader;
using SPICA.Formats.CtrH3D.Texture;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrGfx.LUT;
using SPICA.Formats.CtrGfx.Light;
using SPICA.Formats.CtrGfx.Fog;
using SPICA.Formats.CtrGfx.Scene;
using SPICA.Formats.CtrGfx.Shader;
using Discord;
using static System.Collections.Specialized.BitVector32;
using SPICA.PICA.Shader;
using static CtrLibrary.Bch.BCH;
using Toolbox.Core.Animations;
using SPICA.Formats.CtrGfx.Camera;
using SPICA.Formats.CtrH3D.Camera;
using SPICA.Formats.CtrGfx.AnimGroup;

namespace CtrLibrary.Bcres
{
    /// <summary>
    /// Represents a plugin for loading/editing/saving BCRES/BCMDL binary files.
    /// </summary>

    public class BCRES : FileEditor, IFileFormat, IDisposable
    {
        /// <summary>
        /// The description of the file extension of the plugin.
        /// </summary>
        public string[] Description => new string[] { "Bcres" };

        /// <summary>
        /// The extension of the plugin.
        /// </summary>
        public string[] Extension => new string[] { "*.bcres", "*.bcmdl", "*.bch" };

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
                return reader.CheckSignature(4, "CGFX");
            }
        }

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

            Gfx gfx = new Gfx();
            Load(gfx);

            this.Root.Header = "NewFile.bcres";
            this.Root.Tag = this;

            return true;
        }

        public override bool DisplayViewport => ModelFolder.Children.Count > 0;

        /// <summary>
        /// The render instance used to display the model in 3D view.
        /// </summary>
        public H3DRender Render;

        /// <summary>
        /// The file instance of the bcres data.
        /// </summary>
        public Gfx BcresData;

        //Folder for model data
        private ModelFolder ModelFolder;
        //Folder for texture data
        private Bch.TextureFolder<H3DTexture> TextureFolder;
        private Bch.LUTFolder<H3DLUT> LUTFolder;

        //Shader window for debugging and viewing how shader code is generated
        private ShaderWindow ShaderWindow;

        public void Load(Stream stream)
        {
            Load(Gfx.Open(stream));
        }

        //Creates new + loads from .dae file.
        //Todo add a sort of UI for this?
        private void LoadFromDae(string filePath)
        {
            CreateNew();
            ImportModel(filePath);
        }

        private void Load(Gfx gfx)
        {
            BcresData = gfx;

            var h3d = BcresData.ToH3D();

            Root.TagUI.Tag = h3d;

            ShaderWindow = new ShaderWindow(this.Workspace);
            ShaderWindow.DockDirection = ImGuiNET.ImGuiDir.Down;

            Render = new H3DRender(h3d, null);
            AddRender(Render);

            this.Workspace.Outliner.SelectionChanged += delegate
            {
                var node = this.Workspace.Outliner.SelectedNode;
                if (node is MTOB) //material tree node
                {
                    var mat = ((MTOB)node).Material;
                    ShaderWindow.Material = mat;
                }
            };

            var light = Render.Renderer.Lights[0];
           // AddRender(new SceneLightingUI.LightPreview(light));

            foreach (var lightNode in SceneLightingUI.Setup(Render, Render.Renderer.Lights))
            {
                Root.AddChild(lightNode);
                //only load one scene light for global usage
                break;
            }

            ModelFolder = new ModelFolder(this, BcresData, h3d);
            TextureFolder = new Bch.TextureFolder<H3DTexture>(Render, h3d.Textures);
            LUTFolder = new Bch.LUTFolder<H3DLUT>(Render, h3d.LUTs);

            Root.AddChild(ModelFolder);
            Root.AddChild(TextureFolder);
            Root.AddChild(LUTFolder);

            AddNodeGroup(H3DGroupType.Shaders, BcresData.Shaders);
            AddNodeGroup(H3DGroupType.Cameras, BcresData.Cameras);
            AddNodeGroup(H3DGroupType.Fogs, BcresData.Fogs);
            AddNodeGroup(H3DGroupType.Lights, BcresData.Lights);
            AddNodeGroup(H3DGroupType.Scenes, BcresData.Scenes);
            AddNodeGroup(H3DGroupType.SkeletalAnim, BcresData.SkeletalAnimations);
            AddNodeGroup(H3DGroupType.MaterialAnim, BcresData.MaterialAnimations);
            AddNodeGroup(H3DGroupType.VisibiltyAnim, BcresData.VisibilityAnimations);
            AddNodeGroup(H3DGroupType.CameraAnim, BcresData.CameraAnimations);
            AddNodeGroup(H3DGroupType.LightAnim, BcresData.LightAnimations);
            AddNodeGroup(H3DGroupType.FogAnim, BcresData.FogAnimations);
            AddNodeGroup(H3DGroupType.Emitter, BcresData.Emitters);
        }

        /// <summary>
        /// Imports a model into the file with a given path.
        /// The model is updated to the UI folder directly.
        /// </summary>
        public void ImportModel(string filePath)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);

            if (ModelFolder.Children.Any(x => x.Header == name))
                return;

            ModelFolder.Import(filePath);
        }

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
            BcresData.Models.Clear();
            BcresData.Textures.Clear();
            BcresData.LUTs.Clear();

            foreach (CMDL model in ModelFolder.Children)
            {
                model.OnSave();
                BcresData.Models.Add(model.Model);
            }
            foreach (var tex in TextureFolder.GetTextures())
            {
                BcresData.Textures.Add(GfxTexture.FromH3D(tex));
            }
            foreach (var lut in LUTFolder.SectionList)
                BcresData.LUTs.Add(SPICA.Formats.CtrGfx.LUT.GfxLUT.FromH3D(lut));
            
            foreach (var folder in this.Root.Children)
            {
                if (folder is H3DGroupNode<GfxAnimation>)
                    ((H3DGroupNode<GfxAnimation>)folder).OnSave();

                if (folder is H3DGroupNode<GfxAnimation>)
                {
                    var animNode = (H3DGroupNode<GfxAnimation>)folder;
                    foreach (AnimationNode<GfxAnimation> anim in animNode.Children)
                        anim.OnSave();
                }
                if (folder is H3DGroupNode<GfxFog>)
                {
                    foreach (FogNode<GfxFog> anim in folder.Children)
                        anim.OnSave();
                }
                if (folder is H3DGroupNode<GfxLight>)
                {
                    foreach (LightNode<GfxLight> anim in folder.Children)
                        anim.OnSave();
                }
                if (folder is H3DGroupNode<GfxScene>)
                {
                    foreach (SceneNode<GfxScene> anim in folder.Children)
                        anim.OnSave();
                }
                if (folder is H3DGroupNode<GfxCamera>)
                {
                    foreach (CameraNode<GfxCamera> anim in folder.Children)
                        anim.OnSave();
                }
            }

            //if extension is .bch, convert to BCH
            if (FileInfo.FilePath.EndsWith(".bch"))
                H3D.Save(stream, BcresData.ToH3D());
            else
                Gfx.Save(stream, BcresData);
        }

        /// <summary>
        /// Prepares the dock layouts to be used for the file format.
        /// </summary>
        public override List<DockWindow> PrepareDocks()
        {
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            windows.Add(Workspace.ConsoleWindow);
            windows.Add(Workspace.ViewportWindow);
            windows.Add(Workspace.TimelineWindow);
            windows.Add(Workspace.GraphWindow);

            if (ShaderWindow != null)
                windows.Add(ShaderWindow);
            return windows;
        }

        /// <summary>
        /// Disposes the render data during a workspace close.
        /// </summary>
        public void Dispose()
        {
            Render.Dispose();
        }

        private void AddNodeGroup<T>(H3DGroupType type, GfxDict<T> section) where T : SPICA.Formats.Common.INamed
        {
            H3DGroupNode<T> folder = new H3DGroupNode<T>(type);
            folder.Load(section);

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
            Emitter,
            Particles,
        }

        class H3DGroupNode<T> : NodeBase where T : SPICA.Formats.Common.INamed
        {
            public H3DGroupType Type;
            public GfxDict<T> SectionList;

            public H3DGroupNode(H3DGroupType type)
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
            }

            public void Load(GfxDict<T> subSections)
            {
                SectionList = subSections;
                foreach (var item in subSections)
                {
                    if (item is GfxShader)
                        this.AddChild(new ShaderNode<T>(SectionList, item));
                    else if (item is GfxAnimation)
                        this.AddChild(new AnimationNode<T>(SectionList, item));
                    else if (item is GfxFog)
                        this.AddChild(new FogNode<T>(SectionList, item));
                    else if (item is GfxScene)
                        this.AddChild(new SceneNode<T>(SectionList, item));
                    else if (item is GfxLight)
                        this.AddChild(new LightNode<T>(SectionList, item));
                    else if (item is GfxCamera)
                        this.AddChild(new CameraNode<T>(SectionList, item));
                    else
                        this.AddChild(new NodeSection<T>(SectionList, item));
                }
            }

            private void Add()
            {
                var item = Activator.CreateInstance(typeof(T)) as SPICA.Formats.Common.INamed;
                //Default name
                item.Name = $"New{this.Type}";
                //Auto rename possible dupes
                item.Name = Utils.RenameDuplicateString(item.Name, SectionList.Select(x => x.Name).ToList());
                AddNewSection(item);
            }

            private void ExportAll()
            {
                ImguiFolderDialog dlg = new ImguiFolderDialog();
                if (dlg.ShowDialog())
                {
                    foreach (NodeSection<T> node in this.Children)
                        node.ExportAsJson(Path.Combine(dlg.SelectedPath, $"{node.Header}.json"));
                }
            }

            private void ReplaceAll()
            {

            }

            private void Clear()
            {
                this.Children.Clear();
                this.SectionList.Clear();
            }

            private void Import()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.FileName = $"{Header}.json";
                dlg.AddFilter("json", "json");
                dlg.AddFilter("bcres", "bcres");

                if (dlg.ShowDialog())
                {
                    //Replace as raw binary or json text formats
                    if (dlg.FilePath.ToLower().EndsWith(".bcres"))
                    {
                        var item = Activator.CreateInstance(typeof(T)) as SPICA.Formats.Common.INamed;
                        item.Name = Path.GetFileNameWithoutExtension(dlg.FilePath);

                        var nodeFile = new NodeSection<T>(SectionList, item);
                        item = (T)NodeSection<T>.ReplaceRaw(dlg.FilePath, Type);

                        AddChild(nodeFile);
                        SectionList.Add((T)item);
                    }
                    else
                    {
                        var item = JsonConvert.DeserializeObject<T>(File.ReadAllText(dlg.FilePath));
                        AddNewSection(item);
                    }
                }
            }

            private void AddNewSection(INamed item)
            {
                //Add section list
                SectionList.Add((T)item);

                //Set animation types
                if (this.Type == H3DGroupType.SkeletalAnim)
                    ((GfxAnimation)item).TargetAnimGroupName = "SkeletalAnimation";
                else if (this.Type == H3DGroupType.MaterialAnim)
                    ((GfxAnimation)item).TargetAnimGroupName = "MaterialAnimation";
                else if (this.Type == H3DGroupType.FogAnim)
                    ((GfxAnimation)item).TargetAnimGroupName = "FogAnimation";
                else if (this.Type == H3DGroupType.LightAnim)
                    ((GfxAnimation)item).TargetAnimGroupName = "LightAnimation";
                else if (this.Type == H3DGroupType.VisibiltyAnim)
                    ((GfxAnimation)item).TargetAnimGroupName = "VisibilityAnimation";
                else if (this.Type == H3DGroupType.CameraAnim)
                    ((GfxAnimation)item).TargetAnimGroupName = "CameraAnimation";

                //Add to UI
                if (item is GfxShader)
                    this.AddChild(new ShaderNode<T>(SectionList, item));
                else if (item is GfxAnimation)
                    this.AddChild(new AnimationNode<T>(SectionList, item));
                else if (item is GfxFog)
                    this.AddChild(new FogNode<T>(SectionList, item));
                else if (item is GfxScene)
                    this.AddChild(new SceneNode<T>(SectionList, item));
                else if (item is GfxLight)
                    this.AddChild(new LightNode<T>(SectionList, item));
                else if (item is GfxCamera)
                    this.AddChild(new CameraNode<T>(SectionList, item));
                else
                    this.AddChild(new NodeSection<T>(SectionList, item));
            }

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
                    case H3DGroupType.Emitter: return "Emitter";
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

            public virtual void OnSave()
            {

            }
        }

        class SceneNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            GfxScene Scene => (GfxScene)Section;
            H3DScene H3DScene;

            public SceneNode(GfxDict<T> subSections, object section) : base(subSections, section)
            {
                H3DScene = Scene.ToH3D();

                BchSceneUI sceneUI = new BchSceneUI();
                sceneUI.Init(H3DScene, Scene.MetaData);

                TagUI.UIDrawer += delegate
                {
                    sceneUI.Render();
                };
                Tag = Scene;
            }

            public override void OnSave()
            {
                Scene.FromH3D(this.H3DScene);
            }
        }

        class LightNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            GfxLight Light => (GfxLight)Section;

            H3DLight H3DLight;

            public LightNode(GfxDict<T> subSections, object section) : base(subSections, section)
            {
                H3DLight = Light.ToH3DLight();

                BchLightUI lightUI = new BchLightUI();
                lightUI.Init(H3DLight, Light.MetaData);

                TagUI.UIDrawer += delegate
                {
                    lightUI.Render();
                };
                Tag = Light;
            }

            public override void OnSave()
            {
                int index = this.Dict.Find(((T)Section).Name);
                if (index == -1)
                    return;

                this.Dict.Remove((T)Section);

                var meta_data = ((GfxLight)Section).MetaData.ToList();
                var anim_groups = ((GfxLight)Section).AnimationsGroup.ToList();

                //reinsert instance. GfxLight can change type instance so it has to be reloaded
                Section = GfxLight.FromH3D(this.H3DLight);

                this.Dict.Insert(index, (T)Section);

                //Re insert meta data and anim groups after conversion
                ((GfxLight)Section).MetaData = new GfxDict<GfxMetaData>();
                ((GfxLight)Section).AnimationsGroup = new GfxDict<GfxAnimGroup>();

                foreach (var usd in meta_data)
                    ((GfxLight)Section).MetaData.Add(usd);

                foreach (var animGroup in anim_groups)
                    ((GfxLight)Section).AnimationsGroup.Add(animGroup);
            }
        }

        class CameraNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            GfxCamera Camera => (GfxCamera)Section;

            H3DCamera H3DCamera;

            public CameraNode(GfxDict<T> subSections, object section) : base(subSections, section)
            {
                H3DCamera = Camera.ToH3DCamera();

                BchCameraUI camUI = new BchCameraUI();
                camUI.Init(H3DCamera, Camera.MetaData);

                TagUI.UIDrawer += delegate
                {
                    camUI.Render();
                };
                Tag = Camera;
            }

            public override void OnSave()
            {
                Camera.FromH3D(this.H3DCamera);
            }
        }

        class FogNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            GfxFog Fog => (GfxFog)Section;
            H3DFog H3DFog;

            public FogNode(GfxDict<T> subSections, object section) : base(subSections, section)
            {
                H3DFog = Fog.ToH3D();

                BchFogUI fogUI = new BchFogUI();
                fogUI.Init(H3DFog, Fog.MetaData);

                TagUI.UIDrawer += delegate
                {
                    fogUI.Render();
                };
                Tag = Fog;
            }

            public override void OnSave()
            {
                Fog.FromH3D(this.H3DFog);
            }
        }

        class ShaderNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            GfxShader Shader => (GfxShader)Section;

            ShaderBinary ShBin;

            public ShaderNode(GfxDict<T> subSections, object section) : base(subSections, section)
            {
                if (Shader.ShaderData == null)
                    return;

                ShBin = Shader.ToBinary();

                foreach (var prog in Shader.ShaderInfos)
                {
                    NodeBase proNode = new NodeBase($"Program {this.Children.Count}");

                    var ShaderUI = new ShaderUI(ShBin, prog.VertexProgramIndex, prog.GeometryProgramIndex);
                    proNode.TagUI.UIDrawer += delegate
                    {
                        ShaderUI.Render();
                    };

                    AddChild(proNode);
                }
            }
        }

        class AnimationNode<T> : NodeSection<T> where T : SPICA.Formats.Common.INamed
        {
            public H3DAnimation H3DAnimation;

            public AnimationNode(GfxDict<T> subSections, object section) : base(subSections, section)
            {
                H3DAnimation = ((GfxAnimation)section).ToH3DAnimation();
                var wrapper = new AnimationWrapper(H3DAnimation);
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
                    propertyUI.Render(wrapper, ((GfxAnimation)section).MetaData);
                };

                this.OnSelected += delegate
                {
                    ((AnimationWrapper)Tag).AnimationSet();
                };

                this.ContextMenus.Clear();
                this.ContextMenus.Add(new MenuItemModel("Export", Export));
                this.ContextMenus.Add(new MenuItemModel("Replace", Replace));
                this.ContextMenus.Add(new MenuItemModel(""));

                if (((GfxAnimation)Section).TargetAnimGroupName == "SkeletalAnimation")
                {
                    this.ContextMenus.Add(new MenuItemModel("Replace (Baked Quaternions / MK7 Drivers)", () => ReplaceAsBaked(false)));
                    this.ContextMenus.Add(new MenuItemModel("Replace (Baked Matrices)", () => ReplaceAsBaked(true)));

                    this.ContextMenus.Add(new MenuItemModel(""));
                }
                this.ContextMenus.Add(new MenuItemModel("Rename", () => { ActivateRename = true; }));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Remove", Remove));
            }

            public override void Export()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = $"{Header}.json";
                dlg.AddFilter(".json", "json");
                dlg.AddFilter(".bcres", "bcres");

                if (((GfxAnimation)Section).TargetAnimGroupName == "SkeletalAnimation")
                {
                    dlg.AddFilter(".anim", "anim");
                    dlg.FileName = $"{Header}.anim";
                }

                if (dlg.ShowDialog())
                {
                    OnSave();

                    string ext = Path.GetExtension(dlg.FilePath.ToLower());
                    switch (ext)
                    {
                        case ".json":
                            File.WriteAllText(dlg.FilePath, JsonConvert.SerializeObject(Section, Formatting.Indented));
                            break;
                        case ".bcres":
                            var type = ((H3DGroupNode<T>)this.Parent).Type;
                            ExportRaw(dlg.FilePath, Section, type);
                            break;
                        case ".anim":
                        case ".dae":
                        case ".glb":
                        case ".gltf":
                            BcresSkelAnimationImporter.Export(((GfxAnimation)Section), GetModel(), dlg.FilePath);
                            break;
                        default:
                            throw new Exception($"Unsupported file extension {ext}!");
                    }
                }
            }

            private H3DModel GetModel()
            {
                return H3DRender.GetFirstVisibleModel();
            }

            public override void Replace()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.FileName = $"{Header}.json";
                dlg.AddFilter(".json", "json");
                dlg.AddFilter(".bcres", "bcres");

                if (((GfxAnimation)Section).TargetAnimGroupName == "SkeletalAnimation")
                {
                    dlg.AddFilter(".anim", "anim");
                    dlg.AddFilter(".gltf", "gltf");
                    dlg.AddFilter(".glb", "glb");
                    dlg.AddFilter(".dae", "dae");

                    dlg.FileName = $"{Header}.anim";
                }

                if (dlg.ShowDialog())
                {
                    string ext = Path.GetExtension(dlg.FilePath.ToLower());
                    switch (ext)
                    {
                        case ".json":
                            Section = JsonConvert.DeserializeObject<T>(File.ReadAllText(dlg.FilePath));
                            Dict[this.Header] = (T)Section;
                            Dict[this.Header].Name = this.Header;
                            break;
                        case ".bcres":
                            var type = ((H3DGroupNode<T>)this.Parent).Type;
                            Section = ReplaceRaw(dlg.FilePath, type);
                            break;
                        case ".anim":
                        case ".dae":
                        case ".glb":
                        case ".gltf":
                            BcresSkelAnimationImporter.Import(dlg.FilePath, ((GfxAnimation)Section), GetModel());
                            break;
                        default:
                            throw new Exception($"Unsupported file extension {ext}!");
                    }

                    H3DAnimation = ((GfxAnimation)Section).ToH3DAnimation();
                    ((AnimationWrapper)Tag).Reload(H3DAnimation);

                    ((AnimationWrapper)Tag).AnimationSet();
                }
            }

            public void ReplaceAsBaked(bool asMatrices)
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.AddFilter(".anim", "anim");
                dlg.AddFilter(".gltf", "gltf");
                dlg.AddFilter(".glb", "glb");
                dlg.AddFilter(".dae", "dae");
                dlg.FileName = $"{Header}.anim";

                if (dlg.ShowDialog())
                {
                    BcresSkelAnimationImporter.Import(dlg.FilePath, ((GfxAnimation)Section), GetModel(),
                        new BcresSkelAnimationImporter.BcresImportSettings()
                        {
                            BakeAsMatrices = asMatrices,
                            BakeAsQuat = !asMatrices,
                        });

                    H3DAnimation = ((GfxAnimation)Section).ToH3DAnimation();
                    ((AnimationWrapper)Tag).Reload(H3DAnimation);

                    ((AnimationWrapper)Tag).AnimationSet();
                }
            }

            public void OnSave()
            {
                //check for possible edits
                bool isEdited = ((AnimationWrapper)Tag).IsEdited();
                //Convert the gui to H3D animation
                ((AnimationWrapper)Tag).ToH3D(H3DAnimation);
                //Apply the H3D animation to bcres animation data if edited
                if (isEdited)
                    ((GfxAnimation)Section).FromH3D(H3DAnimation);
                else
                {
                    //only transfer loop and frame count property
                    if (((AnimationWrapper)Tag).Loop)
                        ((GfxAnimation)Section).LoopMode = GfxLoopMode.Loop;
                    else
                        ((GfxAnimation)Section).LoopMode = GfxLoopMode.OneTime;
                    ((GfxAnimation)Section).FramesCount = H3DAnimation.FramesCount;
                }
                //Apply any wrapper data on save
                ((AnimationWrapper)Tag).OnSave();
            }
        }

        class NodeSection<T> : NodeBase where T : SPICA.Formats.Common.INamed
        {
            internal object Section;
            internal GfxDict<T> Dict;

            public NodeSection(GfxDict<T> subSections, object section)
            {
                Header = ((INamed)section).Name;
                Section = section;
                Dict = subSections;
                CanRename = true;
                Icon = IconManager.FILE_ICON.ToString();

                this.ContextMenus.Add(new MenuItemModel("Export", Export));
                this.ContextMenus.Add(new MenuItemModel("Replace", Replace));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Rename", () => { ActivateRename = true; }));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Remove", Remove));

                this.OnHeaderRenamed += delegate
                {
                    var index = this.Dict.Find(((T)this.Section).Name);
                    if (index != -1)
                        this.Dict.Remove((T)this.Section);

                    ((INamed)Section).Name = this.Header;

                    if (index != -1)
                        this.Dict.Insert(index, (T)this.Section);
                };
            }

            public void ExportAsJson(string filePath)
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(Section, Formatting.Indented));
            }

            public virtual void Replace()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.FileName = $"{Header}.json";
                dlg.AddFilter(".json", "json");
                dlg.AddFilter(".bcres", "bcres");
                if (dlg.ShowDialog())
                {
                    if (dlg.FilePath.EndsWith(".json"))
                    {
                        Section = JsonConvert.DeserializeObject<T>(File.ReadAllText(dlg.FilePath));
                        Dict[this.Header] = (T)Section;
                        Dict[this.Header].Name = this.Header;
                    }
                    else
                    {
                        var type = ((H3DGroupNode<T>)this.Parent).Type;
                        Section = ReplaceRaw(dlg.FilePath, type);
                    }
                }
            }

            public virtual void Export()   
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = $"{Header}.json";
                dlg.AddFilter(".json", "json");
                dlg.AddFilter(".bcres", "bcres");
                if (dlg.ShowDialog())
                {
                    OnSave();

                    if (dlg.FilePath.EndsWith(".json"))
                        File.WriteAllText(dlg.FilePath, JsonConvert.SerializeObject(Section, Formatting.Indented));
                    else
                    {
                        var type = ((H3DGroupNode<T>)this.Parent).Type;
                        ExportRaw(dlg.FilePath, Section, type);
                    }
                }
            }

            public virtual void OnSave()
            {

            }

            public virtual void Remove()
            {
                if (Section == null)
                    return;

                int result = TinyFileDialog.MessageBoxInfoYesNo($"Are you sure you want to remove {this.Header}?");
                if (result == 1)
                {
                    Dict.Remove((T)Section);
                    this.Parent.Children.Remove(this);
                }
            }

            public static object ReplaceRaw(string filePath, H3DGroupType type)
            {
                object Section = null;

                Gfx gfx = Gfx.Open(filePath);
                switch (type)
                {
                    case H3DGroupType.Models: Section = gfx.Models[0]; break;
                    case H3DGroupType.Textures: Section = gfx.Textures[0]; break;
                    case H3DGroupType.SkeletalAnim: Section = gfx.SkeletalAnimations[0]; break;
                    case H3DGroupType.MaterialAnim: Section = gfx.MaterialAnimations[0]; break;
                    case H3DGroupType.Lookups: Section = gfx.LUTs[0]; break;
                    case H3DGroupType.Lights: Section = gfx.Lights[0]; break;
                    case H3DGroupType.Fogs: Section = gfx.Fogs[0]; break;
                    case H3DGroupType.Scenes: Section = gfx.Scenes[0]; break;
                    case H3DGroupType.Shaders: Section = gfx.Shaders[0]; break;
                    case H3DGroupType.VisibiltyAnim: Section = gfx.VisibilityAnimations[0]; break;
                    case H3DGroupType.CameraAnim: Section = gfx.CameraAnimations[0]; break;
                    case H3DGroupType.LightAnim: Section = gfx.LightAnimations[0]; break;
                    default:
                        throw new Exception($"Unsupported section! {type}");
                }
                return Section;
            }

            public static void ExportRaw(string filePath, object Section, H3DGroupType type)
            {
                Gfx gfx = new Gfx();
                switch (type)
                {
                    case H3DGroupType.Models: gfx.Models.Add((GfxModel)Section); break;
                    case H3DGroupType.Textures: gfx.Textures.Add((GfxTexture)Section); break;
                    case H3DGroupType.SkeletalAnim: gfx.SkeletalAnimations.Add((GfxAnimation)Section); break;
                    case H3DGroupType.MaterialAnim: gfx.MaterialAnimations.Add((GfxAnimation)Section); break;
                    case H3DGroupType.VisibiltyAnim: gfx.VisibilityAnimations.Add((GfxAnimation)Section); break;
                    case H3DGroupType.CameraAnim: gfx.CameraAnimations.Add((GfxAnimation)Section); break;
                    case H3DGroupType.LightAnim: gfx.LightAnimations.Add((GfxAnimation)Section); break;
                    case H3DGroupType.Lookups: gfx.LUTs.Add((GfxLUT)Section); break;
                    case H3DGroupType.Lights: gfx.Lights.Add((GfxLight)Section); break;
                    case H3DGroupType.Fogs: gfx.Fogs.Add((GfxFog)Section); break;
                    case H3DGroupType.Scenes: gfx.Scenes.Add((GfxScene)Section); break;
                    case H3DGroupType.Shaders: gfx.Shaders.Add((GfxShader)Section); break;
                    default:
                        throw new Exception($"Unsupported section! {type}");
                }
                Gfx.Save(filePath, gfx);
            }
        }
    }
}
