using Ciallo.Data;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;

namespace Ciallo.GuiControl;

public class FrameFileNameSetting
{
    public ReactiveProperty<string> Prefix = new("frame");
    public ReactiveProperty<string> Suffix = new("");
    public ReactiveProperty<string> Separator = new("_");
    public ReactiveProperty<int> StartNumber = new(0);
    public int NumberDigits = 4; // Not show user in current version. 
}

[SceneTree]
public partial class ExportFrameSquence : ConfirmationDialog
{
    public readonly ReactiveProperty<float> Scale = new(1f);
    public readonly ReactiveProperty<Color?> BackgroundColor = new(default); // Use nullable color button
    public readonly ReactiveProperty<string> ExportPath = new("");
    public FrameFileNameSetting NameSetting = new();
    public ReadOnlyReactiveProperty<string> PreviewFrameFileName;

    public Entity Document;
    public ExportFrameSquence()
    {
        PreviewFrameFileName = NameSetting.Prefix.CombineLatest(NameSetting.Suffix, NameSetting.Separator, NameSetting.StartNumber,
            (prefix, suffix, separator, startNumber) => FormatFrameFileName(prefix, suffix, separator, startNumber, NameSetting.NumberDigits))
            .ToReadOnlyReactiveProperty();
    }

    public override void _Ready()
    {
        var subs = new CompositeDisposable();

        ScaleNumber.BindNumber(Scale, subs);
        BackgroundColorButton.BindColor(BackgroundColor, subs);
        BindExportPath(subs);
        PrefixEdit.BindString(NameSetting.Prefix, subs);
        SuffixEdit.BindString(NameSetting.Suffix, subs);
        SeparatorEdit.BindString(NameSetting.Separator, subs);
        StartNumber.BindNumber(NameSetting.StartNumber);
        PreviewFrameFileName.Subscribe(fileName => PreviewFrameFileNameLabel.Text = fileName).AddTo(subs);
        subs.AddTo(this);

        Confirmed += Export;
    }

    private void Export()
    {
        var paintViewport = (SubViewport)Document.Get<WorldView>().GetParent();
        ExportViewport.World2D = paintViewport.World2D;
        paintViewport.CanvasCullMask = 0;
    }

    public void PopupCentered(Entity document)
    {
        Document = document;
        ExportPath.Value = document.Get<DocumentSetting>().FilePath.Value.GetBaseDir();
        base.PopupCentered();
    }

    private void BindExportPath(CompositeDisposable subs)
    {
        ExportPath.Subscribe(path => ExportPathPicker.Path = path).AddTo(subs);
        ExportPathPicker.PathEdit.OnTextChangedAsObservable()
            .Subscribe(path => ExportPath.Value = path)
            .AddTo(subs);
    }

    private static string FormatFrameFileName(string prefix, string suffix, string separator, int frameNumber, int digits)
    {
        // If prefix/suffix is not empty, add separator between prefix/suffix and number. If empty, not add separator.
        var number = frameNumber.ToString($"D{digits}");
        var prefixPart = string.IsNullOrEmpty(prefix) ? "" : $"{prefix}{separator}";
        var suffixPart = string.IsNullOrEmpty(suffix) ? "" : $"{separator}{suffix}";
        return $"{prefixPart}{number}{suffixPart}.png";
    }
}
