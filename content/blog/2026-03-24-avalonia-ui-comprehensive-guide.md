---
title: "Avalonia UI: The Complete Guide — From Hello World to Cross-Platform Mastery"
date: 2026-03-24
author: myblazor-team
summary: Everything you need to know about Avalonia UI — what it is today, how to build desktop and mobile apps with AXAML and C#, why desktop and mobile need different layouts, what is coming in Avalonia 12, and the rendering revolution beyond. Packed with code examples.
tags:
  - avalonia
  - dotnet
  - cross-platform
  - desktop
  - mobile
  - xaml
  - csharp
  - tutorial
featured: true
---

## What Is Avalonia UI?

If you have ever built a website with HTML and CSS, you already understand the core idea behind Avalonia UI: you write a declarative markup language that describes your user interface, and a runtime engine renders it on screen. The difference is that instead of running inside a web browser, Avalonia renders directly onto the operating system's graphics surface using a GPU-accelerated engine. Your application is a native binary — not a browser tab.

Avalonia is an open-source, MIT-licensed UI framework for .NET. It lets you write applications in C# (or F#) with a XAML-based markup language and deploy them to Windows, macOS, Linux, iOS, Android, WebAssembly, and even bare-metal embedded Linux devices. The core framework has been in development since 2013, when Steven Kirk created it as a spiritual successor to Windows Presentation Foundation (WPF) at a time when WPF appeared abandoned by Microsoft.

Today, Avalonia has over 30,000 stars on GitHub, more than 87 million NuGet downloads, and is used in production by companies including JetBrains (their Rider IDE uses Avalonia for parts of its UI), Unity, GitHub, Schneider Electric, and Devolutions. It is one of the most active .NET open-source projects in the ecosystem.

### Why Not Just Use a Web Browser?

You might wonder: if we already know HTML and CSS, why learn another UI framework? There are several compelling reasons.

First, native performance. A Blazor WebAssembly app (like this very website) runs inside a browser engine, which itself runs inside your operating system. Avalonia cuts out the middleman — your C# code compiles to native machine code, and the UI renders directly through GPU-accelerated pipelines. The result is dramatically faster startup, lower memory usage, and smoother animations.

Second, offline-first by default. Native applications do not need a web server. They work on airplanes, in basements, and in places without connectivity.

Third, platform integration. Native apps can access the file system, system tray, notifications, Bluetooth, USB devices, and other hardware that web applications cannot (or can only access through limited, permission-gated APIs).

Fourth, pixel-perfect consistency. Because Avalonia draws every pixel itself (rather than wrapping native platform controls), your application looks identical on every platform. There are no surprises when a button renders differently on Android versus iOS.

### How Avalonia Compares to Other .NET UI Frameworks

There are several .NET UI frameworks competing for developer attention in 2026. Here is how they compare at a high level.

**WPF (Windows Presentation Foundation)** is Microsoft's original XAML-based desktop framework. It is mature and powerful but only runs on Windows. If you know WPF, Avalonia will feel very familiar — the API is intentionally close to WPF, though it is not a 1:1 copy. Avalonia has improvements in its styling system, property system, and template model.

**.NET MAUI (Multi-platform App UI)** is Microsoft's official cross-platform framework. Unlike Avalonia, MAUI wraps native platform controls — a Button on Android is an actual Android Button widget, while a Button on iOS is a UIButton. This means your app looks "native" on each platform, but it also means you are at the mercy of each platform's quirks. MAUI has struggled with adoption, bugs, and slow updates. In early 2026, developers reported significant regressions in the .NET 9 to .NET 10 transition.

**Uno Platform** is another cross-platform option that targets UWP/WinUI APIs. It is capable but has a different design philosophy from Avalonia.

**Avalonia** takes the "drawn UI" approach, similar to Flutter. It renders everything itself using SkiaSharp (the same Skia library that powers Chrome and Flutter), giving you complete control over every pixel. This approach provides more visual consistency across platforms at the cost of not looking "native" by default — though Avalonia ships with a Fluent theme that closely matches modern Windows/macOS aesthetics.

## Getting Started: Your First Avalonia Application

### Prerequisites

You need the .NET SDK installed. As of this writing, .NET 10 is the current LTS release. You can verify your installation:

```bash
dotnet --version
# Should output something like 10.0.104
```

### Installing the Templates

Avalonia provides project templates through the `dotnet new` system:

```bash
dotnet new install Avalonia.Templates
```

This installs several templates. The one you will use most often is `avalonia.mvvm`, which sets up a project with the Model-View-ViewModel pattern:

```bash
dotnet new avalonia.mvvm -o MyFirstAvaloniaApp
cd MyFirstAvaloniaApp
dotnet run
```

That is it. You should see a window appear with a greeting message. If you are on Linux, it works. If you are on macOS, it works. If you are on Windows, it works. Same code, same binary (well, same source — the binary is platform-specific).

### Understanding the Project Structure

After running the template, your project looks like this:

```
MyFirstAvaloniaApp/
├── MyFirstAvaloniaApp.csproj
├── Program.cs
├── App.axaml
├── App.axaml.cs
├── ViewLocator.cs
├── ViewModels/
│   ├── ViewModelBase.cs
│   └── MainWindowViewModel.cs
├── Views/
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
└── Assets/
    └── avalonia-logo.ico
```

Notice the `.axaml` file extension. This stands for "Avalonia XAML" and is used instead of plain `.xaml` to avoid conflicts with WPF and UWP XAML files in IDE tooling. The syntax inside is nearly identical to WPF XAML, with some improvements.

### The Project File

Your `.csproj` file targets .NET 10 and references the Avalonia NuGet packages:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.0" />
    <PackageReference Include="Avalonia.Desktop" Version="11.3.0" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.0" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.0" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />

    <!-- Condition below is used to add dependencies for previewer -->
    <PackageReference Include="Avalonia.Diagnostics" Version="11.3.0"
                      Condition="'$(Configuration)' == 'Debug'" />
  </ItemGroup>

</Project>
```

The `AvaloniaUseCompiledBindingsByDefault` property is important — it tells the XAML compiler to use compiled bindings by default, which are faster than reflection-based bindings and catch errors at build time rather than runtime. In Avalonia 12, this becomes `true` by default even if you do not set it.

### Program.cs — The Entry Point

```csharp
using Avalonia;
using System;

namespace MyFirstAvaloniaApp;

sealed class Program
{
    // The entry point. Don't use any Avalonia, third-party APIs
    // or any SynchronizationContext-reliant code before AppMain
    // is called; things won't be initialized yet and stuff
    // might break.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration; also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

This is conceptually similar to a web application's `Program.cs` where you configure services and middleware. Here you configure the Avalonia application builder. `UsePlatformDetect()` automatically selects the correct rendering backend for your operating system. `WithInterFont()` loads the Inter font family. `LogToTrace()` sends log output to `System.Diagnostics.Trace`.

### App.axaml — The Application Root

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyFirstAvaloniaApp.App"
             RequestedThemeVariant="Default">
    <!-- "Default" follows system theme; use "Dark" or "Light" to force -->

    <Application.DataTemplates>
        <local:ViewLocator />
    </Application.DataTemplates>

    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

Two namespace declarations are required in every AXAML file:

- `xmlns="https://github.com/avaloniaui"` — the Avalonia UI namespace (equivalent to the default HTML namespace)
- `xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"` — the XAML language namespace (for things like `x:Class`, `x:Name`, `x:Key`)

The `<FluentTheme />` element loads a modern Fluent Design theme that looks good on all platforms. Avalonia also ships with a "Simple" theme if you prefer a more minimal starting point.

## AXAML Fundamentals: The Markup Language

If you know HTML, AXAML will feel somewhat familiar. Both are XML-based markup languages for describing visual elements. But there are important conceptual differences.

### Elements Are Controls

In HTML, a `<div>` is a generic container. In AXAML, every element maps to a specific .NET class. A `<Button>` is an instance of `Avalonia.Controls.Button`. A `<TextBlock>` is an instance of `Avalonia.Controls.TextBlock`. There is no generic "div" equivalent — instead, you use layout panels like `<StackPanel>`, `<Grid>`, `<DockPanel>`, and `<WrapPanel>`.

### A Simple Window

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="MyFirstAvaloniaApp.Views.MainWindow"
        Title="My First Avalonia App"
        Width="600" Height="400">

    <StackPanel Margin="20" Spacing="10">
        <TextBlock Text="Hello, Avalonia!"
                   FontSize="24"
                   FontWeight="Bold" />

        <TextBlock Text="This is a cross-platform .NET application."
                   Foreground="Gray" />

        <Button Content="Click Me"
                HorizontalAlignment="Left" />
    </StackPanel>

</Window>
```

Compare this to equivalent HTML:

```html
<div style="margin: 20px; display: flex; flex-direction: column; gap: 10px;">
    <h1 style="font-size: 24px; font-weight: bold;">Hello, Avalonia!</h1>
    <p style="color: gray;">This is a cross-platform .NET application.</p>
    <button>Click Me</button>
</div>
```

The structure is similar, but AXAML uses attributes for properties (`FontSize="24"`) instead of CSS. We will see later how Avalonia has its own styling system that separates style from structure, similar to how CSS works.

### Data Binding — Connecting UI to Code

Data binding is the mechanism that connects your AXAML markup to your C# code. If you have used JavaScript frameworks like React or Vue, data binding is conceptually similar to reactive state — when the underlying data changes, the UI automatically updates.

Here is a simple example. First, the ViewModel (the C# code):

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyFirstAvaloniaApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Hello, Avalonia!";

    [ObservableProperty]
    private int _clickCount;

    [RelayCommand]
    private void IncrementCount()
    {
        ClickCount++;
        Greeting = $"You clicked {ClickCount} time(s)!";
    }
}
```

The `[ObservableProperty]` attribute (from CommunityToolkit.Mvvm) is a source generator that automatically creates a public property with change notification. When `ClickCount` changes, any UI element bound to it automatically updates. The `[RelayCommand]` attribute generates an `ICommand` property that can be bound to a button.

Now, the AXAML that binds to this ViewModel:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:MyFirstAvaloniaApp.ViewModels"
        x:Class="MyFirstAvaloniaApp.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="My First Avalonia App"
        Width="600" Height="400">

    <Design.DataContext>
        <!-- Provides design-time data for the IDE previewer -->
        <vm:MainWindowViewModel />
    </Design.DataContext>

    <StackPanel Margin="20" Spacing="10"
                HorizontalAlignment="Center"
                VerticalAlignment="Center">

        <TextBlock Text="{Binding Greeting}"
                   FontSize="24"
                   FontWeight="Bold"
                   HorizontalAlignment="Center" />

        <TextBlock Text="{Binding ClickCount, StringFormat='Count: {0}'}"
                   HorizontalAlignment="Center"
                   Foreground="Gray" />

        <Button Content="Click Me"
                Command="{Binding IncrementCountCommand}"
                HorizontalAlignment="Center" />
    </StackPanel>

</Window>
```

Key things to notice:

- `xmlns:vm="using:MyFirstAvaloniaApp.ViewModels"` declares a namespace prefix so we can reference our C# types in AXAML
- `x:DataType="vm:MainWindowViewModel"` tells the compiled binding system what type to expect as the DataContext. This enables build-time validation of your bindings.
- `{Binding Greeting}` is a markup extension that binds the `Text` property to the `Greeting` property on the ViewModel
- `{Binding IncrementCountCommand}` binds the button's Command to the auto-generated command from `[RelayCommand]`
- `<Design.DataContext>` provides a ViewModel instance for the IDE's live previewer — it does not affect runtime behavior

## Layout System: Panels and Containers

Avalonia provides several layout panels, each with a different strategy for arranging child controls. If you are coming from CSS, think of these as pre-built `display` modes.

### StackPanel — Flexbox Column/Row

`StackPanel` arranges children in a single line, either vertically (default) or horizontally:

```xml
<!-- Vertical stack (like CSS flex-direction: column) -->
<StackPanel Spacing="10">
    <TextBlock Text="First" />
    <TextBlock Text="Second" />
    <TextBlock Text="Third" />
</StackPanel>

<!-- Horizontal stack (like CSS flex-direction: row) -->
<StackPanel Orientation="Horizontal" Spacing="10">
    <Button Content="One" />
    <Button Content="Two" />
    <Button Content="Three" />
</StackPanel>
```

### Grid — CSS Grid Equivalent

`Grid` divides space into rows and columns. This is the most powerful and commonly used layout panel:

```xml
<Grid RowDefinitions="Auto,*,Auto"
      ColumnDefinitions="200,*"
      Margin="10">

    <!-- Header spanning both columns -->
    <TextBlock Grid.Row="0" Grid.ColumnSpan="2"
               Text="Application Header"
               FontSize="20" FontWeight="Bold"
               Margin="0,0,0,10" />

    <!-- Sidebar -->
    <ListBox Grid.Row="1" Grid.Column="0"
             Margin="0,0,10,0">
        <ListBoxItem Content="Dashboard" />
        <ListBoxItem Content="Settings" />
        <ListBoxItem Content="Profile" />
    </ListBox>

    <!-- Main content area -->
    <Border Grid.Row="1" Grid.Column="1"
            Background="#f0f0f0"
            CornerRadius="8"
            Padding="20">
        <TextBlock Text="Main content goes here"
                   VerticalAlignment="Center"
                   HorizontalAlignment="Center" />
    </Border>

    <!-- Footer spanning both columns -->
    <TextBlock Grid.Row="2" Grid.ColumnSpan="2"
               Text="© 2026 My App"
               HorizontalAlignment="Center"
               Margin="0,10,0,0"
               Foreground="Gray" />
</Grid>
```

Row and column definitions use a size syntax:

- `Auto` — sizes to fit content (like CSS `auto`)
- `*` — takes remaining space proportionally (like CSS `1fr`)
- `2*` — takes twice the remaining space (like CSS `2fr`)
- `200` — fixed pixel size

### DockPanel — Edge Docking

`DockPanel` docks children to the edges of the container. The last child fills the remaining space:

```xml
<DockPanel>
    <!-- Top toolbar -->
    <Menu DockPanel.Dock="Top">
        <MenuItem Header="File">
            <MenuItem Header="Open" />
            <MenuItem Header="Save" />
            <Separator />
            <MenuItem Header="Exit" />
        </MenuItem>
        <MenuItem Header="Edit">
            <MenuItem Header="Undo" />
            <MenuItem Header="Redo" />
        </MenuItem>
    </Menu>

    <!-- Bottom status bar -->
    <Border DockPanel.Dock="Bottom"
            Background="#e0e0e0" Padding="5">
        <TextBlock Text="Ready" FontSize="12" />
    </Border>

    <!-- Left sidebar -->
    <Border DockPanel.Dock="Left"
            Width="200" Background="#f5f5f5"
            Padding="10">
        <TextBlock Text="Navigation" />
    </Border>

    <!-- Remaining space = main content -->
    <Border Padding="20">
        <TextBlock Text="Main Content Area" />
    </Border>
</DockPanel>
```

### WrapPanel — Flex Wrap

`WrapPanel` arranges children left to right, wrapping to the next line when space runs out:

```xml
<WrapPanel Orientation="Horizontal">
    <Button Content="Tag 1" Margin="4" />
    <Button Content="Tag 2" Margin="4" />
    <Button Content="Tag 3" Margin="4" />
    <Button Content="Long Tag Name" Margin="4" />
    <Button Content="Another" Margin="4" />
    <!-- These will wrap to the next line if the container is too narrow -->
</WrapPanel>
```

### UniformGrid — Equal-Size Grid

`UniformGrid` creates a grid where every cell is the same size:

```xml
<UniformGrid Columns="3" Rows="2">
    <Button Content="1" />
    <Button Content="2" />
    <Button Content="3" />
    <Button Content="4" />
    <Button Content="5" />
    <Button Content="6" />
</UniformGrid>
```

## Styling: Avalonia's CSS-Like System

Avalonia has a styling system that is conceptually closer to CSS than WPF's styling. Styles use selectors (similar to CSS selectors) to target controls.

### Basic Styles

```xml
<Window.Styles>
    <!-- Target all TextBlocks -->
    <Style Selector="TextBlock">
        <Setter Property="FontFamily" Value="Inter" />
        <Setter Property="FontSize" Value="14" />
    </Style>

    <!-- Target buttons with the "primary" class -->
    <Style Selector="Button.primary">
        <Setter Property="Background" Value="#0078d4" />
        <Setter Property="Foreground" Value="White" />
        <Setter Property="CornerRadius" Value="4" />
        <Setter Property="Padding" Value="16,8" />
    </Style>

    <!-- Hover state (like CSS :hover) -->
    <Style Selector="Button.primary:pointerover /template/ ContentPresenter">
        <Setter Property="Background" Value="#106ebe" />
    </Style>

    <!-- Target by name (like CSS #id) -->
    <Style Selector="TextBlock#PageTitle">
        <Setter Property="FontSize" Value="28" />
        <Setter Property="FontWeight" Value="Bold" />
    </Style>
</Window.Styles>

<!-- Usage -->
<StackPanel>
    <TextBlock x:Name="PageTitle" Text="Dashboard" />
    <Button Classes="primary" Content="Save Changes" />
    <Button Content="Cancel" />
</StackPanel>
```

Notice the CSS-like selector syntax:

- `TextBlock` — targets all TextBlock controls (like CSS element selectors)
- `Button.primary` — targets Buttons with the "primary" class (like CSS `.primary`)
- `TextBlock#PageTitle` — targets by name (like CSS `#id`)
- `:pointerover` — pseudo-class for mouse hover (like CSS `:hover`)
- `/template/` — navigates into a control's template (unique to Avalonia)

### Styles in External Files

Just like CSS can be in external files, Avalonia styles can live in separate `.axaml` files:

```xml
<!-- Styles/AppStyles.axaml -->
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Style Selector="Button.danger">
        <Setter Property="Background" Value="#dc2626" />
        <Setter Property="Foreground" Value="White" />
    </Style>

    <Style Selector="Button.danger:pointerover /template/ ContentPresenter">
        <Setter Property="Background" Value="#b91c1c" />
    </Style>

</Styles>
```

Then include it in your `App.axaml`:

```xml
<Application.Styles>
    <FluentTheme />
    <StyleInclude Source="/Styles/AppStyles.axaml" />
</Application.Styles>
```

## The MVVM Pattern: Separating Concerns

MVVM (Model-View-ViewModel) is the standard architecture pattern for Avalonia applications. It is analogous to MVC in web development but tailored for data-binding UI frameworks.

- **Model** — your domain objects and business logic (like your database entities and services in a web app)
- **View** — the AXAML markup and code-behind (like your Razor/HTML templates)
- **ViewModel** — the intermediary that exposes data and commands to the View (like a page model or controller)

### A Complete MVVM Example: Todo List

Here is a full example of a todo list application demonstrating MVVM:

**Model:**

```csharp
namespace MyApp.Models;

public class TodoItem
{
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; }
}
```

**ViewModel:**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyApp.Models;

namespace MyApp.ViewModels;

public partial class TodoViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _newItemTitle = "";

    public ObservableCollection<TodoItem> Items { get; } = new()
    {
        new TodoItem { Title = "Learn Avalonia", IsCompleted = false },
        new TodoItem { Title = "Build an app", IsCompleted = false },
        new TodoItem { Title = "Deploy everywhere", IsCompleted = false }
    };

    [RelayCommand(CanExecute = nameof(CanAddItem))]
    private void AddItem()
    {
        Items.Add(new TodoItem { Title = NewItemTitle });
        NewItemTitle = "";
    }

    private bool CanAddItem() =>
        !string.IsNullOrWhiteSpace(NewItemTitle);

    // The source generator knows to re-evaluate CanAddItem
    // when NewItemTitle changes because of this attribute:
    partial void OnNewItemTitleChanged(string value) =>
        AddItemCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void RemoveItem(TodoItem item) =>
        Items.Remove(item);

    [RelayCommand]
    private void ToggleItem(TodoItem item) =>
        item.IsCompleted = !item.IsCompleted;
}
```

**View (AXAML):**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:m="using:MyApp.Models"
             x:Class="MyApp.Views.TodoView"
             x:DataType="vm:TodoViewModel">

    <DockPanel Margin="20">
        <!-- Header -->
        <TextBlock DockPanel.Dock="Top"
                   Text="Todo List"
                   FontSize="24" FontWeight="Bold"
                   Margin="0,0,0,16" />

        <!-- Input area -->
        <Grid DockPanel.Dock="Top"
              ColumnDefinitions="*,Auto"
              Margin="0,0,0,16">
            <TextBox Grid.Column="0"
                     Text="{Binding NewItemTitle}"
                     Watermark="What needs to be done?"
                     Margin="0,0,8,0" />
            <Button Grid.Column="1"
                    Content="Add"
                    Command="{Binding AddItemCommand}"
                    Classes="primary" />
        </Grid>

        <!-- Todo list -->
        <ListBox ItemsSource="{Binding Items}"
                 x:DataType="vm:TodoViewModel">
            <ListBox.ItemTemplate>
                <DataTemplate x:DataType="m:TodoItem">
                    <Grid ColumnDefinitions="Auto,*,Auto">
                        <CheckBox Grid.Column="0"
                                  IsChecked="{Binding IsCompleted}"
                                  Margin="0,0,8,0" />
                        <TextBlock Grid.Column="1"
                                   Text="{Binding Title}"
                                   VerticalAlignment="Center" />
                        <Button Grid.Column="2"
                                Content="✕"
                                Command="{Binding
                                    $parent[ListBox].((vm:TodoViewModel)DataContext).RemoveItemCommand}"
                                CommandParameter="{Binding}"
                                Classes="danger"
                                Padding="4,2" />
                    </Grid>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </DockPanel>

</UserControl>
```

Notice the `$parent[ListBox]` syntax in the Remove button's command binding. This navigates up the visual tree to find the ListBox, then accesses its DataContext (the TodoViewModel). This is how you reach the parent ViewModel from within an `ItemTemplate`. In HTML/JavaScript terms, this is similar to how you might call a parent component's method from a child component in React.

## Desktop vs. Mobile: Why You Need Different Layouts

This is one of the most important sections of this article. If you are coming from web development, you are accustomed to responsive design — writing one set of HTML and CSS that adapts to different screen sizes using media queries. Avalonia can do something similar, but there are fundamental differences between desktop and mobile that go beyond screen size.

### The Core Differences

**Input model.** Desktop users have a mouse with hover states, right-click context menus, precise cursor positioning, and keyboard shortcuts. Mobile users have touch with tap, swipe, pinch-to-zoom, and no hover state. A button that is 24 pixels wide works fine with a mouse cursor but is impossibly small for a human finger.

**Screen real estate.** A desktop monitor might be 1920×1080 or larger. A phone screen is typically 360-430 points wide in portrait mode. You simply cannot show the same information density on both.

**Navigation paradigm.** Desktop apps typically use menus, toolbars, and side panels that are always visible. Mobile apps use bottom navigation bars, hamburger menus, and full-screen page transitions where only one "page" is visible at a time.

**Safe areas.** Mobile devices have notches, rounded corners, and system gesture zones that your content must avoid. Desktop windows do not have these constraints.

**Platform conventions.** iOS users expect a bottom tab bar and back-swipe navigation. Android users expect a top app bar with a back button. Desktop users expect a menu bar and keyboard shortcuts. Violating these conventions makes your app feel foreign.

### Strategy 1: Platform-Specific Styles with OnPlatform

Avalonia provides the `OnPlatform` markup extension that works like a compile-time switch statement. The compiler generates branches for all platforms, but only the matching branch executes at runtime:

```xml
<TextBlock Text="{OnPlatform Default='Hello!',
                              Android='Hello from Android!',
                              iOS='Hello from iPhone!'}" />
```

You can use this for any property, not just strings:

```xml
<Button Padding="{OnPlatform '8,4', Android='16,12', iOS='16,12'}"
        FontSize="{OnPlatform 14, Android=16, iOS=16}"
        CornerRadius="{OnPlatform 4, iOS=20}" />
```

More powerfully, you can load entirely different style sheets per platform:

```xml
<!-- In App.axaml -->
<Application.Styles>
    <FluentTheme />

    <OnPlatform>
        <On Options="Android, iOS">
            <StyleInclude Source="/Styles/Mobile.axaml" />
        </On>
        <On Options="Default">
            <StyleInclude Source="/Styles/Desktop.axaml" />
        </On>
    </OnPlatform>
</Application.Styles>
```

### Strategy 2: Form Factor Detection with OnFormFactor

`OnFormFactor` distinguishes between Desktop and Mobile form factors at runtime:

```xml
<TextBlock Text="{OnFormFactor 'Desktop mode', Mobile='Mobile mode'}" />

<!-- Different margins for different form factors -->
<StackPanel Margin="{OnFormFactor '20', Mobile='12'}">
    <!-- content -->
</StackPanel>
```

### Strategy 3: Container Queries (Introduced in Avalonia 11.3)

This is the most exciting responsive design feature in Avalonia. Container Queries work similarly to CSS Container Queries — instead of checking the viewport size, you check the size of a specific container control. This lets you build truly reusable components that adapt to the space available to them, regardless of the overall screen size.

Here is a practical example — a product card that switches between horizontal and vertical layouts:

```xml
<Border x:Name="CardContainer"
        Container.Name="card"
        Container.Sizing="Width">

    <Border.Styles>
        <!-- Vertical (narrow) layout -->
        <ContainerQuery Name="card" Query="max-width:400">
            <Style Selector="StackPanel#CardContent">
                <Setter Property="Orientation" Value="Vertical" />
            </Style>
            <Style Selector="Image#ProductImage">
                <Setter Property="Width" Value="NaN" />
                <Setter Property="Height" Value="200" />
            </Style>
        </ContainerQuery>

        <!-- Horizontal (wide) layout -->
        <ContainerQuery Name="card" Query="min-width:400">
            <Style Selector="StackPanel#CardContent">
                <Setter Property="Orientation" Value="Horizontal" />
            </Style>
            <Style Selector="Image#ProductImage">
                <Setter Property="Width" Value="200" />
                <Setter Property="Height" Value="NaN" />
            </Style>
        </ContainerQuery>
    </Border.Styles>

    <StackPanel x:Name="CardContent" Spacing="12">
        <Image x:Name="ProductImage"
               Source="/Assets/product.jpg"
               Stretch="UniformToFill" />
        <StackPanel Spacing="4" VerticalAlignment="Center">
            <TextBlock Text="Product Name" FontWeight="Bold" />
            <TextBlock Text="$29.99" Foreground="Green" />
            <TextBlock Text="A great product description..."
                       TextWrapping="Wrap" />
        </StackPanel>
    </StackPanel>
</Border>
```

You can combine multiple conditions with `and` for AND logic and `,` for OR logic:

```xml
<!-- Both width and height conditions must be met -->
<ContainerQuery Name="panel" Query="min-width:600 and min-height:400">
    <Style Selector="UniformGrid#ContentGrid">
        <Setter Property="Columns" Value="3" />
    </Style>
</ContainerQuery>

<!-- Either condition triggers the styles -->
<ContainerQuery Name="panel" Query="max-width:300, max-height:200">
    <Style Selector="UniformGrid#ContentGrid">
        <Setter Property="Columns" Value="1" />
    </Style>
</ContainerQuery>
```

Important rules for Container Queries:

1. You must declare a control as a container by setting `Container.Name` and `Container.Sizing` on it
2. Styles inside a ContainerQuery cannot affect the container itself or its ancestors (this prevents infinite layout loops)
3. ContainerQuery elements must be direct children of a control's `Styles` property — they cannot be nested inside other `Style` elements

### Strategy 4: Completely Separate Views

For maximum control, you can use entirely different AXAML files for desktop and mobile. This is the approach many production applications take:

```
Views/
├── Desktop/
│   ├── MainView.axaml
│   ├── SettingsView.axaml
│   └── DetailView.axaml
├── Mobile/
│   ├── MainView.axaml
│   ├── SettingsView.axaml
│   └── DetailView.axaml
└── Shared/
    ├── ProductCard.axaml
    └── LoadingSpinner.axaml
```

You then use a view locator or conditional logic in your App to load the correct views:

```csharp
// In your ViewLocator or App setup
public Control Build(object? data)
{
    if (data is null) return new TextBlock { Text = "No data" };

    var isMobile = OperatingSystem.IsAndroid() ||
                   OperatingSystem.IsIOS();

    var name = data.GetType().FullName!
        .Replace("ViewModel", "View");

    // Insert platform folder
    var platformFolder = isMobile ? "Mobile" : "Desktop";
    name = name.Replace(".Views.", $".Views.{platformFolder}.");

    var type = Type.GetType(name);

    if (type is not null)
        return (Control)Activator.CreateInstance(type)!;

    return new TextBlock { Text = $"View not found: {name}" };
}
```

### Practical Example: Master-Detail on Desktop vs. Mobile

Here is a concrete example showing how the same feature (a contacts list with detail view) needs fundamentally different UI on desktop versus mobile.

**Desktop Version** — side-by-side layout with the list always visible:

```xml
<!-- Views/Desktop/ContactsView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.Desktop.ContactsView"
             x:DataType="vm:ContactsViewModel">

    <Grid ColumnDefinitions="300,*">
        <!-- Left: always-visible contact list -->
        <Border Grid.Column="0"
                BorderBrush="#e0e0e0"
                BorderThickness="0,0,1,0">
            <DockPanel>
                <TextBox DockPanel.Dock="Top"
                         Text="{Binding SearchText}"
                         Watermark="Search contacts..."
                         Margin="8" />

                <ListBox ItemsSource="{Binding FilteredContacts}"
                         SelectedItem="{Binding SelectedContact}">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal"
                                        Spacing="8" Margin="4">
                                <Ellipse Width="32" Height="32"
                                         Fill="#0078d4" />
                                <StackPanel VerticalAlignment="Center">
                                    <TextBlock Text="{Binding Name}"
                                               FontWeight="SemiBold" />
                                    <TextBlock Text="{Binding Email}"
                                               FontSize="12"
                                               Foreground="Gray" />
                                </StackPanel>
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </DockPanel>
        </Border>

        <!-- Right: detail panel -->
        <ScrollViewer Grid.Column="1" Padding="20">
            <StackPanel Spacing="12"
                        IsVisible="{Binding SelectedContact,
                            Converter={x:Static ObjectConverters.IsNotNull}}">
                <TextBlock Text="{Binding SelectedContact.Name}"
                           FontSize="28" FontWeight="Bold" />
                <TextBlock Text="{Binding SelectedContact.Email}" />
                <TextBlock Text="{Binding SelectedContact.Phone}" />
                <TextBlock Text="{Binding SelectedContact.Notes}"
                           TextWrapping="Wrap" />
            </StackPanel>
        </ScrollViewer>
    </Grid>

</UserControl>
```

**Mobile Version** — full-screen list that pushes to a full-screen detail:

```xml
<!-- Views/Mobile/ContactsView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.Mobile.ContactsView"
             x:DataType="vm:ContactsViewModel">

    <Panel>
        <!-- Contact list (full screen) -->
        <DockPanel IsVisible="{Binding !IsDetailVisible}">
            <TextBox DockPanel.Dock="Top"
                     Text="{Binding SearchText}"
                     Watermark="Search contacts..."
                     Margin="12"
                     Padding="16,12"
                     FontSize="16" />

            <ListBox ItemsSource="{Binding FilteredContacts}"
                     SelectedItem="{Binding SelectedContact}">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <!-- Larger touch targets for mobile -->
                        <StackPanel Orientation="Horizontal"
                                    Spacing="12"
                                    Margin="12,8">
                            <Ellipse Width="48" Height="48"
                                     Fill="#0078d4" />
                            <StackPanel VerticalAlignment="Center">
                                <TextBlock Text="{Binding Name}"
                                           FontSize="16"
                                           FontWeight="SemiBold" />
                                <TextBlock Text="{Binding Email}"
                                           FontSize="14"
                                           Foreground="Gray" />
                            </StackPanel>
                        </StackPanel>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </DockPanel>

        <!-- Detail view (full screen, overlays list) -->
        <DockPanel IsVisible="{Binding IsDetailVisible}">
            <!-- Back button -->
            <Button DockPanel.Dock="Top"
                    Content="← Back"
                    Command="{Binding GoBackCommand}"
                    Padding="16,12"
                    FontSize="16"
                    Background="Transparent"
                    HorizontalAlignment="Left" />

            <ScrollViewer Padding="16">
                <StackPanel Spacing="16">
                    <TextBlock Text="{Binding SelectedContact.Name}"
                               FontSize="24" FontWeight="Bold" />
                    <TextBlock Text="{Binding SelectedContact.Email}"
                               FontSize="16" />
                    <TextBlock Text="{Binding SelectedContact.Phone}"
                               FontSize="16" />
                    <TextBlock Text="{Binding SelectedContact.Notes}"
                               FontSize="16"
                               TextWrapping="Wrap" />
                </StackPanel>
            </ScrollViewer>
        </DockPanel>
    </Panel>

</UserControl>
```

The key differences in the mobile version:

- Larger text (`FontSize="16"` everywhere) for readability
- Larger touch targets (48px avatars, 16px padding on buttons)
- Full-screen navigation instead of side-by-side panels
- An explicit "Back" button since there is no always-visible list
- `IsDetailVisible` boolean that toggles between list and detail views

Both views share the exact same `ContactsViewModel` — the business logic does not change, only the presentation.

### Platform-Specific Code in C#

Sometimes you need to execute different code depending on the platform. The .NET `OperatingSystem` class provides static methods:

```csharp
public void ConfigurePlatformFeatures()
{
    if (OperatingSystem.IsWindows())
    {
        // Set up Windows-specific features like jump lists
    }
    else if (OperatingSystem.IsMacOS())
    {
        // Configure macOS menu bar
    }
    else if (OperatingSystem.IsLinux())
    {
        // Linux-specific setup
    }
    else if (OperatingSystem.IsAndroid())
    {
        // Android permissions, status bar color, etc.
    }
    else if (OperatingSystem.IsIOS())
    {
        // iOS setup, safe areas, etc.
    }
    else if (OperatingSystem.IsBrowser())
    {
        // WebAssembly-specific setup
    }
}
```

## Building for Each Platform

### Desktop (Windows, macOS, Linux)

The default template targets desktop. Build and run with:

```bash
dotnet run
```

To publish a self-contained binary:

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

### Android

Add the Android target to your project. The Avalonia templates include an Android head project:

```bash
dotnet new avalonia.xplat -o MyCrossApp
```

This creates a solution with separate head projects for each platform:

```
MyCrossApp/
├── MyCrossApp/                    # Shared code (ViewModels, Models)
├── MyCrossApp.Desktop/            # Desktop entry point
├── MyCrossApp.Android/            # Android entry point
├── MyCrossApp.iOS/                # iOS entry point
└── MyCrossApp.Browser/            # WebAssembly entry point
```

The Android project's `MainActivity.cs`:

```csharp
using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace MyCrossApp.Android;

[Activity(
    Label = "MyCrossApp",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation
                         | ConfigChanges.ScreenSize
                         | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder)
            .WithInterFont();
}
```

Build and deploy to an Android device:

```bash
dotnet build -t:Run -f net10.0-android
```

### iOS

The iOS entry point is similar:

```csharp
using Avalonia;
using Avalonia.iOS;
using Foundation;
using UIKit;

namespace MyCrossApp.iOS;

[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder)
            .WithInterFont();
}
```

Build for iOS (requires macOS with Xcode):

```bash
dotnet build -t:Run -f net10.0-ios
```

### WebAssembly

The Browser project uses Avalonia's WebAssembly support:

```csharp
using Avalonia;
using Avalonia.Browser;
using MyCrossApp;

internal sealed partial class Program
{
    private static Task Main(string[] args) =>
        BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>();
}
```

Build and serve:

```bash
dotnet run --project MyCrossApp.Browser
```

## Common Controls Reference

Here is a quick reference of the most commonly used controls, with AXAML examples:

### Text Display and Input

```xml
<!-- Read-only text -->
<TextBlock Text="Static text" FontSize="16" />

<!-- Selectable text -->
<SelectableTextBlock Text="You can select and copy this text" />

<!-- Single-line input -->
<TextBox Text="{Binding Name}"
         Watermark="Enter your name"
         MaxLength="100" />

<!-- Multi-line input -->
<TextBox Text="{Binding Notes}"
         AcceptsReturn="True"
         TextWrapping="Wrap"
         Height="120" />

<!-- Password input -->
<TextBox Text="{Binding Password}"
         PasswordChar="●"
         RevealPassword="{Binding ShowPassword}" />

<!-- Numeric input -->
<NumericUpDown Value="{Binding Quantity}"
               Minimum="0" Maximum="100"
               Increment="1" />
```

### Selection Controls

```xml
<!-- Checkbox -->
<CheckBox IsChecked="{Binding AgreeToTerms}"
          Content="I agree to the terms and conditions" />

<!-- Radio buttons -->
<StackPanel Spacing="8">
    <RadioButton GroupName="Size" Content="Small"
                 IsChecked="{Binding IsSmall}" />
    <RadioButton GroupName="Size" Content="Medium"
                 IsChecked="{Binding IsMedium}" />
    <RadioButton GroupName="Size" Content="Large"
                 IsChecked="{Binding IsLarge}" />
</StackPanel>

<!-- Dropdown (ComboBox) -->
<ComboBox ItemsSource="{Binding Countries}"
          SelectedItem="{Binding SelectedCountry}"
          PlaceholderText="Select a country" />

<!-- Slider -->
<Slider Value="{Binding Volume}"
        Minimum="0" Maximum="100"
        TickFrequency="10"
        IsSnapToTickEnabled="True" />

<!-- Toggle switch -->
<ToggleSwitch IsChecked="{Binding DarkMode}"
              OnContent="Dark"
              OffContent="Light" />

<!-- Date picker -->
<DatePicker SelectedDate="{Binding BirthDate}" />
```

### Data Display

```xml
<!-- List with data binding -->
<ListBox ItemsSource="{Binding Customers}"
         SelectedItem="{Binding SelectedCustomer}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>

<!-- Tree view -->
<TreeView ItemsSource="{Binding RootFolders}">
    <TreeView.ItemTemplate>
        <TreeDataTemplate ItemsSource="{Binding Children}">
            <TextBlock Text="{Binding Name}" />
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>

<!-- Tab control -->
<TabControl>
    <TabItem Header="General">
        <TextBlock Text="General settings here" Margin="10" />
    </TabItem>
    <TabItem Header="Advanced">
        <TextBlock Text="Advanced settings here" Margin="10" />
    </TabItem>
    <TabItem Header="About">
        <TextBlock Text="Version 1.0" Margin="10" />
    </TabItem>
</TabControl>
```

### Progress and Status

```xml
<!-- Determinate progress -->
<ProgressBar Value="{Binding DownloadProgress}"
             Maximum="100"
             ShowProgressText="True" />

<!-- Indeterminate (spinning) -->
<ProgressBar IsIndeterminate="True" />

<!-- Expander (collapsible section) -->
<Expander Header="Advanced Options" IsExpanded="False">
    <StackPanel Spacing="8" Margin="0,8,0,0">
        <CheckBox Content="Enable logging" />
        <CheckBox Content="Verbose output" />
    </StackPanel>
</Expander>
```

### Dialogs and Overlays

Avalonia does not have a built-in modal dialog system like web browsers' `alert()` and `confirm()`. Instead, you typically use the window system:

```csharp
// Show a message dialog
var dialog = new Window
{
    Title = "Confirm Delete",
    Width = 400,
    Height = 200,
    WindowStartupLocation = WindowStartupLocation.CenterOwner,
    Content = new StackPanel
    {
        Margin = new Thickness(20),
        Spacing = 16,
        Children =
        {
            new TextBlock
            {
                Text = "Are you sure you want to delete this item?",
                TextWrapping = TextWrapping.Wrap
            },
            new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                Children =
                {
                    new Button { Content = "Cancel" },
                    new Button { Content = "Delete", Classes = { "danger" } }
                }
            }
        }
    }
};

await dialog.ShowDialog(parentWindow);
```

Or you can use a community library like `DialogHost.Avalonia` for overlay-style dialogs.

## What Is Coming in Avalonia 12

Avalonia 12 is currently in preview (Preview 1 was released in February 2026) and is expected to reach stable release in Q4 2026. The two guiding themes are **Performance** and **Stability**.

### Performance and Stability Focus

Unlike Avalonia 11, which was a massive release adding multiple new platforms and a completely new compositional renderer, Avalonia 12 is deliberately conservative. The goal is a rock-solid foundation that the ecosystem can build on for years. Some of the largest enterprise users are already running nightly builds in production to access Android performance improvements.

On the Android platform specifically, Avalonia 12 includes a new dispatcher implementation based on Looper and MessageQueue that improves scheduling reliability. GPU and CPU underutilisation at high refresh rates has been addressed. Multiple activities with Avalonia content are now supported.

### Breaking Changes You Need to Know

**Minimum target is now .NET 8.** Support for `netstandard2.0` and `.NET Framework 4.x` has been dropped. According to Avalonia's telemetry, these targets account for less than 4% of projects. The team has committed to supporting .NET 8 for the full lifecycle of Avalonia 12.

**SkiaSharp 3.0 is required.** SkiaSharp 2.88 support has been removed.

**Compiled bindings are now the default.** The `AvaloniaUseCompiledBindingsByDefault` property is now `true` by default. Any `{Binding}` usage in AXAML maps to `{CompiledBinding}`. This means your bindings are faster and errors are caught at build time, but it also means you must specify `x:DataType` on your views.

**Binding plugins removed.** The binding plugin system (including the data annotations validation plugin) has been removed. This was effectively unused by most developers and conflicted with popular frameworks like CommunityToolkit.Mvvm.

**Window decorations overhaul.** A new `WindowDrawnDecorations` class replaces the old `TitleBar`, `CaptionButtons`, and `ChromeOverlayLayer` types. The `SystemDecorations` property has been renamed to `WindowDecorations`. This enables themeable, fully-drawn window chrome.

**Selection behavior unified.** Touch and pen input now triggers selection on pointer release (not press), matching native platform conventions.

**TopLevel changes.** A `TopLevel` object is no longer necessarily at the root of the visual hierarchy. Code that casts the top Visual to `TopLevel` will break. Use `TopLevel.GetTopLevel(visual)` instead.

### Migration from Avalonia 11

If you have been addressing deprecation warnings in Avalonia 11, migration should be straightforward. The team has published a complete breaking changes guide. Here is a practical migration checklist:

```xml
<!-- Before (Avalonia 11) -->
<Window SystemDecorations="Full" ... >

<!-- After (Avalonia 12) -->
<Window WindowDecorations="Full" ... >
```

```csharp
// Before (Avalonia 11)
var topLevel = (TopLevel)visual.GetVisualRoot()!;

// After (Avalonia 12)
var topLevel = TopLevel.GetTopLevel(visual)!;
```

```xml
<!-- Before (Avalonia 11) — might work without x:DataType -->
<TextBlock Text="{Binding Name}" />

<!-- After (Avalonia 12) — x:DataType required for compiled bindings -->
<UserControl x:DataType="vm:MyViewModel" ...>
    <TextBlock Text="{Binding Name}" />
</UserControl>
```

### WebView Going Open Source

One of the most exciting announcements for Avalonia 12 is that the WebView control is going open source. Previously, WebView was a commercial-only feature in Avalonia's Accelerate product. The WebView uses native platform web rendering (Edge WebView2 on Windows, WebKit on macOS/iOS, WebView on Android) rather than bundling Chromium, keeping your application lean.

The Avalonia team acknowledged that embedding web content has become a baseline requirement for many applications — OAuth flows, documentation rendering, rich content display — and gating it behind a commercial licence was no longer the right decision. The open-source WebView will ship in an upcoming Avalonia 12 pre-release.

### New Table Control

Avalonia 12 will include a new read-only `Table` control for displaying tabular data. This is entirely open-source and free. For complex data grids with editing, sorting, and advanced features, the existing open-source `TreeDataGrid` remains available (and can be forked), or commercial offerings provide additional capabilities.

## Beyond Avalonia 12: The Rendering Revolution

### The Vello Experiment

Avalonia's rendering has been built on SkiaSharp since the project's earliest days. SkiaSharp provides .NET bindings for Skia, Google's 2D graphics library that also powers Chrome and (formerly) Flutter. It is mature, stable, and well-understood.

But Avalonia is now exploring GPU-first rendering as a next step. Among several approaches being investigated, Vello — a modern graphics engine written in Rust — has shown particularly interesting early results.

Vello is "GPU-first" by design. Traditional rendering pipelines (including Skia) perform most work on the CPU and use the GPU primarily for final compositing. Vello inverts this model, pushing nearly all rendering computation to the GPU using compute shaders.

Early stress testing shows tens of thousands of animated vector paths running at smooth 120 FPS. In certain workloads, the Avalonia team observed Vello performing up to 100x faster than SkiaSharp. Even when running through a Skia-compatibility shim built on top of Vello, they saw 8x speed improvements.

The community has already started building on this. Wiesław Šoltés has published VelloSharp, a .NET binding library for Vello with Avalonia integration packages, including chart controls and canvas controls powered by Vello rendering.

However, Vello is not a drop-in replacement. SkiaSharp will remain the default renderer for the foreseeable future. The Vello work will ship as experimental backends during the Avalonia 12 lifecycle.

### The Impeller Partnership with Google

In a surprising move, the Avalonia team announced a partnership with Google's Flutter engineers to bring Impeller — Flutter's next-generation GPU-first renderer — to .NET.

Impeller was created to solve real-world performance challenges Flutter encountered with Skia, particularly shader compilation "jank" (visible stuttering the first time a shader is compiled on a device). It pre-compiles all shader pipelines at build time, eliminating runtime compilation entirely.

Why Impeller over Vello? Early testing revealed an important tradeoff: while Vello achieved identical frame rates to Impeller in benchmarks, it required roughly twelve times more power to do so. For battery-powered mobile devices, that difference is significant.

Flutter's production benchmarks with Impeller show impressive improvements: faster SVG and path rendering, improved Gaussian blur throughput, frame times for complex clipping reduced from 450ms with Skia to 11ms with Impeller, no shader compilation stutter, and around 100MB less memory usage.

The Impeller integration is experimental and all development is happening in public. The goal is to benefit not just Avalonia but the entire .NET ecosystem.

### Avalonia MAUI: Bringing Linux and WASM to .NET MAUI

In another ambitious initiative, the Avalonia team is building handlers that let .NET MAUI applications run on Linux and WebAssembly — two platforms that Microsoft's MAUI does not support. The first preview was announced in March 2026, running on .NET 11 (itself in preview).

The approach works by building a single set of Avalonia-based handlers that map MAUI controls to Avalonia equivalents. Because Avalonia already includes a SkiaSharp-based renderer, it can leverage the existing `Microsoft.Maui.Graphics` and `SkiaSharp.Controls.Maui` libraries. This means many MAUI controls work with minimal changes.

This work has also been driving improvements back into Avalonia itself, with new controls like `SwipeView` and API enhancements like letter-spacing support propagated to every control.

## Licensing and Costs

This is an important topic for the My Blazor Magazine audience, since our philosophy is that everything should be free — no "free for non-commercial" caveats.

**Avalonia UI core framework: MIT license, free forever.** You can build and ship commercial applications with it, no payment required, no restrictions. This is not changing.

**Avalonia Accelerate** is the commercial tooling suite built around the framework. It includes a rewritten Visual Studio extension, Dev Tools (a runtime inspector), and Parcel (a packaging tool). Accelerate has a Community Edition that is free for individual developers, small organizations (fewer than 250 people / less than €1M revenue), and educational institutions. Enterprise organizations need a paid license only if they want to use these new Accelerate tools — they can always use the core framework and the legacy open-source tooling for free.

**JetBrains Rider and VS Code extensions remain free** regardless of organization size.

For our project, we can use Avalonia without any cost, forever. The core framework, the community tooling, and the IDE extensions for Rider and VS Code are all free.

## Setting Up an Avalonia Project with Modern .NET Practices

Here is how to set up an Avalonia project using the same modern .NET practices we use in My Blazor Magazine — `.slnx` solution format, `Directory.Build.props`, and central package management:

### global.json

```json
{
  "sdk": {
    "version": "10.0.104",
    "rollForward": "latestFeature"
  }
}
```

### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>
</Project>
```

### Directory.Packages.props

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <AvaloniaVersion>11.3.0</AvaloniaVersion>
    <CommunityToolkitVersion>8.4.0</CommunityToolkitVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Avalonia" Version="$(AvaloniaVersion)" />
    <PackageVersion Include="Avalonia.Desktop" Version="$(AvaloniaVersion)" />
    <PackageVersion Include="Avalonia.iOS" Version="$(AvaloniaVersion)" />
    <PackageVersion Include="Avalonia.Android" Version="$(AvaloniaVersion)" />
    <PackageVersion Include="Avalonia.Browser" Version="$(AvaloniaVersion)" />
    <PackageVersion Include="Avalonia.Themes.Fluent" Version="$(AvaloniaVersion)" />
    <PackageVersion Include="Avalonia.Fonts.Inter" Version="$(AvaloniaVersion)" />
    <PackageVersion Include="Avalonia.Diagnostics" Version="$(AvaloniaVersion)" />
    <PackageVersion Include="CommunityToolkit.Mvvm"
                    Version="$(CommunityToolkitVersion)" />

    <!-- Testing -->
    <PackageVersion Include="Avalonia.Headless.XUnit" Version="$(AvaloniaVersion)" />
    <PackageVersion Include="xunit.v3" Version="3.2.2" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
  </ItemGroup>
</Project>
```

### Solution File (MyApp.slnx)

```xml
<Solution>
  <Folder Name="/Solution Items/">
    <File Path="Directory.Build.props" />
    <File Path="Directory.Packages.props" />
    <File Path="global.json" />
  </Folder>
  <Folder Name="/src/">
    <Project Path="src/MyApp/MyApp.csproj" />
    <Project Path="src/MyApp.Desktop/MyApp.Desktop.csproj" />
    <Project Path="src/MyApp.Android/MyApp.Android.csproj" />
    <Project Path="src/MyApp.iOS/MyApp.iOS.csproj" />
    <Project Path="src/MyApp.Browser/MyApp.Browser.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/MyApp.Tests/MyApp.Tests.csproj" />
  </Folder>
</Solution>
```

## Testing Avalonia Applications

Avalonia supports headless testing — running your UI without a visible window. This is perfect for CI/CD pipelines:

```csharp
using Avalonia.Headless.XUnit;
using MyApp.ViewModels;
using MyApp.Views;
using Xunit;

namespace MyApp.Tests;

public class MainWindowTests
{
    [AvaloniaFact]
    public void MainWindow_Should_Render_Title()
    {
        var window = new MainWindow
        {
            DataContext = new MainWindowViewModel()
        };

        window.Show();

        // Find the title TextBlock by name
        var title = window.FindControl<TextBlock>("PageTitle");
        Assert.NotNull(title);
        Assert.Equal("Dashboard", title.Text);
    }

    [AvaloniaFact]
    public void Button_Click_Should_Increment_Counter()
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };

        window.Show();

        Assert.Equal(0, vm.ClickCount);

        vm.IncrementCountCommand.Execute(null);

        Assert.Equal(1, vm.ClickCount);
    }
}
```

The `[AvaloniaFact]` attribute (from `Avalonia.Headless.XUnit`) sets up the Avalonia runtime in headless mode before each test.

## Putting It All Together: A Production Architecture

Here is a summary architecture for a production cross-platform Avalonia application:

```
MyProductionApp/
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── MyApp.slnx
│
├── src/
│   ├── MyApp/                          # Shared library
│   │   ├── MyApp.csproj
│   │   ├── App.axaml                   # Application root
│   │   ├── App.axaml.cs
│   │   ├── ViewLocator.cs
│   │   ├── Models/                     # Domain objects
│   │   ├── ViewModels/                 # MVVM ViewModels
│   │   ├── Services/                   # Business logic
│   │   │   ├── IDataService.cs
│   │   │   ├── SqliteDataService.cs
│   │   │   └── ApiDataService.cs
│   │   ├── Views/
│   │   │   ├── Desktop/                # Desktop-specific views
│   │   │   ├── Mobile/                 # Mobile-specific views
│   │   │   └── Shared/                 # Shared components
│   │   └── Styles/
│   │       ├── Desktop.axaml
│   │       └── Mobile.axaml
│   │
│   ├── MyApp.Desktop/                  # Desktop entry point
│   │   ├── MyApp.Desktop.csproj
│   │   └── Program.cs
│   │
│   ├── MyApp.Android/                  # Android entry point
│   │   ├── MyApp.Android.csproj
│   │   └── MainActivity.cs
│   │
│   ├── MyApp.iOS/                      # iOS entry point
│   │   ├── MyApp.iOS.csproj
│   │   └── AppDelegate.cs
│   │
│   └── MyApp.Browser/                  # WebAssembly entry point
│       ├── MyApp.Browser.csproj
│       └── Program.cs
│
└── tests/
    └── MyApp.Tests/
        ├── MyApp.Tests.csproj
        ├── ViewModelTests/
        └── ViewTests/
```

The shared library (`MyApp`) contains all your views, view models, models, and services. The platform-specific projects (`MyApp.Desktop`, `MyApp.Android`, etc.) are thin wrappers that just configure the platform entry point and reference the shared library.

## Conclusion

Avalonia UI occupies a unique position in the .NET ecosystem. It is the only framework that gives you pixel-perfect consistency across Windows, macOS, Linux, iOS, Android, and WebAssembly from a single codebase, using familiar XAML-based tooling. The MIT license means you can use it for anything, forever, at no cost.

The current stable release (11.3) is production-ready and used by major companies. Container Queries bring modern responsive design patterns to native application development. The `OnPlatform` and `OnFormFactor` markup extensions make it straightforward to customize behavior per platform and device type.

Avalonia 12 (currently in preview, targeting Q4 2026 stable release) doubles down on performance and stability, with significant Android improvements, compiled bindings by default, a new open-source WebView, and a new Table control. The upcoming rendering revolution — with experimental Vello backends and the Impeller partnership with Google — points toward a future where Avalonia applications run faster than ever on modern GPU hardware.

If you are a web developer looking to build native cross-platform applications without leaving the .NET ecosystem, Avalonia is the most compelling option available today. The learning curve from web development is manageable — AXAML is conceptually similar to HTML, Avalonia's styling system borrows heavily from CSS concepts, and the MVVM pattern maps naturally to the component-based architecture you already know.

The best way to learn is to build something. Install the templates, create a project, and start experimenting. The community is active on GitHub and the Avalonia documentation continues to improve rapidly.

Welcome to the world of truly cross-platform native development.

## Resources

- **Official Documentation**: [docs.avaloniaui.net](https://docs.avaloniaui.net)
- **GitHub Repository**: [github.com/AvaloniaUI/Avalonia](https://github.com/AvaloniaUI/Avalonia) (30,000+ stars)
- **Sample Projects**: [github.com/AvaloniaUI/Avalonia.Samples](https://github.com/AvaloniaUI/Avalonia.Samples)
- **Avalonia 12 Breaking Changes**: [docs.avaloniaui.net/docs/avalonia12-breaking-changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
- **Container Queries Documentation**: [docs.avaloniaui.net/docs/basics/user-interface/styling/container-queries](https://docs.avaloniaui.net/docs/basics/user-interface/styling/container-queries)
- **Platform-Specific XAML**: [docs.avaloniaui.net/docs/guides/platforms/platform-specific-code/xaml](https://docs.avaloniaui.net/docs/guides/platforms/platform-specific-code/xaml)
