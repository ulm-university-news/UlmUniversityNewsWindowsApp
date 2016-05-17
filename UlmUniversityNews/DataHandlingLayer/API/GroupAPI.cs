using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.API
{
    public class GroupAPI : API
    {
        /// <summary>
        /// Erzeugt eine Instanz der Klasse GroupAPI.
        /// </summary>
        public GroupAPI()
            : base()
        {

        }

        /// <summary>
        /// Sende einen Request an den REST Server, um eine neue Gruppen-Ressource anzulegen.
        /// Dieser Request kann von Nutzern abgesetzt werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="jsonContent">Die Daten der zu erstellenden Gruppe.</param>
        /// <returns>Die Antwort des Servers. Beim erfolgreichen Erstellen ist das die neu angelegte Ressource.
        ///     Antwort wird als String zurückgeliefert.</returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendCreateGroupRequest(string serverAccessToken, string jsonContent)
        {
            // Setzte Request zum Anlegen einer Gruppe ab.
            string serverResponse = await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/group",
                null
                );

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Abfragen aller Gruppen-Ressourcen im System an den REST Server.
        /// Der Request kann von Nutzern abgesetzt werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <returns>Die Antwort des Servers als String. Hierbei handelt es sich um die abgerufenen Gruppen-Ressourcen.</returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetAllGroupsRequest(string serverAccessToken)
        {
            // Setze Request zum Abfragen aller Gruppen-Ressourcen ab.
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group",
                null,
                false);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Abfragen von Gruppen-Ressourcen im System an den REST Server. Die Abfrage kann durch die
        /// Parameterwerte eingeschränkt werden. So können z.B. Gruppen mit einem bestimmten Namen oder einem bestimmten Typ 
        /// abgefragt werden.
        /// Der Request kann von Nutzern abgesetzt werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupName">Der vollständige oder teilweise Name der Gruppen-Ressourcen, die abgerufen werden sollen.
        ///     Parameter wird ignoriert, wenn er null ist.</param>
        /// <param name="groupType">Der Typ der Gruppen-Ressourcen, die abgerufen werden sollen. Der Typ wird als String kodiert,
        ///     entspricht jedoch den Werten des Enums GroupType. Erlaubt sind WORKING und TUTORIAL. Der Parameter wird ignoriert,
        ///     wenn er null ist.</param>
        /// <returns>Die Antwort des Servers als String. Hierbei handelt es sich um die abgerufenen Gruppen-Ressourcen.</returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetGroupsRequest(string serverAccessToken, string groupName, string groupType)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (groupName != null)
            {
                parameters.Add("groupName", groupName);
            }
            if (groupType != null)
            {
                parameters.Add("groupType", groupType);
            }
            
            // Setze Request zum Abfragen aller Gruppen-Ressourcen ab, eingeschränkt durch die Parameter.
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group",
                parameters,
                false);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Abfragen der Informationen zu der Gruppen-Ressource mit der angegebnen Id
        /// an den REST Server.
        /// Der Request kann von Nutzern abgesetzt werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppen-Ressource, die abgerufen werden soll.</param>
        /// <param name="withCaching">Gibt an, ob Caching bei diesem Request zugelassen werden soll.</param>
        /// <returns>Die Antwort des Server als String. Hierbei handelt es sich um die abgerufene Gruppen-Ressource.</returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetGroupRequest(string serverAccessToken, int groupId, bool withCaching)
        {
            // Setze Request zum Abfragen der Informationen zu der Gruppen-Ressource mit der angegebenen Id.
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString(),
                null,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sende Request zum Aktualisieren der Gruppen-Ressource mit der angegebenen Id auf dem REST Server.
        /// Der Inhalt muss in Form eines JSON Merge Patch Dokuments an den Server übermittelt werden.
        /// Der Request kann vom Gruppenadministrator der Gruppe ausgeführt werden. 
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="jsonContent">Das JSON Merge Patch Dokument als String.</param>
        /// <returns>Die Antwort des Servers als String. In diesem Fall wird die aktualisierte Ressource zurückgeliefert.</returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendUpdateGroupRequest(string serverAccessToken, int groupId, string jsonContent)
        {
            // Sende Request zum Aktualisieren der Gruppen-Ressource.
            string serverResponse = await base.SendHttpPatchRequestWithJsonBody(
                serverAccessToken,
                jsonContent,
                "/group/" + groupId.ToString(),
                null);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Löschen der Gruppen-Ressource mit der angegebenen Id. Die Ressource
        /// wird auf dem REST Server gelöscht. Der Request kann vom Gruppenadministrator ausgeführt werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, die gelöscht werden soll.</param>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task SendDeleteGroupRequest(string serverAccessToken, int groupId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString());
        }
    }
}
