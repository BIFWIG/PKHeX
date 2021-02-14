﻿using System.Collections.Generic;

using static PKHeX.Core.MysteryGiftGenerator;
using static PKHeX.Core.EncounterTradeGenerator;
using static PKHeX.Core.EncounterSlotGenerator;
using static PKHeX.Core.EncounterStaticGenerator;
using static PKHeX.Core.EncounterEggGenerator;
using static PKHeX.Core.EncounterMatchRating;

namespace PKHeX.Core
{
    internal static class EncounterGenerator8
    {
        public static IEnumerable<IEncounterable> GetEncounters(PKM pkm)
        {
            var chain = EncounterOrigin.GetOriginChain(pkm);
            return pkm.Version switch
            {
                (int)GameVersion.GO => EncounterGenerator7.GetEncountersGO(pkm, chain),
                _ => GetEncountersMainline(pkm, chain)
            };
        }

        private static IEnumerable<IEncounterable> GetEncountersMainline(PKM pkm, IReadOnlyList<EvoCriteria> chain)
        {
            // Static Encounters can collide with wild encounters (close match); don't break if a Static Encounter is yielded.
            int ctr = 0;
            if (pkm.WasEvent || pkm.WasEventEgg)
            {
                foreach (var z in GetValidGifts(pkm, chain))
                { yield return z; ++ctr; }
                if (ctr != 0) yield break;
            }

            if (pkm.WasBredEgg)
            {
                foreach (var z in GenerateEggs(pkm))
                { yield return z; ++ctr; }
                if (ctr == 0) yield break;
            }

            IEncounterable? deferred = null;
            IEncounterable? partial = null;

            foreach (var z in GetValidStaticEncounter(pkm, chain))
            {
                var match = z.GetMatchRating(pkm);
                if (z is IOverworldCorrelation8 {HasOverworldCorrelation:true} s && !s.IsOverworldCorrelationCorrect(pkm))
                    match = Deferred;
                switch (match)
                {
                    case Match: yield return z; ++ctr; break;
                    case Deferred: deferred ??= z; break;
                    case PartialMatch: partial ??= z; break;
                }
            }

            // if (ctr != 0) yield break;
            foreach (var z in GetValidWildEncounters(pkm, chain))
            {
                var match = z.GetMatchRating(pkm);
                var slot = (EncounterSlot8)z;
                if (slot.HasOverworldCorrelation && !slot.IsOverworldCorrelationCorrect(pkm))
                    match = Deferred;
                switch (match)
                {
                    case Match: yield return z; ++ctr; break;
                    case Deferred: deferred ??= z; break;
                    case PartialMatch: partial ??= z; break;
                }
            }

            if (ctr != 0) yield break;
            foreach (var z in GetValidEncounterTrades(pkm, chain))
            {
                var match = z.GetMatchRating(pkm);
                switch (match)
                {
                    case Match: yield return z; /*++ctr*/ break;
                    case Deferred: deferred ??= z; break;
                    case PartialMatch: partial ??= z; break;
                }
            }

            if (deferred != null)
                yield return deferred;

            if (partial != null)
                yield return partial;
        }
    }
}
