using System;
using System.Text;

namespace Xrv.Qr
{
    /// <summary>
    /// »Hemmelige« QR-koder: kun en app med den rigtige nøgle reagerer på koden.
    ///
    /// Format på QR-koden:   XRV1:&lt;base64&gt;
    /// Indholdet er teksten  "XRV|scene=tower;seed=42;h=10"  XOR'et med nøglen og base64-kodet.
    /// Starter den dekodede tekst med "XRV|", var nøglen rigtig.
    ///
    /// OBS: XOR med en kort nøgle er IKKE rigtig kryptering – det er et filter, så holdets app
    /// ignorerer alle de andre QR-koder i lokalet. Lav jeres egne koder på QR-generator-siden (se README).
    /// </summary>
    public static class SecretCode
    {
        public const string Prefix = "XRV1:";
        public const string Magic = "XRV|";

        /// <summary>Nøglen til de printede ark A–F. Skift den til jeres egen og lav jeres egne koder!</summary>
        public const string DefaultKey = "VIA-XR";

        /// <summary>
        /// Prøver at dekode en QR-kodes tekst med nøglen.
        /// Returnerer true og indholdet (fx "scene=tower;seed=42;h=10"), hvis koden er lavet med denne nøgle.
        /// </summary>
        public static bool TryDecode(string raw, string key, out string content)
        {
            content = null;
            // TODO OPGAVE 2a – Bestem, hvilke QR-koder jeres app reagerer på.
            // En kode ser sådan ud:  XRV1:<base64>   (se forklaringen øverst i filen)
            //   1. Starter raw med Prefix? Ellers: return false.
            //   2. Base64-dekod resten:  Convert.FromBase64String(...)
            //   3. XOR hver byte med nøglens bytes:  Encoding.UTF8.GetBytes(key)
            //   4. Lav bytes om til tekst. Starter den med Magic ("XRV|")? Så er content resten af teksten.
            // Løsningen står på slides – og i branchen "loesning".
            return false;
        }

        /// <summary>Laver QR-tekst ud fra indhold og nøgle. Samme algoritme som QR-generator-siden.</summary>
        public static string Encode(string content, string key)
        {
            var data = Encoding.UTF8.GetBytes(Magic + content);
            var k = Encoding.UTF8.GetBytes(key);
            for (int i = 0; i < data.Length; i++) data[i] ^= k[i % k.Length];
            return Prefix + Convert.ToBase64String(data);
        }
    }
}
