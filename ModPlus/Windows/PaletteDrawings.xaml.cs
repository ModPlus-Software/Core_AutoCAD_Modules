namespace ModPlus.Windows
{
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using Autodesk.AutoCAD.ApplicationServices;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    // ReSharper disable once InconsistentNaming
    internal partial class PaletteDrawings 
    {
        // Переменные
        private readonly DocumentCollection _docs = AcApp.DocumentManager;
        
        internal PaletteDrawings()
        {
            InitializeComponent();
            ModPlusStyle.ThemeManager.ChangeTheme(Resources, "LightBlue");
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
            Loaded += MpPaletteDrawings_Loaded;
        }

        private void MpPaletteDrawings_Loaded(object sender, RoutedEventArgs e)
        {
            // Подключение обработчиков событий для создания и закрытия чертежей
            AcApp.DocumentManager.DocumentCreated += DocumentManager_DocumentCreated;
            AcApp.DocumentManager.DocumentDestroyed += DocumentManager_DocumentDestroyed;
            AcApp.DocumentManager.DocumentActivated += DocumentManager_DocumentActivated;

            GetDocuments();
        }
        
        // документ стал активным
        private void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            GetDocuments();
            CheckUnused();
        }

        private void GetDocuments()
        {
            Drawings.SelectionChanged -= Drawings_SelectionChanged;
            try
            {
                Drawings.Items.Clear();
                foreach (Document doc in _docs)
                {
                    var lbi = new ListBoxItem();
                    var filename = Path.GetFileName(doc.Name);
                    lbi.Content = filename;
                    lbi.ToolTip = doc.Name;
                    Drawings.Items.Add(lbi);
                    Drawings.SelectedItem = lbi;
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                Drawings.SelectionChanged += Drawings_SelectionChanged;
            }
        }

        private void CheckUnused()
        {
            try
            {
                foreach (ListBoxItem item in Drawings.Items)
                {
                    if (item.Content.Equals(Path.GetFileName(_docs.MdiActiveDocument?.Name)) &
                        item.ToolTip.Equals(_docs.MdiActiveDocument?.Name))
                        Drawings.SelectedItem = item;
                }
            }
            catch
            {
                // ignored
            }
        }
        
        // Чертеж закрыт
        private void DocumentManager_DocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            GetDocuments();
            CheckUnused();
        }

        // Документ создан/открыт
        private void DocumentManager_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            GetDocuments();
            CheckUnused();
        }

        // Выбор чертежа в списке открытых
        private void Drawings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var lbi = (ListBoxItem)Drawings.SelectedItem;
                foreach (
                    var doc in
                    from Document doc
                        in _docs
                    let filename = Path.GetFileName(doc.Name)
                    where doc.Name == lbi.ToolTip.ToString() & filename == lbi.Content.ToString()
                    select doc)
                {
                    if (_docs.MdiActiveDocument != null && _docs.MdiActiveDocument != doc)
                    {
                        _docs.MdiActiveDocument = doc;
                    }

                    break;
                }
            }
            catch
            {
                // ignored
            }
        }

        // Нажатие кнопки закрытия чертежа
        private void BtCloseDwg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Drawings.SelectedIndex != -1)
                {
                    var lbi = (ListBoxItem)Drawings.SelectedItem;
                    foreach (var doc in _docs.Cast<Document>().Where(doc => doc.Name == lbi.ToolTip.ToString()))
                    {
                        if (_docs.MdiActiveDocument == doc)
                        {
                            AcApp.DocumentManager.
                                MdiActiveDocument.SendStringToExecute("_CLOSE ", true, false, false);
                        }

                        break;
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void BtAddDrawing_OnClick(object sender, RoutedEventArgs e)
        {
            if (_docs.Count > 0)
                AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute("_NEW ", true, false, false);
        }

        private void BtOpenDrawing_OnClick(object sender, RoutedEventArgs e)
        {
            if (_docs.Count > 0)
                AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute("_OPEN ", true, false, false);
        }
    }
}
