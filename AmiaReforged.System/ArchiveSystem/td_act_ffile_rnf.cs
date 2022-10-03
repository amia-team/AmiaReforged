using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.ArchiveSystem
{
    // Rename File
    [ServiceBinding(typeof(td_act_ffile_rnf))]
    public class td_act_ffile_rnf
    {
        private const string CARG_1 = "csharp_arg_1";
        private const string CARG_2 = "csharp_arg_2";
        private const string CARG_3 = "csharp_arg_3";
        private const string CRET_INT = "csharp_return_int";
        private const int TRUE = 1;
        private const int FALSE = 0;

        [ScriptHandler("td_act_ffile_rnf")]
        public void FPlusRenameFile(CallInfo cinfo)
        {
            NwObject oPC = cinfo.ObjectSelf;
            string cdkey = cinfo.ScriptParams[CARG_1];
            int index = Int32.Parse(cinfo.ScriptParams[CARG_2]);
            string newname = cinfo.ScriptParams[CARG_3];

            newname = newname.ToLower();
            if (newname.Contains(".")) {
                NWScript.SetLocalInt(oPC, CRET_INT, FALSE);
                return;
            }
            newname = newname + ".bic";

            Td_act_file_ex archivesys = new Td_act_file_ex();
            bool success = archivesys.RenameFile(cdkey, index, newname, FALSE);

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
}
