using System.Windows;
using AppPrediosDemo.ViewModels;

namespace AppPrediosDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new PredioFormViewModel(); // Enlaza la UI con el VM
        }
    }
}


