using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.ArchiveSystem;

// Get Archive File
[ServiceBinding(typeof(td_act_ffile_gvf))]
public class td_act_ffile_gvf
{
    private const bool DEBUG_MODE = false;
    private const string CARG_1 = "csharp_arg_1";
    private const string CARG_2 = "csharp_arg_2";
    private const string CRET_STRING = "csharp_return_string";

    [ScriptHandler(scriptName: "td_act_ffile_gvf")]
    public void FPlusGetArchiveFile(CallInfo cinfo)
    {
        NwObject oPC = cinfo.ObjectSelf!;
        string cdkey = cinfo.ScriptParams[CARG_1];
        int index = int.Parse(cinfo.ScriptParams[CARG_2]);

        Td_act_file_ex archivesys = new();
        string filename = archivesys.GetVaultFile(cdkey, index);

        NWScript.SetLocalString(oPC, CRET_STRING, filename);

        if (DEBUG_MODE) NWScript.SendMessageToPC(oPC, "DEBUG: File returned " + filename);
    }
}