﻿using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    /// <summary>
    /// Interaction logic for SaveImageAsDialog.xaml
    /// </summary>
    public partial class SaveImageAsDialog : System.Windows.Window
    {
        public SaveImageAsDialog() {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, Click_close));
        }

        private void Click_close(object sender, System.Windows.RoutedEventArgs e) {
            Close();
        }
    }
}
