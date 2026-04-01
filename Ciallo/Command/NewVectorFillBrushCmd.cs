using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillBrushCmd : CommandBase
{
    public Entity CopyE { get; }

    public NewVectorFillBrushCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var setting = CopyE.IsNull
            ? new VectorFillBrushSetting()
            {
                FillColor = { Value = Colors.LemonChiffon },
                MarkerColor = { Value = Colors.Black },
                MarkerTexture =
                {
                    Value = ImageTexture.CreateFromImage(GD.Load<Image>("res://Rendering/Image/Bullseye0.svg"))
                },
            }
            : CopyE.Get<VectorFillBrushSetting>().Clone();
        targetE.Add(setting);

        // View
        var strokeMaterial = new StrokeBrushMaterial();
        targetE.Add(strokeMaterial);

        setting.MarkerColor.Subscribe(c =>
        {
            strokeMaterial.SetShaderParameter("MaterialColor", c);
        }).AddTo(targetE);
    }

    public override void Do(Entity targetE)
    {
        targetE.Tag<ToSerializeTag>();
        targetE.Document.Get<BrushManager>().VectorFillBrushEs.Add(targetE);
    }

    public override void Undo(Entity targetE)
    {
        var document = targetE.Document;
        document.Get<BrushManager>().VectorFillBrushEs.Remove(targetE);
        targetE.Detach<ToSerializeTag>();
        document.Get<SelectionManager>().SelectedShapes.Remove(targetE);
    }
}