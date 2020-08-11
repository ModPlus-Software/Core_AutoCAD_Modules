namespace ModPlus.Windows.MiniPlugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Xml.Linq;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Windows;
    using global::ModPlus.MiniPlugins;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Visibility = System.Windows.Visibility;

    public partial class FastBlocksSettings
    {
        private static string _langItem = "AutocadDlls";
        private List<FastBlock> _fastBlocks;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastBlocksSettings"/> class.
        /// </summary>
        public FastBlocksSettings()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(_langItem, "h29");
            Loaded += FastBlocksSettings_Loaded;
            Closed += FastBlocksSettings_Closed;
        }

        private static void FastBlocksSettings_Closed(object sender, EventArgs e)
        {
            // off/on menu
            var fastBlocksContextMenu = !bool.TryParse(UserConfigFile.GetValue("FastBlocksCM"), out var b) || b;
            if (fastBlocksContextMenu)
            {
                global::ModPlus.MiniPlugins.MiniFunctionsContextMenuExtensions.FastBlockContextMenu.Detach();
                global::ModPlus.MiniPlugins.MiniFunctionsContextMenuExtensions.FastBlockContextMenu.Attach();
            }
        }

        private void FastBlocksSettings_Loaded(object sender, RoutedEventArgs e)
        {
            _fastBlocks = new List<FastBlock>();
            LoadFromSettingsFile();
            LwFastBlocks.ItemsSource = _fastBlocks;
        }

        // save to settings file
        private void SaveToSettingsFile()
        {
            if (File.Exists(UserConfigFile.FullFileName))
            {
                var configXml = UserConfigFile.ConfigFileXml;
                if (configXml != null)
                {
                    var settingsXml = configXml.Element("Settings");
                    if (settingsXml != null)
                    {
                        var fastBlocksXml = settingsXml.Element("mpFastBlocks");
                        if (fastBlocksXml == null)
                        {
                            fastBlocksXml = new XElement("mpFastBlocks");
                            settingsXml.Add(fastBlocksXml);
                        }
                        else
                        {
                            fastBlocksXml.RemoveAll(); // cleanUp
                        }

                        // add
                        foreach (var fb in _fastBlocks)
                        {
                            var fbXml = new XElement("FastBlock");
                            fbXml.SetAttributeValue("Name", fb.Name);
                            fbXml.SetAttributeValue("BlockName", fb.BlockName);
                            fbXml.SetAttributeValue("File", fb.File);
                            fastBlocksXml.Add(fbXml);
                        }
                    }

                    // Save
                    configXml.Save(UserConfigFile.FullFileName);
                }
            }
            else
            {
                ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(_langItem, "err4"), MessageBoxIcon.Close);
            }
        }

        // load from settings file
        private void LoadFromSettingsFile()
        {
            if (File.Exists(UserConfigFile.FullFileName))
            {
                var configXml = UserConfigFile.ConfigFileXml;
                var settingsXml = configXml?.Element("Settings");
                var fastBlocksXml = settingsXml?.Element("mpFastBlocks");
                if (fastBlocksXml != null)
                {
                    _fastBlocks.Clear();
                    foreach (var fbXml in fastBlocksXml.Elements("FastBlock"))
                    {
                        var fb = new FastBlock
                        {
                            Name = fbXml.Attribute("Name")?.Value,
                            BlockName = fbXml.Attribute("BlockName")?.Value,
                            File = fbXml.Attribute("File")?.Value
                        };
                        _fastBlocks.Add(fb);
                    }
                }
            }
            else
            {
                ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(_langItem, "err4"), MessageBoxIcon.Close);
            }
        }

        private void LwFastBlocks_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lw)
                BtRemoveBlock.IsEnabled = lw.SelectedIndex != -1;
        }

        // remove item from list
        private void BtRemoveBlock_OnClick(object sender, RoutedEventArgs e)
        {
            if (LwFastBlocks.SelectedIndex != -1)
            {
                if (ModPlusAPI.Windows.MessageBox.ShowYesNo(ModPlusAPI.Language.GetItem(_langItem, "err5"), MessageBoxIcon.Question))
                {
                    var selectedItem = LwFastBlocks.SelectedItem as FastBlock;
                    _fastBlocks.Remove(selectedItem);

                    // save
                    SaveToSettingsFile();

                    // reload
                    LwFastBlocks.ItemsSource = null;
                    LwFastBlocks.ItemsSource = _fastBlocks;
                }
            }
        }

        // add item
        private void BtAddNewBlock_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fastBlocks.Count < 10)
                {
                    var ofd = new OpenFileDialog(ModPlusAPI.Language.GetItem(_langItem, "err6"), string.Empty, "dwg", string.Empty,
                        OpenFileDialog.OpenFileDialogFlags.NoFtpSites | OpenFileDialog.OpenFileDialogFlags.NoUrls);
                    Topmost = false;
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        var db = new Database(false, true);
                        db.ReadDwgFile(ofd.Filename, FileShare.Read, true, string.Empty);
                        var blocks = new List<string>();
                        using (var tr = db.TransactionManager.StartTransaction())
                        {
                            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                            blocks.AddRange(from ObjectId id in bt
                                            select (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead)
                                into btRecord
                                            where !btRecord.IsLayout & !btRecord.IsAnonymous
                                            select btRecord.Name);
                        }

                        var validateNames = _fastBlocks.Select(fastBlock => fastBlock.Name).ToList();
                        var fbs = new FastBlockSelection(validateNames)
                        {
                            LbBlocks = { ItemsSource = blocks }
                        };
                        if (fbs.ShowDialog() == true)
                        {
                            var fb = new FastBlock
                            {
                                Name = fbs.TbBlockName.Text,
                                File = ofd.Filename,
                                BlockName = fbs.LbBlocks.SelectedItem.ToString()
                            };
                            _fastBlocks.Add(fb);

                            // save
                            SaveToSettingsFile();

                            // reload
                            LwFastBlocks.ItemsSource = null;
                            LwFastBlocks.ItemsSource = _fastBlocks;
                        }
                    }

                    Topmost = true;
                }
                else
                {
                    ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(_langItem, "h33"), MessageBoxIcon.Alert);
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private class FastBlock
        {
            public string Name { get; set; }

            public string File { get; set; }

            public string BlockName { get; set; }

            public Visibility FileAsBlockVisibility { get; set; }
        }
    }
}
