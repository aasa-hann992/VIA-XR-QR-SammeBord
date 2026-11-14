using System;
using UnityEngine;

namespace Xrv.Qr
{
    /// <summary>
    /// TRIN 3 - "Fælles tid uden netværk".
    /// Alle Quest-headsets henter tiden fra internettet (NTP), så deres ur er ret ens.
    /// Hvis alt der bevæger sig KUN afhænger af uret (og ikke af Time.time, som starter når appen starter),
    /// ser to headsets den samme bevægelse på samme tidspunkt.
    /// </summary>
    public static class SharedClock
    {
        // Vi regner fra en fast dato i stedet for år 0001, så tallet er lille nok til ikke at miste præcision.
        static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Sekunder siden 1. januar 2026 (UTC). Det samme på alle enheder - på nær urets fejl.</summary>
        public static double Seconds => (DateTime.UtcNow - Epoch).TotalSeconds;

        /// <summary>Hvor langt (0..1) vi er inde i en periode af længden periodSeconds.</summary>
        public static float Phase(double periodSeconds, double offsetSeconds = 0)
        {
            double t = (Seconds + offsetSeconds) / periodSeconds;
            return (float)(t - Math.Floor(t));
        }
    }
}
