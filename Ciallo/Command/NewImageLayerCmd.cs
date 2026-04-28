using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewImageLayerCmd : CommandBase
{
    private readonly ImageLayerSetting _imageLayerSetting;
    public readonly Entity CopyE;

    public override void OnDeletedAsDo() => TargetE.Delete();

    public NewImageLayerCmd(Image image)
    {
        _imageLayerSetting = new ImageLayerSetting
        {
            Texture = ImageTexture.CreateFromImage(image)
        };
    }

    public NewImageLayerCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        var commonSetting = CopyE.IsNull
            ? new CommonLayerSetting { Name = { Value = "Image".Tr() } }
            : CopyE.Get<CommonLayerSetting>().Clone();
        targetE.Add(commonSetting);

        var setting = _imageLayerSetting ?? (CopyE.IsNull
            ? new ImageLayerSetting()
            : CopyE.Get<ImageLayerSetting>().Clone());
        targetE.Add(setting);

        // View
        var sprite = new Sprite2D
        {
            Texture = setting.Texture,
        };
        targetE.AddNode(sprite);

        commonSetting.IsVisible.Subscribe(sprite.SetVisible).AddTo(targetE);
        commonSetting.Opacity.Subscribe(v =>
        {
            var color = sprite.SelfModulate;
            color.A = v;
            sprite.SelfModulate = color;
        }).AddTo(targetE);
        setting.ImageTransform.Subscribe(sprite.SetTransform).AddTo(targetE);

        // Overlay
        var layerOverlay = new TransformOverlayBox(setting.ImageSize) { Visible = false };
        targetE.AddNode(layerOverlay);
        setting.ImageTransform.Subscribe(t =>
        {
            layerOverlay.LocalTransform = t;
            layerOverlay.UpdateGeometry();
        }).AddTo(targetE);

        // Layer panel
        targetE.Document.Get<LayerContainer>().Create(targetE);

        // Layer tree events
        var events = layerNode.MovedAsAddedRemoved;

        events.Added.Subscribe(et =>
        {
            // Layer panel
            Document.Get<LayerContainer>().Insert(targetE, et.Index);

            // View
            var worldView = Document.Get<WorldView>();
            worldView.InsertNodeAt(sprite, et.Index);
            sprite.SetOwner(worldView);

            // Overlay
            et.Value.Get<OverlayHolder>().InsertNodeAt(layerOverlay, et.Index);
        }).AddTo(targetE);

        events.Removed.Subscribe(_ =>
        {
            // Layer panel
            Document.Get<LayerContainer>().Remove(targetE);

            // Overlay
            layerOverlay.RemoveFromParent();

            // View
            sprite.RemoveFromParent();
        }).AddTo(targetE);
    }

    public override void Do(Entity targetE)
    {
        targetE.Tag<ToSerializeTag>();
    }

    public override void Undo(Entity targetE)
    {
        targetE.Detach<ToSerializeTag>();
    }
}