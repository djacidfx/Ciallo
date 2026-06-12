using Ciallo.Data;
using Ciallo.Rendering;
using Godot;
using R3;

namespace Ciallo.GuiControl;

[SceneTree]
public partial class ExportImage : ConfirmationDialog
{
    public readonly ReactiveProperty<float> Scale = new(1f);
    public readonly ReactiveProperty<Color> BackgroundColor = new(Colors.Transparent);

    private DocumentSetting _setting;
    private Camera2D _camera;
    private Polygon2D _background;
    private CompositeDisposable _previewSubs = new();

    public override void _Ready()
    {
        var subs = new CompositeDisposable();
        ScaleNumber.BindNumber(Scale, subs);
        BackgroundColorButton.BindColor(BackgroundColor, subs);
        subs.AddTo(this);

        Confirmed += OnExport;
    }

    private async void OnExport()
    {
        var filePath = PathPicker.Path.PathJoin(FileNameEdit.Text) + ".png";
        var imageSize = _setting.ReferenceSize.Value * Scale.Value;
        ImageSubViewport.Size = new((int)imageSize.X, (int)imageSize.Y);
        _camera.Zoom = Vector2.One * Scale.Value;

        ImageSubViewport.RenderTargetClearMode = SubViewport.ClearMode.Once;
        ImageSubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = ImageSubViewport.GetTexture().GetImage();
        image.SavePng(filePath);
        Message.Show();
    }

    public void Init()
    {
        ImageSubViewport.QueueFreeChildren();
        Message.Hide();
        _previewSubs.Dispose();
        _previewSubs = new();

        var document = AppDocumentManager.WorkingDocument.CurrentValue;
        _setting = document.Get<DocumentSetting>();
        var view = document.Get<WorldView>();

        // Duplicate view
        var scene = new PackedScene();
        scene.Pack(view);
        var root = scene.Instantiate();
        _background = new();
        ImageSubViewport.AddChild(_background);
        BackgroundColor.Subscribe(c =>
        {
            _background.Color = c;
            ImageSubViewport.RenderTargetClearMode = SubViewport.ClearMode.Once;
            ImageSubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        }).AddTo(_previewSubs);
        ImageSubViewport.AddChild(root);
        _camera = new Camera2D();
        ImageSubViewport.AddChild(_camera);

        // Size
        var rSize = _setting.ReferenceSize.Value;
        _background.SetPolygonFromRawRing(new Vector2[] { new(-rSize.X / 2, -rSize.Y / 2), new(rSize.X / 2, -rSize.Y / 2), new(rSize.X / 2, rSize.Y / 2), new(-rSize.X / 2, rSize.Y / 2) });
        ReferenceSizeNumber.Text = $"{rSize.X} x {rSize.Y}";
        Vector2I sizei = new((int)rSize.X, (int)rSize.Y);
        Scale.Subscribe(s =>
        {
            Vector2I size = new((int)(rSize.X * s), (int)(rSize.Y * s));
            FinalImageSizeNumber.Text = $"{size.X} x {size.Y}";
        }).AddTo(_previewSubs);

        ImageSubViewport.Size = sizei;
        ImageSubViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        ImageSubViewport.RenderTargetClearMode = SubViewport.ClearMode.Once;

        // Path, filename
        FileNameEdit.Text = _setting.Name.Value;
        PathPicker.Path = _setting.FilePath.Value.GetBaseDir();
    }
}
