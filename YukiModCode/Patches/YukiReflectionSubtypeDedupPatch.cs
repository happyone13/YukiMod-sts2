using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Patches;

[HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.GetSubtypesFromAssembly))]
public static class YukiReflectionSubtypeDedupPatch
{
    public static void Postfix(Assembly assembly, Type parentType, ref IEnumerable<Type> __result)
    {
        if (__result == null)
        {
            return;
        }

        if (assembly.GetName().Name != MainFile.ModId || parentType != typeof(AbstractModel))
        {
            return;
        }

        __result = __result
            .GroupBy(type => type.FullName ?? type.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }
}
