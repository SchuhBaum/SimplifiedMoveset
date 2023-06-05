using static ProcessManager;
using static SimplifiedMoveset.MainModOptions;

namespace SimplifiedMoveset;

internal static class ProcessManagerMod
{
    internal static void OnEnable()
    {
        On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch;
    }

    //
    // private
    //

    private static void ProcessManager_RequestMainProcessSwitch(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager process_manager, ProcessID next_process_id)
    {
        ProcessID current_process_id = process_manager.currentMainLoop.ID;
        orig(process_manager, next_process_id);

        if (current_process_id == ProcessID.Initialization)
        {
            main_mod_options.Log_All_Options();
            return;
        }

        if (current_process_id == ProcessID.ModdingMenu && next_process_id == ProcessID.MainMenu)
        {
            main_mod_options.Log_All_Options();
            return;
        }
    }
}