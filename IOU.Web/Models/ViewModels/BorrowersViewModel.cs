namespace IOU.Web.Models.ViewModels
{
    public class BorrowersViewModel
    {
        public List<Student> Borrowers { get; set; }
        public Dictionary<string, decimal> TotalOwedByBorrower { get; set; }
        public Dictionary<string, int> ActiveLoansByBorrower { get; set; }
    }
}
