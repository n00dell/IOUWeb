namespace IOU.Web.Models.ViewModels
{
    public class ActiveLoansViewModel
    {
        public List<Debt> ActiveDebts { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalExpectedInterest { get; set; }
        public Dictionary<string, decimal> LoansByType { get; set; }
    }
}
