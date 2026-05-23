using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace YukiMod.YukiModCode.Infrastructure;

public static class ModelIdDeduplicator
{
    public static void DeduplicateForMod(string modIdPrefix)
    {
        try
        {
            Type modelDbType = typeof(ModelDb);
            FieldInfo[] fields = modelDbType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            int removed = 0;
            int scanned = 0;

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                object? value;
                try
                {
                    value = f.GetValue(null);
                }
                catch
                {
                    continue;
                }

                if (value is not IList list || value is string)
                {
                    continue;
                }

                if (list.Count < 2)
                {
                    continue;
                }

                scanned++;

                var seen = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int idx = 0; idx < list.Count; idx++)
                {
                    object? model = list[idx];
                    string? id = GetIdEntry(model);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    if (!id.StartsWith(modIdPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!seen.TryAdd(id, idx))
                    {
                        removed++;
                    }
                }

                if (removed == 0)
                {
                    continue;
                }

                for (int idx = list.Count - 1; idx >= 0; idx--)
                {
                    object? model = list[idx];
                    string? id = GetIdEntry(model);
                    if (string.IsNullOrWhiteSpace(id) || !id.StartsWith(modIdPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!seen.TryGetValue(id, out int firstIndex))
                    {
                        continue;
                    }

                    if (idx != firstIndex)
                    {
                        list.RemoveAt(idx);
                    }
                }
            }

            if (removed > 0)
            {
                Log.Warn($"[{YukiModInfo.ModId}] Deduplicated ModelDb entries: removed={removed} scannedLists={scanned} prefix='{modIdPrefix}'.");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[{YukiModInfo.ModId}] ModelId dedup failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string? GetIdEntry(object? model)
    {
        if (model == null)
        {
            return null;
        }

        Type t = model.GetType();
        PropertyInfo? idProp = t.GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (idProp == null)
        {
            return null;
        }

        object? idObj;
        try
        {
            idObj = idProp.GetValue(model);
        }
        catch
        {
            return null;
        }

        if (idObj == null)
        {
            return null;
        }

        PropertyInfo? entryProp = idObj.GetType().GetProperty("Entry", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (entryProp == null)
        {
            return null;
        }

        try
        {
            return entryProp.GetValue(idObj) as string;
        }
        catch
        {
            return null;
        }
    }
}


