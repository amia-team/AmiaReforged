using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.ArchiveSystem
{
    // Get Player BIC
    [ServiceBinding(typeof(td_act_ffile_gpb))]
    public class td_act_ffile_gpb
    {
        private const bool DEBUG = true;
        private const string CRET_STRING = "csharp_return_string";

        [ScriptHandler("td_act_ffile_gpb")]
        public void FPlusGetArchiveFile(NwPlayer caller)
        {
            NwObject oPC = caller.LoginCreature;
            string bic = caller.BicFileName;
         
            if (DEBUG)
            {
                NWScript.SendMessageToPC(oPC, "Player current bic file: " + bic);
            }

            NWScript.SetLocalString(oPC, CRET_STRING, bic);

        }
    }
}
