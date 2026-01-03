using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Tuna.SuperTemplate.BaseInfra.Entities.Interface;

namespace Tuna.SuperTemplate.BaseInfra.Extensions;

public static class ModelBuilderExtension
{
    public static void EntityConfigure(this ModelBuilder modelBuilder ,Assembly assembly = null)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(assembly ?? Assembly.GetExecutingAssembly(), IsEnityConf
           );
    }
    public static bool IsEnityConf(Type type)
    {
        return Array .Exists(type.GetInterfaces(), t =>
            t.IsGenericType && 
            t.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>) &&
            typeof(IEntity).IsAssignableFrom(t.GenericTypeArguments[0]));
    }
}
