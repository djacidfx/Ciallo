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
        // Data
        layerE.Add(new LayerTreeNode());
        _commonSetting ??= new CommonLayerSetting { Name = { Value = "Image".Tr() } };
        layerE.Add(_commonSetting);
        _commonSetting.RegisterProperties(CommandManager).AddTo(layerE);
        layerE.Add(_setting);
        CommandManager.RegisterProperty(_setting.ImageTransform).AddTo(layerE);

        // View
        var sprite = new Sprite2D
        {
            Texture = _setting.Texture,
        };
        layerE.AddNode(sprite);

        _commonSetting.IsVisible.Subscribe(sprite.SetVisible).AddTo(layerE);
        _commonSetting.Opacity.Subscribe(v =>
        {
            var color = sprite.SelfModulate;
            color.A = v;
            sprite.SelfModulate = color;
        }).AddTo(layerE);
        _setting.ImageTransform.Subscribe(sprite.SetTransform).AddTo(layerE);

        // Overlay
        var layerOverlay = new TransformOverlayBox(_setting.ImageSize) { Visible = false };
        layerE.AddNode(layerOverlay);
        _setting.ImageTransform.Subscribe(t =>
        {
            layerOverlay.LocalTransform = t;
            layerOverlay.UpdateGeometry();
        }).AddTo(layerE);
    }

    public override void Do(Entity layerE)
    {
        layerE.Tag<ToSerializeTag>();
        Document.Get<LayerTreeNode>().AddChild(layerE);

        // View
        var worldView = Document.Get<WorldView>();
        var sprite = layerE.Get<Sprite2D>();
        worldView.AddChild(sprite);
        sprite.SetOwner(worldView);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        var layerOverlay = layerE.Get<TransformOverlayBox>();
        worldOverlay.AddChild(layerOverlay);

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
        layerE.Get<TransformOverlayBox>().RemoveFromParent();

        // View
        layerE.Get<Sprite2D>().RemoveFromParent();

        // Data
        Document.Get<LayerTreeNode>().RemoveChild(layerE);
        layerE.Detach<ToSerializeTag>();
    }
}