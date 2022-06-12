using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.IO;
using System.Threading;
using System.Security.Principal;
using System.Net.NetworkInformation;

// запускаться должна от имени администратора (для удаленного компьютера - с правами администратора на нем)
// входные параметры: repfolder="каталог для файла отчета"
//                    log="каталог для файла логов"
//                    pc="имя компьютера" для которого создается отчет  
// если нет параметров, записывает файл отчета в каталог запуска программы, там же лог-файл, для текущего ПК
// выходной файл отчета = "имя ПК" + "_Log.txt"
//                логов = "имя ПК" + или "_Smart.txt" 
// пример: "SMART.exe pc=mypc repfolder=c:\programs log=c:\programs"

namespace SMART
{
    public class Program
    {
        static public string sUserPC = ".";
        static public string sPathStart = Environment.CurrentDirectory;
        static public string sReportFile = ""; 
        static public string sFileLog = ""; 
        static public bool bParamLog = true; //true - писать файл логов

        public static void Main(string[] args)
        {   bool bOk = true;
            bool bPing = true;
            AnalizParameters(args);
            string sRoot = "\\\\" + sUserPC + "\\root\\CIMV2";
            string sRootWMI = "\\\\" + sUserPC + "\\root\\WMI";
            DelFilesReportLog();
          {
                if (sUserPC == ".") { bPing = true; }
                else { bPing = CheckPCReady(sUserPC); }
                //bPing = true;//комментарий для отладки
              if (bPing | sUserPC == ".")
              {
                  try
                  { // retrieve list of drives on computer (this will return both HDD's and CDROM's and Virtual CDROM's)                    
                      var dicDrives = new Dictionary<int, SmartHDD>();
                      ReadModelsFromWin32_DiskDrive(sRoot, dicDrives);//для связки по "Model" с данными SMART в MSStorageDriver_FailurePredictData
                      ReadSNFromWin32_PhysicalMedia(sRoot, dicDrives);// retrieve hdd serial number
                      ReadSNFromMSStorageDriver_FailurePredictStatus(sRootWMI, dicDrives);
                      int[] iRealIndex = { };//массив перестановки индексов дисков в MSStorageDriver_FailurePredictData по моделям dicDrives[i].Model
                      ReadSmartAndSetiRealIndex(sRootWMI, ref iRealIndex, dicDrives);//чтение значений Smart
                      ReadMSStorageDriver_FailurePredictThreshold(sRootWMI, iRealIndex, dicDrives);// retreive threshold values foreach attribute
                      bOk = WriteDriveToReport(dicDrives);
                  }
                  catch (ManagementException e)
                  {
                      if (Program.bParamLog == true) WriteToLog("Ошибка чтения WMI: " + e.Message);
                  }
              }
              else
              {
                  if (Program.bParamLog == true) WriteToLog("Нет ping PC=" + sUserPC);
              }
          }
        }

        //********************
        private static bool UserIsAdmin()
        {   bool isElevated = false;
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            return isElevated;
        }

        //********************
        public static bool WriteToReport(string sSay)
        {   bool bOk = false;
            //DateTime dtmp = System.DateTime.Now - фиксировать дату ?;
            if (File.Exists(Program.sReportFile)) File.Delete(Program.sReportFile);
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");
            sSay = sSay + Environment.NewLine;
            try
            {   File.AppendAllText(sReportFile, sSay, Encoding.UTF8);
                bOk = true;
            }
            catch// (ManagementException err)
            {   if (Program.bParamLog == true) WriteToLog("Ошибка записи в файл " + sReportFile); //err.ToString()) 
            }
            return bOk;
        }

        //********************
        public static void DelFilesReportLog()
        {   if (File.Exists(sReportFile))  File.Delete(sReportFile);
            if (File.Exists(sFileLog))     File.Delete(sFileLog);
        }

        //********************
        public static bool WriteToLog(string sSay)
        {   bool bOk = true;
            if (bParamLog == true)
            {   
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");
                sSay = sSay + Environment.NewLine;
                try
                {   File.AppendAllText(sFileLog, sSay, Encoding.UTF8);
                }
                catch //(ManagementException err)
                {  if (Program.bParamLog == true) WriteToLog("Ошибка записи в файл " + sFileLog);
                }
            }
            return bOk;
        }

        //********************
        private static bool CheckPCReady(string sUserPC)
        {   bool pingable = false;
            Ping pinger = null;
            try
            {   pinger = new Ping();
                PingReply reply = pinger.Send(sUserPC);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {   if (Program.bParamLog == true) WriteToLog("Ошибка ping компьютера " + sUserPC);
            }
            finally
            {   if (pinger != null) pinger.Dispose();
            }
            return pingable;
        }

        //********************
        private static void ReadModelsFromWin32_DiskDrive(string sRoot, Dictionary<int, SmartHDD> dicDrives)
        {   string sSelect = "SELECT * FROM Win32_DiskDrive";//для связки по "Model" с данными SMART в MSStorageDriver_FailurePredictData
            string sModel = "";
            int iDriveIndex = 0;
            try
            {   ManagementObjectSearcher searcher0 = new ManagementObjectSearcher(sRoot, sSelect);
                foreach (ManagementObject queryObj in searcher0.Get())
                {   var hdd = new SmartHDD();
                    if (queryObj["Model"] == null) sModel = "NotDefind"; else sModel = queryObj["Model"].ToString().Trim();
                    hdd.Model = sModel;
                    hdd.Type = queryObj["InterfaceType"].ToString().Trim();
                    dicDrives.Add(iDriveIndex, hdd);
                    iDriveIndex++;
                }
            }
            catch
            {  if (bParamLog == true) WriteToLog("Ошибка чтения Win32_DiskDrive в ReadModelsFromWin32_DiskDrive()");
            }
            for (int i = 0; i < iDriveIndex; i++)
                if (Program.bParamLog == true) WriteToLog(i.ToString() + " dicDrives[" + i.ToString() + "].Model = " + dicDrives[i].Model);
        }

        //********************
        private static void ReadSNFromMSStorageDriver_FailurePredictStatus(string sRootWMI, Dictionary<int, SmartHDD> dicDrives)
        {   int iDriveIndex = 0;
            ManagementObjectSearcher searcher =
                   new ManagementObjectSearcher(sRootWMI, "SELECT * FROM MSStorageDriver_FailurePredictStatus");
            try
            { int iTmp = searcher.Get().Count;
            }
            catch// (ManagementException err) 
            {  if (bParamLog == true) WriteToLog("Ошибка чтения MSStorageDriver_FailurePredictStatus в ReadSNFromMSStorageDriver_FailurePredictStatus(()"); //err.ToString()) 
            }
            foreach (ManagementObject queryObj in searcher.Get())
            { dicDrives[iDriveIndex].IsOK = (bool)queryObj.Properties["PredictFailure"].Value == false;
              iDriveIndex++;
            }
        }

        //********************
        private static void ReadMSStorageDriver_FailurePredictThreshold(string sRootWMI, int[] iRealIndex, Dictionary<int, SmartHDD> dicDrives)
        {  // retreive threshold values foreach attribute
            int iDriveIndex = 0;
            ManagementObjectSearcher searcher1 =
                new ManagementObjectSearcher(sRootWMI, "SELECT * FROM MSStorageDriver_FailurePredictThresholds");
            int iTmp = searcher1.Get().Count;
            foreach (ManagementObject queryObj in searcher1.Get())
            {   Byte[] bytes = (Byte[])queryObj.Properties["VendorSpecific"].Value;
                for (int i = 0; i < 30; ++i)
                {   try
                    {   int id = bytes[i * 12 + 2];
                        int thresh = bytes[i * 12 + 3];
                        if (id == 0) continue;
                        var attr = dicDrives[iRealIndex[iDriveIndex]].Attributes[id];
                        attr.Threshold = thresh;
                    }
                    catch
                    { // given key does not exist in attribute collection (attribute not in the dictionary of attributes)
                    }
                }
                iDriveIndex++;
            }
        }

        //********************
        public static void AnalizParameters(string[] sArrPrarmeters) // выходной файл - имя ПК
        {   int iLen = sArrPrarmeters.Length;
            string sPcName = "";
            if (iLen > 0)
            {  for (int i = 0; i < iLen; i++)
                   if (sArrPrarmeters[i].Contains("pc")) sUserPC = sArrPrarmeters[i].Remove(0, 3);
            }
            if (sUserPC == ".") sPcName = (Environment.MachineName).ToLower();
            else sPcName = sUserPC;
            string sFileRepTxt = "\\" + sPcName + "_Smart.txt";
            sReportFile = Program.sPathStart + sFileRepTxt;
            string sFileLogTxt = "\\" + sPcName + "_Log.txt";
            sFileLog = Program.sPathStart + sFileLogTxt;
            if (iLen > 0)
            {   for (int i = 0; i < iLen; i++)
                {   if (sArrPrarmeters[i].Contains("log")) SetLog(sArrPrarmeters[i], sFileRepTxt, sFileLogTxt);
                    if (sArrPrarmeters[i].Contains("repfolder")) SetRepFolder(sArrPrarmeters[i], sFileRepTxt, sFileLogTxt);
                }
            }
            else
            { //если в логе не задан log=, можно задать в программе static public bool bParqmLog = true; 
                sReportFile = Program.sPathStart + sFileRepTxt;
                sFileLog = Program.sPathStart + sFileLogTxt;
            }
        }

        //********************
        public static void SetRepFolder(string sPrarmeter, string sFileRepTxt, string sFileLogTxt)
        {
            if (sPrarmeter.Length > 0)
            {
                if (sPrarmeter.Substring(0, 10) == "repfolder=")
                    sReportFile = (sPrarmeter.Substring(10)) + sFileRepTxt; // "C:\\Programs\\CheckHwPc" + "\\_CheckHwPcLog_.txt";
                else
                {
                    if (File.Exists(sReportFile)) File.Delete(sReportFile);
                    sReportFile = sPrarmeter + sFileRepTxt;
                }
            }
            if (sFileLog == "") sFileLog = Program.sPathStart + sFileLogTxt;
        }

        //********************
        public static void SetLog(string sPrarmeter, string sFileRepTxt, string sFileLogTxt)
        {
            if (sPrarmeter.Length > 0) // проверить что длина >=4 c:\1 (min)
            {
                if (sPrarmeter.Substring(0, 4) == "log=")
                {
                    bParamLog = true;
                    sFileLog = (sPrarmeter.Substring(4)) + sFileLogTxt;
                }
                else
                {
                    if (File.Exists(sFileLog)) File.Delete(sFileLog);
                    sReportFile = sPrarmeter + sFileRepTxt;
                }
            }
            if (sFileLog == "") sFileLog = Program.sPathStart + sFileLogTxt;
        }

        //********************
        private static string WhatModelInst(int iDriveIndex, string[] aModel)
        {
            string sModelInst = aModel[iDriveIndex];
            if (Program.bParamLog == true) WriteToLog(iDriveIndex.ToString() + " MSStorageDriver_FailurePredictData = " + sModelInst);
            sModelInst = sModelInst.Replace(" ", "");//убираем пробелы в середине
            int iStart = sModelInst.IndexOf("Disk&Ven_") + 9;////
            int iEnd = sModelInst.IndexOf("&Prod_");////
            string sModelInst1 = sModelInst.Substring(iEnd + 6);
            sModelInst = sModelInst.Substring(iStart, iEnd - iStart);
            iEnd = sModelInst1.IndexOf("\\");////
            sModelInst = sModelInst.Trim() + sModelInst1.Substring(0, iEnd).Trim();
            sModelInst = sModelInst.Replace("_", "");
            if (Program.bParamLog == true) WriteToLog(" ->  " + sModelInst);
            return sModelInst;
        }

        //********************
        private static string WhatModelInst2(int iDriveIndex, string[] aModel)
        {
            string sModelInst2 = aModel[iDriveIndex].Replace(" ", "");//убрать пробелы в середине
            if (Program.bParamLog == true) WriteToLog(iDriveIndex.ToString() + " MSStorageDriver_FailurePredictData = " + sModelInst2);
            int iStart = sModelInst2.IndexOf("Ven_ATA&Prod_") + 14;////
            int iEnd = sModelInst2.IndexOf("\\4");////
            sModelInst2 = sModelInst2.Substring(iStart, iEnd - iStart);
            sModelInst2 = sModelInst2.Replace("_", "");
            if (Program.bParamLog == true) WriteToLog(" ->  " + sModelInst2);
            return sModelInst2;
        }

        //********************
        private static void ReadSmartAndSetiRealIndex(string sRootWMI, ref int[] iRealIndex, Dictionary<int, SmartHDD> dicDrives)
        {
            string[] aModel = new string[] { };
            string sModelDiskDrive = "";
            string sModelInst = "", sModelInst2 = "";
            string sDiskType = "";
            int iDriveIndex = 0;
            int iStart = -1, iEnd = -1, iTmpIn = -1;
            int iHddDrives = dicDrives.Count(); ;// кол-во HDD в Win32_DiskDrive (dicDrives) в начале
            int iHddCount = 0;// кол-во HDD в MSStorageDriver_FailurePredictData (здесь)
            bool bFound = false;
            try
            {
                ManagementObjectSearcher searcherHdd =
                    new ManagementObjectSearcher(sRootWMI, "SELECT * FROM MSStorageDriver_FailurePredictData");
                iHddCount = searcherHdd.Get().Count;
                foreach (ManagementObject queryObj in searcherHdd.Get())
                {
                    bFound = false;
                    Array.Resize(ref aModel, iDriveIndex + 1); //массив для связывания модели с hdd.Model из Win32_DiskDrive
                    if (queryObj["InstanceName"] == null) aModel[iDriveIndex] = "NotDefined";
                    else aModel[iDriveIndex] = queryObj["InstanceName"].ToString().Trim();
                    if (iHddCount == 1 & iHddDrives == 1) // диск один - путаницы нет
                    {
                        Array.Resize(ref iRealIndex, iDriveIndex + 1);
                        iRealIndex[iDriveIndex] = iDriveIndex;
                        if (Program.bParamLog == true) WriteToLog(iDriveIndex.ToString() + " MSStorageDriver_FailurePredictData = " + aModel[iDriveIndex]);
                        bFound = true;
                    }
                    else // несколько дисков - выбор индекса по модели из dicDrives
                    {
                        sDiskType = aModel[iDriveIndex].Substring(0, 3);
                        if (sDiskType == "IDE")
                        {
                            sModelInst = aModel[iDriveIndex];
                            if (Program.bParamLog == true) WriteToLog(iDriveIndex.ToString() + " MSStorageDriver_FailurePredictData = " + sModelInst);
                            iStart = sModelInst.IndexOf("Disk") + 4;
                            iEnd = sModelInst.IndexOf("___");
                            sModelInst = sModelInst.Substring(iStart, iEnd - iStart).Replace(" ", "");//убираем пробелы в середине
                            if (Program.bParamLog == true) WriteToLog(" ->  " + sModelInst);
                            for (int i = 0; i < iHddDrives; i++)//выбор индекса из dicDrives
                            {
                                sModelDiskDrive = dicDrives[i].Model;
                                iEnd = sModelDiskDrive.IndexOf("ATA Device");
                                sModelDiskDrive = sModelDiskDrive.Substring(0, iEnd - 1).Replace(" ", "");//убираем пробелы в середине
                                sModelDiskDrive = sModelDiskDrive.Substring(0, iEnd - 1).Replace("_", "");//убираем подчеркивание в середине
                                if (sModelDiskDrive == sModelInst)
                                {
                                    iTmpIn = Array.IndexOf(iRealIndex, i);
                                    if (iTmpIn < 0)//диск с такой моделью еще не выбран (для двух одинаковых)
                                    {
                                        Array.Resize(ref iRealIndex, iDriveIndex + 1);
                                        iRealIndex[iDriveIndex] = i;//выбор индекса из dicDrives
                                        if (Program.bParamLog == true) WriteToLog("  iRealIndex[" + iDriveIndex.ToString() + "] = " + i.ToString() +
                                                " (" + sModelInst2 + " in " + sModelDiskDrive + ")");
                                        i = iHddDrives;//выходим из поиска
                                        bFound = true;
                                    }
                                }
                            }
                            if (bFound == false)
                                if (Program.bParamLog == true) WriteToLog("  " + sModelInst + " не найдена в массиве dicDrives[i].Model");
                        }
                        else // SCSI
                        {
                            sModelInst = WhatModelInst(iDriveIndex, aModel);
                            sModelInst2 = WhatModelInst2(iDriveIndex, aModel);
                            bFound = false;
                            for (int i = 0; i < iHddDrives; i++)
                            {
                                sModelDiskDrive = dicDrives[i].Model.Replace(" ", "");//убираем пробелы в середине
                                iStart = sModelDiskDrive.IndexOf(sModelInst);
                                if (iStart >= 0)
                                {
                                    iTmpIn = Array.IndexOf(iRealIndex, i);
                                    if (iTmpIn < 0)//диск с такой моделью еще не выбран (для двух одинаковых)
                                    {
                                        Array.Resize(ref iRealIndex, iDriveIndex + 1);
                                        iRealIndex[iDriveIndex] = i;//выбор индекса из dicDrives
                                        if (Program.bParamLog == true) WriteToLog("  iRealIndex[" + iDriveIndex.ToString() + "] = " + i.ToString() +
                                                  " (" + sModelInst + " in " + sModelDiskDrive + ")");
                                        i = iHddDrives;//выходим из поиска
                                        bFound = true;
                                    }
                                }
                                if (iStart < 0)
                                {
                                    iStart = sModelDiskDrive.IndexOf(sModelInst2);
                                    if (iStart >= 0)
                                    {
                                        Array.Resize(ref iRealIndex, iDriveIndex + 1);
                                        iRealIndex[iDriveIndex] = i;//выбор индекса из dicDrives
                                        if (Program.bParamLog == true) WriteToLog("  iRealIndex[" + iDriveIndex.ToString() + "] = " + i.ToString() +
                                            " (" + sModelInst2 + " in " + sModelDiskDrive + ")");
                                        i = iHddDrives;
                                        bFound = true;
                                    }
                                }
                            }
                            if (bFound == false)
                                if (Program.bParamLog == true) WriteToLog("  " + sModelInst + " не найдена в массиве dicDrives[i].Model");
                        }
                    }
                    if (bFound)
                    {
                        if (queryObj["VendorSpecific"] != null)
                        {
                            Byte[] bytes = (Byte[])queryObj.Properties["VendorSpecific"].Value;
                            ReadAttr(iDriveIndex, iRealIndex, bytes, dicDrives);
                        }
                    }
                    iDriveIndex++;
                }
            }
            catch (ManagementException e)
            {
                if (Program.bParamLog == true) WriteToLog("Ошибка чтения WMI: " + e.Message);
            }
        }


        //********************
        private static bool WriteDriveToReport(Dictionary<int, SmartHDD> dicDrives)
        {
            bool bOk = true;
            string sSay = "";
            string sPar = "";
            string sFlag = "";
            string sData = "";
            string sTmp = "";
            bool bWrite = true;
            foreach (var drive in dicDrives)
            {
                bWrite = true;
                sSay = sSay + "---------------------------------------------------------------------------" + Environment.NewLine; ;
                sSay = sSay + "DRIVE SN=" + drive.Value.Serial + ", Model=" + drive.Value.Model +
                    ", Type=" + drive.Value.Type + Environment.NewLine;
                sSay = sSay + "---------------------------------------------------------------------------" + Environment.NewLine;
                foreach (var attr in drive.Value.Attributes)
                {
                    if (attr.Value.HasData)
                    {
                        if (bWrite)// убран Status
                        {  
                            sSay = sSay + " ID   Parameter                Current Worst Threshold   Type          Data" + Environment.NewLine;
                            bWrite = false;
                        }
                        sPar = attr.Value.Attribute.ToString().Trim();
                        if (sPar.Length < 20) sPar = sPar + (Char)9;
                        sFlag = attr.Value.Flag;
                        sTmp = attr.Value.Data.ToString().Trim();
                        switch (attr.Key)
                      {
                          case 190: 
                              sData = sTmp.Substring(0, 2)+"/"+sTmp.Substring(2, 2)+"/"+sTmp.Substring(4, 2)+"/"+(int.Parse(sTmp.Substring(6, 3))).ToString();
                            break;
                          case 194:
                            if (Convert.ToInt32(sTmp) > 99)
                              sData = sTmp.Substring(0, 2)+"/"+sTmp.Substring(2, 2)+"/"+sTmp.Substring(4, 2)+"/"+(int.Parse(sTmp.Substring(6, 3))).ToString();
                            else sData = attr.Value.Data.ToString().Trim();
                              break;
                          default:
                              sData = attr.Value.Data.ToString().Trim(); 
                              break;
                      }   
                        sSay = sSay + attr.Key.ToString().PadLeft(3) + " " + sPar + (Char)9 + attr.Value.Current.ToString().PadLeft(3) + (Char)9 +
                          attr.Value.Worst.ToString().PadLeft(3) + (Char)9 + attr.Value.Threshold.ToString().PadLeft(3) + (Char)9 +
                          sFlag.PadLeft(3) + (Char)9 + sData.PadLeft(11) + Environment.NewLine; // 
                    }
                }
                sSay = sSay + Environment.NewLine;
            }
            bOk = WriteToReport(sSay);
            return bOk;
        }

        //********************
        private static void ReadAttr(int iDriveIndex, int[] iRealIndex, byte[] bytes, Dictionary<int, SmartHDD> dicDrives)
        {
            for (int i = 0; i < 30; ++i)
            {
                try
                {
                    int id = bytes[i * 12 + 2];
                    int fl = bytes[i * 12 + 3];
                    bool flagType = (fl & 0x1) == 0x1;//false ->Old_Age true -> PreFail
                    int flags = bytes[i * 12 + 4]; // least significant status byte, +3 most significant byte, but not used so ignored.
                     bool failureImminent = (flags & 0x1) == 0x1;//
                    int value = bytes[i * 12 + 5];
                    int worst = bytes[i * 12 + 6];
                    int vendordata = BitConverter.ToInt32(bytes, i * 12 + 7);
                    if (id == 0) continue;
                    if (id == 190) vendordata = Vendor190(i, bytes);
                    if (id == 194 & vendordata > 99) vendordata = Vendor194(i, bytes);
                    var attr = dicDrives[iRealIndex[iDriveIndex]].Attributes[id];
                    if (flagType) attr.Flag = "PreFail"; else attr.Flag = "Old_Age";
                    attr.Current = value;
                    attr.Worst = worst;
                    attr.Data = vendordata;
                    attr.IsOK = failureImminent == false;
                }
                catch
                { // given key does not exist in attribute collection (attribute not in the dictionary of attributes)
                }
            }
        }

        //*********************
        private static int Vendor190(int i, byte[] bytes)
        {
            int iTmpNow = bytes[i * 12 + 7];
            if (iTmpNow > 99) iTmpNow = 99;
            int iTmpMin = bytes[i * 12 + 9];
            if (iTmpMin > 99) iTmpMin = 99;
            int iTmpMax = bytes[i * 12 + 10];
            if (iTmpMax > 99) iTmpMax = 99;
            int iTmpNumb = bytes[i * 12 + 11];
            if (iTmpNumb > 999) iTmpMin = 999;
            int vendordata = iTmpNumb + iTmpMax * 1000 + iTmpMin * 100000 + iTmpNow * 10000000;
            return vendordata;
        }

        //*********************
        private static int Vendor194(int i, byte[] bytes)
        {
            int iTmpNow = bytes[i * 12 + 7];
            if (iTmpNow > 99) iTmpNow = 99;
            int iTmpMin = bytes[i * 12 + 9];
            if (iTmpMin > 99) iTmpMin = 99;
            int iTmpMax = bytes[i * 12 + 10];
            if (iTmpMax > 99) iTmpMax = 99;
            int iTmpNumb = bytes[i * 12 + 11];
            if (iTmpNumb > 999) iTmpMin = 999;
            int vendordata = iTmpNumb + iTmpMax * 1000 + iTmpMin * 100000 + iTmpNow * 10000000;
            return vendordata;
        }

        //********************
        private static void ReadSNFromWin32_PhysicalMedia(string sRoot, Dictionary<int, SmartHDD> dicDrives)
        {   // retrieve hdd serial number
            var pmsearcher = new ManagementObjectSearcher(sRoot, "SELECT * FROM Win32_PhysicalMedia");
            int iDriveIndex = 0;
            try
            {   
                object ss = pmsearcher.Get();
                foreach (ManagementObject drive in pmsearcher.Get())
                {   // because all physical media will be returned we need to exit
                    // after the hard drives serial info is extracted
                    if (iDriveIndex >= dicDrives.Count)
                        break;
                    dicDrives[iDriveIndex].Serial = drive["SerialNumber"] == null ? "None" : drive["SerialNumber"].ToString().Trim();
                    iDriveIndex++;
                }
            }
            catch (ManagementException err)
            {
                if (bParamLog == true) WriteToLog("Ошибка чтения Win32_PhysicalMedia в ReadSNFromWin32_PhysicalMedia()->" + err.ToString()); 
            }
        }



    }
}
