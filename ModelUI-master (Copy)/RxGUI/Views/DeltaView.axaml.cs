using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RxGUI.Views;

public partial class DeltaView : UserControl
{
    public DeltaView()
    {
        InitializeComponent();
    }

    private WindowMain mainWindow;
    public void MainContext(WindowMain MainWindow)
    {
        mainWindow = MainWindow;
    }
}