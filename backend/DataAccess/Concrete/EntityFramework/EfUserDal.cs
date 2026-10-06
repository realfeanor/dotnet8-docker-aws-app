using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework
{
	public class EfUserDal : EfEntityRepositoryBase<User, NorthwindContext>, IUserDal
	{
		public EfUserDal(NorthwindContext context) : base(context)
		{
		}

        public void AddWithClaims(User user, string[] claimNames)
        {
            // Retry the complete transaction, so an account cannot be saved without its claims.
            var strategy = _context.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                var added = new List<object>();
                using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    user.Id = 0;
                    var claims = new List<OperationClaim>();
                    foreach (var name in claimNames.Distinct())
                    {
                        var claim = _context.OperationClaims.FirstOrDefault(c => c.Name == name);
                        if (claim == null)
                        {
                            claim = new OperationClaim { Name = name };
                            _context.OperationClaims.Add(claim);
                            added.Add(claim);
                        }
                        claims.Add(claim);
                    }
                    _context.Users.Add(user);
                    added.Add(user);
                    _context.SaveChanges();

                    foreach (var claim in claims)
                    {
                        var assignment = new UserOperationClaim { UserId = user.Id, OperationClaimId = claim.Id };
                        _context.UserOperationClaims.Add(assignment);
                        added.Add(assignment);
                    }
                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    // Remove rolled-back entities from tracking before a retry.
                    foreach (var entity in added) _context.Entry(entity).State = EntityState.Detached;
                    throw;
                }
            });
        }

		public List<OperationClaim> GetClaims(User user)
		{
			var result = from operationClaim in _context.OperationClaims
						 join userOperationClaim in _context.UserOperationClaims
							on operationClaim.Id equals userOperationClaim.OperationClaimId
						 where userOperationClaim.UserId == user.Id
						 select new OperationClaim { Id = operationClaim.Id, Name = operationClaim.Name };

			return result.ToList();
		}
	}

}
