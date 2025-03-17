using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Data.Concrete;

namespace SpeakingClub.Data.Configuration
{
    public static  class UnitOfWorkServiceCollectionExtensions
    {
        public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}