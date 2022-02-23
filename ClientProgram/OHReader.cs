
//-DOCUMENTATION: https://github.com/erfg12/memory.dll/wiki
//-SOURCE CODE REPO: https://github.com/erfg12/memory.dll
//-WEBSITE: https://github.com/erfg12/memory.dll
using Memory;

using SharedLibrary.Interfaces;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClientProgram
{
    public class OHReader : IOHReader
    {
        readonly Mem memory;

        public OHReader(string ohTitle)
        {
            if (string.IsNullOrEmpty(ohTitle)) throw new Exception("OpenHoldem(ohTitle) cannot be null or empty");

            memory = new Mem();
        }

        private Process OHProcess(string title)
        {
            // find process
            Process[] processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                if (process.MainWindowTitle.Contains(title) && process.ProcessName.Contains("OpenHoldem"))
                {
                    return process;
                }
            }

            // Process not found
            return null;
        }

        private string _nOuts = "";
        private string _potOdds = "";
        private string _flopOutOdds = "";
        private string _turnOutOdds = "";

        public string GetOHInfo(string title)
        {
            var ohProcess = OHProcess(title);
            if (ohProcess == null) return "Not found";

            memory.OpenProcess(ohProcess.Id);

            // prwin
            var prwin = RoundDoubleToPercentageStr(memory.ReadDouble("OpenHoldem.exe+1643C0"));
            var prtie = RoundDoubleToPercentageStr(memory.ReadDouble("OpenHoldem.exe+1643C8"));
            var prlos = RoundDoubleToPercentageStr(memory.ReadDouble("OpenHoldem.exe+1643D0"));

            // hr2652
            var hr2652 = memory.ReadDouble("OpenHoldem.exe+00139170,0x4,0x88,0x38");

            // from profile
            var potOdds = memory.ReadDouble("OpenHoldem.exe+00164FC8,0x4,0x4,0x8,0x8,0x14,0xBF0");
            var outOdds = memory.ReadDouble("OpenHoldem.exe+00164FC8,0x1A0,0x8C,0x2C,0x5C,0x14,0x4,0x4");
            var nOuts = memory.ReadDouble("OpenHoldem.exe+00164FC8,0xA40,0x14,0x8,0x8,0x4,0x4");
            if (nOuts > 0 || potOdds > 0 || outOdds > 0)
            {
                _potOdds = RoundDoubleToPercentageStr(potOdds);
                _nOuts = RoundDoubleToPercentageStr(nOuts);
                _flopOutOdds = RoundDoubleToPercentageStr(outOdds * 2.0);
                _turnOutOdds = RoundDoubleToPercentageStr(outOdds * 1.2);
            }

            int error = Marshal.GetLastWin32Error();
            if (error > 0)
            {
                return $"ERROR: {error}";
            }

            return $" {prwin} {prtie} {prlos}\n" +
                   $" ({_nOuts}) F{_flopOutOdds}/T{_turnOutOdds}>{_potOdds}\n" +
                   $" {hr2652}/2652  {Math.Round((hr2652 / 2652) * 100, 1)}/100% ";
        }

        public string RoundDoubleToPercentageStr(double value)
        {
            return Math.Round(value * 100, 1).ToString();
        }
    }
}
