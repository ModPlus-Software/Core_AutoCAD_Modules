﻿namespace ModPlus
{
    using System;
    using System.Drawing;
    using System.Reflection;
    using System.Windows.Forms.Integration;
    using Windows;
    using Autodesk.AutoCAD.Runtime;
    using Autodesk.AutoCAD.Windows;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <summary>Методы создания и работы с палитрой ModPlus</summary>
    public static class MpPalette
    {
        private const string LangItem = "AutocadDlls";
        public static PaletteSet MpPaletteSet;
        [CommandMethod("mpPalette")]
        public static void CreatePalette()
        {
            try
            {
                if (MpPaletteSet == null)
                {
                    MpPaletteSet = new PaletteSet(Language.GetItem(LangItem, "h48"), "mpPalette", new Guid("A9C907EF-6281-4FA2-9B6C-E0401E41BB76"));
                    MpPaletteSet.Load += _mpPaletteSet_Load;
                    MpPaletteSet.Save += _mpPaletteSet_Save;
                    AddRemovePaletts();
                    MpPaletteSet.Icon = GetEmbeddedIcon("ModPlus.Resources.mpIcon.ico");
                    MpPaletteSet.Style =
                        PaletteSetStyles.ShowPropertiesMenu |
                        PaletteSetStyles.ShowAutoHideButton |
                        PaletteSetStyles.ShowCloseButton;
                    MpPaletteSet.MinimumSize = new Size(100, 300);
                    MpPaletteSet.DockEnabled = DockSides.Left | DockSides.Right;
                    MpPaletteSet.RecalculateDockSiteLayout();
                    MpPaletteSet.Visible = true;
                }
                else
                {
                    MpPaletteSet.Visible = true;
                }
            }
            catch (System.Exception exception) { ExceptionBox.Show(exception); }
        }

        private static void AddRemovePaletts()
        {
            if (MpPaletteSet == null) return;
            try
            {
                var funName = Language.GetItem(LangItem, "h19");
                var drwName = Language.GetItem(LangItem, "h20");
                // functions
                if (ModPlusAPI.Variables.FunctionsInPalette)
                {
                    var hasP = false;
                    foreach (Palette p in MpPaletteSet)
                    {
                        if (p.Name.Equals(funName)) hasP = true;
                    }
                    if (!hasP)
                    {
                        var palette = new mpPaletteFunctions();
                        var host = new ElementHost
                        {
                            AutoSize = true,
                            Dock = System.Windows.Forms.DockStyle.Fill,
                            Child = palette
                        };
                        MpPaletteSet.Add(funName, host);
                    }
                }
                else
                {
                    for (var i = 0; i < MpPaletteSet.Count; i++)
                    {
                        if (MpPaletteSet[i].Name.Equals(funName))
                        {
                            MpPaletteSet.Remove(i);
                            break;
                        }
                    }
                }
                // drawings
                if (ModPlusAPI.Variables.DrawingsInPalette)
                {
                    var hasP = false;
                    foreach (Palette p in MpPaletteSet)
                    {
                        if (p.Name.Equals(drwName)) hasP = true;
                    }
                    if (!hasP)
                    {
                        var palette = new mpPaletteDrawings();
                        var host = new ElementHost
                        {
                            AutoSize = true,
                            Dock = System.Windows.Forms.DockStyle.Fill,
                            Child = palette
                        };
                        MpPaletteSet.Add(drwName, host);
                    }
                }
                else
                {
                    for (var i = 0; i < MpPaletteSet.Count; i++)
                    {
                        if (MpPaletteSet[i].Name.Equals(drwName))
                        {
                            MpPaletteSet.Remove(i);
                            break;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private static void _mpPaletteSet_Save(object sender, PalettePersistEventArgs e)
        {
            // ReSharper disable once UnusedVariable
            var a = (double)e.ConfigurationSection.ReadProperty("ModPlusPalette", 22.3);
        }

        private static void _mpPaletteSet_Load(object sender, PalettePersistEventArgs e)
        {
            e.ConfigurationSection.WriteProperty("ModPlusPalette", 32.3);
        }

        private static Icon GetEmbeddedIcon(string sName)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(sName));
        }
    }
}