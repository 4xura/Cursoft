using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Cursoft.Models;
using Cursoft.Services;
using Microsoft.Win32;

namespace Cursoft.Views;

public partial class MainWindow : Window
{
	private const string PixelSuffix = "px";
	private const string CurExtension = ".cur";
	private const string AniExtension = ".ani";
	private const string EmptyValue = "";
	private const string FileSearchPattern = "*.*";
	private const string PresetDragFormatName = "Cursoft.PresetId";
	private const string GroupDragFormatName = "Cursoft.GroupId";
	private const double ReorderIndicatorWidth = 4;
	private const double ReorderRowGroupingTolerance = 4;

	private const string BrushTextDim = "Brush.TextDim";
	private const string BrushBg = "Brush.Bg";
	private const string BrushAccent = "Brush.Accent";
	private const string BrushBorder = "Brush.Border";
	private const string BrushSurface = "Brush.Surface";
	private const string BrushSurfaceHover = "Brush.SurfaceHover";

	private const string LocWindowsDefault = "S.WindowsDefault";
	private const string LocMenuEdit = "S.Menu.Edit";
	private const string LocMenuRename = "S.Menu.Rename";
	private const string LocMenuMoveLeft = "S.Menu.MoveLeft";
	private const string LocMenuMoveRight = "S.Menu.MoveRight";
	private const string LocMenuDownload = "S.Menu.Download";
	private const string LocMenuDelete = "S.Menu.Delete";
	private const string LocMenuUseScaling = "S.Menu.UseScaling";
	private const string LocMenuScaleModeNearest = "S.Menu.ScaleMode.Nearest";
	private const string LocMenuScaleModeSmooth = "S.Menu.ScaleMode.Smooth";
	private const string LocPresetContextHint = "S.Preset.ContextHint";
	private const string LocAddPreset = "S.AddPreset";
	private const string LocAddPresetHint = "S.AddPreset.Hint";
	private const string LocErrorApplyFailed = "S.Error.ApplyFailed";
	private const string LocErrorTitle = "S.Error.Title";
	private const string LocErrorSaveFailed = "S.Error.SaveFailed";
	private const string LocConfirmDeleteText = "S.ConfirmDelete.Text";
	private const string LocConfirmDeleteTitle = "S.ConfirmDelete.Title";
	private const string LocToastSaved = "S.Toast.Saved";
	private const string LocToastSizeApplied = "S.Toast.SizeApplied";
	private const string LocToastPresetDownloaded = "S.Toast.PresetDownloaded";
	private const string LocDefaultPresetName = "S.DefaultPresetName";
	private const string LocToastImported = "S.Toast.Imported";
	private const string LocImportFileFilter = "S.Import.FileFilter";
	private const string LocErrorImportUnrecognized = "S.Error.ImportUnrecognized";
	private const string LocErrorImportVersionUnsupported = "S.Error.ImportVersionUnsupported";
	private const string LocErrorImportNothingFound = "S.Error.ImportNothingFound";
	private const string LocMenuDownloadSystemCursors = "S.Menu.DownloadSystemCursors";
	private const string LocMenuDownloadSystemPngGif = "S.Menu.DownloadSystemPngGif";
	private const string LocMenuDownloadSystemCurAni = "S.Menu.DownloadSystemCurAni";
	private const string LocToastSystemCursorsDownloaded = "S.Toast.SystemCursorsDownloaded";
	private const string LocMenuQuickDownload = "S.Menu.QuickDownload";
	private const string LocExportAsLinuxArchive = "S.Export.AsLinuxArchive";
	private const string LocExportAsXcursorTheme = "S.Export.AsXcursorTheme";
	private const string LocExportAsFullPackage = "S.Export.AsFullPackage";
	private const string LocToastExportedLinuxArchive = "S.Toast.ExportedLinuxArchive";
	private const string LocToastExportedXcursorTheme = "S.Toast.ExportedXcursorTheme";
	private const string LocToastExportedFullPackage = "S.Toast.ExportedFullPackage";

	private const string SpinnerStoryboardKey = "SpinnerStoryboard";

	private const string StyleAccentButton = "Style.AccentButton";
	private const string StyleButton = "Style.Button";
	private const string StyleTextBox = "Style.TextBox";

	private const double CellSize = 148;
	private const double CellMargin = 6;
	private const double CellCornerRadius = 20;
	private const double CellBorderThickness = 2;
	private const double CellPreviewSize = 48;
	private const double CellNameFontSize = 13;
	private const double CellCountFontSize = 11;
	private const double CellSizeFontSize = 11;
	private const string MixedBadgeText = "🧩";
	private const double MixedBadgeFontSize = 15;
	private const string LocMixedBadgeTooltip = "S.Gallery.MixedBadgeTooltip";

	private const double GroupOutlinePadding = 7;
	private const double SelectionBorderThickness = 4;
	private const string SelectionBadgeText = "✓";
	private const double SelectionBadgeSize = 20;
	private const double SelectionBadgeFontSize = 12;
	private const double GroupSwatchSize = 22;
	private const double GroupSwatchRingThickness = 2.5;
	private const double GroupAttachZoneMargin = 0.25;
	private const string LocMenuRemoveFromGroup = "S.Menu.RemoveFromGroup";
	private const string LocMenuAssignToGroup = "S.Menu.AssignToGroup";
	private const string LocMenuEditGroup = "S.Menu.EditGroup";
	private const string LocMenuCreateGroup = "S.Menu.CreateGroup";
	private const string LocMenuUngroup = "S.Menu.Ungroup";
	private const string LocMenuDeleteGroup = "S.Menu.DeleteGroup";
	private const string LocMenuConsolidateGroup = "S.Menu.ConsolidateGroup";
	private const string LocMenuRandomPreset = "S.Menu.RandomPreset";
	private const string LocGroupDefaultName = "S.Group.DefaultName";
	private const string LocGroupSelectedCount = "S.Group.SelectedCount";
	private const string LocGroupMembersCount = "S.Group.MembersCount";
	private const string LocGroupCollapsedTooltip = "S.Group.CollapsedTooltip";
	private const string LocGroupExpandedTooltip = "S.Group.ExpandedTooltip";
	private const string LocGroupToastCreated = "S.Group.Toast.Created";
	private const string LocGroupToastConsolidated = "S.Group.Toast.Consolidated";
	private const string LocGroupToastUngrouped = "S.Group.Toast.Ungrouped";
	private const string LocGroupToastDeleted = "S.Group.Toast.Deleted";
	private const string BrushText = "Brush.Text";

	private const string LocInfoTitle = "S.Info.Title";
	private const string LocErrorArchiveExtractFailed = "S.Error.ArchiveExtractFailed";

	private const double UiZoomStep = 0.1;
	private const string ThemeIconDark = "M18,14 A8,8 0 1 1 8,3 A8,8 0 0 0 18,14 Z";
	private const string ThemeIconLight = "M12,8 A4,4 0 1 1 4,8 A4,4 0 1 1 12,8 M8,0 V2 M8,14 V16 M0,8 H2 M14,8 H16 M2.4,2.4 L3.8,3.8 M12.2,12.2 L13.6,13.6 M2.4,13.6 L3.8,12.2 M12.2,3.8 L13.6,2.4";
	private const string ExpandIconUri = "pack://application:,,,/Resources/ExpandIcon32.png";
	private const string StairIconUri = "pack://application:,,,/Resources/StairIcon24.png";
	private const string ExportIconUri = "pack://application:,,,/Resources/DownloadIcon32.png";
	private const string ImportIconUri = "pack://application:,,,/Resources/LinkIcon32.png";

	private sealed record BoardEntry(Preset? Preset, PresetGroup? Group, int BoardIndex);

	private List<Preset> _presets = new();
	private List<PresetGroup> _groups = new();
	private Dictionary<string, PresetGroup> _presetToGroup = new();
	private List<string> _boardOrderIds = new();
	private List<string> _visibleBoardIds = new();
	private readonly List<BoardEntry> _boardOrder = new();
	private readonly HashSet<string> _selectedPresetIds = new();
	private readonly List<Border> _groupColorSwatches = new();
	private string? _pendingGroupColorKey;
	private string? _activePresetId;
	private TextBlock? _activeCellSizeText;
	private readonly Dictionary<string, TextBlock> _cellSizeTexts = new();
	private TextBlock? _defaultCellSizeText;
	private double _cellScale = AppState.GalleryCellScaleDefault;
	private double _uiScale = AppState.UiScaleDefault;
	private bool _cellScaleReady;
	private int _baselineSizePx;
	private Point? _presetDragStartPoint;
	private bool _justDraggedPreset;
	private int? _pendingInsertIndex;
	private string? _pendingGroupAttachId;
	private string? _draggedPresetId;
	private string? _draggedGroupId;
	private bool _justDraggedGroup;
	private Dictionary<string, string>? _activeSourceValues;
	private bool _activeUseScaling;
	private IReadOnlyList<double> _appliedMonitorScales = [];
	private ScaleMode _activeScaleMode = ScaleMode.AreaWeighted;

	public MainWindow()
	{
		InitializeComponent();

		Width = AppState.GetMainWindowWidth();
		Height = AppState.GetMainWindowHeight();

		_activePresetId = AppState.GetActivePresetId();

		_baselineSizePx = RegistryCursorService.GetBaseSize();
		SetSliderSilently(_baselineSizePx);

		_uiScale = AppState.GetUiScale();
		ApplyUiScale(_uiScale);

		_cellScale = AppState.GetGalleryCellScale();
		SetCellScaleSliderSilently(_cellScale);

		UpdateThemeToggleIcon();
		UpdateLanguageButtonText();
		UpdateCursorEffectsButton();
		try { StartupCheckBox.IsChecked = SignInStartup.IsEnabled(); }
		catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
		{
			StartupCheckBox.IsEnabled = false;
			StartupCheckBox.ToolTip = ex.Message;
		}

		UpdateOpenFolderToggleIcon();


		BuildGroupColorSwatches();
		ReloadGallery();
		UpdateUndoButton();

		InitializeResizing();

		_appliedMonitorScales = DisplayDpiService.GetDistinctMonitorScales();
		SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
		SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

		_ = Task.Run(CursorCacheService.RemoveUnreferencedFiles);

	}

	private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];

	private double CellFontScale => Math.Sqrt(_cellScale);

	protected override void OnClosing(CancelEventArgs e)
	{
		base.OnClosing(e);
		((App)Application.Current).HandleMainWindowClosing(this, e);
	}

	protected override void OnClosed(EventArgs e)
	{
		AppState.SetMainWindowSize(Width, Height);
		SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
		SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
		base.OnClosed(e);
	}
}

public sealed class RelayUiCommand(Action execute) : ICommand
{
	public event EventHandler? CanExecuteChanged { add { } remove { } }
	public bool CanExecute(object? parameter) => true;
	public void Execute(object? parameter) => execute();
}
