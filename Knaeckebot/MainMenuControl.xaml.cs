using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace Knaeckebot.Controls
{
    /// <summary>
    /// Interaction logic for MainMenuControl.xaml
    /// </summary>
    public partial class MainMenuControl : UserControl
    {
        public MainMenuControl()
        {
            InitializeComponent();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Knaeckebot Pro\nVersion 1.0\n\nAn application for automating mouse and keyboard inputs.\n\n" +
                "Keyboard shortcuts:\n" +
                "F5 = Play sequence\n" +
                "F6 or ESC = Stop playback\n" +
                "Ctrl+C = Copy actions\n" +
                "Ctrl+V = Paste actions",
                "About Knaeckebot Pro",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}