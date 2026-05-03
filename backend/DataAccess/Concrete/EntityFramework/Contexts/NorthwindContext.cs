using Microsoft.EntityFrameworkCore;
using Core.Entities.Concrete;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework.Contexts
{
	public class NorthwindContext : DbContext
	{
		public NorthwindContext(DbContextOptions<NorthwindContext> options)
			: base(options)
		{
		}

		public DbSet<Product> Products { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<OperationClaim> OperationClaims { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<UserOperationClaim> UserOperationClaims { get; set; }
		public DbSet<Log> Logs { get; set; }
	}
}
