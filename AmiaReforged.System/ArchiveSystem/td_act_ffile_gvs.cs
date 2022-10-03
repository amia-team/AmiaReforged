using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.ArchiveSystem
{
    // Get Vault Size
    [ServiceBinding(typeof(td_act_ffile_gvs))]
    public class td_act_ffile_gvs
    {
        private const string CARG_1 = "csharp_arg_1";
        private const string CRET_INT = "csharp_return_int";
        [ScriptHandler("td_act_ffile_gvs")]
        public void FPlusGetArchiveSize(CallInfo cinfo)
        {
            NwObject oPC = cinfo.ObjectSelf;
            string cdkey = cinfo.ScriptParams[CARG_1];

            Td_act_file_ex archivesys = new Td_act_file_ex();
            int size = archivesys.GetVaultSize(cdkey);

            NWScript.SetLocalInt(oPC, CRET_INT, size);
        }
    }
}
