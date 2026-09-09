using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegistoFinanceiro.Domain.Services
{
    public static class FinancialRecordStateMachine
    {
        public static bool CanSubmit(string currentStatus) => currentStatus == "draft";
        public static bool CanApprove(string currentStatus) => currentStatus == "submitted";
        public static bool CanCancel(string currentStatus) => currentStatus != "cancelled";
        public static bool CanEdit(string currentStatus) => currentStatus is "draft" or "submitted";
    }
}