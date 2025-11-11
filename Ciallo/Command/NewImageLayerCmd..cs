using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

public class NewImageLayerCmd : CommandBase
{
    public Entity LayerE;
    public ImageLayerSetting Setting;
    public Sprite2D Sprite;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(LayerE);

    public NewImageLayerCmd(Image image)
    {
        Setting = new ImageLayerSetting
        {
            Texture = ImageTexture.CreateFromImage(image)
        };
    }

    public NewImageLayerCmd(ImageLayerSetting setting)
    {
        Setting = setting;
    }

    public override void Do()
    {
        // Data
        InitEntity();
        LayerE.Tag<ToSerializeTag>();
        Document.Get<LayerTreeNode>().AddChild(LayerE);

        // View
        var worldView = Document.Get<WorldView>();
        Sprite = new Sprite2D
        {
            Texture = Setting.Texture,
        };
        worldView.AddChild(Sprite);
        Setting.ImageTransform.Subscribe(Sprite.SetTransform).AddTo(Sprite);
        LayerE.Add(Sprite);
        Sprite.SetOwner(worldView);
        LayerE.Get<LayerTreeNode>().IsVisible.Subscribe(Sprite.SetVisible).AddTo(Sprite);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        var layerOverlay = new TransformOverlayBox(Setting.ImageSize) { Visible = false };
        LayerE.Add(layerOverlay);
        worldOverlay.AddChild(layerOverlay);
        Setting.ImageTransform.Subscribe(t =>
        {
            layerOverlay.LocalTransform = t;
            layerOverlay.UpdateGeometry();
        }).AddTo(layerOverlay);

        // Panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateAdd(LayerE);
    }

    public override void Undo()
    {
        // Panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.RemoveFree(LayerE);

        // Overlay
        LayerE.Get<TransformOverlayBox>().QueueFree();
        LayerE.Remove<TransformOverlayBox>();

        // View
        Sprite.QueueFree();

        // Data
        Document.Get<LayerTreeNode>().RemoveChild(LayerE);
        LayerE.Detach<ToSerializeTag>();
    }

    public Entity InitEntity()
    {
        if (LayerE.IsNull)
        {
            LayerE = WorkingWorld.Create();
            var node = new LayerTreeNode { Name = { Value = "Image".Tr() } };
            LayerE.Add(node);
            LayerE.Add(Setting);
        }

        return LayerE;
    }
}