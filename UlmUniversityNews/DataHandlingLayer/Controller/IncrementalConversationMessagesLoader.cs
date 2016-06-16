using DataHandlingLayer.Common;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Controller
{
    public class IncrementalConversationMessagesLoader : IIncrementalSource<ConversationMessage>
    {
        /// <summary>
        /// Referenz auf eine Instanz des GroupDatabaseManager.
        /// </summary>
        private GroupDatabaseManager groupDBManager;

        /// <summary>
        /// Erzeugt eine Instanz der Klasse IncrementalConversationMessagesLoader.
        /// </summary>
        public IncrementalConversationMessagesLoader()
        {
            groupDBManager = new GroupDatabaseManager();
        }

        /// <summary>
        /// Implementierung der GetPagedItems Methode. Diese Methode ruft abhängig von
        /// den übergebenen Parametern Elemente des Typs ConversationMessage innerhalb eines
        /// bestimmten Bereichs aus der Datenbank ab. Der Bereich wird durch den aktuellen
        /// Seitenindex und die Seitengröße festgelegt.
        /// </summary>
        /// <param name="resourceId">Die Id der Resource, von der die Elemente abgefragt werden sollen. Hier die Konversations-Id.</param>
        /// <param name="pageIndex">Der Index der aktuellen Seite.</param>
        /// <param name="pageSize">Die Anzahl der Elemente pro Seite.</param>
        /// <returns>Eine Auflistung von Elementen des Typs ConversationMessage.</returns>
        public async Task<IEnumerable<ConversationMessage>> GetPagedItems(int resourceId, int pageIndex, int pageSize)
        {
            return await Task.Run<IEnumerable<ConversationMessage>>(() =>
            {
                List<ConversationMessage> retrievedConversationMessages = null;
                try
                {
                    // Rufe genau so viele Konversationsnachrichten ab, wie die PageSize angibt. 
                    // Der Offset gibt dabei an, ab welcher Stelle man die Konversationsnachrichten abrufen will.
                    retrievedConversationMessages = groupDBManager.GetLastestConversationMessages(resourceId, pageSize, pageIndex * pageSize);

                    // Sonderfall. Die neusten Nachrichten werden bei PageIndex 0 abgerufen.
                    // Setze dort das Flag bei der neusten Nachricht.
                    if (pageIndex == 0 && 
                    retrievedConversationMessages != null &&
                    retrievedConversationMessages.Count > 0)
                    {
                        retrievedConversationMessages.First<ConversationMessage>().IsLatestMessage = true;
                    }
                }
                catch (DatabaseException ex)
                {
                    // Gebe Exception nicht an Aufrufer weiter, da diese Methode automatisch vom System aufgerufen wird.
                    Debug.WriteLine("Retrieving latest conversation messages in GetPagedItems has failed. Message is: {0}.", ex.Message);
                }

                return retrievedConversationMessages;
            });
        }

    }
}
