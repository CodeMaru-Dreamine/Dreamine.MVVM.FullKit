using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using Wedding.Layouts.Contracts;
using Wedding.Layouts.Editor.Wpf.Presentation;
using Wedding.Layouts.Editor.Wpf.Preview;
using Wedding.Layouts.Editor.Wpf.Services;

namespace Wedding.Layouts.Editor.Wpf;

public partial class MainWindow : Window
{
    private const int MaximumPolicyResponseBytes = 32 * 1024;

    private static readonly HttpClient PolicyHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(6),
        MaxResponseContentBufferSize = MaximumPolicyResponseBytes,
    };

    private static readonly HashSet<LayoutBlockKind> StructuralKinds =
    [
        LayoutBlockKind.Page,
        LayoutBlockKind.Section,
        LayoutBlockKind.Container,
        LayoutBlockKind.Stack,
        LayoutBlockKind.Grid,
        LayoutBlockKind.Card,
        LayoutBlockKind.Hero,
    ];

    private static readonly HashSet<LayoutBlockKind> TextKinds =
    [
        LayoutBlockKind.Heading,
        LayoutBlockKind.Text,
        LayoutBlockKind.Button,
    ];

    private static readonly HashSet<LayoutBlockKind> ImageKinds =
    [
        LayoutBlockKind.Image,
        LayoutBlockKind.Hero,
        LayoutBlockKind.Gallery,
    ];

    private static readonly HashSet<LayoutBlockKind> ActionKinds =
    [
        LayoutBlockKind.Button,
        LayoutBlockKind.Navigation,
    ];

    private LayoutPackage _package = CreateStarterPackage();
    private int[]? _selectedBlockPath;
    private string? _filePath;
    private bool _openedPackageHasCompatibilityTier;
    private bool _loadingPackageIntoEditor;
    private bool _refreshingEasyEditor;
    private bool _refreshingBlockEditor;
    private bool _refreshingBlockTree;
    private bool _restoringBlockSelection;
    private bool _blockEditorDirty;
    private bool _transitionPreviewRunning;
    private bool _isDirty;
    private bool _previewAudioPlaying;
    private bool _previewVideoPlaying;
    private LayoutPresentationMode _easyPreviewPresentation =
        LayoutPresentationMode.Flow;
    private bool _easyPreviewIsPhone = true;
    private int _easyPreviewPageIndex;
    private int[]? _easyEditableTextPath;
    private int _tierPolicyRequestVersion;
    private PreviewMediaSet _previewMedia =
        PreviewMediaService.LoadBundledSample();

    public IReadOnlyList<LayoutPresentationMode> PresentationOptions { get; } =
        Enum.GetValues<LayoutPresentationMode>();

    public IReadOnlyList<LayoutTransitionKind> TransitionKindOptions { get; } =
        Enum.GetValues<LayoutTransitionKind>();

    public IReadOnlyList<LayoutStyleToken> StyleTokenOptions { get; } =
        Enum.GetValues<LayoutStyleToken>();

    public IReadOnlyList<LayoutBlockKind> BlockKindOptions { get; } =
        Enum.GetValues<LayoutBlockKind>();

    public IReadOnlyList<LayoutBindingKey> BindingOptions { get; } =
        Enum.GetValues<LayoutBindingKey>();

    public IReadOnlyList<LayoutVisualVariant> VariantOptions { get; } =
        Enum.GetValues<LayoutVisualVariant>();

    public IReadOnlyList<LayoutGap> GapOptions { get; } =
        Enum.GetValues<LayoutGap>();

    public ObservableCollection<EditableStyleToken> StyleTokens { get; } = [];

    public ObservableCollection<EditorSectionItem> EasySections { get; } = [];

    public IReadOnlyList<EditorChoice<LayoutPresentationMode>>
        FriendlyPresentationOptions { get; } =
        EditorDisplayCatalog.PresentationChoices;

    public IReadOnlyList<EditorChoice<LayoutVisualVariant>>
        FriendlyVariantOptions { get; } =
        EditorDisplayCatalog.VariantChoices;

    public IReadOnlyList<EditorChoice<LayoutGap>> FriendlyGapOptions { get; } =
        EditorDisplayCatalog.GapChoices;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LoadPackageIntoEditor(_package, null);
    }

    private void NewStarter_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        LoadPackageIntoEditor(CreateStarterPackage(), null);
        MarkDirty();
        SetStatus("새 블록 기반 샘플 패키지를 만들었습니다.");
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "레이아웃 JSON 패키지 열기",
            Filter = "Layout package (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            var package = JsonSerializer.Deserialize<LayoutPackage>(
                json,
                LayoutPackageJson.CreateOptions());
            if (package is null)
            {
                throw new JsonException("JSON 문서가 비어 있습니다.");
            }

            var validation = LayoutPackageValidator.Validate(package);
            if (!validation.IsValid)
            {
                ShowPackageValidationLoadError(
                    validation.Errors,
                    dialog.FileName);
                return;
            }

            LoadPackageIntoEditor(
                NormalizePackageForEditor(package),
                dialog.FileName);
            ValidatePackage();
            SetStatus("JSON 패키지를 열었습니다.");
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or JsonException)
        {
            ShowLoadError(exception);
        }
    }

    private void Validate_Click(object sender, RoutedEventArgs e)
    {
        ValidatePackage();
    }

    private async void RefreshTierPolicy_Click(object sender, RoutedEventArgs e)
    {
        await RefreshTierPolicyAsync(reportStatus: true);
    }

    private async void KeyTextBox_LostKeyboardFocus(
        object sender,
        System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (!_loadingPackageIntoEditor)
        {
            await RefreshTierPolicyAsync(reportStatus: false);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            SaveAs_Click(sender, e);
            return;
        }

        SaveTo(_filePath);
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "레이아웃 JSON 패키지 저장",
            Filter = "Layout package (*.json)|*.json",
            AddExtension = true,
            DefaultExt = ".json",
            FileName = BuildSuggestedFileName(),
        };

        if (dialog.ShowDialog(this) == true)
        {
            SaveTo(dialog.FileName);
        }
    }

    private void EditorTextValue_Changed(
        object sender,
        TextChangedEventArgs e)
    {
        if (!_refreshingBlockEditor)
        {
            if (sender is FrameworkElement element
                && element.Name.StartsWith("Block", StringComparison.Ordinal))
            {
                _blockEditorDirty = true;
            }

            MarkDirty();
        }
    }

    private void EditorSelectionValue_Changed(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!_refreshingBlockEditor)
        {
            if (sender is FrameworkElement element
                && element.Name.StartsWith("Block", StringComparison.Ordinal))
            {
                _blockEditorDirty = true;
            }

            MarkDirty();
        }
    }

    private void EditorToggleValue_Changed(
        object sender,
        RoutedEventArgs e)
    {
        MarkDirty();
    }

    private void StyleTokensGrid_CellEditEnding(
        object sender,
        DataGridCellEditEndingEventArgs e)
    {
        MarkDirty();
    }

    private void AdvancedModeCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (AdvancedEditorTab is null || EditorModeTabs is null)
        {
            return;
        }

        var showAdvanced = AdvancedModeCheckBox.IsChecked == true;
        AdvancedEditorTab.Visibility = showAdvanced
            ? Visibility.Visible
            : Visibility.Collapsed;
        EditorModeTabs.SelectedItem = showAdvanced
            ? AdvancedEditorTab
            : EasyEditorTab;
        SetStatus(showAdvanced
            ? "배포 정보와 블록의 기술 속성을 표시했습니다."
            : "간편 편집 화면으로 돌아왔습니다.");
    }

    private void ToggleValidationDetails_Click(
        object sender,
        RoutedEventArgs e)
    {
        ValidationDetailsExpander.IsExpanded =
            !ValidationDetailsExpander.IsExpanded;
    }

    private void PreviewDevice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string device })
        {
            return;
        }

        var phone = string.Equals(
            device,
            "Phone",
            StringComparison.OrdinalIgnoreCase);
        _easyPreviewIsPhone = phone;
        EasyPreviewFrame.Width = phone ? 370 : 700;
        EasyPreviewFrame.Height = phone ? 640 : 560;
        EasyPreviewFrame.Padding = phone
            ? new Thickness(12)
            : new Thickness(16);
        EasyPreviewFrame.BorderThickness = phone
            ? new Thickness(7)
            : new Thickness(2);
        EasyPreviewFrame.CornerRadius = phone
            ? new CornerRadius(28)
            : new CornerRadius(10);
        PhonePreviewButton.Background = CreateBrush(
            phone ? "#D8B488" : "#FFFDFC");
        PhonePreviewButton.BorderBrush = CreateBrush(
            phone ? "#B88A58" : "#D8C7B4");
        PcPreviewButton.Background = CreateBrush(
            phone ? "#FFFDFC" : "#D8B488");
        PcPreviewButton.BorderBrush = CreateBrush(
            phone ? "#D8C7B4" : "#B88A58");
        ApplyEasyPresentationPreviewMode(_easyPreviewPresentation);
        SetStatus(phone
            ? "고정된 폰 화면 안에서 스크롤하며 미리 보고 있습니다."
            : "고정된 PC 화면 안에서 스크롤하며 미리 보고 있습니다.");
    }

    private void ShowEasyHelp_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "이 편집기는 실제 신랑·신부 정보를 입력하는 화면이 아니라 "
            + "여러 사용자가 함께 쓸 레이아웃 틀을 만드는 도구입니다.\n\n"
            + "1. 새 레이아웃을 누르거나 기존 JSON을 엽니다.\n"
            + "2. 왼쪽 섹션 버튼을 누릅니다. 이미 있으면 해당 위치로 이동하고, "
            + "없으면 새로 추가됩니다.\n"
            + "3. 가운데 프레임 안에서 마우스 휠로 이동한 뒤 꾸밀 카드를 클릭합니다.\n"
            + "4. 오른쪽에서 모양·칸 수·간격·고정 문구와 순서를 바꿉니다.\n"
            + "5. 필요 없는 섹션은 '선택한 섹션 빼기'로 제거합니다. "
            + "필수 표지와 묶음 섹션은 보호됩니다.\n"
            + "6. 왼쪽의 사진·영상·음악은 분위기 확인용이며 JSON에는 들어가지 않습니다.\n"
            + "7. 문제 확인 → 저장 후 웹사이트의 레이아웃 JSON 업로드에서 제출합니다.\n\n"
            + "화면에 보이는 이름·예식장·계좌·지도는 샘플입니다. "
            + "실제 값은 각 청첩장 관리 페이지에서 입력하면 같은 레이아웃에 자동 연결됩니다.",
            "레이아웃 편집기 사용법",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void EasyPresentationComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_loadingPackageIntoEditor
            || _refreshingEasyEditor
            || EasyPresentationComboBox.SelectedItem
                is not EditorChoice<LayoutPresentationMode> choice)
        {
            return;
        }

        var transition = choice.Value switch
        {
            LayoutPresentationMode.FlipCard => LayoutTransitionKind.FlipCard,
            LayoutPresentationMode.PagedBook => LayoutTransitionKind.PageTurn,
            _ => LayoutTransitionKind.None,
        };
        EasyPresentationDescriptionText.Text = choice.Description;
        UpdateEasyPresentationModeText(choice.Value);
        ApplyEasyPresentationPreviewMode(choice.Value);
        PresentationComboBox.SelectedItem = choice.Value;
        TransitionKindComboBox.SelectedItem = transition;
        _package = _package with
        {
            Definition = _package.Definition with
            {
                Presentation = choice.Value,
                Transition = _package.Definition.Transition with
                {
                    Kind = transition,
                },
            },
        };
        MarkDirty();
        SetStatus($"'{choice.Label}' 방식으로 바꿨습니다.");
    }

    private void ThemeColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string primaryColor })
        {
            return;
        }

        var tokens = ThemePaletteService.CreateFromPrimary(primaryColor);
        _package = _package with
        {
            Definition = _package.Definition with
            {
                StyleTokens = tokens,
            },
        };
        ReloadStyleTokenRows(tokens);
        ApplyEasyThemePreview(tokens);
        RefreshEasySections(_selectedBlockPath);
        EasyThemeStatusText.Text =
            "대표 색상에 맞춰 배경·글자·버튼 색상을 자동으로 조정했습니다.";
        MarkDirty();
        SetStatus("읽기 편한 색상 팔레트를 자동으로 만들었습니다.");
    }

    private void ChoosePreviewPhotos_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "미리보기에 사용할 사진 선택",
            Filter =
                "사진 파일|*.jpg;*.jpeg;*.png;*.bmp|모든 파일|*.*",
            Multiselect = true,
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _previewMedia = PreviewMediaService.WithPhotos(
                _previewMedia,
                dialog.FileNames);
            RefreshPreviewMedia();
            SetStatus(
                $"미리보기 사진 {_previewMedia.PhotoCount}장을 불러왔습니다.");
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or FormatException)
        {
            ShowPreviewMediaError(exception);
        }
    }

    private void ChoosePreviewVideo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "미리보기에 사용할 영상 선택",
            Filter = "영상 파일|*.mp4;*.wmv;*.avi;*.mov|모든 파일|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _previewMedia = PreviewMediaService.WithVideo(
            _previewMedia,
            dialog.FileName);
        RefreshPreviewMedia();
        SetStatus("미리보기 영상을 불러왔습니다.");
    }

    private void ChoosePreviewAudio_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "미리보기에 사용할 배경음악 선택",
            Filter = "음악 파일|*.mp3;*.wav;*.wma;*.m4a|모든 파일|*.*",
            CheckFileExists = true,
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _previewMedia = PreviewMediaService.WithAudio(
            _previewMedia,
            dialog.FileName);
        RefreshPreviewMedia();
        SetStatus("미리보기 배경음악을 불러왔습니다.");
    }

    private void ResetPreviewMedia_Click(object sender, RoutedEventArgs e)
    {
        _previewMedia = PreviewMediaService.LoadBundledSample();
        RefreshPreviewMedia();
        SetStatus("현우 · 지은 기본 샘플 미디어로 되돌렸습니다.");
    }

    private void TogglePreviewAudio_Click(object sender, RoutedEventArgs e)
    {
        if (_previewMedia.AudioSource is null)
        {
            SetStatus("먼저 미리보기 배경음악을 선택하세요.");
            return;
        }

        if (_previewAudioPlaying)
        {
            PreviewAudioPlayer.Pause();
            _previewAudioPlaying = false;
            PreviewAudioToggleButton.Content = "재생";
            return;
        }

        PreviewAudioPlayer.Play();
        _previewAudioPlaying = true;
        PreviewAudioToggleButton.Content = "일시정지";
    }

    private void TogglePreviewVideo_Click(object sender, RoutedEventArgs e)
    {
        if (_previewMedia.VideoSource is null)
        {
            SetStatus("먼저 미리보기 영상을 선택하세요.");
            return;
        }

        if (_previewVideoPlaying)
        {
            PreviewVideoPlayer.Pause();
            _previewVideoPlaying = false;
            PreviewVideoToggleButton.Content = "재생";
            return;
        }

        PreviewVideoPlayer.Play();
        _previewVideoPlaying = true;
        PreviewVideoToggleButton.Content = "일시정지";
    }

    private void StopPreviewVideo_Click(object sender, RoutedEventArgs e)
    {
        PreviewVideoPlayer.Stop();
        PreviewVideoPlayer.Position = TimeSpan.Zero;
        _previewVideoPlaying = false;
        PreviewVideoToggleButton.Content = "재생";
    }

    private void PreviewVideoPlayer_MediaEnded(
        object sender,
        RoutedEventArgs e)
    {
        StopPreviewVideo_Click(sender, e);
    }

    private void PreviewAudioPlayer_MediaEnded(
        object sender,
        RoutedEventArgs e)
    {
        PreviewAudioPlayer.Stop();
        PreviewAudioPlayer.Position = TimeSpan.Zero;
        _previewAudioPlaying = false;
        PreviewAudioToggleButton.Content = "재생";
    }

    private void PreviewMediaPlayer_MediaFailed(
        object sender,
        ExceptionRoutedEventArgs e)
    {
        _previewAudioPlaying = false;
        _previewVideoPlaying = false;
        PreviewAudioToggleButton.Content = "재생";
        PreviewVideoToggleButton.Content = "재생";
        SetStatus(
            "이 PC의 미디어 코덱으로 재생할 수 없습니다. MP3 또는 H.264 MP4를 권장합니다.");
    }

    private void AddSectionPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string sectionName }
            || !Enum.TryParse<LayoutSectionKey>(
                sectionName,
                ignoreCase: true,
                out var section))
        {
            SetStatus("추가할 섹션 정보를 확인할 수 없습니다.");
            return;
        }

        for (var index = 0;
             index < _package.Definition.Root.Children.Count;
             index++)
        {
            if (LayoutRecipeCatalog.GetSectionKeys(
                    _package.Definition.Root.Children[index])
                .Contains(section))
            {
                RefreshBlockTree([index]);
                SetStatus($"'{EditorDisplayCatalog.GetSectionTitle(section)}' 섹션은 이미 있습니다.");
                return;
            }
        }

        var sectionBlock = MakeBlockIdsUnique(
            LayoutRecipeCatalog.CreateSection(section));
        var newIndex = _package.Definition.Root.Children.Count;
        UpdateBlockAtPath(
            [],
            root => root with
            {
                Children = root.Children.Append(sectionBlock).ToArray(),
            });
        SynchronizeSectionOrder();
        RefreshBlockTree([newIndex]);
        MarkDirty();
        SetStatus($"'{EditorDisplayCatalog.GetSectionTitle(section)}' 섹션을 추가했습니다.");
    }

    private void EasySectionList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_refreshingEasyEditor)
        {
            return;
        }

        if (EasySectionList.SelectedItem is not EditorSectionItem selected)
        {
            ClearEasySelectedSection();
            return;
        }

        var refreshedPreview = false;
        if (_blockEditorDirty
            && _selectedBlockPath is { } previousPath)
        {
            var previousTopLevelIndex =
                previousPath.Length == 0 ? 0 : previousPath[0];
            if (!TryApplySelectedBlock(out var error))
            {
                SetStatus("현재 블록의 잘못된 값을 먼저 수정해 주세요.");
                MessageBox.Show(
                    this,
                    error,
                    "블록 속성",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                _refreshingEasyEditor = true;
                try
                {
                    if (previousTopLevelIndex >= 0
                        && previousTopLevelIndex < EasySections.Count)
                    {
                        EasySectionList.SelectedItem =
                            EasySections[previousTopLevelIndex];
                    }
                }
                finally
                {
                    _refreshingEasyEditor = false;
                }

                return;
            }

            refreshedPreview = true;
        }

        _selectedBlockPath = selected.Path;
        var block = GetBlockAtPath(_package.Definition.Root, selected.Path);
        ShowSelectedBlock(block, selected.Path);
        ShowEasySelectedSection(selected, block);
        SelectTreePath(selected.Path);
        RefreshEasyPresentationContent();
        UpdateEasyPresentationNavigation();
        if (refreshedPreview)
        {
            RefreshEasySections(selected.Path);
        }
    }

    private void MoveEasySection_Click(object sender, RoutedEventArgs e)
    {
        if (EasySectionList.SelectedItem is not EditorSectionItem selected
            || sender is not Button { Tag: string directionText }
            || !int.TryParse(directionText, out var direction))
        {
            return;
        }

        var children = _package.Definition.Root.Children.ToArray();
        var destination = selected.Index + direction;
        if (destination < 0 || destination >= children.Length)
        {
            SetStatus(direction < 0
                ? "이미 맨 위 섹션입니다."
                : "이미 맨 아래 섹션입니다.");
            return;
        }

        (children[selected.Index], children[destination]) =
            (children[destination], children[selected.Index]);
        UpdateBlockAtPath([], root => root with { Children = children });
        SynchronizeSectionOrder();
        RefreshBlockTree([destination]);
        MarkDirty();
        SetStatus("섹션 순서를 바꿨습니다.");
    }

    private void RemoveEasySection_Click(object sender, RoutedEventArgs e)
    {
        if (EasySectionList.SelectedItem is not EditorSectionItem selected)
        {
            return;
        }

        if (selected.IsComposite)
        {
            SetStatus(
                "여러 기능이 묶인 섹션은 간편 편집에서 통째로 삭제할 수 없습니다.");
            MessageBox.Show(
                this,
                "이 항목 안에는 여러 섹션이 함께 들어 있습니다.\n"
                + "내용 손실을 막기 위해 간편 편집에서는 삭제를 막았습니다. "
                + "고급 도구의 블록 트리에서 필요한 하위 블록만 선택해 주세요.",
                "묶음 섹션 보호",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (selected.SectionKey == LayoutSectionKey.Hero)
        {
            SetStatus("표지는 모든 레이아웃에 필요한 필수 섹션입니다.");
            MessageBox.Show(
                this,
                "표지는 청첩장 레이아웃에 반드시 하나 있어야 해서 뺄 수 없습니다.",
                "필수 섹션",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(
                this,
                $"'{selected.Title}' 섹션을 레이아웃에서 삭제할까요?",
                "섹션 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        UpdateBlockAtPath(
            [],
            root => root with
            {
                Children = root.Children
                    .Where((_, index) => index != selected.Index)
                    .ToArray(),
            });
        SynchronizeSectionOrder();
        var nextSelection = _package.Definition.Root.Children.Count == 0
            ? Array.Empty<int>()
            : [Math.Min(selected.Index, _package.Definition.Root.Children.Count - 1)];
        RefreshBlockTree(nextSelection);
        MarkDirty();
        SetStatus($"'{selected.Title}' 섹션을 삭제했습니다.");
    }

    private void EasySectionProperty_Changed(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_loadingPackageIntoEditor
            || _refreshingEasyEditor
            || EasySectionList.SelectedItem is not EditorSectionItem selected
            || EasyVariantComboBox.SelectedItem
                is not EditorChoice<LayoutVisualVariant> variant
            || EasyGapComboBox.SelectedItem
                is not EditorChoice<LayoutGap> gap
            || EasyColumnsComboBox.SelectedItem
                is not ComboBoxItem { Tag: string columnText }
            || !int.TryParse(columnText, out var columns))
        {
            return;
        }

        UpdateBlockAtPath(
            selected.Path,
            block => block with
            {
                Variant = variant.Value,
                ContainerSettings = StructuralKinds.Contains(block.Kind)
                    ? (block.ContainerSettings ?? new LayoutContainerSettings())
                        with
                        {
                            Columns = columns,
                            Gap = gap.Value,
                        }
                    : null,
            });
        RefreshBlockTree(selected.Path);
        MarkDirty();
        SetStatus("섹션 모양을 바로 반영했습니다.");
    }

    private void EasyFixedTextTextBox_LostKeyboardFocus(
        object sender,
        System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (CommitEasyFixedTextIfChanged(refreshPreview: true))
        {
            SetStatus("고정 문구를 바로 반영했습니다.");
        }
    }

    private void EasyFixedTextTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (!_refreshingEasyEditor && _easyEditableTextPath is not null)
        {
            MarkDirty();
        }
    }

    private bool CommitEasyFixedTextIfChanged(bool refreshPreview)
    {
        if (_refreshingEasyEditor || _easyEditableTextPath is null)
        {
            return false;
        }

        var current = GetBlockAtPath(
            _package.Definition.Root,
            _easyEditableTextPath);
        if (string.Equals(
                current.Text,
                EasyFixedTextTextBox.Text,
                StringComparison.Ordinal))
        {
            return false;
        }

        UpdateBlockAtPath(
            _easyEditableTextPath,
            block => block with { Text = EasyFixedTextTextBox.Text });
        if (_selectedBlockPath is not null
            && _selectedBlockPath.SequenceEqual(_easyEditableTextPath))
        {
            _refreshingBlockEditor = true;
            try
            {
                BlockTextTextBox.Text = EasyFixedTextTextBox.Text;
            }
            finally
            {
                _refreshingBlockEditor = false;
            }
        }

        var canRefreshPreview = true;
        if (_blockEditorDirty
            && _selectedBlockPath is not null
            && !TryApplySelectedBlock(out var pendingError))
        {
            canRefreshPreview = false;
            SetStatus(
                $"고급 블록의 값을 먼저 확인해 주세요: {pendingError}");
        }

        if (refreshPreview && canRefreshPreview)
        {
            var sectionPath = _easyEditableTextPath.Length == 0
                ? Array.Empty<int>()
                : [_easyEditableTextPath[0]];
            RefreshBlockTree(sectionPath);
        }

        MarkDirty();
        return true;
    }

    private void AddToken_Click(object sender, RoutedEventArgs e)
    {
        var used = StyleTokens.Select(item => item.Token).ToHashSet();
        var token = StyleTokenOptions.FirstOrDefault(candidate => !used.Contains(candidate));
        if (used.Count >= StyleTokenOptions.Count)
        {
            SetStatus("추가할 수 있는 안전한 색상 토큰을 모두 사용했습니다.");
            return;
        }

        var row = new EditableStyleToken(token, "#B88A58");
        StyleTokens.Add(row);
        StyleTokensGrid.SelectedItem = row;
        StyleTokensGrid.ScrollIntoView(row);
        MarkDirty();
    }

    private void RemoveToken_Click(object sender, RoutedEventArgs e)
    {
        if (StyleTokensGrid.SelectedItem is EditableStyleToken selected)
        {
            StyleTokens.Remove(selected);
            MarkDirty();
        }
    }

    private void PresentationComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_loadingPackageIntoEditor
            || PresentationComboBox.SelectedItem is not LayoutPresentationMode presentation)
        {
            return;
        }

        if (_transitionPreviewRunning)
        {
            ResetTransitionPreview();
        }

        TransitionKindComboBox.SelectedItem = presentation switch
        {
            LayoutPresentationMode.FlipCard => LayoutTransitionKind.FlipCard,
            LayoutPresentationMode.PagedBook => LayoutTransitionKind.PageTurn,
            _ => LayoutTransitionKind.None,
        };
        TransitionPreviewLabel.Text = presentation switch
        {
            LayoutPresentationMode.FlipCard => "Flip Card",
            LayoutPresentationMode.PagedBook => "Paged Book",
            _ => "Flow",
        };
        SetTransitionPreviewMode(
            presentation == LayoutPresentationMode.PagedBook
                ? LayoutTransitionKind.PageTurn
                : presentation == LayoutPresentationMode.FlipCard
                    ? LayoutTransitionKind.FlipCard
                    : LayoutTransitionKind.None);
        MarkDirty();
    }

    private void PreviewTransition_Click(object sender, RoutedEventArgs e)
    {
        if (_transitionPreviewRunning)
        {
            SetStatus("현재 페이지 전환이 끝난 뒤 다시 시도하세요.");
            return;
        }

        if (PresentationComboBox.SelectedItem is not LayoutPresentationMode presentation
            || TransitionKindComboBox.SelectedItem is not LayoutTransitionKind transition)
        {
            SetStatus("표현 방식과 전환 효과를 먼저 선택하세요.");
            return;
        }

        if (!int.TryParse(TransitionDurationTextBox.Text, out var durationMilliseconds)
            || durationMilliseconds is < 150 or > 2000)
        {
            SetStatus("전환 시간은 150~2000ms 사이의 정수여야 합니다.");
            return;
        }

        ResetTransitionPreview();
        SetTransitionPreviewMode(transition);
        TransitionPreviewLabel.Text = $"{presentation} · {transition}";
        var duration = TimeSpan.FromMilliseconds(durationMilliseconds);

        switch (transition)
        {
            case LayoutTransitionKind.FlipCard:
                TransitionPreviewScale.BeginAnimation(
                    System.Windows.Media.ScaleTransform.ScaleXProperty,
                    KeyFrameAnimation(
                        duration,
                        (0, 1d),
                        (.48, .04d),
                        (1, 1d)));
                TransitionPreviewPage.BeginAnimation(
                    OpacityProperty,
                    KeyFrameAnimation(
                        duration,
                        (0, .45d),
                        (.48, .78d),
                        (1, 1d)));
                break;

            case LayoutTransitionKind.PageTurn:
                BeginBookPageTurnPreview(duration);
                return;

            default:
                TransitionPreviewTranslate.BeginAnimation(
                    System.Windows.Media.TranslateTransform.YProperty,
                    new DoubleAnimation
                    {
                        From = 0,
                        To = -5,
                        Duration = TimeSpan.FromMilliseconds(
                            Math.Max(75, durationMilliseconds / 2d)),
                        AutoReverse = true,
                        FillBehavior = FillBehavior.Stop,
                    });
                break;
        }

        SetStatus("Contracts 전환 설정으로 미리보기를 재생했습니다.");
    }

    private void BlockTree_SelectedItemChanged(
        object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not TreeViewItem { Tag: int[] path })
        {
            if (_refreshingBlockTree)
            {
                return;
            }

            ClearSelectedBlockEditor();
            return;
        }

        if (_restoringBlockSelection)
        {
            _selectedBlockPath = path;
            return;
        }

        var refreshedPreview = false;
        if (!_refreshingBlockTree
            && _blockEditorDirty
            && _selectedBlockPath is { } previousPath
            && !previousPath.SequenceEqual(path))
        {
            var previousPathCopy = previousPath.ToArray();
            if (!TryApplySelectedBlock(out var error))
            {
                SetStatus("현재 블록의 잘못된 값을 먼저 수정해 주세요.");
                MessageBox.Show(
                    this,
                    error,
                    "블록 속성",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                _restoringBlockSelection = true;
                try
                {
                    SelectTreePath(previousPathCopy);
                }
                finally
                {
                    _restoringBlockSelection = false;
                }

                e.Handled = true;
                return;
            }

            refreshedPreview = true;
        }

        _selectedBlockPath = path;
        ShowSelectedBlock(GetBlockAtPath(_package.Definition.Root, path), path);
        if (refreshedPreview)
        {
            RefreshEasySections(path);
        }
    }

    private void ApplyBlock_Click(object sender, RoutedEventArgs e)
    {
        if (TryApplySelectedBlock(out var error))
        {
            RefreshBlockTree(_selectedBlockPath);
            MarkDirty();
            SetStatus("선택한 블록 속성을 적용했습니다.");
        }
        else if (!string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show(
                this,
                error,
                "블록 속성",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void AddChild_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBlockPath is null)
        {
            SetStatus("먼저 부모 블록을 선택하세요.");
            return;
        }

        if (!TryApplySelectedBlock(out var error))
        {
            MessageBox.Show(
                this,
                error,
                "블록 속성",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var parent = GetBlockAtPath(_package.Definition.Root, _selectedBlockPath);
        if (!StructuralKinds.Contains(parent.Kind))
        {
            SetStatus($"'{parent.Kind}' 블록에는 자식 블록을 추가할 수 없습니다.");
            return;
        }

        var child = new LayoutBlock
        {
            Id = CreateUniqueBlockId("stack"),
            Kind = LayoutBlockKind.Stack,
            Binding = LayoutBindingKey.None,
            ContainerSettings = new LayoutContainerSettings(),
        };
        var childIndex = parent.Children.Count;
        UpdateBlockAtPath(
            _selectedBlockPath,
            current => current with
            {
                Children = current.Children.Append(child).ToArray(),
            });

        var childPath = _selectedBlockPath.Append(childIndex).ToArray();
        RefreshBlockTree(childPath);
        MarkDirty();
        SetStatus("새 Stack 블록을 추가했습니다.");
    }

    private void RemoveBlock_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBlockPath is null)
        {
            SetStatus("삭제할 블록을 선택하세요.");
            return;
        }

        if (_selectedBlockPath.Length == 0)
        {
            SetStatus("Page 루트 블록은 삭제할 수 없습니다.");
            return;
        }

        var parentPath = _selectedBlockPath[..^1];
        var childIndex = _selectedBlockPath[^1];
        UpdateBlockAtPath(
            parentPath,
            parent => parent with
            {
                Children = parent.Children
                    .Where((_, index) => index != childIndex)
                    .ToArray(),
            });
        RefreshBlockTree(parentPath);
        SynchronizeSectionOrder();
        MarkDirty();
        SetStatus("선택한 블록을 삭제했습니다.");
    }

    private void LoadPackageIntoEditor(LayoutPackage package, string? filePath)
    {
        _loadingPackageIntoEditor = true;
        _package = package;
        _filePath = filePath;
        _isDirty = false;
        _openedPackageHasCompatibilityTier = filePath is not null;
        _easyPreviewPageIndex = 0;

        KeyTextBox.Text = package.Manifest.Key;
        VersionTextBox.Text = package.Manifest.Version;
        LabelTextBox.Text = package.Manifest.Label;
        DescriptionTextBox.Text = package.Manifest.Description;
        PresentationComboBox.SelectedItem = package.Definition.Presentation;
        TransitionKindComboBox.SelectedItem = package.Definition.Transition.Kind;
        TransitionDurationTextBox.Text =
            package.Definition.Transition.DurationMilliseconds.ToString();
        TransitionSwipeCheckBox.IsChecked =
            package.Definition.Transition.EnableSwipe;
        TransitionKeyboardCheckBox.IsChecked =
            package.Definition.Transition.EnableKeyboard;
        TransitionNavigationCheckBox.IsChecked =
            package.Definition.Transition.ShowNavigation;
        TransitionPreviewLabel.Text = package.Definition.Presentation switch
        {
            LayoutPresentationMode.FlipCard => "Flip Card",
            LayoutPresentationMode.PagedBook => "Paged Book",
            _ => "Flow",
        };
        SetTransitionPreviewMode(package.Definition.Transition.Kind);
        SectionOrderTextBox.Text = string.Join(", ", package.Definition.SectionOrder);

        ReloadStyleTokenRows(package.Definition.StyleTokens);

        var easyPresentation = FriendlyPresentationOptions.First(choice =>
            choice.Value == package.Definition.Presentation);
        EasyPresentationComboBox.SelectedItem = easyPresentation;
        EasyPresentationDescriptionText.Text = easyPresentation.Description;
        UpdateEasyPresentationModeText(package.Definition.Presentation);
        ApplyEasyPresentationPreviewMode(package.Definition.Presentation);
        ApplyEasyThemePreview(package.Definition.StyleTokens);
        EasyThemeStatusText.Text =
            "현재 패키지의 색상을 유지하고 있습니다.";
        ApplyPreviewMediaPlayers();
        UpdateFilePathText();
        ValidationErrorsList.ItemsSource = Array.Empty<LayoutValidationError>();
        ValidationSummaryText.Text = "아직 문제를 확인하지 않았습니다.";
        ValidationSummaryText.Foreground = Brushes.DimGray;
        ValidationDetailsExpander.IsExpanded = false;
        RefreshBlockTree([]);
        _loadingPackageIntoEditor = false;
        UpdateTierPolicyDisplay();
        _ = RefreshTierPolicyAsync(reportStatus: false);
    }

    private static LayoutPackage NormalizePackageForEditor(
        LayoutPackage package) =>
        package with
        {
            Definition = package.Definition with
            {
                Root = NormalizeBlockForEditor(package.Definition.Root),
                SectionOrder =
                    package.Definition.SectionOrder
                    ?? Array.Empty<LayoutSectionKey>(),
                StyleTokens =
                    package.Definition.StyleTokens
                    ?? Array.Empty<LayoutStyleTokenValue>(),
                Responsive =
                    package.Definition.Responsive
                    ?? new LayoutResponsiveSettings(),
                Transition =
                    package.Definition.Transition
                    ?? new LayoutTransitionDefinition(),
            },
        };

    private static LayoutBlock NormalizeBlockForEditor(LayoutBlock block) =>
        block with
        {
            Responsive =
                block.Responsive
                ?? new LayoutResponsiveSettings(),
            Children = (block.Children ?? Array.Empty<LayoutBlock>())
                .Select(NormalizeBlockForEditor)
                .ToArray(),
        };

    private void RefreshPreviewMedia()
    {
        ApplyPreviewMediaPlayers();
        RefreshEasySections(_selectedBlockPath);
    }

    private void ApplyPreviewMediaPlayers()
    {
        PreviewAudioPlayer.Stop();
        PreviewVideoPlayer.Stop();
        _previewAudioPlaying = false;
        _previewVideoPlaying = false;
        PreviewAudioToggleButton.Content = "재생";
        PreviewVideoToggleButton.Content = "재생";
        PreviewAudioPlayer.Source = _previewMedia.AudioSource;
        PreviewVideoPlayer.Source = _previewMedia.VideoSource;
        PreviewAudioTitleText.Text = _previewMedia.AudioLabel;
        PreviewVideoTitleText.Text = _previewMedia.VideoLabel;
        PreviewMediaSummaryText.Text =
            $"사진 {_previewMedia.PhotoCount}장 · "
            + (_previewMedia.VideoSource is null ? "영상 없음" : "영상 1개")
            + " · "
            + (_previewMedia.AudioSource is null ? "음악 없음" : "음악 1곡");
    }

    private void ShowPreviewMediaError(Exception exception)
    {
        SetStatus("미리보기 자료를 불러오지 못했습니다.");
        MessageBox.Show(
            this,
            exception.Message,
            "미리보기 자료",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void ReloadStyleTokenRows(
        IReadOnlyList<LayoutStyleTokenValue> tokens)
    {
        StyleTokens.Clear();
        foreach (var token in tokens)
        {
            StyleTokens.Add(new EditableStyleToken(token.Token, token.Value));
        }
    }

    private void ApplyEasyThemePreview(
        IReadOnlyList<LayoutStyleTokenValue> tokens)
    {
        var background = tokens.FirstOrDefault(item =>
            item.Token == LayoutStyleToken.BackgroundColor)?.Value;
        if (string.IsNullOrWhiteSpace(background))
        {
            return;
        }

        try
        {
            if (ColorConverter.ConvertFromString(background)
                is Color backgroundColor)
            {
                EasyPreviewFrame.Background =
                    new SolidColorBrush(backgroundColor);
            }
        }
        catch (FormatException)
        {
            // 공통 검증기가 정확한 토큰 오류를 표시한다. 미리보기는 기존 색을 유지한다.
        }
    }

    private void UpdateEasyPresentationModeText(
        LayoutPresentationMode presentation)
    {
        EasyPreviewModeText.Text = presentation switch
        {
            LayoutPresentationMode.FlipCard =>
                "출력 방식: 카드 덱 · 고정된 카드 안에서 내용을 스크롤하고 한 장씩 넘깁니다.",
            LayoutPresentationMode.PagedBook =>
                _easyPreviewIsPhone
                    ? "출력 방식: 포토북 · 폰에서는 책의 한 면씩 표시합니다."
                    : "출력 방식: 포토북 · PC에서는 중앙 책등을 둔 두 면을 함께 표시합니다.",
            _ =>
                "출력 방식: 세로 스크롤 · 모든 섹션을 위에서 아래로 이어서 표시합니다.",
        };
    }

    private void ApplyEasyPresentationPreviewMode(
        LayoutPresentationMode presentation)
    {
        _easyPreviewPresentation = presentation;
        var flow = presentation == LayoutPresentationMode.Flow;
        var flipCard = presentation == LayoutPresentationMode.FlipCard;
        var pagedBook = presentation == LayoutPresentationMode.PagedBook;
        EasySectionList.Tag = "Flow";
        EasySectionList.Visibility =
            flow ? Visibility.Visible : Visibility.Collapsed;
        EasyFlipCardHost.Visibility =
            flipCard ? Visibility.Visible : Visibility.Collapsed;
        EasyPagedBookHost.Visibility =
            pagedBook ? Visibility.Visible : Visibility.Collapsed;
        EasyPagedBookPhoneHost.Visibility =
            pagedBook && _easyPreviewIsPhone
                ? Visibility.Visible
                : Visibility.Collapsed;
        EasyPagedBookPcHost.Visibility =
            pagedBook && !_easyPreviewIsPhone
                ? Visibility.Visible
                : Visibility.Collapsed;
        EasyPresentationNavigationPanel.Visibility =
            flow ? Visibility.Collapsed : Visibility.Visible;
        EasyPresentationHintText.Text = presentation switch
        {
            LayoutPresentationMode.PagedBook =>
                _easyPreviewIsPhone
                    ? "한 면씩 보기 · 홀짝에 따라 책등 방향이 달라집니다."
                    : "두 면씩 보기 · 각 면의 내용은 따로 스크롤됩니다.",
            LayoutPresentationMode.FlipCard =>
                "한 장씩 보기 · 카드 안에서 내용을 스크롤합니다.",
            _ => "",
        };
        UpdateEasyPresentationModeText(presentation);
        EasySectionList.Items.Refresh();
        RefreshEasyPresentationContent();
        UpdateEasyPresentationNavigation();
    }

    private void MoveEasyPreviewPage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string directionText }
            || !int.TryParse(directionText, out var direction)
            || EasySections.Count == 0)
        {
            return;
        }

        var step =
            _easyPreviewPresentation == LayoutPresentationMode.PagedBook
            && !_easyPreviewIsPhone
                ? 2
                : 1;
        var currentIndex = GetEasyPreviewNavigationIndex();
        var destination = Math.Clamp(
            currentIndex + direction * step,
            0,
            EasySections.Count - 1);
        if (step == 2)
        {
            destination -= destination % 2;
        }

        if (destination == currentIndex)
        {
            SetStatus(direction < 0
                ? "첫 번째 카드 또는 펼침면입니다."
                : "마지막 카드 또는 펼침면입니다.");
            return;
        }

        _easyPreviewPageIndex = destination;
        RefreshEasyPresentationContent();
        ResetEasyPagedPreviewScroll();
        UpdateEasyPresentationNavigation();
        SetStatus(_easyPreviewPresentation == LayoutPresentationMode.PagedBook
            ? _easyPreviewIsPhone
                ? $"{GetBookPageLabel(destination)}을(를) 보고 있습니다."
                : $"{GetBookPageLabel(destination)}부터 시작하는 펼침면을 보고 있습니다."
            : $"{destination + 1}번째 카드를 보고 있습니다.");
    }

    private void UpdateEasyPresentationNavigation()
    {
        if (EasyPresentationPageLabel is null
            || EasyPreviewPreviousButton is null
            || EasyPreviewNextButton is null)
        {
            return;
        }

        var count = EasySections.Count;
        if (count == 0)
        {
            EasyPresentationPageLabel.Text =
                _easyPreviewPresentation == LayoutPresentationMode.PagedBook
                    ? "페이지 없음"
                    : "카드 없음";
            EasyPreviewPreviousButton.IsEnabled = false;
            EasyPreviewNextButton.IsEnabled = false;
            return;
        }

        _easyPreviewPageIndex = Math.Clamp(
            _easyPreviewPageIndex,
            0,
            count - 1);
        var navigationIndex = GetEasyPreviewNavigationIndex();
        if (_easyPreviewPresentation == LayoutPresentationMode.PagedBook
            && !_easyPreviewIsPhone)
        {
            var rightIndex = navigationIndex + 1;
            var rightLabel = rightIndex < count
                ? $" · {GetBookPageLabel(rightIndex)}"
                : "";
            EasyPresentationPageLabel.Text =
                $"{GetBookPageLabel(navigationIndex)}{rightLabel} / 총 {count}면";
            EasyPreviewPreviousButton.IsEnabled = navigationIndex > 0;
            EasyPreviewNextButton.IsEnabled = navigationIndex + 2 < count;
            return;
        }

        EasyPresentationPageLabel.Text =
            _easyPreviewPresentation == LayoutPresentationMode.PagedBook
                ? $"{GetBookPageLabel(_easyPreviewPageIndex)} / 총 {count}면"
                : $"카드 {_easyPreviewPageIndex + 1} / {count}";
        EasyPreviewPreviousButton.IsEnabled = _easyPreviewPageIndex > 0;
        EasyPreviewNextButton.IsEnabled =
            _easyPreviewPageIndex < count - 1;
    }

    private int GetEasyPreviewNavigationIndex()
    {
        if (EasySections.Count == 0)
        {
            return 0;
        }

        var index = Math.Clamp(
            _easyPreviewPageIndex,
            0,
            EasySections.Count - 1);
        return _easyPreviewPresentation == LayoutPresentationMode.PagedBook
               && !_easyPreviewIsPhone
            ? index - index % 2
            : index;
    }

    private void RefreshEasyPresentationContent()
    {
        if (EasyFlipCardContent is null
            || EasyBookLeftContent is null
            || EasyBookRightContent is null
            || EasyBookPhoneContent is null)
        {
            return;
        }

        if (EasySections.Count == 0)
        {
            EasyFlipCardContent.Content = null;
            EasyBookLeftContent.Content = null;
            EasyBookRightContent.Content = null;
            EasyBookPhoneContent.Content = null;
            EasyFlipCardTitleText.Text = "카드 없음";
            EasyBookLeftEmptyText.Visibility = Visibility.Visible;
            EasyBookRightEmptyText.Visibility = Visibility.Visible;
            EasyBookLeftPageNumberText.Text = "";
            EasyBookRightPageNumberText.Text = "";
            EasyBookPhonePageNumberText.Text = "";
            return;
        }

        _easyPreviewPageIndex = Math.Clamp(
            _easyPreviewPageIndex,
            0,
            EasySections.Count - 1);
        var selectedIndex = EasySectionList.SelectedIndex;
        var current = EasySections[_easyPreviewPageIndex];

        EasyFlipCardContent.Content = current;
        EasyFlipCardTitleText.Text =
            $"{current.Icon}  {current.Title}";
        ApplyPreviewSelection(
            EasyFlipCardBorder,
            selectedIndex == _easyPreviewPageIndex,
            normalThickness: 2);

        EasyBookPhoneContent.Content = current;
        EasyBookPhonePageNumberText.Text =
            GetBookPageLabel(_easyPreviewPageIndex);
        var rightFacingPage =
            _easyPreviewPageIndex == 0 || _easyPreviewPageIndex % 2 == 1;
        EasyBookPhoneLeftSpine.Visibility =
            rightFacingPage ? Visibility.Visible : Visibility.Collapsed;
        EasyBookPhoneRightSpine.Visibility =
            rightFacingPage ? Visibility.Collapsed : Visibility.Visible;
        EasyBookPhonePageNumberText.HorizontalAlignment =
            _easyPreviewPageIndex == 0
                ? HorizontalAlignment.Center
                : rightFacingPage
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left;
        ApplyPreviewSelection(
            EasyBookPhonePageBorder,
            selectedIndex == _easyPreviewPageIndex,
            normalThickness: 1);

        var leftIndex = GetEasyPreviewNavigationIndex();
        var rightIndex = leftIndex + 1;
        EasyBookLeftContent.Content = EasySections[leftIndex];
        EasyBookLeftEmptyText.Visibility = Visibility.Collapsed;
        EasyBookLeftPageNumberText.Text = GetBookPageLabel(leftIndex);
        ApplyPreviewSelection(
            EasyBookLeftPageBorder,
            selectedIndex == leftIndex,
            normalThickness: 1);

        if (rightIndex < EasySections.Count)
        {
            EasyBookRightContent.Content = EasySections[rightIndex];
            EasyBookRightEmptyText.Visibility = Visibility.Collapsed;
            EasyBookRightPageNumberText.Text = GetBookPageLabel(rightIndex);
            ApplyPreviewSelection(
                EasyBookRightPageBorder,
                selectedIndex == rightIndex,
                normalThickness: 1);
        }
        else
        {
            EasyBookRightContent.Content = null;
            EasyBookRightEmptyText.Visibility = Visibility.Visible;
            EasyBookRightPageNumberText.Text = "";
            ApplyPreviewSelection(
                EasyBookRightPageBorder,
                selected: false,
                normalThickness: 1);
        }
    }

    private void EasyPreviewPage_MouseLeftButtonUp(
        object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string page })
        {
            return;
        }

        var index = page switch
        {
            "Card" or "BookPhone" => _easyPreviewPageIndex,
            "BookLeft" => GetEasyPreviewNavigationIndex(),
            "BookRight" => GetEasyPreviewNavigationIndex() + 1,
            _ => -1,
        };
        if (index < 0 || index >= EasySections.Count)
        {
            return;
        }

        EasySectionList.SelectedItem = EasySections[index];
        SetStatus(
            $"'{EasySections[index].Title}' 섹션을 꾸미기 대상으로 선택했습니다.");
        e.Handled = true;
    }

    private static string GetBookPageLabel(int index) =>
        index == 0 ? "표지" : $"{index}쪽";

    private static void ApplyPreviewSelection(
        Border border,
        bool selected,
        double normalThickness)
    {
        border.BorderBrush = CreateBrush(
            selected ? "#B88A58" : "#CDBBA8");
        border.BorderThickness = new Thickness(
            selected ? 3 : normalThickness);
    }

    private void ResetEasyPagedPreviewScroll()
    {
        EasyFlipCardScrollViewer?.ScrollToTop();
        EasyBookLeftScrollViewer?.ScrollToTop();
        EasyBookRightScrollViewer?.ScrollToTop();
        EasyBookPhoneScrollViewer?.ScrollToTop();
    }

    private bool ValidatePackage()
    {
        var editorErrors = CaptureEditorValues();
        var validation = LayoutPackageValidator.Validate(_package);
        var allErrors = editorErrors.Concat(validation.Errors).ToArray();

        ValidationErrorsList.ItemsSource = allErrors;
        ValidationSummaryText.Text = allErrors.Length == 0
            ? "검증 통과 — 업로드 가능한 패키지입니다."
            : $"검증 실패 — {allErrors.Length}개 문제";
        ValidationSummaryText.Foreground = allErrors.Length == 0
            ? System.Windows.Media.Brushes.SeaGreen
            : System.Windows.Media.Brushes.Firebrick;
        SetStatus(allErrors.Length == 0
            ? "Contracts 공통 검증을 통과했습니다."
            : "아래 오류를 수정한 뒤 다시 검증하세요.");

        return allErrors.Length == 0;
    }

    private IReadOnlyList<LayoutValidationError> CaptureEditorValues()
    {
        var editorErrors = new List<LayoutValidationError>();
        StyleTokensGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        StyleTokensGrid.CommitEdit(DataGridEditingUnit.Row, true);
        CommitEasyFixedTextIfChanged(refreshPreview: false);

        if (_selectedBlockPath is not null
            && !TryApplySelectedBlock(out var blockError))
        {
            editorErrors.Add(new(
                "$.definition.root",
                "editor.block",
                blockError));
        }

        var sectionOrder = new List<LayoutSectionKey>();
        var rawSections = SectionOrderTextBox.Text.Split(
            [',', ';', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var rawSection in rawSections)
        {
            if (Enum.TryParse<LayoutSectionKey>(
                    rawSection,
                    ignoreCase: true,
                    out var section)
                && Enum.IsDefined(section))
            {
                sectionOrder.Add(section);
            }
            else
            {
                editorErrors.Add(new(
                    "$.definition.sectionOrder",
                    "editor.section",
                    $"'{rawSection}'은(는) 지원하는 섹션 이름이 아닙니다."));
            }
        }

        var presentation =
            PresentationComboBox.SelectedItem is LayoutPresentationMode selectedPresentation
                ? selectedPresentation
                : LayoutPresentationMode.Flow;
        var transitionKind =
            TransitionKindComboBox.SelectedItem is LayoutTransitionKind selectedTransition
                ? selectedTransition
                : LayoutTransitionKind.None;
        if (!int.TryParse(
                TransitionDurationTextBox.Text,
                out var transitionDuration)
            || transitionDuration is < 150 or > 2000)
        {
            editorErrors.Add(new(
                "$.definition.transition.durationMilliseconds",
                "editor.transition.duration",
                "전환 시간은 150~2000ms 사이의 정수여야 합니다."));
            transitionDuration = 650;
        }

        _package = _package with
        {
            Manifest = _package.Manifest with
            {
                SchemaVersion = LayoutSchema.CurrentVersion,
                Key = KeyTextBox.Text,
                Version = VersionTextBox.Text,
                Label = LabelTextBox.Text,
                Description = DescriptionTextBox.Text,
            },
            Definition = _package.Definition with
            {
                Presentation = presentation,
                Transition = new LayoutTransitionDefinition
                {
                    Kind = transitionKind,
                    DurationMilliseconds = transitionDuration,
                    EnableSwipe = TransitionSwipeCheckBox.IsChecked == true,
                    EnableKeyboard =
                        TransitionKeyboardCheckBox.IsChecked == true,
                    ShowNavigation =
                        TransitionNavigationCheckBox.IsChecked == true,
                },
                SectionOrder = sectionOrder.ToArray(),
                StyleTokens = StyleTokens
                    .Select(item => new LayoutStyleTokenValue
                    {
                        Token = item.Token,
                        Value = item.Value,
                    })
                    .ToArray(),
            },
        };

        return editorErrors;
    }

    private void UpdateTierPolicyDisplay()
    {
        if (_openedPackageHasCompatibilityTier)
        {
            TierPolicyValueText.Text = "서버 정책 조회 전";
            TierPolicyText.Text =
                $"package 호환 값 {_package.Manifest.Tier}은 JSON round-trip 용도로만 " +
                "보존하며 권한 판단에 사용하지 않습니다. 조회 버튼으로 LayoutKey의 실제 " +
                "서버 정책을 확인할 수 있고 WPF에서는 변경할 수 없습니다.";
            return;
        }

        TierPolicyValueText.Text = "서버 정책 조회 전";
        TierPolicyText.Text =
            "WPF 편집기에는 등급을 결정하거나 변경할 권한이 없습니다. " +
            "기존 LayoutKey라면 서버 정책을 표시하고, 신규 키라면 미분류로 표시합니다.";
    }

    private async Task RefreshTierPolicyAsync(bool reportStatus)
    {
        var requestVersion = Interlocked.Increment(ref _tierPolicyRequestVersion);
        var layoutKey = KeyTextBox.Text.Trim();
        var serverText = PolicyServerTextBox.Text.Trim();
        if (layoutKey.Length == 0)
        {
            TierPolicyValueText.Text = "LayoutKey를 입력하세요";
            TierPolicyText.Text =
                "등급은 서버의 LayoutKey 정책이며 WPF에서는 결정하거나 변경할 수 없습니다.";
            return;
        }

        if (!TryBuildPolicyUri(serverText, layoutKey, out var policyUri))
        {
            TierPolicyValueText.Text = "정책 서버 주소 오류";
            TierPolicyText.Text =
                "http 또는 https 형식의 Wedding 서버 기본 주소를 입력하세요.";
            if (reportStatus)
            {
                SetStatus("정책 서버 주소가 올바르지 않습니다.");
            }

            return;
        }

        RefreshTierPolicyButton.IsEnabled = false;
        TierPolicyValueText.Text = "서버 정책 조회 중…";
        TierPolicyText.Text =
            $"'{layoutKey}'의 읽기 전용 등급 정책을 {policyUri.Host}에서 확인하고 있습니다.";
        try
        {
            using var response = await PolicyHttpClient.GetAsync(policyUri);
            if (requestVersion != _tierPolicyRequestVersion)
            {
                return;
            }

            if (!string.Equals(
                    layoutKey,
                    KeyTextBox.Text.Trim(),
                    StringComparison.Ordinal))
            {
                UpdateTierPolicyDisplay();
                return;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                TierPolicyValueText.Text = "미분류";
                TierPolicyText.Text =
                    "이 LayoutKey에는 아직 서버 등급 정책이 없습니다. 패키지를 업로드한 뒤 " +
                    "슈퍼관리자가 별도 정책 화면에서 Free/Premium을 지정해야 승인할 수 있습니다.";
                if (reportStatus)
                {
                    SetStatus($"'{layoutKey}'는 서버에서 아직 분류되지 않았습니다.");
                }

                return;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<LayoutDefinitionPolicyStatus>(
                json,
                LayoutPackageJson.CreateOptions());
            if (status is null
                || !string.Equals(
                    status.LayoutKey,
                    layoutKey,
                    StringComparison.Ordinal))
            {
                throw new JsonException(
                    "서버 정책 응답의 LayoutKey가 요청과 일치하지 않습니다.");
            }

            TierPolicyValueText.Text = status.IsBuiltIn
                ? $"{status.Tier} · 기본 제공 · 변경 불가"
                : $"{status.Tier} · Revision {status.Revision}";
            var compatibilityText = _openedPackageHasCompatibilityTier
                ? $" package 호환 값 {_package.Manifest.Tier}은 이 정책을 바꾸지 않습니다."
                : "";
            TierPolicyText.Text =
                "이 등급은 서버가 LayoutKey의 모든 버전에 공통 적용합니다. " +
                "WPF는 조회만 가능하며 업로드·버전 변경으로 수정되지 않습니다." +
                compatibilityText;
            if (reportStatus)
            {
                SetStatus(
                    $"'{status.LayoutKey}' 서버 정책: {status.Tier}, Revision {status.Revision}");
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or TaskCanceledException
            or IOException
            or JsonException)
        {
            if (requestVersion != _tierPolicyRequestVersion)
            {
                return;
            }

            TierPolicyValueText.Text = "서버 정책 조회 실패";
            TierPolicyText.Text =
                "서버 연결 또는 배포 버전을 확인하세요. 오프라인 상태에서는 package의 " +
                "Manifest.Tier를 실제 등급으로 사용하거나 표시하지 않습니다.";
            if (reportStatus)
            {
                SetStatus($"서버 정책을 조회하지 못했습니다: {exception.Message}");
            }
        }
        finally
        {
            if (requestVersion == _tierPolicyRequestVersion)
            {
                RefreshTierPolicyButton.IsEnabled = true;
            }
        }
    }

    private static bool TryBuildPolicyUri(
        string serverText,
        string layoutKey,
        out Uri policyUri)
    {
        policyUri = null!;
        if (!Uri.TryCreate(serverText, UriKind.Absolute, out var serverUri)
            || (serverUri.Scheme != Uri.UriSchemeHttp
                && serverUri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        var baseUri = new Uri(
            serverUri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/",
            UriKind.Absolute);
        policyUri = new Uri(
            baseUri,
            $"api/layout-definition-policies/{Uri.EscapeDataString(layoutKey)}");
        return true;
    }

    private bool TryApplySelectedBlock(out string error)
    {
        error = "";
        if (_selectedBlockPath is null)
        {
            return true;
        }

        if (BlockKindComboBox.SelectedItem is not LayoutBlockKind kind
            || BlockBindingComboBox.SelectedItem is not LayoutBindingKey binding
            || BlockVariantComboBox.SelectedItem is not LayoutVisualVariant variant
            || BlockGapComboBox.SelectedItem is not LayoutGap gap)
        {
            error = "블록의 Kind, Binding, Variant, Gap 값을 선택하세요.";
            return false;
        }

        if (!int.TryParse(BlockColumnsTextBox.Text, out var columns)
            || columns is < 1 or > 12)
        {
            error = "컨테이너 열 수는 1~12 사이의 정수여야 합니다.";
            return false;
        }

        var current = GetBlockAtPath(
            _package.Definition.Root,
            _selectedBlockPath);
        var updated = current with
        {
            Id = BlockIdTextBox.Text,
            Kind = kind,
            Binding = binding,
            Text = BlockTextTextBox.Text,
            Variant = variant,
            TextSettings = TextKinds.Contains(kind)
                ? current.TextSettings ?? new LayoutTextSettings()
                : null,
            ImageSettings = ImageKinds.Contains(kind)
                ? current.ImageSettings ?? new LayoutImageSettings()
                : null,
            ContainerSettings = StructuralKinds.Contains(kind)
                ? (current.ContainerSettings ?? new LayoutContainerSettings()) with
                {
                    Columns = columns,
                    Gap = gap,
                }
                : null,
            ActionSettings = ActionKinds.Contains(kind)
                ? current.ActionSettings ?? new LayoutActionSettings()
                : null,
        };

        if (current != updated)
        {
            UpdateBlockAtPath(_selectedBlockPath, _ => updated);
            MarkDirty();
        }

        _blockEditorDirty = false;
        return true;
    }

    private void SaveTo(string path)
    {
        if (!ValidatePackage())
        {
            MessageBox.Show(
                this,
                "공통 Contracts 검증을 통과해야 저장할 수 있습니다.",
                "레이아웃 패키지 저장",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var canonical = LayoutPackageCanonicalizer.Canonicalize(_package);
        var json = JsonSerializer.Serialize(
            canonical,
            LayoutPackageJson.CreateOptions(indented: true));
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new IOException("저장 폴더를 확인할 수 없습니다.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            File.Move(temporaryPath, fullPath, overwrite: true);
            _package = canonical;
            _filePath = fullPath;
            _isDirty = false;
            UpdateFilePathText();
            SetStatus("검증된 JSON 패키지를 원자적으로 저장했습니다.");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "저장 실패",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                    // The original save error is more useful than cleanup failure.
                }
            }
        }
    }

    private void RefreshBlockTree(IReadOnlyList<int>? selectionPath)
    {
        _refreshingBlockTree = true;
        try
        {
            BlockTree.Items.Clear();
            var root = CreateTreeItem(_package.Definition.Root, []);
            BlockTree.Items.Add(root);
            root.IsExpanded = true;

            if (selectionPath is not null
                && TryFindTreeItem(root, selectionPath, out var selected))
            {
                selected.IsSelected = true;
                selected.BringIntoView();
            }

            RefreshEasySections(selectionPath);
        }
        finally
        {
            _refreshingBlockTree = false;
        }
    }

    private void RefreshEasySections(IReadOnlyList<int>? selectionPath)
    {
        _refreshingEasyEditor = true;
        try
        {
            EasySections.Clear();
            for (var index = 0;
                 index < _package.Definition.Root.Children.Count;
                 index++)
            {
                var block = _package.Definition.Root.Children[index];
                var sectionKeys = LayoutRecipeCatalog.GetSectionKeys(block);
                LayoutSectionKey? section = null;
                if (sectionKeys.Count > 0)
                {
                    section = sectionKeys[0];
                }

                var item = EditorDisplayCatalog.CreateSectionItem(
                    block,
                    index,
                    section,
                    _previewMedia,
                    _package.Definition.StyleTokens);
                if (sectionKeys.Count > 1)
                {
                    var titles = string.Join(
                        " · ",
                        sectionKeys.Select(
                            EditorDisplayCatalog.GetSectionTitle));
                    item = item with
                    {
                        IsComposite = true,
                        Title = $"묶음 섹션 ({sectionKeys.Count}개)",
                        Description =
                            $"{titles} 기능이 한 블록 안에 함께 들어 있습니다. "
                            + "간편 편집에서는 묶음 전체의 모양만 바꿀 수 있습니다.",
                        LayoutSummary =
                            $"{item.LayoutSummary} · 묶음: {titles}",
                    };
                }

                EasySections.Add(item);
            }

            if (EasySections.Count == 0)
            {
                EasySectionList.SelectedItem = null;
                ClearEasySelectedSection();
                RefreshEasyPresentationContent();
                UpdateEasyPresentationNavigation();
                return;
            }

            var requestedIndex =
                selectionPath is { Count: > 0 }
                    ? selectionPath[0]
                    : 0;
            requestedIndex = Math.Clamp(
                requestedIndex,
                0,
                EasySections.Count - 1);
            var selected = EasySections[requestedIndex];
            EasySectionList.SelectedItem = selected;
            EasySectionList.ScrollIntoView(selected);
            ShowEasySelectedSection(
                selected,
                GetBlockAtPath(_package.Definition.Root, selected.Path));
            _easyPreviewPageIndex = Math.Clamp(
                _easyPreviewPageIndex,
                0,
                EasySections.Count - 1);
            RefreshEasyPresentationContent();
            UpdateEasyPresentationNavigation();
        }
        finally
        {
            _refreshingEasyEditor = false;
        }
    }

    private void ShowEasySelectedSection(
        EditorSectionItem selected,
        LayoutBlock block)
    {
        var wasRefreshing = _refreshingEasyEditor;
        _refreshingEasyEditor = true;
        try
        {
            EasySelectionPanel.IsEnabled = true;
            EasySelectionHintText.Text =
                "모양과 간격을 바꾸면 가운데 화면에 바로 반영됩니다.";
            EasySectionTitleText.Text = $"{selected.Icon}  {selected.Title}";
            EasySectionDescriptionText.Text = selected.Description;
            var canEditContainer = StructuralKinds.Contains(block.Kind);
            EasyColumnsComboBox.IsEnabled = canEditContainer;
            EasyGapComboBox.IsEnabled = canEditContainer;
            var canRemoveSection = !selected.IsComposite
                && selected.SectionKey != LayoutSectionKey.Hero;
            RemoveEasySectionButton.IsEnabled = canRemoveSection;
            RemoveSelectedSectionButton.IsEnabled = canRemoveSection;
            EasyContainerSettingsHelpText.Text = canEditContainer
                ? "칸 수와 간격은 이 묶음 안의 요소 배치에 적용됩니다."
                : "이 기능 블록은 자체 배치를 사용하므로 칸 수와 간격을 바꾸지 않습니다.";
            EasyVariantComboBox.SelectedItem =
                FriendlyVariantOptions.First(choice =>
                    choice.Value == block.Variant);

            var columns = Math.Clamp(
                block.ContainerSettings?.Columns ?? 1,
                1,
                12);
            EasyColumnsComboBox.SelectedItem =
                EasyColumnsComboBox.Items
                    .OfType<ComboBoxItem>()
                    .First(item =>
                        string.Equals(
                            item.Tag?.ToString(),
                            columns.ToString(),
                            StringComparison.Ordinal));
            var gap = block.ContainerSettings?.Gap ?? LayoutGap.Medium;
            EasyGapComboBox.SelectedItem =
                FriendlyGapOptions.First(choice => choice.Value == gap);

            var bindings = EnumerateBlocks(block)
                .Select(item => item.Binding)
                .Where(binding => binding != LayoutBindingKey.None)
                .Distinct()
                .Select(EditorDisplayCatalog.GetBindingLabel)
                .ToArray();
            EasyDataSourceText.Text = bindings.Length == 0
                ? "직접 입력한 문구와 디자인 요소"
                : string.Join(" · ", bindings);

            _easyEditableTextPath = FindFirstEditableTextPath(
                block,
                selected.Path);
            if (_easyEditableTextPath is null)
            {
                EasyFixedTextTextBox.Text = "";
                EasyFixedTextTextBox.IsEnabled = false;
                EasyFixedTextHelpText.Text =
                    "이 섹션은 각 청첩장 관리 화면의 실제 정보와 자동으로 연결됩니다.";
            }
            else
            {
                var editable = GetBlockAtPath(
                    _package.Definition.Root,
                    _easyEditableTextPath);
                EasyFixedTextTextBox.Text = editable.Text;
                EasyFixedTextTextBox.IsEnabled = true;
                EasyFixedTextHelpText.Text =
                    "이 문구는 레이아웃에 포함되며 모든 사용자에게 기본값으로 보입니다.";
            }
        }
        finally
        {
            _refreshingEasyEditor = wasRefreshing;
        }
    }

    private void ClearEasySelectedSection()
    {
        _easyEditableTextPath = null;
        EasySelectionPanel.IsEnabled = false;
        EasySelectionHintText.Text =
            EasySections.Count == 0
                ? "왼쪽에서 첫 번째 섹션을 추가하세요."
                : "가운데에서 섹션을 선택하세요.";
        EasySectionTitleText.Text = "";
        EasySectionDescriptionText.Text = "";
        EasyDataSourceText.Text = "";
        EasyFixedTextTextBox.Text = "";
        EasyFixedTextHelpText.Text = "";
        EasyColumnsComboBox.IsEnabled = false;
        EasyGapComboBox.IsEnabled = false;
        RemoveEasySectionButton.IsEnabled = false;
        RemoveSelectedSectionButton.IsEnabled = false;
        EasyContainerSettingsHelpText.Text = "";
    }

    private void SelectTreePath(IReadOnlyList<int> path)
    {
        if (BlockTree.Items.Count == 0
            || BlockTree.Items[0] is not TreeViewItem root
            || !TryFindTreeItem(root, path, out var selected))
        {
            return;
        }

        selected.IsSelected = true;
        selected.BringIntoView();
    }

    private static int[]? FindFirstEditableTextPath(
        LayoutBlock block,
        IReadOnlyList<int> path)
    {
        if (block.Binding == LayoutBindingKey.None
            && block.Kind is LayoutBlockKind.Heading or LayoutBlockKind.Text)
        {
            return path.ToArray();
        }

        for (var index = 0; index < block.Children.Count; index++)
        {
            var result = FindFirstEditableTextPath(
                block.Children[index],
                path.Append(index).ToArray());
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static TreeViewItem CreateTreeItem(
        LayoutBlock block,
        int[] path)
    {
        var binding = block.Binding == LayoutBindingKey.None
            ? ""
            : $"  ← {block.Binding}";
        var item = new TreeViewItem
        {
            Header = $"{block.Kind}  ·  {block.Id}{binding}",
            Tag = path,
            IsExpanded = path.Length < 2,
        };

        for (var index = 0; index < block.Children.Count; index++)
        {
            item.Items.Add(CreateTreeItem(
                block.Children[index],
                path.Append(index).ToArray()));
        }

        return item;
    }

    private static bool TryFindTreeItem(
        TreeViewItem current,
        IReadOnlyList<int> path,
        out TreeViewItem result)
    {
        if (current.Tag is int[] currentPath
            && currentPath.SequenceEqual(path))
        {
            result = current;
            return true;
        }

        foreach (var child in current.Items.OfType<TreeViewItem>())
        {
            if (TryFindTreeItem(child, path, out result))
            {
                current.IsExpanded = true;
                return true;
            }
        }

        result = null!;
        return false;
    }

    private void ShowSelectedBlock(LayoutBlock block, IReadOnlyList<int> path)
    {
        _refreshingBlockEditor = true;
        try
        {
            SelectedBlockPathText.Text = path.Count == 0
                ? "$.definition.root"
                : "$.definition.root" + string.Concat(
                    path.Select(index => $".children[{index}]"));
            BlockIdTextBox.Text = block.Id;
            BlockKindComboBox.SelectedItem = block.Kind;
            BlockBindingComboBox.SelectedItem = block.Binding;
            BlockVariantComboBox.SelectedItem = block.Variant;
            BlockTextTextBox.Text = block.Text;
            BlockColumnsTextBox.Text =
                (block.ContainerSettings?.Columns ?? 1).ToString();
            BlockGapComboBox.SelectedItem =
                block.ContainerSettings?.Gap ?? LayoutGap.Medium;
            SetBlockEditorEnabled(true);
        }
        finally
        {
            _blockEditorDirty = false;
            _refreshingBlockEditor = false;
        }
    }

    private void ClearSelectedBlockEditor()
    {
        _refreshingBlockEditor = true;
        try
        {
            _selectedBlockPath = null;
            SelectedBlockPathText.Text = "블록을 선택하세요.";
            BlockIdTextBox.Text = "";
            BlockTextTextBox.Text = "";
            SetBlockEditorEnabled(false);
        }
        finally
        {
            _blockEditorDirty = false;
            _refreshingBlockEditor = false;
        }
    }

    private void SetBlockEditorEnabled(bool enabled)
    {
        BlockIdTextBox.IsEnabled = enabled;
        BlockKindComboBox.IsEnabled = enabled;
        BlockBindingComboBox.IsEnabled = enabled;
        BlockVariantComboBox.IsEnabled = enabled;
        BlockTextTextBox.IsEnabled = enabled;
        BlockColumnsTextBox.IsEnabled = enabled;
        BlockGapComboBox.IsEnabled = enabled;
        ApplyBlockButton.IsEnabled = enabled;
    }

    private LayoutBlock GetBlockAtPath(LayoutBlock root, IReadOnlyList<int> path)
    {
        var current = root;
        foreach (var index in path)
        {
            if (index < 0 || index >= current.Children.Count)
            {
                throw new InvalidOperationException("블록 트리 선택 경로가 유효하지 않습니다.");
            }

            current = current.Children[index];
        }

        return current;
    }

    private void UpdateBlockAtPath(
        IReadOnlyList<int> path,
        Func<LayoutBlock, LayoutBlock> update)
    {
        _package = _package with
        {
            Definition = _package.Definition with
            {
                Root = ReplaceBlock(_package.Definition.Root, path, 0, update),
            },
        };
    }

    private static LayoutBlock ReplaceBlock(
        LayoutBlock block,
        IReadOnlyList<int> path,
        int depth,
        Func<LayoutBlock, LayoutBlock> update)
    {
        if (depth == path.Count)
        {
            return update(block);
        }

        var childIndex = path[depth];
        var children = block.Children.ToArray();
        children[childIndex] = ReplaceBlock(
            children[childIndex],
            path,
            depth + 1,
            update);
        return block with { Children = children };
    }

    private string CreateUniqueBlockId(string prefix)
    {
        var ids = EnumerateBlocks(_package.Definition.Root)
            .Select(block => block.Id)
            .ToHashSet(StringComparer.Ordinal);
        for (var index = 1; ; index++)
        {
            var candidate = $"{prefix}-{index}";
            if (!ids.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private LayoutBlock MakeBlockIdsUnique(LayoutBlock candidate)
    {
        var used = EnumerateBlocks(_package.Definition.Root)
            .Select(block => block.Id)
            .ToHashSet(StringComparer.Ordinal);

        LayoutBlock Visit(LayoutBlock block)
        {
            var preferred = string.IsNullOrWhiteSpace(block.Id)
                ? EditorDisplayCatalog.GetBlockLabel(block.Kind)
                    .Replace(" ", "-", StringComparison.Ordinal)
                    .ToLowerInvariant()
                : block.Id;
            var id = preferred;
            for (var suffix = 2; !used.Add(id); suffix++)
            {
                id = $"{preferred}-{suffix}";
            }

            return block with
            {
                Id = id,
                Children = block.Children.Select(Visit).ToArray(),
            };
        }

        return Visit(candidate);
    }

    private static IEnumerable<LayoutBlock> EnumerateBlocks(LayoutBlock root)
    {
        yield return root;
        foreach (var child in root.Children)
        {
            foreach (var descendant in EnumerateBlocks(child))
            {
                yield return descendant;
            }
        }
    }

    private void SynchronizeSectionOrder()
    {
        var order = new List<LayoutSectionKey>();
        foreach (var child in _package.Definition.Root.Children)
        {
            foreach (var section in LayoutRecipeCatalog.GetSectionKeys(child))
            {
                if (!order.Contains(section))
                {
                    order.Add(section);
                }
            }
        }

        _package = _package with
        {
            Definition = _package.Definition with
            {
                SectionOrder = order,
            },
        };
        SectionOrderTextBox.Text = string.Join(", ", order);
    }

    private bool ConfirmDiscardChanges()
    {
        if (!_isDirty)
        {
            return true;
        }

        return MessageBox.Show(
                this,
                "저장하지 않은 변경 내용이 있습니다. 현재 작업을 버리고 계속할까요?",
                "저장하지 않은 변경",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            e.Cancel = true;
            return;
        }

        PreviewAudioPlayer.Stop();
        PreviewVideoPlayer.Stop();
    }

    private void MarkDirty()
    {
        if (_loadingPackageIntoEditor || FilePathText is null)
        {
            return;
        }

        _isDirty = true;
        UpdateFilePathText();
    }

    private void UpdateFilePathText()
    {
        var label = string.IsNullOrWhiteSpace(_filePath)
            ? "아직 저장되지 않은 레이아웃"
            : Path.GetFileName(_filePath);
        FilePathText.Text = _isDirty ? $"● {label}" : label;
        FilePathText.ToolTip = _filePath;
    }

    private string BuildSuggestedFileName()
    {
        var key = KeyTextBox.Text.Trim();
        var version = VersionTextBox.Text.Trim();
        return string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(version)
            ? "layout-package.json"
            : $"{key}-{version}.json";
    }

    private void ShowLoadError(Exception exception)
    {
        var error = new LayoutValidationError(
            "$",
            "json.read",
            exception.Message);
        ValidationErrorsList.ItemsSource = new[] { error };
        ValidationSummaryText.Text = "JSON 패키지를 열 수 없습니다.";
        ValidationSummaryText.Foreground =
            System.Windows.Media.Brushes.Firebrick;
        SetStatus("엄격한 JSON 역직렬화에 실패했습니다.");
        MessageBox.Show(
            this,
            exception.Message,
            "열기 실패",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void ShowPackageValidationLoadError(
        IReadOnlyList<LayoutValidationError> errors,
        string filePath)
    {
        ValidationErrorsList.ItemsSource = errors;
        ValidationSummaryText.Text =
            $"JSON 패키지를 열 수 없습니다 — {errors.Count}개 문제";
        ValidationSummaryText.Foreground =
            System.Windows.Media.Brushes.Firebrick;
        ValidationDetailsExpander.IsExpanded = true;
        SetStatus("안전하지 않거나 불완전한 JSON은 편집 화면에 불러오지 않았습니다.");

        var preview = string.Join(
            Environment.NewLine,
            errors.Take(5).Select(error =>
                $"• {error.Path}: {error.Message}"));
        var remainder = errors.Count > 5
            ? $"{Environment.NewLine}… 외 {errors.Count - 5}개"
            : "";
        MessageBox.Show(
            this,
            $"'{Path.GetFileName(filePath)}'은(는) 공통 Contracts 검증을 "
            + $"통과하지 못했습니다.{Environment.NewLine}{Environment.NewLine}"
            + preview
            + remainder,
            "패키지 검증 실패",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
    }

    private static SolidColorBrush CreateBrush(string colorText)
    {
        var brush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(colorText));
        brush.Freeze();
        return brush;
    }

    private void SetTransitionPreviewMode(LayoutTransitionKind transition)
    {
        var showBook = transition == LayoutTransitionKind.PageTurn;
        TransitionPreviewFlatHost.Visibility =
            showBook ? Visibility.Collapsed : Visibility.Visible;
        TransitionPreviewBookHost.Visibility =
            showBook ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BeginBookPageTurnPreview(TimeSpan duration)
    {
        _transitionPreviewRunning = true;
        TransitionPreviewButton.IsEnabled = false;
        SetTransitionPreviewMode(LayoutTransitionKind.PageTurn);

        if (!SystemParameters.ClientAreaAnimation)
        {
            BeginReducedMotionBookPreview(duration);
            return;
        }

        var rotation = KeyFrameAnimation(
            duration,
            (0, 0d),
            (.46, -78d),
            (.56, -102d),
            (1, -180d));
        rotation.FillBehavior = FillBehavior.HoldEnd;
        rotation.Completed += (_, _) =>
            CompleteTransitionPreview("3D PageTurn 미리보기를 완료했습니다.");

        var curvature = KeyFrameAnimation(
            duration,
            (0, 1d),
            (.46, .93d),
            (.56, .91d),
            (1, 1d));
        curvature.FillBehavior = FillBehavior.HoldEnd;

        var shadowOpacity = KeyFrameAnimation(
            duration,
            (0, 0d),
            (.38, .34d),
            (.52, .7d),
            (.68, .3d),
            (1, 0d));
        shadowOpacity.FillBehavior = FillBehavior.HoldEnd;

        var shadowTravel = KeyFrameAnimation(
            duration,
            (0, 48d),
            (.5, 0d),
            (1, -48d));
        shadowTravel.FillBehavior = FillBehavior.HoldEnd;

        TransitionPreviewBookRotation.BeginAnimation(
            AxisAngleRotation3D.AngleProperty,
            rotation);
        TransitionPreviewBookCurveScale.BeginAnimation(
            ScaleTransform3D.ScaleXProperty,
            curvature);
        TransitionPreviewBookShadow.BeginAnimation(
            OpacityProperty,
            shadowOpacity);
        TransitionPreviewBookShadowTranslate.BeginAnimation(
            TranslateTransform.XProperty,
            shadowTravel);

        SetStatus("오른쪽 페이지의 앞면과 뒷면을 3D로 넘기고 있습니다.");
    }

    private void BeginReducedMotionBookPreview(TimeSpan requestedDuration)
    {
        // 시스템 애니메이션이 꺼진 환경에서는 회전 없이 최종 페이지를
        // 먼저 준비하고, 현재 펼침면 overlay만 짧게 사라지게 한다.
        TransitionPreviewBookRotation.Angle = -180;
        TransitionPreviewBookFadeOverlay.Visibility = Visibility.Visible;
        TransitionPreviewBookFadeOverlay.Opacity = 1;

        var fadeDuration = TimeSpan.FromMilliseconds(
            Math.Clamp(requestedDuration.TotalMilliseconds * .25, 100, 180));
        var fade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = fadeDuration,
            FillBehavior = FillBehavior.HoldEnd,
        };
        fade.Completed += (_, _) =>
        {
            TransitionPreviewBookFadeOverlay.Visibility = Visibility.Collapsed;
            CompleteTransitionPreview(
                "시스템 모션 설정에 따라 짧은 페이드로 미리보기를 완료했습니다.");
        };
        TransitionPreviewBookFadeOverlay.BeginAnimation(
            OpacityProperty,
            fade);
        SetStatus("시스템 모션 설정에 따라 짧은 페이드로 전환합니다.");
    }

    private void CompleteTransitionPreview(string status)
    {
        if (!_transitionPreviewRunning)
        {
            return;
        }

        _transitionPreviewRunning = false;
        TransitionPreviewButton.IsEnabled = true;
        SetStatus(status);
    }

    private void ResetTransitionPreview()
    {
        _transitionPreviewRunning = false;
        TransitionPreviewButton.IsEnabled = true;

        TransitionPreviewScale.BeginAnimation(
            System.Windows.Media.ScaleTransform.ScaleXProperty,
            null);
        TransitionPreviewScale.ScaleX = 1;
        TransitionPreviewRotate.BeginAnimation(
            System.Windows.Media.RotateTransform.AngleProperty,
            null);
        TransitionPreviewRotate.Angle = 0;
        TransitionPreviewTranslate.BeginAnimation(
            System.Windows.Media.TranslateTransform.XProperty,
            null);
        TransitionPreviewTranslate.BeginAnimation(
            System.Windows.Media.TranslateTransform.YProperty,
            null);
        TransitionPreviewTranslate.X = 0;
        TransitionPreviewTranslate.Y = 0;
        TransitionPreviewPage.BeginAnimation(OpacityProperty, null);
        TransitionPreviewPage.Opacity = 1;

        TransitionPreviewBookRotation.BeginAnimation(
            AxisAngleRotation3D.AngleProperty,
            null);
        TransitionPreviewBookRotation.Angle = 0;
        TransitionPreviewBookCurveScale.BeginAnimation(
            ScaleTransform3D.ScaleXProperty,
            null);
        TransitionPreviewBookCurveScale.ScaleX = 1;
        TransitionPreviewBookShadow.BeginAnimation(OpacityProperty, null);
        TransitionPreviewBookShadow.Opacity = 0;
        TransitionPreviewBookShadowTranslate.BeginAnimation(
            TranslateTransform.XProperty,
            null);
        TransitionPreviewBookShadowTranslate.X = 48;
        TransitionPreviewBookFadeOverlay.BeginAnimation(OpacityProperty, null);
        TransitionPreviewBookFadeOverlay.Opacity = 1;
        TransitionPreviewBookFadeOverlay.Visibility = Visibility.Collapsed;
    }

    private static DoubleAnimationUsingKeyFrames KeyFrameAnimation(
        TimeSpan duration,
        params (double Progress, double Value)[] frames)
    {
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = duration,
            FillBehavior = FillBehavior.Stop,
        };
        foreach (var (progress, value) in frames)
        {
            animation.KeyFrames.Add(new SplineDoubleKeyFrame(
                value,
                KeyTime.FromTimeSpan(
                    TimeSpan.FromMilliseconds(
                        duration.TotalMilliseconds * progress)),
                new KeySpline(.2, .72, .18, 1)));
        }

        return animation;
    }

    private static LayoutPackage CreateStarterPackage()
    {
        static LayoutBlock Bound(
            string id,
            LayoutBlockKind kind,
            LayoutBindingKey binding,
            string text = "") =>
            new()
            {
                Id = id,
                Kind = kind,
                Binding = binding,
                Text = text,
            };

        static LayoutBlock Section(
            string id,
            LayoutBindingKey binding,
            params LayoutBlock[] children) =>
            new()
            {
                Id = id,
                Kind = LayoutBlockKind.Section,
                Binding = binding,
                ContainerSettings = new LayoutContainerSettings(),
                Children = children,
            };

        return new LayoutPackage
        {
            Manifest = new LayoutManifest
            {
                SchemaVersion = LayoutSchema.CurrentVersion,
                Key = "my-wedding-layout",
                Version = "1.0.0",
                Label = "나만의 청첩장",
                Description = "WPF 편집기에서 만든 블록 기반 청첩장 레이아웃입니다.",
                // Schema v1 requires a valid transport value. The editor never
                // treats it as authority; the server assigns the effective tier.
                Tier = LayoutTier.Free,
            },
            Definition = new LayoutDefinition
            {
                Presentation = LayoutPresentationMode.FlipCard,
                Transition = new LayoutTransitionDefinition
                {
                    Kind = LayoutTransitionKind.FlipCard,
                    DurationMilliseconds = 720,
                    EnableSwipe = true,
                    EnableKeyboard = true,
                    ShowNavigation = true,
                },
                SectionOrder =
                [
                    LayoutSectionKey.Hero,
                    LayoutSectionKey.Invitation,
                    LayoutSectionKey.Calendar,
                    LayoutSectionKey.Gallery,
                    LayoutSectionKey.Story,
                    LayoutSectionKey.Location,
                    LayoutSectionKey.Accounts,
                    LayoutSectionKey.Guestbook,
                    LayoutSectionKey.Contact,
                ],
                StyleTokens =
                [
                    new LayoutStyleTokenValue
                    {
                        Token = LayoutStyleToken.PrimaryColor,
                        Value = "#B88A58",
                    },
                    new LayoutStyleTokenValue
                    {
                        Token = LayoutStyleToken.BackgroundColor,
                        Value = "#FFFCF8",
                    },
                    new LayoutStyleTokenValue
                    {
                        Token = LayoutStyleToken.SurfaceColor,
                        Value = "#FFFFFF",
                    },
                    new LayoutStyleTokenValue
                    {
                        Token = LayoutStyleToken.TextColor,
                        Value = "#2F2924",
                    },
                ],
                Root = new LayoutBlock
                {
                    Id = "page",
                    Kind = LayoutBlockKind.Page,
                    Binding = LayoutBindingKey.Invitation,
                    ContainerSettings = new LayoutContainerSettings(),
                    Children =
                    [
                        new LayoutBlock
                        {
                            Id = "hero",
                            Kind = LayoutBlockKind.Hero,
                            Binding = LayoutBindingKey.Invitation,
                            Variant = LayoutVisualVariant.Hero,
                            ImageSettings = new LayoutImageSettings
                            {
                                AspectRatio = LayoutImageAspectRatio.Portrait,
                                Fit = LayoutImageFit.Cover,
                                CornerRadius = LayoutCornerRadius.None,
                                AltText = "신랑 신부 대표 사진",
                            },
                            ContainerSettings = new LayoutContainerSettings
                            {
                                Alignment = LayoutAlignment.Center,
                            },
                            Children =
                            [
                                Bound(
                                    "couple-name",
                                    LayoutBlockKind.Heading,
                                    LayoutBindingKey.CoupleName),
                                Bound(
                                    "wedding-date",
                                    LayoutBlockKind.Text,
                                    LayoutBindingKey.WeddingDate),
                            ],
                        },
                        Section(
                            "invitation-section",
                            LayoutBindingKey.Invitation,
                            Bound(
                                "invitation-title",
                                LayoutBlockKind.Heading,
                                LayoutBindingKey.None,
                                "초대합니다"),
                            Bound(
                                "invitation-message",
                                LayoutBlockKind.Text,
                                LayoutBindingKey.Subtitle)),
                        Section(
                            "calendar-section",
                            LayoutBindingKey.None,
                            Bound(
                                "calendar",
                                LayoutBlockKind.Calendar,
                                LayoutBindingKey.Calendar)),
                        Section(
                            "gallery-section",
                            LayoutBindingKey.None,
                            Bound(
                                "gallery",
                                LayoutBlockKind.Gallery,
                                LayoutBindingKey.Gallery)),
                        Section(
                            "story-section",
                            LayoutBindingKey.None,
                            Bound(
                                "story",
                                LayoutBlockKind.Text,
                                LayoutBindingKey.Story)),
                        Section(
                            "location-section",
                            LayoutBindingKey.None,
                            Bound(
                                "map",
                                LayoutBlockKind.Map,
                                LayoutBindingKey.Map)),
                        Section(
                            "accounts-section",
                            LayoutBindingKey.None,
                            Bound(
                                "accounts",
                                LayoutBlockKind.AccountList,
                                LayoutBindingKey.Accounts)),
                        Section(
                            "guestbook-section",
                            LayoutBindingKey.None,
                            Bound(
                                "guestbook",
                                LayoutBlockKind.Guestbook,
                                LayoutBindingKey.Guestbook)),
                        Section(
                            "contact-section",
                            LayoutBindingKey.None,
                            Bound(
                                "contacts",
                                LayoutBlockKind.ContactList,
                                LayoutBindingKey.Contacts)),
                    ],
                },
            },
        };
    }
}

public sealed class EditableStyleToken
{
    public EditableStyleToken(LayoutStyleToken token, string value)
    {
        Token = token;
        Value = value;
    }

    public LayoutStyleToken Token { get; set; }

    public string Value { get; set; }
}
