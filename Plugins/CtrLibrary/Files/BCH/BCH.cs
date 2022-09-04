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
        /// The render instance used to display the model in 3D view.
        /// </summary>
        public H3DRender Render;

        /// <summary>
        /// The file instance for handling bch data.
        /// </summary>
        private H3D H3DData;

        //Shader window for debugging and viewing how shader code is generated
        ShaderWindow ShaderWindow;

        //Optional .mbn used by smash 3ds
        private MBn ModelBinary;

        //Folder for model data
        private ModelFolder ModelFolder;

        //Folder for texture data
        private TextureFolder TextureFolder;

        public void Load(Stream stream)
        {
            H3DData = H3D.Open(new MemoryStream(stream.ToArray()));
            if (FileInfo.FilePath.EndsWith(".bch") && File.Exists(FileInfo.FilePath.Replace(".bch", ".mbn")))
            {
                ModelBinary = new MBn(new BinaryReader(File.OpenRead(FileInfo.FilePath.Replace(".bch", ".mbn"))), H3DData);
                H3DData = ModelBinary.ToH3D();
            }

            ShaderWindow = new ShaderWindow(this.Workspace);
                ShaderWindow.DockDirection = ImGuiNET.ImGuiDir.Down;

            Render = new H3DRender(H3DData, null);
            AddRender(Render);

            Runtime.DisplayBones = true;

            this.Workspace.Outliner.SelectionChanged += delegate
            {
                var node = this.Workspace.Outliner.SelectedNode;
                if (node is MaterialWrapper) //material tree node
                {
                    var mat = ((MaterialWrapper)node).Material;
                    ShaderWindow.Material = mat;
                }
            };

            this.Workspace.Outliner.SelectionChanged += delegate
            {
                var node = this.Workspace.Outliner.SelectedNode;
            };

            var light = Render.Renderer.Lights[0];
            AddRender(new SceneLightingUI.LightPreview(light));

            foreach (var lightNode in SceneLightingUI.Setup(Render, Render.Renderer.Lights))
            {
                Root.AddChild(lightNode);
            }

            ModelFolder = new ModelFolder(this, H3DData);
            TextureFolder = new TextureFolder(Render, H3DData.Textures.ToList());

            Root.AddChild(ModelFolder);
            Root.AddChild(TextureFolder);
            Root.AddChild(new LUTFolder(Render, H3DData));

            AddNodeGroup(H3DData.Shaders, new H3DGroupNode(H3DGroupType.Shaders));
            AddNodeGroup(H3DData.Cameras, new H3DGroupNode(H3DGroupType.Cameras));
            AddNodeGroup(H3DData.Lights, new H3DGroupNode(H3DGroupType.Lights));
            AddNodeGroup(H3DData.Fogs, new H3DGroupNode(H3DGroupType.Fogs));
            AddNodeGroup(H3DData.Scenes, new H3DGroupNode(H3DGroupType.Scenes));
            AddNodeGroup(H3DData.SkeletalAnimations, new H3DGroupNode(H3DGroupType.SkeletalAnim));
            AddNodeGroup(H3DData.MaterialAnimations, new H3DGroupNode(H3DGroupType.MaterialAnim));
            AddNodeGroup(H3DData.VisibilityAnimations, new H3DGroupNode(H3DGroupType.VisibiltyAnim));
            AddNodeGroup(H3DData.CameraAnimations, new H3DGroupNode(H3DGroupType.CameraAnim));
            AddNodeGroup(H3DData.LightAnimations, new H3DGroupNode(H3DGroupType.LightAnim));
            AddNodeGroup(H3DData.FogAnimations, new H3DGroupNode(H3DGroupType.EmitterAnim));
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
            foreach (CMDL model in ModelFolder.Children)
                model.OnSave();

            H3DData.Textures = TextureFolder.GetTextures();


            H3D.Save(stream, H3DData);
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
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            windows.Add(Workspace.ConsoleWindow);
            windows.Add(Workspace.ViewportWindow);
            windows.Add(Workspace.TimelineWindow);
            windows.Add(ShaderWindow);
            return windows;
        }


        private void AddNodeGroup<T>(H3DDict<T> subSections, H3DGroupNode folder)
   where T : SPICA.Formats.Common.INamed
        {
            foreach (var item in subSections)
            {
                folder.AddChild(new NodeSection<T>(subSections, item.Name, item));
            }
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
            EmitterAnim,
            Particles,
        }

        class H3DGroupNode : NodeBase
        {
            H3DGroupType Type;

            public H3DGroupNode(H3DGroupType type)
            {
                Type = type;
                Header = GetName();
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
                    case H3DGroupType.SkeletalAnim: return "Skeletal Animations";
                    case H3DGroupType.MaterialAnim: return "Material Animations";
                    case H3DGroupType.VisibiltyAnim: return "Visibilty Animations";
                    case H3DGroupType.CameraAnim: return "Camera Animations";
                    case H3DGroupType.LightAnim: return "Light Animations";
                    case H3DGroupType.EmitterAnim: return "Emitter Animations";
                    case H3DGroupType.Particles: return "Particles";
                    default:
                        throw new System.Exception("Unknown type? " + Type);
                }
            }
        }

        class NodeSection<T> : NodeBase where T : SPICA.Formats.Common.INamed
        {
            private object Section;
            private H3DDict<T> Dict;

            public NodeSection(H3DDict<T> subSections, string name, object section)
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
            }

            void Replace()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                dlg.FileName = $"{Header}.json";
                dlg.AddFilter("json", "json");
                if (dlg.ShowDialog())
                {
                    Section = JsonConvert.DeserializeObject<T>(File.ReadAllText(dlg.FilePath));
                    Dict[this.Header] = (T)Section;
                }
            }

            void Export()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = $"{Header}.json";
                dlg.AddFilter("json", "json");
                if (dlg.ShowDialog())
                {
                    File.WriteAllText(dlg.FilePath, JsonConvert.SerializeObject(Section, Formatting.Indented));
                }
            }
        }
    }
}
