using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;

namespace calibre_net.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{

    public virtual DbSet<UserCredential> UserCredentials { get; set; }
    public virtual DbSet<UserPermission> UserPermissions { get; set; }

    public virtual DbSet<Bookmark> Bookmarks {get;set;}
    public virtual DbSet<Read> ReadStates {get;set;}


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityUserLogin<string>>().HasKey(l => new { l.UserId, l.LoginProvider });
        modelBuilder.Entity<IdentityUserRole<string>>().HasKey(r => new { r.UserId, r.RoleId });
        modelBuilder.Entity<IdentityUserToken<string>>().HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
       
        modelBuilder.Entity<UserPermission>().HasKey(p => new {p.UserId, p.PermissionName});
        // modelBuilder.Entity<UserCredential>().OwnsOne(
        //     customer => customer.Descriptor, ownedNavigationBuilder =>
        //     {
        //         ownedNavigationBuilder.ToJson();
        //         // ownedNavigationBuilder.OwnsOne( a => a.Id).
        //         // ownedNavigationBuilder.OwnsOne(contactDetails => contactDetails.);
        //     });

        modelBuilder.Entity<Bookmark>().HasKey(x => new {x.UserId, x.BookId, x.Format});
        modelBuilder.Entity<Read>().HasKey(x => new {x.UserId, x.BookId});
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.EnableSensitiveDataLogging();
}
}


