using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.ArchiveSystem
{
    // Get Archive File
    [ServiceBinding(typeof(td_act_ffile_gaf))]
    public class td_act_ffile_gaf
    {
        private const string CARG_1 = "csharp_arg_1";
        private const string CARG_2 = "csharp_arg_2";
        private const string CRET_STRING = "csharp_return_string";

        [ScriptHandler("td_act_ffile_gaf")]
        public void FPlusGetArchiveFile(CallInfo cinfo)
        {
            NwObject oPC = cinfo.ObjectSelf!;
            string cdkey = cinfo.ScriptParams[CARG_1];
            int index = Int32.Parse(cinfo.ScriptParams[CARG_2]);

            Td_act_file_ex archivesys = new();
            string filename = archivesys.GetArchiveFile(cdkey, index);

            NWScript.SetLocalString(oPC, CRET_STRING, filename);
        }
    }
}
