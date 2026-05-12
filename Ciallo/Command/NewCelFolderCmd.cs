using Ciallo.Data;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class NewCelFolderCmd : NewFolderLayerCmd
{
    public override void BeforeFirstDo(Entity targetE)
    {
        CreateData(targetE);
        targetE.Get<CommonLayerSetting>().Name.Value = "Cel folder".Tr();
        targetE.Get<FolderLayerSetting>().IsCelFolder = true;
        CreateOther(targetE);
    }
}