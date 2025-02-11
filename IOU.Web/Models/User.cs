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

        
    }
    public enum UserType
    {
        Admin,
        Lender,
        Student,
        Guardian
    }
}
