using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Misc;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewImageLayerCmd : CommandBase
{
    private readonly ImageLayerSetting _setting;
    private CommonLayerSetting _commonSetting;
    private Sprite2D _sprite;
    private CompositeDisposable _subs;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public NewImageLayerCmd(Image image, CommonLayerSetting commonSetting = null)
    {
        _setting = new ImageLayerSetting
        {
            Texture = ImageTexture.CreateFromImage(image)
        };
        _commonSetting = commonSetting;
    }

    public NewImageLayerCmd(ImageLayerSetting setting, CommonLayerSetting commonSetting = null)
    {
        _setting = setting;
        _commonSetting = commonSetting;
    }

    public override void BeforeFirstDo(Entity layerE)
    {
        layerE.Add(new LayerTreeNode());

        _commonSetting ??= new CommonLayerSetting { Name = { Value = "Image".Tr() } };
        layerE.Add(_commonSetting);
        _commonSetting.RegisterProperties(CommandManager).AddTo(layerE);
    }

    public override void Do(Entity layerE)
    {
        _subs = new();
        _subs.AddTo(layerE);

        layerE.Tag<ToSerializeTag>();
        Document.Get<LayerTreeNode>().AddChild(layerE);
        layerE.Add(_setting);
        CommandManager.RegisterProperty(_setting.ImageTransform).AddTo(_subs);

        // View
        var worldView = Document.Get<WorldView>();
        _sprite = new Sprite2D
        {
            Texture = _setting.Texture,
        };
        worldView.AddChild(_sprite);
        _setting.ImageTransform.Subscribe(_sprite.SetTransform).AddTo(_subs);
        layerE.Add(_sprite);
        _sprite.SetOwner(worldView);

        _commonSetting.IsVisible.Subscribe(_sprite.SetVisible).AddTo(_subs);
        _commonSetting.Opacity.Subscribe(v =>
        {
            var color = _sprite.SelfModulate;
            color.A = v;
            _sprite.SelfModulate = color;
        }).AddTo(_subs);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        var layerOverlay = new TransformOverlayBox(_setting.ImageSize) { Visible = false };
        layerE.Add(layerOverlay);
        worldOverlay.AddChild(layerOverlay);
        _setting.ImageTransform.Subscribe(t =>
        {
            layerOverlay.LocalTransform = t;
            layerOverlay.UpdateGeometry();
        }).AddTo(_subs);

        // Panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateAdd(layerE);
    }

    public override void Undo(Entity layerE)
    {
        // Panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.RemoveFree(layerE);

        // Overlay
        layerE.Get<TransformOverlayBox>().QueueFree();
        layerE.Remove<TransformOverlayBox>();

        // View
        _sprite.QueueFree();
        layerE.Remove<Sprite2D>();

        // Data
        layerE.Remove<ImageLayerSetting>();
        Document.Get<LayerTreeNode>().RemoveChild(layerE);
        layerE.Detach<ToSerializeTag>();

        _subs.Dispose();
    }
}