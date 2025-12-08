using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewImageLayerCmd : CommandBase
{
    private Entity _layerE;
    private readonly ImageLayerSetting _setting;
    private Sprite2D _sprite;
    private CompositeDisposable _subs;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(_layerE);

    public NewImageLayerCmd(Image image)
    {
        _setting = new ImageLayerSetting
        {
            Texture = ImageTexture.CreateFromImage(image)
        };
        InitEntity();
    }

    public NewImageLayerCmd(ImageLayerSetting setting)
    {
        _setting = setting;
        InitEntity();
    }

    public override void Do()
    {
        _subs = new();
        _subs.AddTo(_layerE);

        // Data
        _layerE.Tag<ToSerializeTag>();
        Document.Get<LayerTreeNode>().AddChild(_layerE);
        _layerE.Add(_setting);
        CommandManager.RegisterProperty(_setting.ImageTransform).AddTo(_subs);

        // View
        var worldView = Document.Get<WorldView>();
        _sprite = new Sprite2D
        {
            Texture = _setting.Texture,
        };
        worldView.AddChild(_sprite);
        _setting.ImageTransform.Subscribe(_sprite.SetTransform).AddTo(_subs);
        _layerE.Add(_sprite);
        _sprite.SetOwner(worldView);

        var node = _layerE.Get<LayerTreeNode>();
        node.IsVisible.Subscribe(_sprite.SetVisible).AddTo(_subs);
        node.Opacity.Subscribe(v =>
        {
            var color = _sprite.SelfModulate;
            color.A = v;
            _sprite.SelfModulate = color;
        }).AddTo(_subs);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        var layerOverlay = new TransformOverlayBox(_setting.ImageSize) { Visible = false };
        _layerE.Add(layerOverlay);
        worldOverlay.AddChild(layerOverlay);
        _setting.ImageTransform.Subscribe(t =>
        {
            layerOverlay.LocalTransform = t;
            layerOverlay.UpdateGeometry();
        }).AddTo(_subs);

        // Panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateAdd(_layerE);
    }

    public override void Undo()
    {
        // Panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.RemoveFree(_layerE);

        // Overlay
        _layerE.Get<TransformOverlayBox>().QueueFree();
        _layerE.Remove<TransformOverlayBox>();

        // View
        _sprite.QueueFree();
        _layerE.Remove<Sprite2D>();

        // Data
        _layerE.Remove<ImageLayerSetting>();
        Document.Get<LayerTreeNode>().RemoveChild(_layerE);
        _layerE.Detach<ToSerializeTag>();

        _subs.Dispose();
    }

    public Entity InitEntity()
    {
        if (!_layerE.IsNull) return _layerE;
        _layerE = WorkingWorld.Create();
        var node = new LayerTreeNode { Name = { Value = "Image".Tr() } };
        _layerE.Add(node);
        node.RegisterProperties(Document.Get<CommandManager>()).AddTo(_layerE);
        return _layerE;
    }
}