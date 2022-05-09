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
    public partial class Page1 : Page
    {
        private static readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
        public Page1()
        {
            InitializeComponent();
            List<String> ListOfExtensions = DatabaseViewModel.ListExtensions("extensions");
            foreach (string extension in ListOfExtensions)
            {
                ExtensionsList.Items.Add(extension);
            }

            List<String> ListOfHPExtensions = DatabaseViewModel.ListExtensions("hpextensions");
            foreach (string extension in ListOfHPExtensions)
            {
                HPExtensionsList.Items.Add(extension);
            }

            List<String> ListOfSoftwares = DatabaseViewModel.ListSoftwares();
            foreach (string software in ListOfSoftwares)
            {
                SoftwaresList.Items.Add(software);
            }
            var MyIni = new IniFile("Settings.ini");
            SizeAnswer.Text = MyIni.Read("DefaultSize");
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

        public void NewSoftwareWindow(object sender, RoutedEventArgs e)
        {
            PromptSoftware window1 = new PromptSoftware();
            if (window1.ShowDialog() == true)
            {
                string software = window1.Answer;
                if (!DatabaseViewModel.VerifySoftware(software))
                {
                    DatabaseViewModel.AddSoftware(software);
                    SoftwaresList.Items.Add(software);
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

        public void ClickRemoveSoftware(object sender, RoutedEventArgs e)
        {
            var software = SoftwaresList.SelectedItem;
            if (software != null)
            {
                PromptVerification window1 = new PromptVerification();
                if (window1.ShowDialog() == true)
                {
                    DatabaseViewModel.DeleteSoftware(software.ToString());
                    SoftwaresList.Items.Remove(software.ToString());
                }
            }
        }

        private void ResetDefault(object sender, SelectionChangedEventArgs e)
        {
            // IF EVERYTHING IS STOPPED
            // DELETE EASYSAVE.DB
            // THEN RE-CREATE IT
        }

        private void SizeAnswer_TextChanged(object sender, TextChangedEventArgs e)
        {
            var MyIni = new IniFile("Settings.ini");
            MyIni.Write("DefaultSize", $"{SizeAnswer.Text}");
        }

        private void SizeAnswer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        public void NewHPExtensionWindow(object sender, RoutedEventArgs e)
        {
            PromptExtension window1 = new PromptExtension();
            if (window1.ShowDialog() == true)
            {
                string extension = window1.Answer;
                if (!DatabaseViewModel.VerifyExtension(extension, "hpextensions"))
                {
                    DatabaseViewModel.AddExtension(extension, "hpextensions");
                    HPExtensionsList.Items.Add(extension);
                }
            }
        }

        public void ClickRemoveHPExtension(object sender, RoutedEventArgs e)
        {
            var extension = ExtensionsList.SelectedItem;
            if (extension != null)
            {
                PromptVerification window1 = new PromptVerification();
                if (window1.ShowDialog() == true)
                {
                    DatabaseViewModel.DeleteExtension(extension.ToString(), "hpextensions");
                    HPExtensionsList.Items.Remove(extension.ToString());

                }
            }
        }

        public void ChangeFrench(object sender, RoutedEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");
        }

        public void ChangeEnglish(object sender, RoutedEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-EN");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-EN");
        }
    }

}
