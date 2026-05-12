using System;
using NXOpen;
using KnBalloonTool.Dialog;

namespace KnBalloonTool
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                using (var dialog = new KnBalloonDialog())
                {
                    dialog.Show();
                }
                return 0;
            }
            catch (NXException ex)
            {
                Session.GetSession().ListingWindow.Open();
                Session.GetSession().ListingWindow.WriteLine("KnBalloonTool NXException: " + ex.Message);
                return ex.ErrorCode;
            }
            catch (Exception ex)
            {
                Session.GetSession().ListingWindow.Open();
                Session.GetSession().ListingWindow.WriteLine("KnBalloonTool Error: " + ex.Message);
                return 1;
            }
        }

        public static void ufusr(string param, ref int retCode, int paramLen)
        {
            retCode = Main(new[] { param ?? string.Empty });
        }

        public static int GetUnloadOption(string arg)
        {
            return (int)Session.LibraryUnloadOption.Immediately;
        }
    }
}
