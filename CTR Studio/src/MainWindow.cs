using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using UIFramework;
using MapStudio.UI;
using OpenTK.Input;
using OpenTK.Graphics;
using System.IO;
using Toolbox.Core;
using GLFrameworkEngine;
using ImGuiNET;

namespace CTRStudio
{
    public class MainWindow : UIFramework.MainWindow
    {
        /// <summary>
        /// List of workspace windows used to load and edit file data.
        /// </summary>
        List<Workspace> Workspaces = new List<Workspace>();
        /// <summary>
        /// The recently opened files.
        /// </summary>
        List<string> RecentFiles = new List<string>();
        /// <summary>
        /// The recently opened or saved project files.
        /// </summary>
        List<string> RecentProjects = new List<string>();
        //Argument data from the command line
        private Program.Arguments _arguments;
        //Program settings
        private GlobalSettings GlobalSettings;
        //Plugin settings
        private IPluginConfig[] PluginSettingsUI;
        //Menus
        private MenuItem SaveMenu;
        private MenuItem SaveAsMenu;
        //Debug
        private bool showStyleEditor = false;
        private bool showDemoWindow = false;
        //Settings window
        private SettingsWindow SettingsWindow;
        //About Window
        private AboutWindow AboutWindow;

        public MainWindow(Program.Arguments arguments)
        {
            _arguments = arguments;
            Init();
        }

        public override void OnLoad()
        {
            if (loaded)
                return;

            base.OnLoad();

            //Load global settings like language configuration
            GlobalSettings = GlobalSettings.Load();
            GlobalSettings.ReloadLanguage();
            GlobalSettings.ReloadTheme();
            GlobalSettings.ReloadInput();

            if (!Directory.Exists(GlobalSettings.Program.ProjectDirectory))
                Directory.CreateDirectory(GlobalSettings.Program.ProjectDirectory);

            SettingsWindow = new SettingsWindow(GlobalSettings);


            AboutWindow = new AboutWindow();
            Windows.Add(SettingsWindow);
            Windows.Add(AboutWindow);

            //Set the adjustable global font scale
            ImGui.GetIO().FontGlobalScale = GlobalSettings.Program.FontScale;
            //Init common render resources typically for debugging purposes
            RenderTools.Init();
            //Share resources between contexts
            GraphicsContext.ShareContexts = true;
            //Load outlier icons to cache
            IconManager.LoadTextureFile("Node", Properties.Resources.Object, 32, 32);
            //Load icons for map objects
            if (Directory.Exists(Path.Combine(Runtime.ExecutableDir, "Lib", "Images", "MapObjects")))
            {
                foreach (var imageFile in Directory.GetFiles(Path.Combine(Runtime.ExecutableDir, "Lib", "Images", "MapObjects")))
                {
                    IconManager.LoadTextureFile(imageFile, 64, 64);
                }
            }
            //Load recent file lists
            RecentFileHandler.LoadRecentList(Path.Combine(Runtime.ExecutableDir, "Recent.txt"), RecentFiles);
            RecentFileHandler.LoadRecentList(Path.Combine(Runtime.ExecutableDir, "RecentProjects.txt"), RecentProjects);

            foreach (var file in _arguments.FileInput)
                LoadFileFormat(file);

            PluginSettingsUI = Toolbox.Core.FileManager.GetPluginSettings();
        }

        public override void Render()
        {
            base.Render();

            //Window spawn sizes
            var contentSize = ImGui.GetWindowSize();

            //Show demo window for debugging UI 
            if (showDemoWindow)
                ImGui.ShowDemoWindow();
            //Show style editor for debugging styles
            if (showStyleEditor)
            {
                if (ImGui.Begin("Style Editor", ref showStyleEditor))
                {
                    ImGui.ShowStyleEditor();
                }
                ImGui.End();
            }

            SetupDocks();

            List<Workspace> removedWindows = new List<Workspace>();
            foreach (var workspace in Workspaces)
            {
                //Constrain the docked windows within a workspace using window classes
                unsafe
                {
                    //Constrain the docked windows within a workspace using window classes
                    ImGui.SetNextWindowClass(window_class);
                    //Set the window size on load
                    ImGui.SetNextWindowSize(contentSize, ImGuiCond.Once);
                }

                if (workspace.IsFocused && Workspace.ActiveWorkspace != workspace)
                    Workspace.UpdateActive(workspace);

                workspace.Show();

                if (!workspace.Opened)
                    removedWindows.Add(workspace);
            }

            //Remove windows that are not opened
            if (removedWindows.Count > 0)
            {
                int result = TinyFileDialog.MessageBoxInfoYesNo(TranslationSource.GetText("REMOVE_NOTIFY"));
                if (result == 1)
                    RemoveWorkspaces(removedWindows);
                else
                {
                    foreach (var window in removedWindows)
                        window.Opened = true;
                }
            }

            foreach (var window in Windows)
                window.Show();

            //Show any pop up dialogs if active
            DialogHandler.RenderActiveWindows();

            //Progress bar
            if (ProcessLoading.Instance.IsLoading)
                ProcessLoading.Instance.Draw(_window.Width, _window.Height);
        }

        private void SetupDocks()
        {
            var dock_id = ImGui.GetID("##DockspaceRoot");

            SetupParentDock(dock_id, Workspaces);
        }

        #region MainMenuBar

        public override void MainMenuDraw()
        {
            base.MainMenuDraw();

            MapStudio.UI.ImGuiHelper.IncrementCursorPosX(20);

            if (Workspace.ActiveWorkspace != null)
            {
                var workspace = Workspace.ActiveWorkspace;
                workspace.ActiveEditor.DrawMainMenuBar();

                //A full screen menu for going back to the previous non full screen state
                if (workspace.IsFullScreen)
                {
                    if (ImGui.Button(TranslationSource.GetText("BACK_TO_PREVIOUS")))
                        workspace.DisableFullScreen();
                }
                else
                {
                    //A tool list for selecting different workspace tools
                    foreach (var menu in workspace.WorkspaceTools)
                    {
                        if (menu == workspace.ActiveWorkspaceTool)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, ThemeHandler.ActiveTextHighlight);
                            MapStudio.UI.ImGuiHelper.LoadMenuItem(menu);
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
                            MapStudio.UI.ImGuiHelper.LoadMenuItem(menu);
                            ImGui.PopStyleColor();
                        }

                        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
                        {
                            workspace.ActiveWorkspaceTool = menu;
                        }
                    }
                }
            }
        }

        private void Init()
        {
            MenuItems.Clear();
            var fileMenu = new MenuItem("FILE")
            {
                RenderItems = () =>
                {
                    //Improve the window popup for the "NEW" menu so the items aren't so small.
                    //Todo improve this more
                    ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 24 * UIManager.CreateNewEditors.Count));
                }
            };
            fileMenu.MenuItems.Add(new MenuItem($"NEW", IconManager.NEW_FILE_ICON) { RenderItems = LoadNewFileMenu });
            fileMenu.MenuItems.Add(new MenuItem($"OPEN", IconManager.OPEN_ICON, OpenFileWithDialog) { Shortcut = "Ctrl+O" });
            fileMenu.MenuItems.Add(new MenuItem($"RECENT", ' ') { RenderItems = LoadRecentFiles });

            SaveMenu = new MenuItem($"SAVE", IconManager.SAVE_ICON, () =>
            {
                Workspace.ActiveWorkspace.SaveFileData();
            })
            { Shortcut = "Ctrl+S" };
            SaveAsMenu = new MenuItem($"SAVE_AS", IconManager.SAVE_ICON, () =>
            {
                Workspace.ActiveWorkspace.SaveFileWithDialog();
            })
            { Shortcut = "Ctrl+Alt+S" };
            fileMenu.MenuItems.Add(SaveMenu);
            fileMenu.MenuItems.Add(SaveAsMenu);
            fileMenu.MenuItems.Add(new MenuItem("")); //splitter

            fileMenu.MenuItems.Add(new MenuItem($"CLEAR_WORKSPACE", IconManager.DELETE_ICON, ClearWorkspace));
            fileMenu.MenuItems.Add(new MenuItem($"EXIT", ' ', () => { _window.Exit(); }));

            var saveConfigMenu = new MenuItem("SAVE_CONFIG") { RenderItems = LoadFileConfigMenu };
            var settingsMenu = new MenuItem("SETTINGS", LoadSettingsWindow);
            var windowsMenu = new MenuItem("WINDOWS") { RenderItems = LoadWindowMenu };
            var pluginsMenu = new MenuItem("PLUGINS") { RenderItems = LoadPluginMenus };
            var helpMenu = new MenuItem("HELP");
            helpMenu.MenuItems.Add(new MenuItem("CHECK_UPDATES", CheckUpdates));
            helpMenu.MenuItems.Add(new MenuItem("DOCUMENTATION", OpenDocsOnline));
            helpMenu.MenuItems.Add(new MenuItem("ABOUT", () =>
            {
                AboutWindow.Opened = !AboutWindow.Opened;
            }));
            helpMenu.MenuItems.Add(new MenuItem(""));
            helpMenu.MenuItems.Add(new MenuItem("DONATE", WebUtil.OpenDonation));



            MenuItems.Add(fileMenu);
            MenuItems.Add(saveConfigMenu);
            MenuItems.Add(settingsMenu);
            MenuItems.Add(windowsMenu);
            MenuItems.Add(pluginsMenu);
            MenuItems.Add(helpMenu);

            SaveMenu.Enabled = false;
            SaveAsMenu.Enabled = false;
        }

        private void CheckUpdates()
        {
            try
            {
                UpdaterHelper.Setup("MapStudioProject", "CTR-Studio", "Version.txt", "CTRStudio.exe");

                var release = UpdaterHelper.TryGetLatest(Runtime.ExecutableDir, 0);
                if (release == null)
                    TinyFileDialog.MessageBoxInfoOk($"Build is up to date with the latest repo!");
                else
                {
                    int result = TinyFileDialog.MessageBoxInfoYesNo($"Found new release {release.Name}! Do you want to update?");
                    if (result == 1)
                    {
                        ProcessLoading.Instance.IsLoading = true;
                        //Download
                        UpdaterHelper.DownloadRelease(Runtime.ExecutableDir, release, 0, () =>
                        {
                            ProcessLoading.Instance.Update(100, 100, $"Update will now install.", "Updater");

                            Console.WriteLine("Installing update..");
                            //Exit the tool and install via the updater
                            UpdaterHelper.InstallUpdate("-b");

                            ProcessLoading.Instance.IsLoading = false;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message.Replace("'", "");

                Clipboard.SetText($"{ex.Message} \n{ex.StackTrace}");
                TinyFileDialog.MessageBoxErrorOk($"Failed to update tool! {message} Details copied to clipboard!");
            }
        }

        private void OpenDocsOnline() => WebUtil.OpenURL("https://github.com/MapStudioProject/CTR-Studio/wiki");

        private void OpenDocsOffline()
        {
        }

        private void LoadSettingsWindow()
        {
            SettingsWindow.Opened = !SettingsWindow.Opened;
        }

        private void LoadPluginMenus()
        {
            foreach (var plugin in PluginManager.LoadPlugins())
            {
                ImGui.Text(plugin.PluginHandler.Name);
            }
        }

        private void LoadRecentFiles()
        {
            foreach (var file in RecentFiles)
            {
                if (ImGui.Selectable(file))
                    UIManager.ActionExecBeforeUIDraw = () => { LoadFileFormat(file); };
            }
        }

        private void LoadFileConfigMenu()
        {
            if (Workspace.ActiveWorkspace != null)
                Workspace.ActiveWorkspace.RenderFileSaveSettings();
        }

        private void LoadEditMenu()
        {
            if (Workspace.ActiveWorkspace != null)
            {
                foreach (var item in Workspace.ActiveWorkspace.GetEditMenuItems())
                    MapStudio.UI.ImGuiHelper.LoadMenuItem(item, false);
            }
        }

        private void SaveProject()
        {
            var workspace = Workspace.ActiveWorkspace;

            var settings = GlobalSettings.Current;
            string dir = Path.Combine(settings.Program.ProjectDirectory, workspace.Name);

            workspace.SaveProject(dir);

            RecentFileHandler.SaveRecentFile(dir, "RecentProjects.txt", this.RecentProjects);
        }

        private void SaveProjectWithDialog()
        {
            var workspace = Workspace.ActiveWorkspace;
            //Make project name from the folder it is inside of.
            string projectName = "New Project";
            if (Directory.Exists(workspace.Resources.ProjectFolder))
                projectName = new DirectoryInfo(workspace.Resources.ProjectFolder).Name;
            //Create a save dialog
            ProjectSaveDialog projectDialog = new ProjectSaveDialog(projectName);

            DialogHandler.Show("Save Project", 500, 100, () =>
            {
                projectDialog.LoadUI();
            }, (result) =>
            {
                if (!result)
                    return;

                workspace.SaveProject(projectDialog.GetProjectDirectory());
                RecentFileHandler.SaveRecentFile(projectDialog.GetProjectDirectory(), "RecentProjects.txt", this.RecentProjects);
            });
        }

        private void LoadWindowMenu()
        {
            if (ImGui.MenuItem(TranslationSource.GetText("RESET")))
            {
                if (Workspace.ActiveWorkspace != null)
                    Workspace.ActiveWorkspace.ReloadDefaultDockSettings();
            }
            bool isFullscreen = _window.WindowState == OpenTK.WindowState.Fullscreen;
            if (ImGui.MenuItem(TranslationSource.GetText("FULL_SCREEN"), "", isFullscreen))
                _window.WindowState = isFullscreen ? OpenTK.WindowState.Normal : OpenTK.WindowState.Fullscreen;

            //Docked windows
            if (Workspace.ActiveWorkspace != null)
            {
                foreach (var window in Workspace.ActiveWorkspace.DockedWindows)
                {
                    if (ImGui.MenuItem(TranslationSource.GetText(window.Name), "", window.Opened))
                        window.Opened = window.Opened ? false : true;
                }
            }

#if DEBUG
            if (ImGui.MenuItem($"{TranslationSource.GetText("STYLE_EDITOR")}", "", showStyleEditor))
                showStyleEditor = showStyleEditor ? false : true;
            if (ImGui.MenuItem($"Demo Window", "", showDemoWindow))
                showDemoWindow = showDemoWindow ? false : true;
#endif

            if (Workspace.ActiveWorkspace != null)
            {
                foreach (var window in Workspace.ActiveWorkspace.Windows)
                {
                    if (ImGui.MenuItem(window.Name, "", window.Opened))
                    {
                        window.Opened = window.Opened ? false : true;
                    }
                }
            }
        }

        private void LoadPluginsMenu()
        {
            foreach (var config in PluginSettingsUI)
                config.DrawUI();
        }

        private void OpenProject()
        {
            var projectList = new ProjectList();
            DialogHandler.Show("", 450, 600, () =>
            {
                projectList.Render();
            }, (e) =>
            {
                if (e)
                {
                    UIManager.ActionExecBeforeUIDraw = () => {
                        LoadFileFormat(Path.Combine(projectList.SelectedProject, "Project.json"));
                    };
                }
            });
        }

        private void LoadRecentProjects()
        {
            foreach (var project in RecentProjects)
            {
                DisplayRecentProject(project);
            }
        }

        private void DisplayRecentProject(string folder)
        {
            string thumbFile = Path.Combine(folder, "Thumbnail.png");
            string projectFile = Path.Combine(folder, "Project.json");
            string projectName = new DirectoryInfo(folder).Name;

            if (!File.Exists(projectFile))
                return;

            //Get the thumbnail for the project.
            var icon = IconManager.GetTextureIcon("BLANK");
            if (File.Exists(thumbFile))
            {
                IconManager.LoadTextureFile(thumbFile, 64, 64);
                icon = IconManager.GetTextureIcon(thumbFile);
            }

            //Make the whole menu item region selectable
            var width = ImGui.CalcItemWidth();
            var size = new System.Numerics.Vector2(width, 64);

            //Make the entire region selectable. Need to move back for drawing image/text over
            var pos = ImGui.GetCursorPos();
            bool isSelected = ImGui.Selectable($"##{folder}", false, ImGuiSelectableFlags.None, size);
            ImGui.SetCursorPos(pos);

            //Load an icon preview of the project
            ImGui.AlignTextToFramePadding();
            ImGui.Image((IntPtr)icon, new System.Numerics.Vector2(64, 64));
            ImGui.SameLine();
            //Project name
            ImGui.SameLine();
            ImGui.Text(projectName);
            //Load file when clicked on
            if (isSelected)
                UIManager.ActionExecBeforeUIDraw = () => { LoadFileFormat(projectFile); };
        }

        private void LoadNewFileMenu()
        {
            foreach (var file in UIManager.CreateNewEditors)
            {
                if (ImGui.Selectable(file.Key))
                    UIManager.ActionExecBeforeUIDraw = () => { CreateNewProject(file.Value); };
            }
        }

        private void CreateNewProject(Type editor)
        {
            var workspace = new Workspace(GlobalSettings, GetWorkspaceID());
            workspace.CreateNewProject(editor, (bool created) =>
            {
                if (created)
                    Workspaces.Add(workspace);
                else
                {
                    Workspace.ActiveWorkspace = null;
                    if (Workspaces.Count > 0)
                        Workspace.ActiveWorkspace = Workspaces.FirstOrDefault();
                }

                ProcessLoading.Instance.IsLoading = false;
                ForceFocus = true;
            });
            OnWorkspaceChanged();
        }

        private void OpenFileWithDialog()
        {
            ImguiFileDialog ofd = new ImguiFileDialog();
            if (ofd.ShowDialog("OPEN_FILE"))
            {
                foreach (var file in ofd.FilePaths)
                    LoadFileFormat(file);
            }
        }

        #endregion

        private string GetWorkspaceID()
        {
            return Utils.RenameDuplicateString("Space", Workspaces.Select(x => x.ID).ToList());
        }

        private void LoadFileFormat(string filePath)
        {
            ForceFocus = true;

            //Check if the format is supported in the current editors.
            string ext = Path.GetExtension(filePath);
            bool isProject = ext == ".json";
            ProcessLoading.Instance.IsLoading = true;

            //Load asset based format
            if (!isProject)
            {
                bool createNew = true;

                Workspace workspace = Workspace.ActiveWorkspace;
                if (createNew || workspace == null)
                {
                    workspace = new Workspace(this.GlobalSettings, GetWorkspaceID());
                    createNew = true;
                }

                bool loaded = workspace.LoadFileFormat(filePath) != null;
                if (!loaded)
                {
                    ProcessLoading.Instance.IsLoading = false;
                    ForceFocus = true;
                    //Remove active workspace when none of them are present.
                    if (Workspaces.Count == 0)
                        Workspace.ActiveWorkspace = null;
                    else
                        Workspace.ActiveWorkspace = Workspaces.LastOrDefault();

                    return;
                }
                if (createNew)
                    Workspaces.Add(workspace);

                RecentFileHandler.SaveRecentFile(filePath, "Recent.txt", this.RecentFiles);
            }
            else
            {
                //Load project format
                var workspace = new Workspace(GlobalSettings, GetWorkspaceID());
                bool initialized = workspace.LoadProjectFile(filePath);
                if (!initialized)
                    return;

                Workspaces.Add(workspace);
            }

            ProcessLoading.Instance.IsLoading = false;
            ForceFocus = true;

            OnWorkspaceChanged();
        }

        private void OnWorkspaceChanged()
        {
            bool canSave = Workspace.ActiveWorkspace != null;
            SaveMenu.Enabled = canSave;
            SaveAsMenu.Enabled = canSave;
            // SaveProjectMenu.Enabled = canSave;
            //  SaveAsProjectMenu.Enabled = canSave;
            UpdateDockLayout = true;
        }

        private void ClearWorkspace()
        {
            //No workspaces. Skip
            if (Workspaces.Count == 0)
                return;

            //Notify before removing
            int result = TinyFileDialog.MessageBoxInfoYesNo(TranslationSource.GetText("CLEAR_NOTIFY"));
            if (result != 1)
                return;

            //Dispose resources
            foreach (var workspace in Workspaces)
                workspace.Dispose();

            Workspaces.Clear();
            GC.Collect();

            //Remove active
            Workspace.ActiveWorkspace = null;
            OnWorkspaceChanged();
        }

        private void RemoveWorkspaces(List<Workspace> workspaces)
        {
            foreach (var workspace in workspaces)
            {
                Workspaces.Remove(workspace);
                workspace.Dispose();
            }
            if (Workspaces.Count == 0)
                Workspace.ActiveWorkspace = null;

            OnWorkspaceChanged();
        }

        public override void OnFileDrop(string filePath)
        {
            if (Workspace.ActiveWorkspace != null && Workspace.ActiveWorkspace.ActiveEditor.OnFileDrop(filePath))
                return;

             LoadFileFormat(filePath);
        }

        public override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            var state = InputState.CreateKeyState();
            KeyEventInfo.State = state;

            //Make sure the key cannot be repeated when held down
            if (!e.IsRepeat)
            {
                //Save As shortcut
                if (state.KeyAlt && state.KeyCtrl && e.Key == Key.S)
                {
                    Workspace.ActiveWorkspace?.SaveFileWithDialog();
                    return;
                }
                //Save shortcut
                else if (state.KeyCtrl && e.Key == Key.S)
                {
                    Workspace.ActiveWorkspace?.SaveFileData();
                    return;
                }
                //Open shortcut
                if (state.KeyCtrl && e.Key == Key.O)
                {
                    OpenFileWithDialog();
                    return;
                }
            }

            Workspace.ActiveWorkspace?.OnKeyDown(state, e.IsRepeat);
        }

        public override void OnFocusedChanged()
        {
            base.OnFocusedChanged();

            if (Workspace.ActiveWorkspace != null)
                Workspace.ActiveWorkspace.OnApplicationEnter();
        }

        public override void OnClosing(CancelEventArgs e)
        {
            //Check if there is opened workspaces in the tool
            if (Workspaces.Count > 0)
            {
                int result = TinyFileDialog.MessageBoxInfoYesNo(TranslationSource.GetText("EXIT_NOTIFY"));
                if (result != 1)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnClosing(e);
        }
    }
}