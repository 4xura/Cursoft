using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cursoft.Models;
using Cursoft.Services;

namespace Cursoft.Views;

public partial class ExportWindow : Window
{
	private readonly IReadOnlyList<Preset> _presets;
	private readonly Dictionary<string, CheckBox> _choices = new();
	private readonly List<(PresetGroup Group, CheckBox Box)> _groups = new();
	private bool _updating;

	public ExportWindow(IReadOnlyList<Preset> presets, IReadOnlyList<PresetGroup>? groups = null)
	{
		InitializeComponent();
		_presets = presets;
		var empty = presets.Count == 0;
		var width = empty ? 458 : Width;
		var scale = Math.Min(AppState.GetUiScale(), Math.Min(SystemParameters.WorkArea.Width / width, SystemParameters.WorkArea.Height / 465));
		UiScaleTransform.ScaleX = UiScaleTransform.ScaleY = scale;
		Width = width * scale;

		EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
		ExportFields.Visibility = ExportAction.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
		Dismiss.Margin = new Thickness(0, 0, empty ? 0 : 8, 0);
		if (empty)
		{
			Dismiss.SetResourceReference(ContentProperty, "S.About.Close");
			return;
		}

		foreach (var group in groups ?? Array.Empty<PresetGroup>())
		{
			var box = AddChoice(group.Name, Loc.Format("S.Group.MembersCount", group.MemberPresetIds.Count));
			_groups.Add((group, box));
			void SelectMembers(object sender, RoutedEventArgs e)
			{
				if (!_updating)
					SetChoices(group.MemberPresetIds, box.IsChecked == true);
			}
			box.Checked += SelectMembers;
			box.Unchecked += SelectMembers;
		}
		foreach (var preset in presets)
		{
			var path = PresetStore.GetRoleFilePath(preset, CursorRoles.ArrowRoleName) ??
				preset.Roles.Keys.Concat(preset.RoleRefs.Keys).Select(key => PresetStore.GetRoleFilePath(preset, key)).FirstOrDefault(file => file != null);
			var icon = new Image { Width = 24, Height = 24, Margin = new Thickness(0, 0, 9, 0) };
			CursorPreviewService.ApplyPreview(icon, path);
			var box = AddChoice(preset.Name, $"{preset.BaseSize} px", icon);
			_choices.Add(preset.Id, box);
			box.Checked += OnSelectionChanged;
			box.Unchecked += OnSelectionChanged;
		}
		UpdateSelection();
	}

	private CheckBox AddChoice(string name, string detail, Image? preview = null)
	{
		var line = new DockPanel();
		if (preview != null)
		{
			DockPanel.SetDock(preview, Dock.Left);
			line.Children.Add(preview);
		}
		var metadata = new TextBlock { Text = detail, FontSize = 11, Margin = new Thickness(7, 0, 0, 0),
			VerticalAlignment = VerticalAlignment.Center, Foreground = (Brush)FindResource("Brush.TextDim") };
		DockPanel.SetDock(metadata, Dock.Right);
		line.Children.Add(metadata);
		line.Children.Add(new TextBlock { Text = name, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center });
		var box = new CheckBox { Content = line, IsChecked = true, Style = (Style)FindResource("ExportChoice"),
			ToolTip = name, Margin = new Thickness(0, 4, 0, 4), MinHeight = 30 };
		System.Windows.Automation.AutomationProperties.SetName(box, name);
		PresetRows.Children.Add(box);
		return box;
	}

	private void OnSelectionChanged(object sender, RoutedEventArgs e)
	{
		if (!_updating)
			UpdateSelection();
	}

	private void SetChoices(IEnumerable<string> ids, bool selected)
	{
		_updating = true;
		try
		{
			foreach (var id in ids)
				if (_choices.TryGetValue(id, out var choice)) choice.IsChecked = selected;
		}
		finally { _updating = false; }
		UpdateSelection();
	}

	private void UpdateSelection()
	{
		_updating = true;
		try
		{
			foreach (var (group, box) in _groups)
				box.IsChecked = group.MemberPresetIds.Count > 0 && group.MemberPresetIds.All(id => _choices.GetValueOrDefault(id)?.IsChecked == true);
		}
		finally { _updating = false; }
		var selected = _choices.Values.Count(box => box.IsChecked == true);
		SelectionCountText.Text = Loc.Format("S.Import.SelectionCount", selected, _choices.Count);
		ExportAction.IsEnabled = selected > 0;
	}

	private void OnSelectAllClick(object sender, RoutedEventArgs e) => SetChoices(_choices.Keys, true);
	private void OnSelectNoneClick(object sender, RoutedEventArgs e) => SetChoices(_choices.Keys, false);
	private void OnInfoButtonClick(object sender, RoutedEventArgs e) =>
		new InfoHelpWindow(Loc.Get("S.Info.Title"), HelpTextService.Get("Export")) { Owner = this }.ShowDialog();

	private void OnGuideClick(object sender, RoutedEventArgs e)
	{
		try
		{
			var path = PresetPackageService.DownloadReadme();
			ToastService.Show(RootGrid, Loc.Format("S.Toast.ReadmeDownloaded", System.IO.Path.GetFileName(path)));
			if (AppState.GetOpenFolderAfterDownload()) ExplorerService.RevealFile(path);
		}
		catch (Exception ex) { ShowExportError(ex); }
	}

	private void OnExportClick(object sender, RoutedEventArgs e)
	{
		var selected = _presets.Where(preset => _choices[preset.Id].IsChecked == true).ToList();
		if (selected.Count == 0) return;
		try
		{
			var name = ExportNameBox.Text;
			var (result, message) = FormatChoice.SelectedIndex switch
			{
				1 => (PresetPackageService.ExportArchive(selected, name), "S.Toast.ExportedArchive"),
				2 => (PresetPackageService.ExportLinuxArchive(selected, name), "S.Toast.ExportedLinuxArchive"),
				3 => (PresetPackageService.ExportXcursorTheme(selected, name), "S.Toast.ExportedXcursorTheme"),
				_ => (PresetPackageService.ExportBundle(selected, name), "S.Toast.ExportedBundle"),
			};
			ToastService.Show(RootGrid, Loc.Format(message, result.Count, System.IO.Path.GetFileName(result.Path)));
			if (AppState.GetOpenFolderAfterDownload()) ExplorerService.RevealFile(result.Path);
		}
		catch (Exception ex) { ShowExportError(ex); }
	}

	private void ShowExportError(Exception ex) =>
		MessageBox.Show(this, Loc.Format("S.Error.ExportFailed", ex.Message), Loc.Get("S.Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
}
