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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
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
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task SendDeleteConversationRequest(string serverAccessToken, int groupId, int conversationId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/conversation/" + conversationId.ToString());
        }

        // ******************* Requests bezüglich Konversationsnachrichten *****************************************

        /// <summary>
        /// Sende einen Request zum Anlegen einer neuen Nachricht in der angegebenen Konversation an den Server.
        /// Die Nachrichten einer Konversation werden an alle Teilnehmer der Gruppe verteilt. Der Anfrager muss
        /// Teilnehmer der Gruppe sein, um den Request absetzen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="conversationId">Die Id der Konversation.</param>
        /// <param name="jsonContent">Die Daten der neuen Nachricht in Form eines JSON-Dokuments.</param>
        /// <returns>Die Antwort des Servers in Form eines Strings. Hier die erzeugte Konversationsnachricht.</returns>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendCreateConversationMessageRequest(string serverAccessToken, int groupId,
            int conversationId, string jsonContent)
        {
            string serverResponse = await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/group/" + groupId.ToString() + "/conversation/" + conversationId.ToString() + "/message",
                null);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Abfragen aller Nachrichten, die zu der spezifizierten Konversation
        /// gehören, an den Server. Die Abfrage kann durch die Nachrichtennummer beschränkt werden.
        /// Der Anfrager muss Teilnehmer der Gruppe sein, um die Anfrage ausführen
        /// zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation, zu der die Nachrichten abgefragt werden sollen.</param>
        /// <param name="messageNr">Die Nachrichtennummer, ab der die Nachrichten abgefragt werden sollen. Dadurch kann die 
        ///     Abfrage eingeschränkt werden. Es werden nur Nachrichten abgerufen, die eine größere Nachrichtennummer als die angegebene
        ///     besitzen.</param>
        /// <param name="withCaching">Gibt an, ob Caching für diesen Request zugelassen sein soll.</param>
        /// <returns>Die Antwort des Servers als String. Hier eine Menge von Konversationsnachrichten.</returns>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetConversationMessagesRequest(string serverAccessToken, int groupId,
            int conversationId, int messageNr, bool withCaching)
        {
            Dictionary<string, string> urlParams = new Dictionary<string, string>();
            urlParams.Add("messageNr", messageNr.ToString());

            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/conversation/" + conversationId.ToString() + "/message",
                urlParams,
                withCaching);

            return serverResponse;
        }

        // **************************** Requests bezüglich Abstimmungen **********************************************

        /// <summary>
        /// Sende einen Request zum Anlegen einer neuen Abstimmung. Die übergebenen Daten im 
        /// JSON Format werden an den Server übermittelt. Um diese Aktion ausführen zu können, 
        /// muss man in einer Gruppe vom Typ Arbeitsgruppe ein Teilnehmer sein, in einer Gruppe vom Typ
        /// Tutoriumsgruppe muss man sogar Administrator (Tutor) dieser Gruppe sein.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="jsonContent">Die Daten der neu anzulegenden Abstimmung in Form eines JSON Dokuments.</param>
        /// <returns>Die Antwort des Servers als String. Hier die erzeugte Abstimmungsressource.</returns>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendCreateBallotRequest(string serverAccessToken, int groupId, string jsonContent)
        {
            string serverResponse = await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/group/" + groupId.ToString() + "/ballot",
                null);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Abfragen von Abstimmungen, die einer bestimmten Gruppe zugeordnet sind.
        /// Es kann angegeben werden, ob die Abstimmungen inklusive ihrer Subresourcen (Options und Votes)
        /// abgefragt werden sollen. Der Anfrager muss Teilnehmer der Gruppe sein, um diese Anfrage durchführen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmungen abgefragt werden sollen.</param>
        /// <param name="subresources">Gibt an, ob die Subresourcen mit abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob Caching bei diesem Request möglich sein soll.</param>
        /// <returns>Die Antwort des Servers als String. Hier die übermittelten Abstimmungsresourcen in einer Liste.</returns>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetBallotsRequest(string serverAccessToken, int groupId, bool subresources, bool withCaching)
        {
            Dictionary<string, string> urlParams = new Dictionary<string, string>();
            urlParams.Add("subresources", subresources.ToString());

            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/ballot",
                urlParams,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Abfragen einer spezifischen Abstimmung einer Gruppe.
        /// Es kann angegeben werden, ob die Abstimmung inklusive ihrer Subresourcen (Options und Votes) abgefragt werden soll.
        /// Der Anfrager muss Teilnehmer der Gruppe sein, um diese Anfrage durchführen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, die abgefragt werden soll.</param>
        /// <param name="subresources">Gibt an, ob die Subressourcen (Options und Votes) ebenfalls abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob Caching bei diesem Request zugelassen werden soll.</param>
        /// <returns>Die Antwort des Servers als String. Hier die abgefragte Abstimmungsressource.</returns>
        /// <exception cref="APIException">Wirft APIException, falls Request fehlschlägt, oder vom Server abgelehnt wird.</exception>
        public async Task<string> SendGetBallotRequest(string serverAccessToken, int groupId, int ballotId, bool subresources, bool withCaching)
        {
            Dictionary<string, string> urlParams = new Dictionary<string, string>();
            urlParams.Add("subresources", subresources.ToString());

            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString(),
                urlParams,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Aktualisieren einer spezifischen Abstimmung. Die durchzuführenden Änderungen 
        /// werden in Form einer Beschreibung im JSON Merge Patch Format an den Server geschickt. Der Anfrager muss 
        /// Administrator der angegebenen Abstimmung sein, um diesen Request durchführen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der diese Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, die geändert werden soll.</param>
        /// <param name="jsonContent">Die Beschreibung der durchzuführenden Änderungen als JSON Merge Patch Dokument.</param>
        /// <returns>Die Antwort des Servers als String. Hier die aktualisierte Abstimmungsressource.</returns>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendUpdateBallotRequest(string serverAccessToken, int groupId, int ballotId, string jsonContent)
        {
            string serverResponse = await base.SendHttpPatchRequestWithJsonBody(
                serverAccessToken,
                jsonContent,
                "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString(),
                null);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Löschen einer spezifischen Abstimmung vom Server. 
        /// Der Anfrager muss Administrator dieser Abstimmung sein, um diese löschen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, die gelöscht werden soll.</param>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task SendDeleteBallotRequest(string serverAccessToken, int groupId, int ballotId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString());
        }

        // **************************** Requests bezüglich Abstimmungsoptionen **********************************************************

        /// <summary>
        /// Sende einen Request zum Anlegen einer neuen Abstimmungsoption für eine spezifische Abstimmung. 
        /// Die Daten der neuen Abstimmungsoption werden in Form eines JSON Dokuments an den Server übermittelt.
        /// Der Anfrager muss Administrator der betroffenen Abstimmung sein, um eine Option hinzufügen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, zu der eine neue Abstimmungsoption hinzugefügt werden sollen.</param>
        /// <param name="jsonContent">Die Daten der neuen Abstimmungsoption in Form eines JSON Dokuments.</param>
        /// <returns>Die Antwort des Servers als String. Hier die erzeugte Optionressource.</returns>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendCreateOptionRequest(string serverAccessToken, int groupId, int ballotId, string jsonContent)
        {
            string serverResponse = await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString() + "/option",
                null);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Abfragen aller Abstimmungsoptionen einer bestimmten Abstimmung. Die Abfrage kann
        /// zusätzlich die Subresourcen der Abstimmungsoptionen umfassen (Votes). Der Anfrager muss Teilnehmer der 
        /// Gruppe sein, um diese Abfrage durchführen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoptionen abgefragt werden sollen.</param>
        /// <param name="subresources">Gibt an, ob die Subresourcen (Votes) ebenfalls abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob Caching bei dieser Anfrage zugelassen werden soll.</param>
        /// <returns>Die Antwort des Servers als String. Hier die Liste von Abstimmungsoptionen.</returns>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetOptionsRequest(string serverAccessToken, int groupId, int ballotId, bool subresources, bool withCaching)
        {
            Dictionary<string, string> urlParams = new Dictionary<string, string>();
            urlParams.Add("subresources", subresources.ToString());

            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString() + "/option",
                urlParams,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zur Abfrage einer speziellen Abstimmungsoption einer bestimmten Abstimmung. Der Anfrager
        /// muss Teilnehmer der Gruppe sein, um diese Abfrage durchführen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoption gehört.</param>
        /// <param name="optionId">Die Id der Abstimmungsoption, die abgefragt werden soll.</param>
        /// <returns>Die Antwort des Servers als String. Hier die Abstimmungsoption.</returns>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetOptionRequest(string serverAccessToken, int groupId, int ballotId, int optionId)
        {
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString() + "/option/" + optionId.ToString(),
                null,
                false);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Löschen der angegebenen Abstimmungsoption von der spezifizierten Abstimmung. 
        /// Der Anfrager muss Abstimmungsadministrator der Abstimmung sein, um eine Abstimmungsoption entfernen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, von der die Abstimmungsoption entfernt werden soll.</param>
        /// <param name="optionId">Die Id der Abstimmungsoption, die entfernt werden soll.</param>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task SendDeleteOptionRequest(string serverAccessToken, int groupId, int ballotId, int optionId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString() + "/option/" + optionId.ToString());
        }

        /// <summary>
        /// Sende einen Request, um den Anfrager mit der spezifizierten Abstimmungsoption zu verknüpfen. Der Anfrager
        /// hat dann für diese Abstimmungsoption abgestimmt. Der Anfrager muss Teilnehmer der Gruppe sein, um 
        /// abstimmen zu können.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoption gehört.</param>
        /// <param name="optionId">die Id der Abstimmungsoption, für die der Nutzer abstimmen will.</param>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task SendVoteForOptionRequest(string serverAccessToken, int groupId, int ballotId, int optionId)
        {
            await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                null,
                "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString() + "/option/" + optionId.ToString() + "/user",
                null);
        }

        /// <summary>
        /// Sende einen Request zur Abfrager der Nutzer, die für eine bestimmte Abstimmungsoption einer Abstimmung abgestimmt haben.
        /// Die Anfrager liefert eine Liste von Nutzern zurück. Der Anfrager muss Teilnehmer der Gruppe sein.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoption gehört.</param>
        /// <param name="optionId">Die Id der Abstimmungsoption, zu der die Nutzer abgefragt werden sollen, die für diese Option abgestimmt haben.</param>
        /// <returns>Die Antwort des Servers als String. Hier eine Liste von Nutzer Ressourcen.</returns>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task<string> SendGetVotersRequest(string serverAccessToken, int groupId, int ballotId, int optionId, bool withCaching)
        {
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString() + "/option/" + optionId.ToString() + "/user",
                null,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Entfernen der Verknüpfung zwischen Nutzer und Abstimmungsergebnis. Der Nutzer hat dann nicht
        /// mehr für diese Abstimmungsoption abgestimmt. Der Anfrager kann jeweils nur die von ihm selbst platzierten Votes
        /// wieder entfernen. Der Anfrager muss zudem Teilnehmer der Gruppe sein.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Nutzers.</param>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoption gehört.</param>
        /// <param name="optionId">die Id der Abstimmungsoption, für die der Nutzer seine Wahl widerrufen will.</param>
        /// <exception cref="APIException">Falls der Request fehlschlägt, oder vom Server abgelehnt wurde.</exception>
        public async Task SendRemoveVoteRequest(string serverAccessToken, int groupId, int ballotId, int optionId, int userId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                 "/group/" + groupId.ToString() + "/ballot/" + ballotId.ToString() + "/option/" + optionId.ToString() + "/user/" + userId.ToString());
        }
    }
}
