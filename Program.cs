using System;
using System.IO;

namespace PlanktonPopulations
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                using (PlanktonPopulations game = new PlanktonPopulations())
                {
                    game.Run();
                }
            }
            catch (Exception ex)
            {
                string stackTrace = ex.StackTrace;
                //string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), System.DateTime.Now.ToString("MM-dd-yyyy hhmm") + ".mdmp");
                //using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
                //{
                //    MiniDump.Write(fs.SafeFileHandle, MiniDump.Option.WithFullMemory, MiniDump.ExceptionInfo.Present);
                //    fs.Close();
                //}

                string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), System.DateTime.Now.ToString("MM-dd-yyyy hhmm") + ".txt");
                StreamWriter sw = new StreamWriter(fileName);
                sw.WriteLine(ex.ToString());
                sw.Write(stackTrace);
                sw.Flush();
                sw.Close();

                System.Windows.Forms.Application.Restart();
            }
        }
    }
#endif
}

