namespace IOU.Web.Models.ViewModels
{
    public class TotalDebtOverviewViewModel
    {
        public int TotalDebts { get; set; }
        public decimal TotalOwed { get; set; }
        public decimal TotalPrincipal { get; set; }
        public decimal TotalInterest { get; set; }
        public Dictionary<string, int> DebtsByStatus { get; set; }
        public List<Debt> Debts { get; set; }
    }
}
