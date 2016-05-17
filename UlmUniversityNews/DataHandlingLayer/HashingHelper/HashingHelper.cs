using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace DataHandlingLayer.HashingHelper
{
    public class HashingHelper
    {
        /// <summary>
        /// Erzeugt eine Instanz der Klasse HashingHelper.
        /// </summary>
        public HashingHelper()
        {

        }

        /// <summary>
        /// Generiert einen SHA256 Hash über den übergebenen Inhalt.
        /// </summary>
        /// <param name="content">Der zu hashende Inhalt.</param>
        /// <returns>Den berechneten SHA256 Hash.</returns>
        public string GenerateSHA256Hash(string content)
        {
            IBuffer bufferedContent = CryptographicBuffer.ConvertStringToBinary(content, BinaryStringEncoding.Utf8);

            // Apply SHA-256 Hash function.
            var hashFunction = HashAlgorithmProvider.OpenAlgorithm("SHA256");
            IBuffer hashedContent = hashFunction.HashData(bufferedContent);

            string hash = CryptographicBuffer.EncodeToHexString(hashedContent);

            return hash;
        }

    }
}
