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
        public string[] Extension => new string[] { "*.bcres" , "*.bcmdl" };

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
        private Gfx BcresData;

        //Folder for model data
        private ModelFolder ModelFolder;
        //Folder for texture data
        private Bch.TextureFolder TextureFolder;
        private Bch.LUTFolder LUTFolder;

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

            Runtime.DisplayBones = true;

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
            AddRender(new SceneLightingUI.LightPreview(light));

            foreach (var lightNode in SceneLightingUI.Setup(Render, Render.Renderer.Lights))
            {
                Root.AddChild(lightNode);
            }

            ModelFolder = new ModelFolder(this, BcresData, h3d);
            TextureFolder = new Bch.TextureFolder(Render, h3d.Textures.ToList());
            LUTFolder = new Bch.LUTFolder(Render, h3d);

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

            foreach (CMDL model in ModelFolder.Children)
                if (model.SkeletonRenderer != null)
                    Render.Skeletons.Add(model.SkeletonRenderer);
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
            foreach (var lut in LUTFolder.GetLuts())
                BcresData.LUTs.Add(SPICA.Formats.CtrGfx.LUT.GfxLUT.FromH3D(lut));
            
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
         //   windows.Add(Workspace.GraphWindow);

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
            GfxDict<T> SectionList;

            public H3DGroupNode(H3DGroupType type)
            {
                Type = type;
                Header = GetName();
                this.ContextMenus.Add(new MenuItemModel("Export All", ExportAll));
                this.ContextMenus.Add(new MenuItemModel("Import", Import));

            }

            public void Load(GfxDict<T> subSections)
            {
                SectionList = subSections;
                foreach (var item in subSections)
                {
                    var section = new NodeSection<T>(SectionList, item.Name, item);
                    this.AddChild(section);
                }
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

            private void Import()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.FileName = $"{Header}.json";
                dlg.AddFilter("json", "json");
                if (dlg.ShowDialog())
                {
                    var item = JsonConvert.DeserializeObject<T>(File.ReadAllText(dlg.FilePath));
                    var nodeFile = new NodeSection<T>(SectionList, item.Name, item);
                    AddChild(nodeFile);
                    SectionList.Add(item);
                }
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
        }

        class NodeSection<T> : NodeBase where T : SPICA.Formats.Common.INamed
        {
            private object Section;
            private GfxDict<T> Dict;

            public NodeSection(GfxDict<T> subSections, string name, object section)
            {
                Header = name;
                Section = section;
                Dict = subSections;
                CanRename = true;
                Icon = IconManager.FILE_ICON.ToString();

                this.ContextMenus.Add(new MenuItemModel("Export", Export));
                this.ContextMenus.Add(new MenuItemModel("Replace", Replace));
                this.ContextMenus.Add(new MenuItemModel(""));
                this.ContextMenus.Add(new MenuItemModel("Rename", () => { ActivateRename = true; }));

                if (section is GfxAnimation)
                {
                    var anim = ((GfxAnimation)section).ToH3DAnimation();
                    var wrapper = new AnimationWrapper((H3DAnimation)anim);
                    Tag = wrapper;
                }
                this.OnSelected += delegate
                {
                    if (Tag is AnimationWrapper)
                        ((AnimationWrapper)Tag).AnimationSet();
                };
                this.OnHeaderRenamed += delegate
                {
                    ((INamed)Section).Name = this.Header;
                };
            }

            public void ExportAsJson(string filePath)
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(Section, Formatting.Indented));
            }

            void Replace()
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

            void Export()   
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = $"{Header}.json";
                dlg.AddFilter(".json", "json");
                dlg.AddFilter(".bcres", "bcres");
                if (dlg.ShowDialog())
                {
                    if (dlg.FilePath.EndsWith(".json"))
                        File.WriteAllText(dlg.FilePath, JsonConvert.SerializeObject(Section, Formatting.Indented));
                    else
                    {
                        var type = ((H3DGroupNode<T>)this.Parent).Type;
                        ExportRaw(dlg.FilePath, Section, type);
                    }
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
