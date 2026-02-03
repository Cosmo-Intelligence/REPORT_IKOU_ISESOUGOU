using System;
using System.IO;
using System.Windows;

namespace WpfFolderApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            // We use the full namespace to avoid conflicts with WPF's 'Window' class
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select a folder";
                dialog.ShowNewFolderButton = true;

                // Set the default directory to start in (optional)
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                // Show the dialog
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    // Update the WPF TextBox with the path
                    txtFolderPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string path = txtFolderPath.Text;

            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Please select a folder path.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(path))
            {
                MessageBox.Show("The path entered does not exist.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Success Logic
            MessageBox.Show($"Action Confirmed for: {path}", "Success");
        }
    }
}