using System;
using System.Data;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    // Telegrami se samo čitaju, kroz pogled nad dbo.Telegrami.
    internal static class TelegramiRepo
    {
        internal static DataTable ZaPeriod(DateTime od, DateTime doDatuma)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT * FROM planiranje.OP_vTelegram WHERE VaziOd <= @do AND (VaziDo IS NULL OR VaziDo >= @od)",
                    "@od", od, "@do", doDatuma);
        }
    }
}
