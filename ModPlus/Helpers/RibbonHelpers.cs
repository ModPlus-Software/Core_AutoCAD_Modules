namespace ModPlus.Helpers
{
    using System;
    using System.Linq;
    using System.Windows.Controls;
    using Autodesk.Windows;
    using JetBrains.Annotations;
    using ModPlusAPI;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    /// <summary>
    /// Utils for Ribbon creation
    /// </summary>
    [PublicAPI]
    public static class RibbonHelpers
    {
        /// <summary>Создание маленькой кнопки</summary>
        /// <param name="fName">Название функции (= параметр запуска функции)</param>
        /// <param name="lName">Локальное название функции</param>
        /// <param name="img">Иконка</param>
        /// <param name="description">Описание функции</param>
        /// <param name="fullDescription">Полное описание</param>
        /// <param name="helpImage">Имя файла картинки для ToolTip. Файл должен располагаться в каталоге /Resources/Help проекта</param>
        /// <returns></returns>
        public static RibbonButton AddSmallButton(string fName, string lName, string img, string description, string fullDescription, string helpImage/*, string helpLink*/)
        {
            try
            {
                var tt = new RibbonToolTip
                {
                    IsHelpEnabled = true,
                    Content = description,
                    Command = fName,
                    IsProgressive = true,
                    HelpTopic = Language.RusWebLanguages.Contains(Language.CurrentLanguageName)
                        ? $"https://modplus.org/ru/autocadplugins/{fName.ToLower()}"
                        : $"https://modplus.org/en/autocadplugins/{fName.ToLower()}"
                };

                return GetSmallRibbonButton(fName, lName, img, fullDescription, helpImage, tt);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Создание маленькой кнопки</summary>
        /// <param name="fName">Название функции (= параметр запуска функции)</param>
        /// <param name="lName">Локальное название функции</param>
        /// <param name="img">Иконка</param>
        /// <param name="description">Описание функции</param>
        /// <param name="fullDescription">Полное описание</param>
        /// <param name="helpImage">Имя файла картинки для ToolTip. Файл должен располагаться в каталоге /Resources/Help проекта</param>
        /// <param name="helpLink">Ссылка онлайн-справки, не включающая https://modplus.org/. Если null, то справка строится по имени функции</param>
        /// <returns></returns>
        public static RibbonButton AddSmallButton(string fName, string lName, string img, string description, string fullDescription, string helpImage, string helpLink)
        {
            try
            {
                var tt = new RibbonToolTip
                {
                    IsHelpEnabled = true,
                    Content = description,
                    Command = fName,
                    IsProgressive = true
                };
                if (string.IsNullOrEmpty(helpLink))
                {
                    tt.HelpTopic = Language.RusWebLanguages.Contains(Language.CurrentLanguageName)
                        ? $"https://modplus.org/ru/autocadplugins/{fName.ToLower()}"
                        : $"https://modplus.org/en/autocadplugins/{fName.ToLower()}";
                }
                else
                {
                    tt.HelpTopic = Language.RusWebLanguages.Contains(Language.CurrentLanguageName)
                        ? $"https://modplus.org/ru/{helpLink.ToLower()}"
                        : $"https://modplus.org/en/{helpLink.ToLower()}";
                }

                return GetSmallRibbonButton(fName, lName, img, fullDescription, helpImage, tt);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static RibbonButton GetSmallRibbonButton(string fName, string lName, string img, string fullDescription, string helpImage, RibbonToolTip tt)
        {
            if (!string.IsNullOrEmpty(fullDescription))
                tt.ExpandedContent = fullDescription;
            try
            {
                if (!string.IsNullOrEmpty(helpImage))
                    tt.ExpandedImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(helpImage, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                // ignored
            }

            var ribBtn = new RibbonButton
            {
                CommandParameter = tt.Command = fName,
                Name = tt.Title = lName,
                CommandHandler = new RibbonCommandHandler(),
                Orientation = Orientation.Horizontal,
                Size = RibbonItemSize.Standard,
                ShowImage = true,
                ShowText = false,
                ToolTip = tt
            };
            try
            {
                if (!string.IsNullOrEmpty(img))
                    ribBtn.Image = new System.Windows.Media.Imaging.BitmapImage(new Uri(img, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                // ignored
            }

            return ribBtn;
        }

        /// <summary>Создание большой кнопки</summary>
        /// <param name="fName">Название функции (= параметр запуска функции)</param>
        /// <param name="lName">Локальное название функции</param>
        /// <param name="img">Иконка</param>
        /// <param name="description">Описание функции</param>
        /// <param name="orientation">Ориентация кнопки: горизонтальная или вертикальная</param>
        /// <param name="fullDescription">Полное описание</param>
        /// <param name="helpImage">Имя файла картинки для ToolTip. Файл должен располагаться в каталоге /Resources/Help проекта</param>
        /// <returns></returns>
        public static RibbonButton AddBigButton(
            string fName, 
            string lName, 
            string img,
            string description,
            Orientation orientation,
            string fullDescription, 
            string helpImage)
        {
            try
            {
                var tt = new RibbonToolTip
                {
                    IsHelpEnabled = true,
                    Content = description,
                    Command = fName,
                    IsProgressive = true,
                    HelpTopic = Language.RusWebLanguages.Contains(Language.CurrentLanguageName)
                        ? $"https://modplus.org/ru/autocadplugins/{fName.ToLower()}"
                        : $"https://modplus.org/en/autocadplugins/{fName.ToLower()}"
                };
                return GetBigButton(fName, lName, img, orientation, fullDescription, helpImage, tt);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Создание большой кнопки</summary>
        /// <param name="fName">Название функции (= параметр запуска функции)</param>
        /// <param name="lName">Локальное название функции</param>
        /// <param name="img">Иконка</param>
        /// <param name="description">Описание функции</param>
        /// <param name="orientation">Ориентация кнопки: горизонтальная или вертикальная</param>
        /// <param name="fullDescription">Полное описание</param>
        /// <param name="helpImage">Имя файла картинки для ToolTip. Файл должен располагаться в каталоге /Resources/Help проекта</param>
        /// <param name="helpLink">Ссылка онлайн-справки, не включающая https://modplus.org/. Если null, то справка строится по имени функции</param>
        /// <returns></returns>
        public static RibbonButton AddBigButton(
            string fName,
            string lName,
            string img,
            string description,
            Orientation orientation,
            string fullDescription,
            string helpImage, 
            string helpLink)
        {
            try
            {
                var tt = new RibbonToolTip
                {
                    IsHelpEnabled = true,
                    Content = description,
                    Command = fName,
                    IsProgressive = true
                };
                if (string.IsNullOrEmpty(helpLink))
                {
                    tt.HelpTopic = Language.RusWebLanguages.Contains(Language.CurrentLanguageName)
                        ? $"https://modplus.org/ru/autocadplugins/{fName.ToLower()}"
                        : $"https://modplus.org/en/autocadplugins/{fName.ToLower()}";
                }
                else
                {
                    tt.HelpTopic = Language.RusWebLanguages.Contains(Language.CurrentLanguageName)
                        ? $"https://modplus.org/ru/{helpLink.ToLower()}"
                        : $"https://modplus.org/en/{helpLink.ToLower()}";
                }

                return GetBigButton(fName, lName, img, orientation, fullDescription, helpImage, tt);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static RibbonButton GetBigButton(string fName, string lName, string img, Orientation orientation, string fullDescription, string helpImage, RibbonToolTip tt)
        {
            if (!string.IsNullOrEmpty(fullDescription))
                tt.ExpandedContent = fullDescription;
            try
            {
                if (!string.IsNullOrEmpty(helpImage))
                    tt.ExpandedImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(helpImage, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                // ignored
            }

            var ribBtn = new RibbonButton
            {
                CommandParameter = tt.Command = fName,
                Name = tt.Title = lName,
                Text = ConvertLName(lName),
                CommandHandler = new RibbonCommandHandler(),
                Orientation = orientation,
                Size = RibbonItemSize.Large,
                ShowImage = true,
                ShowText = true,
                ToolTip = tt
            };
            try
            {
                if (!string.IsNullOrEmpty(img))
                {
                    ribBtn.LargeImage =
                        new System.Windows.Media.Imaging.BitmapImage(new Uri(img, UriKind.RelativeOrAbsolute));
                }
            }
            catch
            {
                // ignored
            }

            return ribBtn;
        }

        public static RibbonButton AddButton(
            string fName, 
            string lName,
            string img16,
            string img32,
            string description, 
            Orientation orientation,
            string fullDescription,
            string helpImage,
            string helpLink = null)
        {
            try
            {
                var tt = new RibbonToolTip
                {
                    IsHelpEnabled = true,
                    Content = description,
                    Command = fName,
                    IsProgressive = true
                };
                if (string.IsNullOrEmpty(helpLink))
                {
                    tt.HelpTopic = Language.RusWebLanguages.Contains(Language.CurrentLanguageName)
                        ? $"https://modplus.org/ru/autocadplugins/{fName.ToLower()}"
                        : $"https://modplus.org/en/autocadplugins/{fName.ToLower()}";
                }
                else
                {
                    tt.HelpTopic = Language.RusWebLanguages.Contains(Language.CurrentLanguageName)
                        ? $"https://modplus.org/ru/{helpLink.ToLower()}"
                        : $"https://modplus.org/en/{helpLink.ToLower()}";
                }

                if (!string.IsNullOrEmpty(fullDescription))
                    tt.ExpandedContent = fullDescription;
                try
                {
                    if (!string.IsNullOrEmpty(helpImage))
                        tt.ExpandedImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(helpImage, UriKind.RelativeOrAbsolute));
                }
                catch
                {
                    // ignored
                }

                var ribBtn = new RibbonButton
                {
                    CommandParameter = tt.Command = fName,
                    Name = tt.Title = lName,
                    Text = ConvertLName(lName),
                    CommandHandler = new RibbonCommandHandler(),
                    Orientation = orientation,
                    Size = RibbonItemSize.Large,
                    ShowImage = true,
                    ShowText = true,
                    ToolTip = tt
                };
                try
                {
                    if (!string.IsNullOrEmpty(img16))
                        ribBtn.Image = new System.Windows.Media.Imaging.BitmapImage(new Uri(img16, UriKind.RelativeOrAbsolute));
                    if (!string.IsNullOrEmpty(img32))
                        ribBtn.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(img32, UriKind.RelativeOrAbsolute));
                }
                catch
                {
                    // ignored
                }

                return ribBtn;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public class RibbonCommandHandler : System.Windows.Input.ICommand
        {
            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                if (parameter is RibbonButton)
                {
                    var button = (RibbonButton)parameter;
                    AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute(
                        button.CommandParameter + " ", true, false, true);
                }
            }
        }

        private static string ConvertLName(string lName)
        {
            if (!lName.Contains(" "))
                return lName;
            if (lName.Length <= 8)
                return lName;
            if (lName.Count(x => x == ' ') == 1)
            {
                return lName.Split(' ')[0] + Environment.NewLine + lName.Split(' ')[1];
            }

            var center = lName.Length * 0.5;
            var nearestDelta = lName.Select((c, i) => new { index = i, value = c }).Where(w => w.value == ' ')
                .OrderBy(x => Math.Abs(x.index - center)).First().index;
            return lName.Substring(0, nearestDelta) + Environment.NewLine + lName.Substring(nearestDelta + 1);
        }
    }
}
