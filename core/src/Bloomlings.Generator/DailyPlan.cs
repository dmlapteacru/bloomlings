using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Random;

namespace Bloomlings.Generator
{
    /// <summary>
    /// The Daily Challenge pool's plan (the owner, 2026-10-07): which of the Daily Challenge's own pictures
    /// (<see cref="PicturePicker.DailyTheme"/>) each entry shows, and its class. Every entry shows a picture no other entry
    /// shows, and a subject (the pictures of one drawing, <see cref="SubjectOf"/>) comes back only after
    /// <see cref="SubjectWindow"/> entries; the pictures the plan leaves over are spares for an entry whose picture makes no
    /// level. The classes follow the week (<see cref="WeeklyClass"/>): four Normal days, Hard on Wednesday and Saturday and
    /// Super Hard on Sunday, as the pool's day index falls in 2026. The plan depends on the pictures and the seed only.
    /// </summary>
    public sealed class DailyPlan
    {
        /// <summary>The fewest entries between two pictures of the same subject.</summary>
        public const int SubjectWindow = 60;

        /// <summary>The weekday of pool entry 1: the pool's first day, 2026-01-01, was a Thursday.</summary>
        public const DayOfWeek FirstDay = DayOfWeek.Thursday;

        private readonly string[] _entries;
        private readonly List<string> _spares;

        private DailyPlan(string[] entries, List<string> spares)
        {
            _entries = entries;
            _spares = spares;
        }

        /// <summary>The number of entries.</summary>
        public int Count => _entries.Length;

        /// <summary>The pictures no entry shows yet, in the order a failed entry tries them.</summary>
        public IReadOnlyList<string> Spares => _spares;

        /// <summary>The picture of pool entry <paramref name="entry"/> (1-based), or null outside the pool.</summary>
        public string? PictureOf(int entry) => entry >= 1 && entry <= _entries.Length ? _entries[entry - 1] : null;

        /// <summary>
        /// The subject of a picture: its id without the number of the drawing (<c>dragon_02</c> is a <c>dragon</c>), as
        /// <c>sketch_pictures.py --daily</c> names them.
        /// </summary>
        public static string SubjectOf(string pictureId)
        {
            int cut = pictureId.LastIndexOf('_');
            if (cut <= 0 || cut == pictureId.Length - 1)
            {
                return pictureId;
            }

            for (int i = cut + 1; i < pictureId.Length; i++)
            {
                if (pictureId[i] < '0' || pictureId[i] > '9')
                {
                    return pictureId;
                }
            }

            return pictureId.Substring(0, cut);
        }

        /// <summary>The class of pool entry <paramref name="entry"/> (1-based) by its weekday (<see cref="FirstDay"/>).</summary>
        public static DifficultyClass WeeklyClass(int entry)
        {
            var day = (DayOfWeek)(((int)FirstDay + entry - 1) % 7);
            return day switch
            {
                DayOfWeek.Sunday => DifficultyClass.SuperHard,
                DayOfWeek.Wednesday or DayOfWeek.Saturday => DifficultyClass.Hard,
                _ => DifficultyClass.Normal,
            };
        }

        /// <summary>
        /// Plans <paramref name="count"/> entries on <paramref name="pictures"/>: each entry draws, from the subjects with
        /// the most pictures left that are not within <see cref="SubjectWindow"/> entries of their last use, one subject
        /// and one of its pictures, both seeded. Throws when the pictures cannot fill the pool under these rules.
        /// </summary>
        public static DailyPlan Build(IEnumerable<BasePicture> pictures, int count, ulong seed)
        {
            var ids = new List<string>();
            foreach (BasePicture picture in pictures)
            {
                ids.Add(picture.Id);
            }

            return Build(ids, count, seed);
        }

        /// <summary>The plan of <see cref="Build(IEnumerable{BasePicture}, int, ulong)"/> on the pictures' ids.</summary>
        public static DailyPlan Build(IEnumerable<string> pictureIds, int count, ulong seed)
        {
            var bySubject = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (string id in pictureIds)
            {
                string subject = SubjectOf(id);
                if (!bySubject.TryGetValue(subject, out List<string>? ids))
                {
                    bySubject[subject] = ids = new List<string>();
                }

                if (!ids.Contains(id))
                {
                    ids.Add(id);
                }
            }

            foreach (List<string> ids in bySubject.Values)
            {
                ids.Sort(StringComparer.Ordinal);
            }

            var rng = new Xoshiro256StarStar(seed ^ 0xDA11UL);
            var subjects = new List<string>(bySubject.Keys);
            var last = new Dictionary<string, int>(StringComparer.Ordinal);
            var entries = new string[count];
            for (int e = 0; e < count; e++)
            {
                int most = 0;
                var candidates = new List<string>();
                foreach (string subject in subjects)
                {
                    int left = bySubject[subject].Count;
                    if (left == 0 || (last.TryGetValue(subject, out int at) && e - at < SubjectWindow))
                    {
                        continue;
                    }

                    if (left > most)
                    {
                        most = left;
                        candidates.Clear();
                    }

                    if (left == most)
                    {
                        candidates.Add(subject);
                    }
                }

                if (candidates.Count == 0)
                {
                    throw new InvalidOperationException($"daily plan: no picture for entry {e + 1} of {count}: {bySubject.Count} subjects cannot fill the pool with a subject only every {SubjectWindow} entries.");
                }

                string chosen = candidates[rng.NextInt(candidates.Count)];
                List<string> left2 = bySubject[chosen];
                int pick = rng.NextInt(left2.Count);
                entries[e] = left2[pick];
                left2.RemoveAt(pick);
                last[chosen] = e;
            }

            var spares = new List<string>();
            foreach (List<string> ids in bySubject.Values)
            {
                spares.AddRange(ids);
            }

            return new DailyPlan(entries, spares);
        }

        /// <summary>
        /// Gives entry <paramref name="entry"/> the first spare whose subject is at least <see cref="SubjectWindow"/>
        /// entries from every other entry of that subject, and returns it; the entry's old picture is used no more. Null,
        /// with the entry unchanged, when no spare fits.
        /// </summary>
        public string? Replace(int entry)
        {
            for (int i = 0; i < _spares.Count; i++)
            {
                string subject = SubjectOf(_spares[i]);
                bool fits = true;
                for (int e = 0; e < _entries.Length && fits; e++)
                {
                    fits = e == entry - 1 || Math.Abs(e - (entry - 1)) >= SubjectWindow || !string.Equals(SubjectOf(_entries[e]), subject, StringComparison.Ordinal);
                }

                if (fits)
                {
                    string spare = _spares[i];
                    _spares.RemoveAt(i);
                    _entries[entry - 1] = spare;
                    return spare;
                }
            }

            return null;
        }
    }
}
