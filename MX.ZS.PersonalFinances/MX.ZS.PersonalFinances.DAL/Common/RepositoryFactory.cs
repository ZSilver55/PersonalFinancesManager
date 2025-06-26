namespace MX.ZS.PersonalFinances.DAL.Common
{
    public class RepositoryFactory : IRepositoryFactory
    {
        IServiceProvider _serviceProvider;
        public RepositoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public ICommandRepository<TResult> FabricateCommandRespository<TResult>()
        {
            var repositoryType = typeof(ICommandRepository<TResult>);
            return (ICommandRepository<TResult>)_serviceProvider.GetService(repositoryType);
        }

        public IQueryRepository<TResult, TFilter> FabricateQueryRepository<TResult, TFilter>()
        {
            var repositoryType = typeof(IQueryRepository<TResult, TFilter>);
            return (IQueryRepository<TResult, TFilter>)_serviceProvider.GetService(repositoryType);
        }
    }
}
