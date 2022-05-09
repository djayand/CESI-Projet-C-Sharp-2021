using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Projet_ProgSys.ViewModel;
using Ookii.Dialogs.Wpf;
using Projet_ProgSys.Model;

namespace Projet_ProgSys_Graphical
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PromptEditBackup : Window
    {
        public PromptEditBackup(BackupModel backup)
        {
            InitializeComponent();
            NameAnswer.Text = backup.Name;
            SourceAnswer.Text = backup.SourcePath;
            DestinationAnswer.Text = backup.DestinationPath;
            TypeAnswer.SelectedIndex = (int)(BackupModel.BackupTypes)backup.BackupType;
        }

        public void ClickAddBackup(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void SelectSourcePath(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                SourceAnswer.Text = dialog.SelectedPath;
            }

        }

        private void SelectDestinationPath(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                DestinationAnswer.Text = dialog.SelectedPath;
            }

        }

        public BackupModel Answer
        {
            get { return new BackupModel(NameAnswer.Text, SourceAnswer.Text, DestinationAnswer.Text, (BackupModel.BackupTypes)TypeAnswer.SelectedIndex, "STOPPED"); }
        }

        private void DestinationAnswer_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(DestinationAnswer.Text == SourceAnswer.Text)
            {
                OK.IsEnabled = false;
            } 
            else
            {
                OK.IsEnabled = true;
            }
        }
    }
}
