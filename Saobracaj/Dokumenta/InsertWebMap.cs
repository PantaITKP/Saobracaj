using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using Syncfusion.Windows.Forms.Grid.Grouping;

namespace Saobracaj.Dokumenta
{
    internal class InsertWebMap
    {
        string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
        public void InsertAPI(int IDRecords, string IDVehicles, DateTime RecordTime, decimal Latitude, decimal Longitude, decimal Altitude, decimal Direction, int SV, decimal MainVoltage, decimal SpeedAPI,
            DateTime Datum, string LocomotiveID, string DriverId, string NforceFw, string LocoMode, string Speed, string GboxState, string PwrContactState, string ThrottlePos, string TracMotorAvgCurr,
            string TargetPower, string TracPower, string TracForce, string TracMotor1Curr, string TracMotor2Curr, string TracMotor3Curr, string TracMotor4Curr, string CompressorState, string MainResPress,
            string BrakeCylPress, string BrakePipePress, string McoMask, string AsMask, string AlerterMask, string Trip, string AsssState, string ExcLimit, string WheelSlip, string ResetCnt, string SecEngAllow,
            string Eng1State, string Eng1Rpm, string Eng1WorkHours, string Eng1WaterTemp, string Eng1OilTemp, string Eng1OilLevel, string Eng1FuelCons, string Eng2State, string Eng2Rpm, string Eng2WorkHours,
            string Eng2WaterTemp, string Eng2OilTemp, string Eng2OilLevel, string Eng2FuelCons, string ActiveFaultCnt, string ActiveFault1, string ActiveFault2, string ActiveFault3, string ActiveFault4,
            string ActiveFault5, string FaultSync, string FaultAck,DateTime DatumUpisa)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = "InsertWebMap";
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.Add(new SqlParameter("@IDRecords", SqlDbType.Int) { Value = IDRecords });
                            cmd.Parameters.Add(new SqlParameter("@IDVehicles", SqlDbType.NVarChar, 20) { Value = IDVehicles });
                            cmd.Parameters.Add(new SqlParameter("@RecordTime", SqlDbType.DateTime) { Value = RecordTime });
                            cmd.Parameters.Add(new SqlParameter("@Latitude", SqlDbType.Decimal) { Value = Latitude });
                            cmd.Parameters.Add(new SqlParameter("@Longitude", SqlDbType.Decimal) { Value = Longitude });
                            cmd.Parameters.Add(new SqlParameter("@Altitude", SqlDbType.Decimal) { Value = Altitude });
                            cmd.Parameters.Add(new SqlParameter("@Direction", SqlDbType.Decimal) { Value = Direction });
                            cmd.Parameters.Add(new SqlParameter("@SV", SqlDbType.Int) { Value = SV });
                            cmd.Parameters.Add(new SqlParameter("@MainVoltage", SqlDbType.Decimal) { Value = MainVoltage });
                            cmd.Parameters.Add(new SqlParameter("@SpeedAPI", SqlDbType.Decimal) { Value = SpeedAPI });
                            cmd.Parameters.Add(new SqlParameter("@Datum", SqlDbType.DateTime) { Value = Datum });
                            cmd.Parameters.Add(new SqlParameter("@LOCOMOTIVE_ID", SqlDbType.NVarChar, 5) { Value = LocomotiveID });
                            cmd.Parameters.Add(new SqlParameter("@DRIVER_ID", SqlDbType.NVarChar, 5) { Value = DriverId });
                            cmd.Parameters.Add(new SqlParameter("@NFORCE_FW", SqlDbType.NVarChar, 5) { Value = NforceFw });
                            cmd.Parameters.Add(new SqlParameter("@LOCO_MODE", SqlDbType.NVarChar, 5) { Value = LocoMode });
                            cmd.Parameters.Add(new SqlParameter("@SPEED", SqlDbType.NVarChar, 5) { Value = Speed });
                            cmd.Parameters.Add(new SqlParameter("@GBOX_STATE", SqlDbType.NVarChar, 3) { Value = GboxState });
                            cmd.Parameters.Add(new SqlParameter("@PWR_CONTACT_STATE", SqlDbType.NVarChar, 3) { Value = PwrContactState });
                            cmd.Parameters.Add(new SqlParameter("@THROTTLE_POS", SqlDbType.NVarChar, 3) { Value = ThrottlePos });
                            cmd.Parameters.Add(new SqlParameter("@TRAC_MOTOR_AVG_CURR", SqlDbType.NVarChar, 5) { Value = TracMotorAvgCurr });
                            cmd.Parameters.Add(new SqlParameter("@TARGET_POWER", SqlDbType.NVarChar, 5) { Value = TargetPower });
                            cmd.Parameters.Add(new SqlParameter("@TRAC_POWER", SqlDbType.NVarChar, 5) { Value = TracPower });
                            cmd.Parameters.Add(new SqlParameter("@TRAC_FORCE", SqlDbType.NVarChar, 5) { Value = TracForce });
                            cmd.Parameters.Add(new SqlParameter("@TRAC_MOTOR_1_CURR", SqlDbType.NVarChar, 5) { Value = TracMotor1Curr });
                            cmd.Parameters.Add(new SqlParameter("@TRAC_MOTOR_2_CURR", SqlDbType.NVarChar, 5) { Value = TracMotor2Curr });
                            cmd.Parameters.Add(new SqlParameter("@TRAC_MOTOR_3_CURR", SqlDbType.NVarChar, 5) { Value = TracMotor3Curr });
                            cmd.Parameters.Add(new SqlParameter("@TRAC_MOTOR_4_CURR", SqlDbType.NVarChar, 5) { Value = TracMotor4Curr });
                            cmd.Parameters.Add(new SqlParameter("@COMPRESSOR_STATE", SqlDbType.NVarChar, 3) { Value = CompressorState });
                            cmd.Parameters.Add(new SqlParameter("@MAIN_RES_PRESS", SqlDbType.NVarChar, 5) { Value = MainResPress });
                            cmd.Parameters.Add(new SqlParameter("@BRAKE_CYL_PRESS", SqlDbType.NVarChar, 5) { Value = BrakeCylPress });
                            cmd.Parameters.Add(new SqlParameter("@BRAKE_PIPE_PRESS", SqlDbType.NVarChar, 5) { Value = BrakePipePress });
                            cmd.Parameters.Add(new SqlParameter("@MCO_MASK", SqlDbType.NVarChar, 3) { Value = McoMask });
                            cmd.Parameters.Add(new SqlParameter("@AS_MASK", SqlDbType.NVarChar, 3) { Value = AsMask });
                            cmd.Parameters.Add(new SqlParameter("@ALERTER_MASK", SqlDbType.NVarChar, 3) { Value = AlerterMask });
                            cmd.Parameters.Add(new SqlParameter("@TRIP", SqlDbType.NVarChar, 8) { Value = Trip });
                            cmd.Parameters.Add(new SqlParameter("@ASSS_STATE", SqlDbType.NVarChar, 3) { Value = AsssState });
                            cmd.Parameters.Add(new SqlParameter("@EXC_LIMIT", SqlDbType.NVarChar, 3) { Value = ExcLimit });
                            cmd.Parameters.Add(new SqlParameter("@WHEEL_SLIP", SqlDbType.NVarChar, 3) { Value = WheelSlip });
                            cmd.Parameters.Add(new SqlParameter("@RESET_CNT", SqlDbType.NVarChar, 3) { Value = ResetCnt });
                            cmd.Parameters.Add(new SqlParameter("@SEC_ENG_ALLOW", SqlDbType.NVarChar, 3) { Value = SecEngAllow });
                            cmd.Parameters.Add(new SqlParameter("@ENG_1_STATE", SqlDbType.NVarChar, 3) { Value = Eng1State });
                            cmd.Parameters.Add(new SqlParameter("@ENG_1_RPM", SqlDbType.NVarChar, 5) { Value = Eng1Rpm });
                            cmd.Parameters.Add(new SqlParameter("@ENG_1_WORK_HOURS", SqlDbType.NVarChar, 5) { Value = Eng1WorkHours });
                            cmd.Parameters.Add(new SqlParameter("@ENG_1_WATER_TEMP", SqlDbType.NVarChar, 5) { Value = Eng1WaterTemp });
                            cmd.Parameters.Add(new SqlParameter("@ENG_1_OIL_TEMP", SqlDbType.NVarChar, 5) { Value = Eng1OilTemp });
                            cmd.Parameters.Add(new SqlParameter("@ENG_1_OIL_LEVEL", SqlDbType.NVarChar, 5) { Value = Eng1OilLevel });
                            cmd.Parameters.Add(new SqlParameter("@ENG_1_FUEL_CONS", SqlDbType.NVarChar, 5) { Value = Eng1FuelCons });
                            cmd.Parameters.Add(new SqlParameter("@ENG_2_STATE", SqlDbType.NVarChar, 3) { Value = Eng2State });
                            cmd.Parameters.Add(new SqlParameter("@ENG_2_RPM", SqlDbType.NVarChar, 5) { Value = Eng2Rpm });
                            cmd.Parameters.Add(new SqlParameter("@ENG_2_WORK_HOURS", SqlDbType.NVarChar, 5) { Value = Eng2WorkHours });
                            cmd.Parameters.Add(new SqlParameter("@ENG_2_WATER_TEMP", SqlDbType.NVarChar, 5) { Value = Eng2WaterTemp });
                            cmd.Parameters.Add(new SqlParameter("@ENG_2_OIL_TEMP", SqlDbType.NVarChar, 5) { Value = Eng2OilTemp });
                            cmd.Parameters.Add(new SqlParameter("@ENG_2_OIL_LEVEL", SqlDbType.NVarChar, 5) { Value = Eng2OilLevel });
                            cmd.Parameters.Add(new SqlParameter("@ENG_2_FUEL_CONS", SqlDbType.NVarChar, 5) { Value = Eng2FuelCons });
                            cmd.Parameters.Add(new SqlParameter("@ACTIVE_FAULT_CNT", SqlDbType.NVarChar, 3) { Value = ActiveFaultCnt });
                            cmd.Parameters.Add(new SqlParameter("@ACTIVE_FAULT_1", SqlDbType.NVarChar, 5) { Value = ActiveFault1 });
                            cmd.Parameters.Add(new SqlParameter("@ACTIVE_FAULT_2", SqlDbType.NVarChar, 5) { Value = ActiveFault2 });
                            cmd.Parameters.Add(new SqlParameter("@ACTIVE_FAULT_3", SqlDbType.NVarChar, 5) { Value = ActiveFault3 });
                            cmd.Parameters.Add(new SqlParameter("@ACTIVE_FAULT_4", SqlDbType.NVarChar, 5) { Value = ActiveFault4 });
                            cmd.Parameters.Add(new SqlParameter("@ACTIVE_FAULT_5", SqlDbType.NVarChar, 5) { Value = ActiveFault5 });
                            cmd.Parameters.Add(new SqlParameter("@FAULT_SYNC", SqlDbType.NVarChar, 3) { Value = FaultSync });
                            cmd.Parameters.Add(new SqlParameter("@FAULT_ACK", SqlDbType.NVarChar, 3) { Value = FaultAck });
                            cmd.Parameters.Add(new SqlParameter("@DatumUpisa", SqlDbType.DateTime) { Value = DatumUpisa });

                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Neuspešan upis u bazu", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MessageBox.Show(ex.ToString());
                    }
                }
                conn.Close();
            }
        }
    }
}
