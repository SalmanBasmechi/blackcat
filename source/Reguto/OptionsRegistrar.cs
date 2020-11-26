﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reguto.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Reguto
{
    public static class OptionsRegistrar
    {
        public static void ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            services.ConfigureOptions(configuration, assemblies);
        }

        public static void ConfigureOptions(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
        {
            if(assemblies is null)
            {
                return;
            }

            var options = from assembly in assemblies
                          from option in assembly.FindOptions()
                          select option;

            foreach (var option in options)
            {
                services.ConfigureOption(configuration, option);
            }
        }

        private static IEnumerable<(Type Type, string Section)> FindOptions(this Assembly assembly)
        {
            var options = from type in assembly.GetTypes()
                          let optionsAttibute = (OptionsAttribute)type.GetCustomAttributes()
                                                                      .FirstOrDefault(c => c.GetType() == typeof(OptionsAttribute))
                          where optionsAttibute is not null
                          select (type, optionsAttibute.Section);

            return options;
        }

        private static void ConfigureOption(this IServiceCollection services, IConfiguration configuration, (Type Type, string Section) option)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var section = configuration.GetSection(option.Section);
            var extensionType = typeof(OptionsConfigurationServiceCollectionExtensions);
            string methodName = nameof(OptionsConfigurationServiceCollectionExtensions.Configure);

            extensionType.GetMethods()
                         .Where(c => c.Name == methodName)
                         .First(c =>
                         {
                             var parameters = c.GetParameters()
                                               .ToArray();

                             return parameters.Length == 2 &&
                                    parameters[0].ParameterType == typeof(IServiceCollection) &&
                                    parameters[1].ParameterType == typeof(IConfiguration);
                         })
                         .MakeGenericMethod(option.Type)
                         .Invoke(null, new object[] { services, section });
        }
    }
}
