using IOU.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IOU.Web.Data
{
    public class IOUWebContext : IdentityDbContext<ApplicationUser>
    {
        
            public IOUWebContext(DbContextOptions<IOUWebContext> options)
                : base(options)
            {
            }
        
    }
}
