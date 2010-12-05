using Trinity.Encore.Framework.Persistence.Entities;

namespace Trinity.Encore.Services.Account.Database.Implementation
{
    public abstract class AccountDatabaseRecord : IActiveRecord
    {
        public virtual void Create()
        {
            AccountApplication.Instance.AccountDbContext.Post(x => x.Add(this));
        }

        public virtual void Update()
        {
            AccountApplication.Instance.AccountDbContext.Post(x => x.Update(this));
        }

        public virtual void Delete()
        {
            AccountApplication.Instance.AccountDbContext.Post(x => x.Delete(this));
        }
    }
}