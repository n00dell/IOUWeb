using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace IOU.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; }

        [Required]
        public UserType UserType { get; set; }

        public virtual Student Student { get; set; }
        public virtual Lender Lender { get; set; }
        public virtual Guardian Guardian { get; set; }
    }
    public enum UserType
    {
        Admin,
        Lender,
        Student,
        Guardian
    }
}
