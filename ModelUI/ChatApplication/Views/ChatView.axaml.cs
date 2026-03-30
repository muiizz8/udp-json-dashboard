using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ChatApplication.Views;

public partial class ChatView : UserControl
{
    private readonly ChatViewModel viewModel = new();

    public ChatView()
    {
        InitializeComponent();
        DataContext = viewModel;

        // Auto-scroll when new messages are added
        viewModel.Messages.CollectionChanged += (_, _) =>
        {
            MessagesScrollViewer.ScrollToEnd();
        };
    }

    private void StartServer_Click(object? sender, RoutedEventArgs e) => viewModel.StartServer();
    private void StopServer_Click(object? sender, RoutedEventArgs e) => viewModel.StopServer();
    private void Connect_Click(object? sender, RoutedEventArgs e) => viewModel.ConnectClient();
    private void Disconnect_Click(object? sender, RoutedEventArgs e) => viewModel.DisconnectClient();
    private void Send_Click(object? sender, RoutedEventArgs e) => viewModel.SendMessage();

    private void MessageInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            viewModel.SendMessage();
            e.Handled = true;
        }
    }
}
