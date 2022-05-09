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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Projet_ProgSys.ViewModel;
using Projet_ProgSys.Model;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Resources;
using System.Linq;
using System.Windows.Markup;

namespace Projet_ProgSys_Graphical
{
    /// <summary>
    /// Logique d'interaction pour Page1.xaml
    /// </summary>
    public partial class CryptPage : Page
    {
        private static readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
        public CryptPage()
        {
            InitializeComponent();
            List<String> ListOfExtensions = DatabaseViewModel.ListExtensions("extensions");
            foreach (string extension in ListOfExtensions)
            {
                ExtensionsList.Items.Add(extension);
            }

            var MyIni = new IniFile("Settings.ini");
            KeyAnswer.Text = MyIni.Read("EncryptionKey");
        }

        public void NewExtensionWindow(object sender, RoutedEventArgs e)
        {
            PromptExtension window1 = new PromptExtension();
            if (window1.ShowDialog() == true)
            {
                string extension = window1.Answer;
                if (!DatabaseViewModel.VerifyExtension(extension, "extensions"))
                {
                    DatabaseViewModel.AddExtension(extension, "extensions");
                    ExtensionsList.Items.Add(extension);
                }
            }
        }


        public void ClickRemoveExtension(object sender, RoutedEventArgs e)
        {
            var extension = ExtensionsList.SelectedItem;
            if (extension != null)
            {
                PromptVerification window1 = new PromptVerification();
                if (window1.ShowDialog() == true)
                {
                  DatabaseViewModel.DeleteExtension(extension.ToString(), "extensions");
                  ExtensionsList.Items.Remove(extension.ToString());

                }
            }
        }

        public void ClickUploadKey(object sender, RoutedEventArgs e)
        {
            string key = KeyAnswer.Text;
            if (key != null)
            {
                var MyIni = new IniFile("Settings.ini");
                MyIni.Write("EncryptionKey", key);
            }
        }

    }

}
