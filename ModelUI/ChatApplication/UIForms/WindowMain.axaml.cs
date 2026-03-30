using ActiproSoftware.UI.Avalonia.Themes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Database;
using HarmonyLib;
using ChatApplication.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ChatApplication;

/// <summary>
/// Main window for the application, managing navigation and layout between views.
/// </summary>
public partial class WindowMain : Window
{
    /// <summary>
    /// Main view instance displayed by default.
    /// </summary>
    public MainView MainView = new MainView();

    /// <summary>
    /// Log view instance to show logs and debug output.
    /// </summary>
    public LogView LogView = new LogView();

    /// <summary>
    /// Chat view instance for machine-to-machine messaging.
    /// </summary>
    public ChatView ChatView = new ChatView();

    public DatabaseHelper databaseHelper = new DatabaseHelper();

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowMain"/> class.
    /// </summary>
    ///
    public WindowMain()
    {
        InitializeComponent();
        setApplicationVersioninTitle(); // Set application title using assembly metadata
        Globals.InitConfigs(); // Load global config values
        loadDBValue();

        titleTextBlock.Text = Globals.MainTitle;
        subtitleTextBlock.Text = Globals.SubTitle;

        MainView.MainContext(this);

        LogView.DataContext = MainView.DataContext;

        transitionControl.Content = MainView; // Set initial view
    }

    private void loadDBValue()
    {
        List<generalDataClass> getAllDBData = databaseHelper.GetAllDataFromTable("GeneralSettings");
        generalDataClass SomeKey = databaseHelper.GetKeyValue("SomeKey", new generalDataClass
        {
            ID = 0,
            Name = "SomeKey",
            JSON = "{Some JSON}",
            AnyData = "some default value"
        });
        //Test Code
        // databaseHelper.InsertUpdateInTable(new generalDataClass
        // {
        //     ID = 0,
        //     Name = "SomeOtherKey",
        //     JSON = "{Some Other JSON}",
        //     AnyData = "some other default value"
        // }, "GeneralSettings");

        generalDataClass SomeOtherKey = databaseHelper.GetKeyValue("SomeOtherKey", new generalDataClass
        {
            ID = 0,
            Name = "SomeOtherKey",
            JSON = "{Some Other JSON test}",
            AnyData = "some other default value test"
        });
    }

    /// <summary>
    /// Handles selection change for the sidebar menu and switches views accordingly.
    /// </summary>
    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox list)
            return;
        if (transitionControl is null)
            return;

        object? selectedItem = null;
        if (e.AddedItems is { Count: > 0 })
            selectedItem = e.AddedItems[0];
        else
            selectedItem = list.SelectedItem;

        if (selectedItem is null)
            return;

        string? viewKey = null;

        if (selectedItem is ListBoxItem listBoxItem)
            viewKey = listBoxItem.Tag?.ToString();
        else if (selectedItem is StackPanel stackPanel && stackPanel.Children.Count > 1 && stackPanel.Children[1] is TextBlock label)
            viewKey = label.Text;

        if (string.IsNullOrWhiteSpace(viewKey))
            return;

        if (viewKey.Contains("Main View", StringComparison.OrdinalIgnoreCase) || viewKey.Equals("MainView", StringComparison.OrdinalIgnoreCase))
            transitionControl.Content = MainView;
        else if (viewKey.Contains("Log View", StringComparison.OrdinalIgnoreCase) || viewKey.Equals("LogView", StringComparison.OrdinalIgnoreCase))
            transitionControl.Content = LogView;
        else if (viewKey.Contains("Chat", StringComparison.OrdinalIgnoreCase) || viewKey.Equals("ChatView", StringComparison.OrdinalIgnoreCase))
            transitionControl.Content = ChatView;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    // NON-PUBLIC PROCEDURES
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Sets the window title and version text from the assembly product attribute.
    /// The version number automatically increments with each build.
    /// The app name is read from the assembly's product attribute stated in csproj file.
    /// </summary>
    private void setApplicationVersioninTitle()
    {
        AssemblyProductAttribute productAttr = typeof(WindowMain).Assembly
            .GetCustomAttributes(typeof(AssemblyProductAttribute), true)[0] as AssemblyProductAttribute;

        // Title = $"{productAttr.Product}";
        versionTextBlock.Text = $"{productAttr.Product}";
    }

    /// <summary>
    /// Handles property changes in the ViewModel, especially for navigation direction.
    /// </summary>
    private void OnApplicationViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ApplicationViewModel.ViewTransitionForward))
        {
            if (transitionControl.PageTransition is DirectionalPageSlide transition)
                transition.UseForwardDirection = ViewModel?.ViewTransitionForward ?? true;
        }
    }

    /// <summary>
    /// Prepares the View menu based on the current theme. (Stubbed - logic not implemented)
    /// </summary>
    private void OnViewMenuOpening(object? sender, EventArgs e)
    {
        if (ModernTheme.TryGetCurrent(out var theme))
        {
            var definition = theme.Definition;
            if (definition is not null)
            {
                // Add theme-based logic if required
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    // PUBLIC PROCEDURES
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Called when the window is attached to the visual tree. Initializes notification manager.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        var notificationManager = new WindowNotificationManager(TopLevel.GetTopLevel(this));
        if ((notificationManager is not null) && (ViewModel is not null))
            ViewModel.MessageService = new NotificationMessageService(notificationManager);

        base.OnAttachedToVisualTree(e);
    }

    /// <summary>
    /// Toggles the sidebar pane visibility when the application button is clicked.
    /// </summary>
    private void ApplicationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        splitView.IsPaneOpen = !splitView.IsPaneOpen;
    }

    /// <summary>
    /// Handles pointer (mouse) release to support back/forward navigation via mouse buttons.
    /// </summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!e.Handled)
        {
            switch (e.InitialPressMouseButton)
            {
                case MouseButton.XButton1:
                    ViewModel?.NavigateViewBackward();
                    break;
                case MouseButton.XButton2:
                    ViewModel?.NavigateViewForward();
                    break;
            }
        }
    }

    /// <summary>
    /// Gets or sets the ViewModel associated with the window.
    /// </summary>
    public ApplicationViewModel? ViewModel
    {
        get => DataContext as ApplicationViewModel;
        set => DataContext = value;
    }
}
