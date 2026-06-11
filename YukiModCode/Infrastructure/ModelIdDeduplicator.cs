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
            var allOccurrences = new Dictionary<string, (IList list, int index)>(StringComparer.OrdinalIgnoreCase);
            var toRemove = new List<(IList list, int index, string id)>();

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
                for (int idx = 0; idx < list.Count; idx++)
                {
                    object? model = list[idx];
                    string? id = GetIdEntry(model);
                    if (string.IsNullOrWhiteSpace(id) || !id.StartsWith(modIdPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!allOccurrences.TryAdd(id, (list, idx)))
                    {
                        toRemove.Add((list, idx, id));
                    }
                }
            }

            if (toRemove.Count > 0)
            {
                var removeGroups = new Dictionary<IList, List<(int index, string id)>>();
                for (int i = 0; i < toRemove.Count; i++)
                {
                    (IList list, int index, string id) = toRemove[i];
                    if (!removeGroups.TryGetValue(list, out var entries))
                    {
                        entries = new List<(int, string)>();
                        removeGroups[list] = entries;
                    }

                    entries.Add((index, id));
                }

                foreach (KeyValuePair<IList, List<(int index, string id)>> kv in removeGroups)
                {
                    kv.Value.Sort((a, b) => b.index.CompareTo(a.index));
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        int idx = kv.Value[i].index;
                        if (idx < 0 || idx >= kv.Key.Count)
                        {
                            continue;
                        }

                        string? id = GetIdEntry(kv.Key[idx]);
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            continue;
                        }

                        if (!id.StartsWith(modIdPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        kv.Key.RemoveAt(idx);
                        removed++;
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
