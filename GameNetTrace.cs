using System;
using System.Diagnostics;
namespace GameNet
{

    public static class GameNetTrace
    {
        public static TraceSource ts = new TraceSource("GameNet");

         public static void Error(string msg) => ts.TraceEvent(TraceEventType.Error, 2, msg);        
        public static void Warn(string msg) => ts.TraceEvent(TraceEventType.Warning, 4, msg); 
        public static void Info(string msg) => ts.TraceEvent(TraceEventType.Information, 8, msg);        
        public static void Debug(string msg) => ts.TraceEvent(TraceEventType.Verbose, 16, msg);     


        public static void InitInCode(TraceLevel defaultLevel = TraceLevel.Error)
        {
            SourceSwitch sourceSwitch = new SourceSwitch("GameNetTraceSwitch", defaultLevel.ToString() );
            ts.Switch = sourceSwitch;
            //int idxConsole = ts.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());  NOTE: .Net core 2.2 does not have a ConsoleTraceListener
            int idxConsole = ts.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out, "console"));
            //ts.Listeners[idxConsole].TraceOutputOptions |= TraceOptions.Timestamp;            
            ts.Listeners[idxConsole].Name = "console";        
        }

    }

}