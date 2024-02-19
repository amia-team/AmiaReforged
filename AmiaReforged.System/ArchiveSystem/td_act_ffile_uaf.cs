using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.ArchiveSystem;

// Un Archive File
[ServiceBinding(typeof(td_act_ffile_uaf))]
public class td_act_ffile_uaf
{
    private const string CARG_1 = "csharp_arg_1";
    private const string CARG_2 = "csharp_arg_2";
    private const string CRET_INT = "csharp_return_int";
    private const int TRUE = 1;
    private const int FALSE = 0;
    [ScriptHandler("td_act_ffile_uaf")]
    public void FPlusUnArchiveFile(CallInfo cinfo)
    {
        NwObject oPC = cinfo.ObjectSelf!;
        string cdkey = cinfo.ScriptParams[CARG_1];
        string fname = cinfo.ScriptParams[CARG_2];
        Td_act_file_ex archivesys = new Td_act_file_ex();
        bool success = archivesys.UnArchiveFile(cdkey, fname);

        if (success)
        {
            NWScript.SetLocalInt(oPC, CRET_INT, TRUE);
        }
        else
        {
            NWScript.SetLocalInt(oPC, CRET_INT, FALSE);
        }

    }
}