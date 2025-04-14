using System;

namespace Knaeckebot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                // In some cases there can be problems with assembly loading
                // This handler helps to find the correct assemblies
                return null;
            };
        }
    }
}