using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewCelFolderCmd : NewFolderLayerCmd
{
    public override void BeforeFirstDo(Entity targetE)
    {
        base.BeforeFirstDo(targetE);
        targetE.Get<FolderLayerSetting>().IsCelFolder = true;
        targetE.Get<CommonLayerSetting>().Name.Value = "Animation folder".Tr();
    }
}