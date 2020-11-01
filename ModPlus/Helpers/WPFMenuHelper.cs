namespace ModPlus.Helpers
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    internal static class WPFMenuHelper
    {
        public static Button AddButton(
            FrameworkElement sourceWindow, string name,
            string lname, string img32, string description, string fullDescription, string helpImage,
            bool statTextWidth)
        {
            var brd = new Border
            {
                Padding = new Thickness(1),
                Margin = new Thickness(1),
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            try
            {
                var img = new Image
                {
                    Source = new BitmapImage(new Uri(img32, UriKind.RelativeOrAbsolute)),
                    Stretch = Stretch.Uniform,
                    Width = 32,
                    Height = 32
                };
                brd.Child = img;
            }
            catch
            {
                // ignored
            }

            var txt = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Text = lname,
                Margin = new Thickness(3, 0, 1, 0)
            };
            if (statTextWidth)
                txt.Width = 150;
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.Children.Add(brd);
            grid.Children.Add(txt);
            brd.SetValue(Grid.ColumnProperty, 0);
            txt.SetValue(Grid.ColumnProperty, 1);
            var btn = new Button
            {
                Name = name,
                Content = grid,
                ToolTip = AddTooltip(description, fullDescription, helpImage),
                Margin = new Thickness(1),
                Padding = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            btn.Click += CommandButtonClick;

            return btn;
        }

        private static ToolTip AddTooltip(string description, string fullDescription, string imgUri)
        {
            var tt = new ToolTip();
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            var txtDescription = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500,
                Text = description,
                Margin = new Thickness(2)
            };
            stackPanel.Children.Add(txtDescription);
            var txtFullDescription = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 500,
                Text = fullDescription,
                Margin = new Thickness(2)
            };
            if (!string.IsNullOrEmpty(fullDescription))
                stackPanel.Children.Add(txtFullDescription);
            try
            {
                if (!string.IsNullOrEmpty(imgUri))
                {
                    var img = new Image
                    {
                        Source = new BitmapImage(new Uri(imgUri, UriKind.RelativeOrAbsolute)),
                        Stretch = Stretch.Uniform,
                        MaxWidth = 350
                    };
                    stackPanel.Children.Add(img);
                }
            }
            catch
            {
                // ignored
            }

            tt.Content = stackPanel;
            return tt;
        }

        // Обработка запуска функций        
        private static void CommandButtonClick(object sender, RoutedEventArgs e)
        {
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute(
                $"_{((Button)sender).Name} ",
                false, false, false);
        }
    }
}