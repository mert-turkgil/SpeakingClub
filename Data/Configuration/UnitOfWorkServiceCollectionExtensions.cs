using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Data.Concrete;
using SpeakingClub.Identity;

namespace SpeakingClub.Data.Configuration
{
    public static  class UnitOfWorkServiceCollectionExtensions
    {
        public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(
                sp.GetRequiredService<SpeakingClubContext>(),
                sp.GetRequiredService<ApplicationDbContext>()
            ));
            return services;
        }
    }
}