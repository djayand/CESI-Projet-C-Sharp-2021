using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Resources;
using Projet_ProgSys.Languages;

namespace Projet_ProgSys.ViewModel
{
    class LanguageViewModel
    {
        public ResourceManager res_mng = new ResourceManager(typeof(Languages.Resource));

        public string buttonFR { get; set; }
        public string buttonEN { get; set; }

        public LanguageViewModel()
        {
            buttonFR = Resource.French;
            buttonEN = Resource.English;
            
        }

        public void Language_Menu(string userInput)
        {
            if (userInput == res_mng.GetString("English"))
            {
                    //Change culture to us-US
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("us-US");
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("us-US");
            }
            else if (userInput == res_mng.GetString("French"))
            {
                    //Change culture to fr-FR
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");
            }
        }
    }
}
