namespace Destrospean.Common
{
    public static class ProgramUtils
    {
        public static void WriteError(System.Exception ex)
        {
            System.IO.File.WriteAllText(System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "error-" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".log", ex.Message + "\n" + ex.StackTrace);
        }
    }
}
