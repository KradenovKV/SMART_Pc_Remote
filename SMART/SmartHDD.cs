using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SMART
{
    public class SmartHDD
    {
        public int Index { get; set; }
        public string Flag { get; set; }
        public bool IsOK { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string Serial { get; set; }
        public Dictionary<int, Smart> Attributes = new Dictionary<int, Smart>() 
        {
                {0x00, new Smart("Invalid")},
                {0x01, new Smart("Raw_Read_Error_Rate")},
                {0x02, new Smart("Throughput_Performance")},
                {0x03, new Smart("Spin_Up_Time")},
                {0x04, new Smart("Start_Stop_Count")},
                {0x05, new Smart("Reallocated_Sector_Ct")},
                {0x06, new Smart("Read_Channel_Margin,HDD")},
                {0x07, new Smart("Seek_Error_Rate,HDD")},
                {0x08, new Smart("Seek_Time_Performance,HDD")},
                {0x09, new Smart("Power_On_Hours")},
                {0x0A, new Smart("Spin_Retry_Count,HDD")},
                {0x0B, new Smart("Calibration_Retry_Count,HDD")},
                {0x0C, new Smart("Power_Cycle_Count")},
                {0x0D, new Smart("Read_Soft_Error_Rate")},
                  {0xAA, new Smart("Reserved_Block_Count")}, 
                  {0xAB, new Smart("Program_fail_count")}, 
                  {0xAC, new Smart("Erase_fail_block_count")}, 
                  {0xAD, new Smart("Wear_level_count")}, 
                  {0xAE, new Smart("Unexpected_power_loss_count")},
                    {0xAF, new Smart("Program_Fail_Count_Chip,SSD")},
                    {0xB0, new Smart("Erase_Fail_Count_Chip,SSD")},
                    {0xB1, new Smart("Wear_Leveling_Count,SSD")},
                    {0xB2, new Smart("Used_Rsvd_Blk_Cnt_Chip,SSD")},
                    {0xB3, new Smart("Used_Rsvd_Blk_Cnt_Tot,SSD")},
                    {0xB4, new Smart("Unused_Rsvd_Blk_Cnt_Tot,SSD")},
                    {0xB5, new Smart("Program_Fail_Cnt_Total")},
                    {0xB6, new Smart("Erase_Fail_Count_Total,SSD")},
                  {0xB7, new Smart("Runtime_Bad_Block")},
                {0xB8, new Smart("End-to-End_Error")},
                  {0xBB, new Smart("Reported_Uncorrect")},
                    {0xBC, new Smart("Command_Timeout")},
                    {0xBD, new Smart("High_Fly_Writes,HDD")},
                {0xBE, new Smart("Airflow_Temperature_Cel")},
                {0xBF, new Smart("G-Sense_Error_Rate,HDD")},
                {0xC0, new Smart("Power-Off_Retract_Count")},
                {0xC1, new Smart("Load_Cycle_Count,HDD")},
                {0xC2, new Smart("Temperature_Celsius")},
                {0xC3, new Smart("Hardware_ECC_Recovered")},
                {0xC4, new Smart("Reallocated_Event_Count")},
                {0xC5, new Smart("Current_Pending_Sector")},
                {0xC6, new Smart("Offline_Uncorrectable")},
                {0xC7, new Smart("UDMA_CRC_Error_Count")},
                {0xC8, new Smart("Multi_Zone_Error_Rate,HDD")},
                {0xC9, new Smart("Reserved_Block_Count,HDD")},
                {0xCA, new Smart("Data_Address_Mark_Errs,HDD")},
                {0xCB, new Smart("Run_Out_Cancel ")},
                {0xCC, new Smart("Soft_ECC_Correction")},
                {0xCD, new Smart("Thermal_Asperity_Rate_(TAR)")},
                {0xCE, new Smart("Flying_Height,HDD")},
                {0xCF, new Smart("Spin_High_Current,HDD")},
                {0xD0, new Smart("Spin_Buzz,HDD")},
                {0xD1, new Smart("Offline_Seek_Performnce,HDD")},
                   {0xD2, new Smart("Vibration_During_Write")},
                   {0xD3, new Smart("Vibration_During_Read")},
                   {0xD4, new Smart("Shock_During_Write")},
                {0xDC, new Smart("Disk_Shift,HDD")},
                {0xDD, new Smart("G-Sense_Error_Rate,HDD")},
                {0xDE, new Smart("Loaded_Hours,HDD")},
                {0xDF, new Smart("Load_Retry_Count,HDD")},
                {0xE0, new Smart("Load_Friction,HDD")},
                {0xE1, new Smart("Load_Cycle_Count,HDD")},
                {0xE2, new Smart("Load-in_Time,HDD")},
                {0xE3, new Smart("Torq-amp_Count,HDD")},
                {0xE4, new Smart("Power-off_Retract_Count")},
                {0xE6, new Smart("Head_Amplitude,HDD")},
                {0xE7, new Smart("SSD_Life_Left,HDD")},
                   {0xE8, new Smart("Available_Reservd_Space")}, 
                   {0xE9, new Smart("Media_Wearout_Indicator,SSD")},
                {0xF0, new Smart("Head_Flying_Hours,HDD")},
                   {0xF1, new Smart("Total_LBAs_Written")},
                   {0xF2, new Smart("Total_LBAs_Read")},
                   {0xF9, new Smart("Life time writes (NAND)")},
                {0xFA, new Smart("Read_Retry_Coun")},
                   {0xFE, new Smart("Free_Fall_Sensor,HDD")}
                /* slot in any new codes you find in here */
            };
    }
}
