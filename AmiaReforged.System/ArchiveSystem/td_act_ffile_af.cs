using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.ArchiveSystem
{

    [ServiceBinding(typeof(td_act_ffile_af))]
    public class td_act_ffile_af
    {
        private const bool DEBUG = true;
        private const string CARG_1 = "csharp_arg_1";
        private const string CARG_2 = "csharp_arg_2";
        private const string CARG_3 = "csharp_arg_3";
        private const string CRET_INT = "csharp_return_int";
        private const int TRUE = 1;
        private const int FALSE = 0;
        [ScriptHandler("td_act_ffile_af")]
        public void FPlusArchiveFile(CallInfo cinfo)
        {
            NwObject oPC = cinfo.ObjectSelf;
            string cdkey = cinfo.ScriptParams[CARG_1];
            string fname = cinfo.ScriptParams[CARG_2];
            Td_act_file_ex archivesys = new Td_act_file_ex();


            string curchar = cinfo.ScriptParams[CARG_3];
            if (DEBUG) {
                NWScript.SendMessageToPC(oPC, "PC bic file: " + curchar);
                NWScript.SendMessageToPC(oPC, "Target bic file: " + fname);
            }
            if (curchar == fname) {
                NWScript.SendMessageToPC(oPC, "ERROR: You may not archive the file of the character you are playing");
                NWScript.SetLocalInt(oPC, CRET_INT, FALSE);
                return;
            }


            bool success = archivesys.ArchiveFile(cdkey, fname);

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

