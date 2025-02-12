namespace IOU.Web.Models
{
    public class StudentGuardian
    {
        public string StudentUserId { get; set; }
        public Student Student { get; set; }

        public string GuardianUserId { get; set; }
        public Guardian Guardian { get; set; }
    }
}
