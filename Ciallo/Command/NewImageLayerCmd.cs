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

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);
        _commonSetting ??= new CommonLayerSetting { Name = { Value = "Image".Tr() } };
        targetE.Add(_commonSetting);
        targetE.Add(_setting);

        // View
        var sprite = new Sprite2D
        {
            Texture = _setting.Texture,
        };
        targetE.AddNode(sprite);

        _commonSetting.IsVisible.Subscribe(sprite.SetVisible).AddTo(targetE);
        _commonSetting.Opacity.Subscribe(v =>
        {
            var color = sprite.SelfModulate;
            color.A = v;
            sprite.SelfModulate = color;
        }).AddTo(targetE);
        _setting.ImageTransform.Subscribe(sprite.SetTransform).AddTo(targetE);

        // Overlay
        var layerOverlay = new TransformOverlayBox(_setting.ImageSize) { Visible = false };
        targetE.AddNode(layerOverlay);
        _setting.ImageTransform.Subscribe(t =>
        {
            layerOverlay.LocalTransform = t;
            layerOverlay.UpdateGeometry();
        }).AddTo(targetE);

        // Layer tree events
        layerNode.TreeEntered.Subscribe(et =>
        {
            OnAdd(et.Value, et.Index);
        }).AddTo(targetE);

        layerNode.TreeExited.Subscribe(_ => OnRemove()).AddTo(targetE);

        layerNode.Moved.Subscribe(et =>
        {
            OnRemove();
            OnAdd(et.Value, et.NewIndex);
        }).AddTo(targetE);

        return;

        void OnAdd(Entity parentE, int index)
        {
            // Panel
            var layerContainer = Document.Get<LayerContainer>();
            layerContainer.CreateInsert(targetE, index);

            // View
            var worldView = Document.Get<WorldView>();
            worldView.InsertNodeAt(sprite, index);
            sprite.SetOwner(worldView);

            // Overlay
            parentE.Get<OverlayHolder>().InsertNodeAt(layerOverlay, index);
        }

        void OnRemove()
        {
            // Overlay
            layerOverlay.RemoveFromParent();

            // View
            sprite.RemoveFromParent();

            // Panel
            Document.Get<LayerContainer>().RemoveFree(targetE);
        }
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