using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AppDoctor
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("User32.dll")]
        public static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int cch);

        [DllImport("user32.dll")]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName,int nMaxCount);
        
        static void Main(string[] args)
        {
            Console.WriteLine("此程式目的：主動發現程式無回應後砍掉重啟");
            do
            {
                Process[] procs = Process.GetProcesses();//.Where(pr=>pr.ProcessNameByName("IEXPLORE");
                List<CWatchAppParam> allWatchApp = new List<CWatchAppParam>();
                /*
                //MT5MrgNotify||Microsoft|F:\codes\MT5MrgNotify.git\MT5MrgNotify\bin\Release\MT5MrgNotify.exe
                CWatchAppParam tmpApp = new CWatchAppParam(@"MT5MrgNotify||Microsoft|F:\codes\MT5MrgNotify.git\MT5MrgNotify\bin\Release\MT5MrgNotify.exe");
                allWatchApp.Add(tmpApp);
                */
                if (true)
                {
                    string line;
                    try {
                        System.IO.StreamReader file = new System.IO.StreamReader("AppDoctor.cfg");
                        while ((line = file.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (line.IndexOf("#") == 0 || line.Length < 10)
                                continue;
                            CWatchAppParam tmpApp = new CWatchAppParam(line);
                            allWatchApp.Add(tmpApp);
                        }
                        file.Close();
                    }catch(Exception e) {
                        Console.WriteLine("讀 AppDoctor.cfg錯誤! Msg={0}", e.ToString());
                    }
                }
                int iAppIDX = 1;
                foreach (CWatchAppParam app in allWatchApp)
                {
                    string szFindName = app.szFindName;
                    string szIncludeCap = app.szIncludeCap;
                    string szExcludeCap = app.szExcludeCap;
                    string szProg = app.szProg;
                    string szLog = string.Format("#{0} [程式名:{1}|包含字串:'{2}'|不含字串:'{3}']", iAppIDX, szFindName, szIncludeCap, szExcludeCap);
                    iAppIDX++;
                    System.Console.WriteLine(szLog);
                    int FoundCnt = 0;
                    foreach (Process proc in procs)
                    {
                        IntPtr handle = proc.MainWindowHandle;
                        if (handle.ToInt32() == 0)
                            continue;
                        string szClassName, szCaption;
                        if (true)
                        {
                            StringBuilder lpClassName = new StringBuilder(512);
                            GetClassName(handle, lpClassName, 510);
                            StringBuilder lpCaption = new StringBuilder(512);
                            GetWindowText(handle, lpCaption, 510);
                            szClassName = lpClassName.ToString();
                            szCaption = lpCaption.ToString();
                        }



                        if ((proc.ProcessName.ToUpper().IndexOf(szFindName.ToUpper()) == 0 || szCaption.ToUpper().IndexOf(szFindName.ToUpper()) >= 0) &&
                            (szIncludeCap.Length == 0 || szCaption.IndexOf(szIncludeCap) >= 0) &&
                            (szExcludeCap.Length == 0 || szCaption.IndexOf(szExcludeCap) < 0))
                        {
                            FoundCnt++;
                            szLog = string.Format("'{0}':'{1}'", szCaption, szClassName);
                            if (proc.Responding)
                                System.Console.WriteLine(string.Format("  {0} OK.", szLog));
                            else
                            {
                                System.Console.WriteLine(string.Format("  {0} Hang!!!", szLog));
                                proc.Kill();
                                if (szProg.Length > 0)
                                {
                                    int iLastSlashPosi = -1;
                                    while (true)
                                    {
                                        int iNextPosi = szProg.IndexOf(@"\", iLastSlashPosi >= 0 ? iLastSlashPosi + 1 : 0);
                                        if (iNextPosi >= 0)
                                            iLastSlashPosi = iNextPosi;
                                        else
                                            break;
                                    }
                                    string szDir = szProg.Substring(0, iLastSlashPosi);
                                    ProcessStartInfo Info = new ProcessStartInfo();
                                    Info.FileName = "cmd.exe"; //執行的檔案名稱
                                    Info.Arguments = string.Format("/c {0}", szProg);
                                    Info.WorkingDirectory = szDir; //檔案所在的目錄
                                    Info.UseShellExecute = false;
                                    Info.RedirectStandardInput = true;
                                    Info.RedirectStandardOutput = true;
                                    Info.RedirectStandardError = true;
                                    Info.CreateNoWindow = true;
                                    Process.Start(Info);
                                }
                            }
                        }
                    }
                    if (FoundCnt == 0)
                    {
                        System.Console.WriteLine("  Not Found");
                    }
                }
                System.Console.WriteLine("-----------------------------------------");
                Thread.Sleep(5000);
            } while (true);
        }
    }

    class CWatchAppParam
    {
        public string szFindName;
        public string szIncludeCap;
        public string szExcludeCap;
        public string szProg;
        public CWatchAppParam(string szOneLine)
        {
            string[] Parameters = szOneLine.Split('|');
            szFindName = Parameters[0];
            szIncludeCap = Parameters[1];
            szExcludeCap = Parameters[2];
            szProg = Parameters[3];
        }
    }
}
