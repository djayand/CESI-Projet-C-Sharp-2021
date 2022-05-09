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

namespace Projet_ProgSys_Graphical
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PromptSoftware : Window
    {
        public PromptSoftware()
        {
            InitializeComponent();
        }

        public void ClickAddSoftware(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public string Answer
        {
            get { return txtAnswer.Text; }
        }

    }
}
