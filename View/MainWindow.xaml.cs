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

namespace Projet_ProgSys_Graphical
{
    /// <summary>
    /// Logique d'interaction pour Page1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private const string Unique = "EasySave";

        private BrushConverter bc = new BrushConverter();

        public int langue = 0;
        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetParameters(object sender, RoutedEventArgs e)
        {
            Frame.Source = new Uri("Page1.xaml", UriKind.Relative);
            // CLEAR BG
            Parameters.Background = (Brush)bc.ConvertFrom("#2C2F33");
            // DARK BG
            Chiffrage.Background = (Brush)bc.ConvertFrom("#23272A");
            Backups.Background = (Brush)bc.ConvertFrom("#23272A");
            Logo.Source = new BitmapImage(new Uri("LogoButtonDark.png", UriKind.Relative));
        }

        private void Backups_Click(object sender, RoutedEventArgs e)
        {
            Frame.Source = new Uri("BackupPage.xaml", UriKind.Relative);
            // CLEAR BG
            Backups.Background = (Brush)bc.ConvertFrom("#2C2F33");
            // DARK BG
            Chiffrage.Background = (Brush)bc.ConvertFrom("#23272A");
            Parameters.Background = (Brush)bc.ConvertFrom("#23272A");
            Logo.Source = new BitmapImage(new Uri("LogoButtonDark.png", UriKind.Relative));

        }

        private void Encryption_Click(object sender, RoutedEventArgs e)
        {
            Frame.Source = new Uri("CryptPage.xaml", UriKind.Relative);
            // CLEAR BG
            Chiffrage.Background = (Brush)bc.ConvertFrom("#2C2F33");
            // DARK BG
            Parameters.Background = (Brush)bc.ConvertFrom("#23272A");
            Backups.Background = (Brush)bc.ConvertFrom("#23272A");
            Logo.Source = new BitmapImage(new Uri("LogoButtonDark.png", UriKind.Relative));

        }

        private void SetHomePage(object sender, RoutedEventArgs e)
        {
            Frame.Source = new Uri("HomePage.xaml", UriKind.Relative);
            Logo.Source = new BitmapImage(new Uri("LogoButton.png", UriKind.Relative));
            Parameters.Background = (Brush)bc.ConvertFrom("#23272A");
            Backups.Background = (Brush)bc.ConvertFrom("#23272A");
            Chiffrage.Background = (Brush)bc.ConvertFrom("#23272A");
        }
    }
}
