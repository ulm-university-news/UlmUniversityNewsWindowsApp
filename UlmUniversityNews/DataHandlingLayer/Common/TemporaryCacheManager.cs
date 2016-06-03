using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DataHandlingLayer.Common
{
    /// <summary>
    /// Siehe auch https://www.jayway.com/2014/04/23/windows-phone-8-1-for-developers-application-data/
    /// für Infos bezüglich der einzelnen Ordner.
    /// </summary>
    public abstract class TemporaryCacheManager
    {
        /// <summary>
        /// Methode zur Speicherung von Objekten im temporären Speicher.
        /// Das Objekt wird unter einem bestimmten Schlüssel abgelegt. Mittels diesem
        /// kann es wieder extrahiert werden.
        /// </summary>
        /// <param name="key">Der Schlüssel, unter dem das Objekt abgelegt wird.</param>
        /// <param name="element">Das zu speichernde Objekt.</param>
        public async static Task StoreObjectInTmpCacheAsync(string key, object element)
        {
            StorageFolder temp = ApplicationData.Current.TemporaryFolder;
            var tmpFile = await temp.CreateFileAsync(key + ".txt", CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(tmpFile, serialize(element));

            Debug.WriteLine("StoreObjectInTmpCacheAsync: Stored an object in temp directory.");
        }

        /// <summary>
        /// Ruft das im temporären Verzeichnis gespeicherte Objekt ab. 
        /// </summary>
        /// <typeparam name="T">Der Typ des gespeicherten Objekts.</typeparam>
        /// <param name="key">Der Schlüssel, unter dem das Objekt gespeichert ist.</param>
        /// <returns>Das Objekt vom angegebenen Typ.</returns>
        public async static Task<T> RetrieveObjectFromTmpCacheAsync<T>(string key)
        {
            Debug.WriteLine("RetrieveObjectFromTmpCacheAsync: Starting to retrieve object from temp directory.");

            StorageFolder temp = ApplicationData.Current.TemporaryFolder;
            var tmpFile = await temp.GetFileAsync(key + ".txt");

            string fileContent = await FileIO.ReadTextAsync(tmpFile);
            return deserialize<T>(fileContent);
        }

        /// <summary>
        /// Deserialisierung des Objekts in den angegebenen Typ. 
        /// Diese Methode ist übernommen aus:
        /// http://stackoverflow.com/questions/10965829/how-do-i-de-serialize-json-in-winrt
        /// </summary>
        /// <typeparam name="T">Der Typ des Objekts.</typeparam>
        /// <param name="json">Das Objekt in Form eines Strings.</param>
        /// <returns>Objekt vom angegebenen Typ.</returns>
        private static T deserialize<T>(string json)
        {
            var _Bytes = Encoding.Unicode.GetBytes(json);
            using (MemoryStream _Stream = new MemoryStream(_Bytes))
            {
                var _Serializer = new DataContractJsonSerializer(typeof(T));
                return (T)_Serializer.ReadObject(_Stream);
            }
        }

        /// <summary>
        /// Serialisierung des Objekts in eine Zeichenfolge. 
        /// Die Methode ist übernommen aus:
        /// http://stackoverflow.com/questions/10965829/how-do-i-de-serialize-json-in-winrt
        /// </summary>
        /// <param name="instance">Die Objektinstanz.</param>
        /// <returns>Das Objekt serialisiert als Zeichenkette.</returns>
        private static string serialize(object instance)
        {
            using (MemoryStream _Stream = new MemoryStream())
            {
                var _Serializer = new DataContractJsonSerializer(instance.GetType());
                _Serializer.WriteObject(_Stream, instance);
                _Stream.Position = 0;
                using (StreamReader _Reader = new StreamReader(_Stream))
                { return _Reader.ReadToEnd(); }
            }
        }
    }
}
