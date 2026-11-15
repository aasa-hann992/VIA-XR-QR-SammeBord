using System.Collections.Generic;

namespace Xrv.Qr
{
    /// <summary>
    /// Hvad står der i QR-koden – og er den til os?
    ///
    /// En kode til øvelsen er almindelig, læsbar tekst:
    ///
    ///     XRV;id=7;model=tower;seed=42;h=10
    ///
    ///   XRV   = det er en kode til denne øvelse (alle andre QR-koder i lokalet ignoreres)
    ///   id    = JERES id. Kun headsets, hvor myId er det samme tal, reagerer på koden.
    ///           Begge headsets i et par sætter samme id, så de reagerer på den samme kode.
    ///   model = hvad der skal stå på koden: tower | orbit | blocks | physics
    ///   seed  = starttallet for tilfældigheden, så begge headsets bygger det SAMME tårn
    ///   h     = tårnets højde i klodser
    ///
    /// Lav koder på QR-generator-siden (se README) – eller med en hvilken som helst QR-generator.
    /// </summary>
    public static class QrCode
    {
        public const string Prefix = "XRV;";

        /// <summary>
        /// Er det en XRV-kode? Så er content teksten efter "XRV;", fx "id=7;model=tower;seed=42;h=10".
        /// Ser IKKE på id – bruges af fysik-demoen, hvor alle grupper kigger på det samme ark.
        /// </summary>
        public static bool TryReadAny(string raw, out string content)
        {
            content = null;
            if (string.IsNullOrEmpty(raw) || !raw.StartsWith(Prefix)) return false;
            content = raw.Substring(Prefix.Length);
            return true;
        }

        /// <summary>
        /// Er koden til os? Kun hvis det er en XRV-kode, OG dens id er det samme som myId.
        /// </summary>
        public static bool TryRead(string raw, string myId, out string content)
        {
            // TODO OPGAVE 2a – Reagér kun på koder med JERES id.
            // Lige nu tager vi ALLE XRV-koder – også de andre gruppers. Prøv det, før I retter!
            //   1. TryReadAny(raw, out content) giver teksten efter "XRV;", fx "id=7;model=tower;seed=42;h=10".
            //   2. PayloadContent.ParsePayload(content) laver teksten om til en Dictionary<string, string>.
            //   3. Returnér kun true, hvis feltet "id" findes OG er det samme som myId.  (Dictionary.TryGetValue)
            return TryReadAny(raw, out content);
        }

        /// <summary>Laver teksten til en kode. Samme format som QR-generator-siden.</summary>
        public static string Make(string id, string model, int seed, int height) =>
            $"{Prefix}id={id};model={model};seed={seed};h={height}";
    }
}
