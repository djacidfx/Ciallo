using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Rendering;
using Godot;
using Massive;
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
        LayerE.Add<ToSerializeTag>();
        Document.Get<LayerTreeManager>().Root.AddChild(LayerE);
        
        // View
        var worldView = Document.Get<WorldView>();
        Sprite = new Sprite2D
        {
            Texture = Setting.Texture,
        };
        worldView.AddChild(Sprite);
        Setting.ImageTransform.Subscribe(Sprite.SetTransform).AddTo(Sprite);
        LayerE.Set(Sprite);
        Sprite.SetOwner(worldView);
        LayerE.Get<LayerTreeNode>().IsVisible.Subscribe(Sprite.SetVisible).AddTo(Sprite);
        
        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        var layerOverlay = new ImageLayerOverlay(Setting.GetCorners()){Visible = false};
        LayerE.Set(layerOverlay);
        worldOverlay.AddChild(layerOverlay);
        
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
        LayerE.Get<ImageLayerOverlay>().QueueFree();
        LayerE.Remove<ImageLayerOverlay>();

        // View
        Sprite.QueueFree();
        
        // Data
        Document.Get<LayerTreeManager>().Root.RemoveChild(LayerE);
        LayerE.Remove<ToSerializeTag>();
    }

    public Entity InitEntity()
    {
        if (LayerE.IsNull())
        {
            LayerE = WorkingWorld.CreateEntity();
            var node = new LayerTreeNode { Name = { Value = "Image".Tr() } };
            LayerE.Set(node);
            LayerE.Set(Setting);
        }

        return LayerE;
    }
}