using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MX.ZS.PersonalFinances.BLL;
using MX.ZS.PersonalFinances.Commands.Common;
using MX.ZS.PersonalFinances.DAL;
using MX.ZS.PersonalFinances.DAL.Common;
using MX.ZS.PersonalFinances.Domain.DTO.Request;
using MX.ZS.PersonalFinances.Domain.Entities;
using MX.ZS.PersonalFinances.Queries.Common;
using System.Configuration;

namespace MX.ZS.PersonalFinances.WinForms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IQueryHandler, QueryHandler>();
            services.AddSingleton<ICommandHandler, CommandHandler>();
            services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
            services.AddSingleton<IProfileManager, ProfileManager>();
            services.AddTransient<IQueryRepository<List<Account>, RequestAccounts>, AccountsQueryRepository>();
            services.AddTransient<ICommandRepository<Account>, AccountsCommandRepository>();
            services.AddSingleton<IConfiguration>((provider) =>
            {
                return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "liteDBFilePath", @"C:\PersonalFinancesManager\MX.ZS.PersonalFinances\MX.ZS.PersonalFinances.WinForms\bin\MX.ZS.PersonalFinances.WinForms.db" }
                }).Build();
            });
            
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            var provider = services.BuildServiceProvider();
            Application.Run(new Form1(provider.GetRequiredService<IProfileManager>()));
        }
    }
}