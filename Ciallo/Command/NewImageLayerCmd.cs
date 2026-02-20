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
    private readonly ImageLayerSetting _imageLayerSetting;
    public readonly Entity CopyE;

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

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