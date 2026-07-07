using System.Reflection;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using YukiMod.YukiModCode.Cards;
using YukiMod.YukiModCode.Character;
using YukiMod.YukiModCode.Potions;
using YukiMod.YukiModCode.Powers;
using YukiMod.YukiModCode.Relics;

namespace YukiMod.YukiModCode.Migration;

internal static class YukiRitsuContentRegistration
{
    private static readonly MethodInfo ApplyFixedPublicEntryForModelMethod =
        typeof(ModContentRegistry).GetMethod(
            "ApplyFixedPublicEntryForModel",
            BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(
            typeof(ModContentRegistry).FullName,
            "ApplyFixedPublicEntryForModel");

    public static void Register(Assembly assembly)
    {
        var registry = ModContentRegistry.For(MainFile.ModId);

        ApplyLegacyPublicEntry(registry, typeof(Character.YukiMod));
        registry.RegisterCharacter<Character.YukiMod>();

        foreach (var type in GetConcreteTypes(assembly))
        {
            if (typeof(YukiModCard).IsAssignableFrom(type) || typeof(YukiModTokenCard).IsAssignableFrom(type))
            {
                var poolType = type.GetCustomAttribute<PoolAttribute>(inherit: true)?.PoolType ?? typeof(YukiModCardPool);
                registry.RegisterCard(poolType, type, LegacyPublicEntry(type));
                continue;
            }

            if (typeof(YukiModRelic).IsAssignableFrom(type))
            {
                var poolType = type.GetCustomAttribute<PoolAttribute>(inherit: true)?.PoolType ?? typeof(YukiModRelicPool);
                registry.RegisterRelic(poolType, type, LegacyPublicEntry(type));
                continue;
            }

            if (typeof(YukiModPotion).IsAssignableFrom(type))
            {
                var poolType = type.GetCustomAttribute<PoolAttribute>(inherit: true)?.PoolType ?? typeof(YukiModPotionPool);
                registry.RegisterPotion(poolType, type, LegacyPublicEntry(type));
                continue;
            }

            if (typeof(YukiModPower).IsAssignableFrom(type))
            {
                ApplyLegacyPublicEntry(registry, type);
                registry.RegisterPower(type);
            }
        }
    }

    private static IEnumerable<Type> GetConcreteTypes(Assembly assembly)
    {
        return assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false });
    }

    private static ModelPublicEntryOptions LegacyPublicEntry(Type type)
    {
        var stem = ModContentRegistry.NormalizePublicStem(type.Name);
        return ModelPublicEntryOptions.FromFullPublicEntry($"YUKIMOD_{stem}");
    }

    private static void ApplyLegacyPublicEntry(ModContentRegistry registry, Type type)
    {
        ApplyFixedPublicEntryForModelMethod.Invoke(registry, [type, LegacyPublicEntry(type)]);
    }
}
