using UnityEngine;

namespace vtolvrTelemetry
{
    public class Support
    {

        public static void WriteLog(string line)
        {
            Debug.Log($"{Globals.projectName} - {line}");
        }

        public static void WriteErrorLog(string line)
        {
            Debug.LogError($"{Globals.projectName} - {line}");
        }

    }
}
