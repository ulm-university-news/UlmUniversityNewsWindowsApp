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
            if (groupName != null && groupName.Trim().Length > 0)
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

        // ************ Nutzer/Teilnehmer bezogene Requests. **********************************

        /// <summary>
        /// Sende einen Request zum Abfragen aller Teilnehmer der Gruppe mit der angegebenen Id zum Server.
        /// Für die Ausfürhung des Requests muss der Nutzer Teilnehmer der Gruppe sein.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="withCaching">Gibt an, ob Caching für die Abfrage zugelassen werden soll.</param>
        /// <returns>Die Antwort des Server als String. Hierbei handelt es sich um die abgerufene Teilnehmer in
        ///     Form von Nutzer-Ressourcen. </returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetParticipantsRequest(string serverAccessToken, int groupId, bool withCaching)
        {
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/user",
                null,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Beitreten zur Gruppe mit der angegebenen Id. Für das Beitreten zur Gruppe
        /// ist ein Passwort erforderlich. Das Passwort wird kodiert im Body des Requests übertragen.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, in die der Nutzer eintreten möchte.</param>
        /// <param name="jsonContent">Der Inhalt des Requests. In diesem Fall wird das Passwort in Form
        ///     eines JSON Dokuments an den Server übermittelt.</param>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task SendJoinGroupRequest(string serverAccessToken, int groupId, string jsonContent)
        {
            await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/group/" + groupId + "/user",
                null);
        }

        /// <summary>
        /// Sende einen Request, um den Teilnehmer mit der angegebnen Id von der Gruppe zu entfernen.
        /// Man kann als Nutzer selbst aus der Gruppe austreten, wenn man Teilnehmer der Gruppe ist. Ist man
        /// Gruppenadministrator, so kann man auch andere Teilnehmer von der Gruppe entfernen.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="participantId">Die Id des Teilnehmers, der entfernt werden soll.</param>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task SendLeaveGroupRequest(string serverAccessToken, int groupId, int participantId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/group/" + groupId + "/user/" + participantId);
        }

        // **************** Requests bezüglich Konversationen *****************************************************

        /// <summary>
        /// Sende einen Request zum Anlegen einer neuen Konversation an den Server.
        /// Die Konversation wird für die Gruppe angelegt, die durch die angegebene Id identifiziert wird.
        /// Der Ersteller der Konversation wird automatisch als Konversationsadministrator für diese Konversation festgelegt.
        /// Der Anfrager muss Teilnehmer der angegebenen Gruppe sein.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, für die eine neue Konversation angelegt wird.</param>
        /// <param name="jsonContent">Die Daten der neuen Konversation in Form eines JSON-Dokuments.</param>
        /// <returns>Die Antwort des Servers als String. Hier die neu angelegte Konversations-Ressource.</returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendCreateConversationRequest(string serverAccessToken, int groupId, string jsonContent)
        {
            string serverResponse = await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/group/" + groupId.ToString() + "/conversation",
                null);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Abfragen aller Konversationen der Gruppe mit der angegebenen Id.
        /// Der Anfrager muss Teilnehmer der Gruppe sein, um die Abfrage durchführen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, für die die Konversationen abgefragt werden sollen.</param>
        /// <param name="subresources">Gibt an, ob zu den Konversationsdaten zusätzlich noch die Daten der 
        ///     Subresourcen abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob für diesen Request Caching erlaubt werden soll.</param>
        /// <returns>Die Antwort des Servers als String. In diesem Fall eine Auflistung von Kondersations-Ressourcen.</returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetConversationsRequest(string serverAccessToken, int groupId, bool? subresources, bool withCaching)
        {
            Dictionary<string, string> urlParams = new Dictionary<string, string>();
            if (subresources.HasValue)
            {
                urlParams.Add("subresources", subresources.Value.ToString().ToLowerInvariant());
            }

            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/conversation",
                urlParams,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Abfragen einer spezifischen Konversations-Ressource. Es wird
        /// die Konversations-Ressource abgefragt, die zu der spezifizierten Gruppe gehört und
        /// die durch die angegebene Id repräsentiert ist.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation.</param>
        /// <param name="withCaching">Gibt an, ob für diesen Request Caching erlaubt werden soll.</param>
        /// <returns>Die Antwort des Servers als String. Hier die abgefragte Konversations-Ressource.</returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetConversationRequest(string serverAccessToken, int groupId, int conversationId, bool withCaching)
        {
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/conversation/" + conversationId.ToString(),
                null,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sendet einen Request zum Ändern einer Konversations-Ressource. Es wird eine Beschreibung
        /// an durchzuführenden Änderungen an den Server in Form eines JSON-Merge-PATCH Dokuments geschickt.
        /// Der Anfrager muss der Administrator der Gruppe sein, um diese Anfrage ausführen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation.</param>
        /// <param name="jsonContent">Die Beschreibung der durchzuführenden Änderungen in Form eines
        ///     Json-Merge PATCH Dokuments.</param>
        /// <returns>Die Antwort des Servers als String. Hier eine aktualisierte Version der Konversations-Ressource.</returns>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendUpdateConversationRequest(string serverAccessToken, int groupId, int conversationId, string jsonContent)
        {
            string serverResponse = await base.SendHttpPatchRequestWithJsonBody(
                serverAccessToken,
                jsonContent,
                "/group/" + groupId.ToString() + "/conversation/" + conversationId.ToString(),
                null);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Löschen der Konversation, die durch die angegebene Id eindeutig
        /// identifiziert wird.
        /// Der Anfrager muss Gruppenadministrator der Gruppe sein, um diese Anfrage ausführen zu 
        /// können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation.</param>
        /// <exception cref="APIException">Wenn der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task SendDeleteConversationRequest(string serverAccessToken, int groupId, int conversationId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/conversation/" + conversationId.ToString());
        }
    }
}
