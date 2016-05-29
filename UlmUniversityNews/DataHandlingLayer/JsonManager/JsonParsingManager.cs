using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.JsonManager
{
    public class JsonParsingManager
    {
        /// <summary>
        /// Erzeugt eine Instanz der Klasse JsonParsingManager.
        /// </summary>
        public JsonParsingManager()
        {

        }

        #region User
        /// <summary>
        /// Erzeugt ein JSON-Dokument aus dem übergebenen User-Objekt.
        /// </summary>
        /// <param name="user">Das zu serialisierende User Objekt.</param>
        /// <returns>Ein generiertes JSON-Dokument mit den Nutzerdaten, oder null falls Serialisierung fehlschlägt.</returns>
        public string ParseUserToJsonString(User user)
        {
            string jsonContent = null;
            try
            {
                jsonContent = JsonConvert.SerializeObject(user);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("JsonParsingManager: Error during serialization of user object. " +
                    "Exception is: " + ex.Message);
            }
            return jsonContent;
        }

        /// <summary>
        /// Erzeugt ein User Objekt aus dem übergebenen JSON String. Ist eine
        /// Umwandlung des JSON Strings nicht möglich, so wird null zurück gegeben.
        /// </summary>
        /// <param name="jsonString">Der JSON String, der in ein User Objekt umgewandelt werden soll.</param>
        /// <returns>Eine Instanz der Klasse User, oder null falls Deserialisierung fehlschlägt.</returns>
        public User ParseUserFromJson(string jsonString)
        {
            User localUser = null;
            try
            {
                localUser = JsonConvert.DeserializeObject<User>(jsonString);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("JsonParsingManager: Error during deserialization of user object. " +
                    "Exception is: " + ex.Message);
            }
            return localUser;
        }
        #endregion User

        #region Moderator
        /// <summary>
        /// Erzeugt ein Moderator Objekt aus dem übergebenen JSON-Dokument. Ist eine
        /// Umwandlung des JSON Strings nicht möglich, so wird null zurückgegeben.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument, der in ein Moderator Objekt umgewandelt werden soll.</param>
        /// <returns>Eine Instanz der Klasse Moderator, oder null falls Deserialisierung fehlschlägt.</returns>
        public Moderator ParseModeratorFromJson(string jsonString)
        {
            Moderator moderator = null;
            try
            {
                moderator = JsonConvert.DeserializeObject<Moderator>(jsonString);
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("JsonParsingManager: Error during deserialization of moderator. " +
                    "Exception message is: " + jsonEx.Message);
            }
            return moderator;
        }

        /// <summary>
        /// Extrahiert eine Liste von Moderatorenobjekten aus dem übergebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument mit den Daten.</param>
        /// <returns>Eine Liste von Moderatoren-objekten, oder null falls die Deserialisierung fehlschlägt.</returns>
        public List<Moderator> ParseModeratorListFromJson(string jsonString)
        {
            List<Moderator> moderators = null;
            try
            {
                moderators = JsonConvert.DeserializeObject<List<Moderator>>(jsonString);
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("JsonParsingManager: Error during deserialization of moderator list. " +
                    "Exception message is: " + jsonEx.Message);
            }
            return moderators;
        }

        /// <summary>
        /// Wandelt ein Objekt vom Typ Moderator in eine JSON Repräsentation um.
        /// </summary>
        /// <param name="moderator">Das umzuwandelnde Objekt.</param>
        /// <returns>Ein JSON-Dokument, welches das übergebene Objekt repräsentiert. Null falls Umwandlung fehlschlägt.</returns>
        public string ParseModeratorToJson(Moderator moderator)
        {
            string jsonContent = null;
            try
            {
                jsonContent = JsonConvert.SerializeObject(moderator);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("JsonParsingManager: Exception occurred during json parsing of moderator. " +
                    "Msg is {0}.", ex.Message);
            }
            return jsonContent;
        }
        #endregion Moderator

        #region Channel
        /// <summary>
        /// Erstellt ein JSON Dokument aus einem Channel Objekt.
        /// </summary>
        /// <param name="channel">Das Objekt, das umgewandelt werden soll.</param>
        /// <returns>JSON-Dokument des Objekts, oder null, falls Serialisierung fehlgeschlagen ist.</returns>
        public string ParseChannelToJsonString(Channel channel)
        {
            if (channel == null)
                return null;

            string jsonContent = null;
            try
            {
                switch (channel.Type)
                {
                    case ChannelType.LECTURE:
                        Lecture lecture = channel as Lecture;
                        jsonContent = JsonConvert.SerializeObject(lecture);
                        break;
                    case ChannelType.EVENT:
                        Event eventObj = channel as Event;
                        jsonContent = JsonConvert.SerializeObject(eventObj);
                        break;
                    case ChannelType.SPORTS:
                        Sports sportObj = channel as Sports;
                        jsonContent = JsonConvert.SerializeObject(sportObj);
                        break;
                    default:
                        jsonContent = JsonConvert.SerializeObject(channel);
                        break;
                }
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("JsonParsingManager: Exception during serialization of an channel object.");
                Debug.WriteLine("JsonParsingManager: Message is: {0}.", jsonEx.Message);
            }

            return jsonContent;
        }

        /// <summary>
        /// Extrahiert ein Objekt vom Typ Channel aus dem übergebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument.</param>
        /// <returns>Eine Instanz der Klasse Channel, oder null, falls die Deserialisierung fehlschlägt.</returns>
        public Channel ParseChannelFromJson(string jsonString)
        {
            Channel extractedChannel = null;
            try
            {
                var token = Newtonsoft.Json.Linq.JToken.Parse(jsonString);
                if (token.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                {
                    // Frage den Wert des Attributs "type" ab.
                    string typeValue = token.Value<string>("type");

                    ChannelType type;
                    if (Enum.TryParse(typeValue.ToString(), false, out type))
                    {
                        // Parse Objekt abhängig vom Typ des Kanals.
                        switch (type)
                        {
                            case ChannelType.LECTURE:
                                Lecture lecture = JsonConvert.DeserializeObject<Lecture>(jsonString);
                                extractedChannel = lecture;
                                break;
                            case ChannelType.EVENT:
                                Event eventObj = JsonConvert.DeserializeObject<Event>(jsonString);
                                extractedChannel = eventObj;
                                break;
                            case ChannelType.SPORTS:
                                Sports sportsObj = JsonConvert.DeserializeObject<Sports>(jsonString);
                                extractedChannel = sportsObj;
                                break;
                            default:
                                // Für Student-Group und Other gibt es keine eigene Klasse.
                                extractedChannel = JsonConvert.DeserializeObject<Channel>(jsonString);
                                break;
                        }
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("JsonParsingManager: Could not extract Announcement object from json string.");
                Debug.WriteLine("JsonParsingManager: Message is: {0}.", jsonEx.Message);
            }

            return extractedChannel;
        }

        /// <summary>
        /// Erzeugt eine Liste von Objekten vom Typ Kanal aus dem übergebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument.</param>
        /// <returns>Liste von Kanal-Objekten.</returns>
        public List<Channel> ParseChannelListFromJson(string jsonString)
        {
            List<Channel> channels = new List<Channel>();
            try
            {
                // Parse JSON List in eine JArray Repräsentation. JArray repräsentiert ein JSON Array. 
                Newtonsoft.Json.Linq.JArray jsonArray = Newtonsoft.Json.Linq.JArray.Parse(jsonString);
                foreach (var item in jsonArray)
                {
                    if (item.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                    {
                        // Frage den Wert des Attributs "type" ab.
                        string typeValue = item.Value<string>("type");

                        ChannelType type;
                        if (Enum.TryParse(typeValue.ToString(), false, out type))
                        {
                            // Führe weiteres Parsen abhängig von dem Typ des Kanals durch.
                            switch (type)
                            {
                                case ChannelType.LECTURE:
                                    Lecture lecture = JsonConvert.DeserializeObject<Lecture>(item.ToString());
                                    channels.Add(lecture);
                                    break;
                                case ChannelType.EVENT:
                                    Event eventObj = JsonConvert.DeserializeObject<Event>(item.ToString());
                                    channels.Add(eventObj);
                                    break;
                                case ChannelType.SPORTS:
                                    Sports sportsObj = JsonConvert.DeserializeObject<Sports>(item.ToString());
                                    channels.Add(sportsObj);
                                    break;
                                default:
                                    // Für Student-Group und Other gibt es keine eigene Klasse.
                                    Channel channel = JsonConvert.DeserializeObject<Channel>(item.ToString());
                                    channels.Add(channel);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("JsonParsingManager: Error during deserialization. Exception is: " + ex.Message);
            }

            return channels;
        }
        #endregion Channel

        #region Announcement
        /// <summary>
        /// Erstellt ein JSON Dokument aus einem Announcement Objekt.
        /// </summary>
        /// <param name="announcement">Das Objekt, das umgewandelt werden soll.</param>
        /// <returns>JSON-Dokument des Objekts, oder null, falls Serialisierung fehlgeschlagen ist.</returns>
        public string ParseAnnouncementToJsonString(Announcement announcement)
        {
            string jsonContent = null;
            try
            {
                jsonContent = JsonConvert.SerializeObject(announcement);
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("JsonParsingManager: Exception during serialization of an announcement object.");
                Debug.WriteLine("JsonParsingManager: Message is: {0}.", jsonEx.Message);
            }
            return jsonContent;
        }

        /// <summary>
        /// Extrahiert ein Announcement Objekt aus einem JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument.</param>
        /// <returns>Eine Instanz von Announcement, oder null, falls die Deserialisierung fehlschlägt.</returns>
        public Announcement ParseAnnouncementFromJsonString(string jsonString)
        {
            Announcement announcement = null;
            try
            {
                announcement = JsonConvert.DeserializeObject<Announcement>(jsonString);
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("JsonParsingManager: Could not extract Announcement object from json string.");
                Debug.WriteLine("JsonParsingManager: Message is: {0}.", jsonEx.Message);
            }
            return announcement;
        }

        /// <summary>
        /// Extrahiere eine Liste von Announcement Objekten aus einem gegebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument.</param>
        /// <returns>Liste von Announcement-Objekten, oder null falls Deserialisierung fehlschlägt.</returns>
        public List<Announcement> ParseAnnouncementListFromJson(string jsonString)
        {
            List<Announcement> announcements = null;
            try
            {
                announcements = JsonConvert.DeserializeObject<List<Announcement>>(jsonString);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("JsonParsingManager: Error during deserialization. Exception is: " + ex.Message);
            }
            return announcements;
        }
        #endregion Announcement

        #region Reminder
        /// <summary>
        /// Extrahiert ein Objekt vom Typ Reminder aus dem übergebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument.</param>
        /// <returns>Eine Instanz der Klasse Reminder, oder null, falls die Deserialisierung fehlschlägt.</returns>
        public Reminder ParseReminderFromJson(string jsonString)
        {
            Reminder reminder = null;
            try
            {
                reminder = JsonConvert.DeserializeObject<Reminder>(jsonString);
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("JsonParsingManager: Could not extract reminder object from json string.");
                Debug.WriteLine("JsonParsingManager: Message is: {0}.", jsonEx.Message);
            }

            return reminder;
        }

        /// <summary>
        /// Extrahiert eine Liste von Reminder Objekten aus dem übergebenen JSON Dokument.
        /// </summary>
        /// <param name="jsonString">Das übergebene JSON-Dokument.</param>
        /// <returns>Liste von Reminder Objekten, oder null, wenn Deserialisierung fehlschlägt.</returns>
        public List<Reminder> ParseReminderListFromJson(string jsonString)
        {
            List<Reminder> reminders = null;
            try
            {
                reminders = JsonConvert.DeserializeObject<List<Reminder>>(jsonString);
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("JsonParsingManager: Could not extract list of reminders from json string.");
                Debug.WriteLine("JsonParsingManager: Message is: {0}.", jsonEx.Message);
            }
            return reminders;
        }

        /// <summary>
        /// Serialisiert ein Objekt vom Typ Reminder in ein JSON-Dokument.
        /// </summary>
        /// <param name="reminder">Das zu serialisierende Objekt.</param>
        /// <returns>Das Objekt in Form eines JSON-Dokument, oder null, wenn die Umwandlung fehlgeschlagen ist.</returns>
        public string ParseReminderToJson(Reminder reminder)
        {
            if (reminder == null)
                return null;

            string jsonContent = null;
            try
            {
                //// Spezielle Serialisierungssettings für Datum.
                //JsonSerializerSettings utcDateSettings = new JsonSerializerSettings();
                //utcDateSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

                //Newtonsoft.Json.Converters.IsoDateTimeConverter dateConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter
                //{
                //    DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzz"
                //};
                //utcDateSettings.Converters.Add(dateConverter);

                jsonContent = JsonConvert.SerializeObject(reminder);

                // Anpassung Json Content zwecks Datumsformat.
                string finalJson = string.Empty;
                string[] parts = jsonContent.Split(new string[] { "," }, StringSplitOptions.None);
                foreach (string part in parts)
                {
                    if (part.Contains("startDate") || part.Contains("endDate"))
                    {
                        string[] splittedDate = part.Split(new string[] { ":" }, StringSplitOptions.None);

                        // Füge Teil 1 wieder an, ebenso wie den Doppelpunkt.
                        finalJson += splittedDate[0] + ":";

                        string date = string.Empty;
                        if (splittedDate[0].Contains("\"startDate\""))
                        {
                            // Parse Datum mit eigener Parse Methode.
                            date = "\"" + parseDateTimeToISO8601Format(reminder.StartDate) + "\"";
                        }
                        else if (splittedDate[0].Contains("\"endDate\""))
                        {
                            // Parse Datum mit eigener Parse Methode.
                            date = "\"" + parseDateTimeToISO8601Format(reminder.EndDate) + "\"";
                        }

                        // Füge Datum an.
                        finalJson += date;

                        //finalJson += part.Substring(0, finalJson.Length - 1) +  ".000Z" + part.Substring(finalJson.Length - 1, 1);        // Füge Millisekunden Bereich an.
                    }
                    else
                    {
                        finalJson += part;
                    }
                    finalJson += ",";
                }
                finalJson = finalJson.Substring(0, finalJson.Length - 1);

                Debug.WriteLine("JsonParsingManager: Final json is: {0}", finalJson);

                jsonContent = finalJson;
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("JsonParsingManager: Could not serialize reminder to json.");
                Debug.WriteLine("JsonParsingManager: Message is: {0}.", jsonEx.Message);
            }

            return jsonContent;
        }
        #endregion Reminder

        #region PushMessage
        /// <summary>
        /// Extrahiert ein PushMessage Objekten aus dem übergebenen JSON Dokument.
        /// </summary>
        /// <param name="jsonString">Das übergebene JSON-Dokument.</param>
        /// <returns>Eine Instanz der Klasse PushMessage, oder null, wenn Deserialisierung fehlschlägt.</returns>
        public PushMessage ParsePushMessageFromJson(string jsonString)
        {
            PushMessage pushMessage = null;
            try
            {
                pushMessage = JsonConvert.DeserializeObject<PushMessage>(jsonString);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("ParsePushMessageFromJson: Json parser error occurred. " +
                    "Message is {0}.", ex.Message);
            }
            return pushMessage;
        }
        #endregion PushMessage

        #region Group
        /// <summary>
        /// Wandelt ein Objekt der Klasse Group in ein entsprechendes JSON Dokument um.
        /// </summary>
        /// <param name="group">Das umzuwandelnde Group Objekt.</param>
        /// <returns>Ein JSON Dokument in Form eines Strings. Oder null, wenn die Umwandlung fehlgeschlagen hat.</returns>
        public string ParseGroupToJson(Group group)
        {
            string groupJson = null;
            try
            {
                groupJson = JsonConvert.SerializeObject(group);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("ParseGroupToJson: Json parser error occurred. " +
                    "Message is {0}.", ex.Message);
            }

            return groupJson;
        }

        /// <summary>
        /// Extrahiert eine Instanz der Klasse Group aus dem übergebenen JSON Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON Dokument.</param>
        /// <returns>Eine Instanz der Klasse Group, oder null, falls das Umwandeln fehlgeschlagen ist.</returns>
        public Group ParseGroupFromJson(string jsonString)
        {
            Group parsedGroup = null;
            try
            {
                parsedGroup = JsonConvert.DeserializeObject<Group>(jsonString);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("ParseGroupFromJson: Json parser error occurred. " +
                    "Message is {0}.", ex.Message);
            }

            return parsedGroup;
        }

        /// <summary>
        /// Extrahiert eine Liste von Gruppen-Ressourcen aus dem übergebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON Dokument.</param>
        /// <returns>Eine Liste von Group Objekten, oder null, falls das Umwandeln fehlgeschlagen ist.</returns>
        public List<Group> ParseGroupListFromJson(string jsonString)
        {
            List<Group> groupList = null;
            try
            {
                groupList = JsonConvert.DeserializeObject<List<Group>>(jsonString);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("ParseGroupListFromJson: Json parser error occurred. " +
                    "Message is {0}.", ex.Message);
            }

            return groupList;
        }
        #endregion Group

        ///// <summary>
        ///// Eine Hilfsmethode, die ein DateTimeOffset Objekt in das Format der koordinierten Weltzeit umwandelt und
        ///// als String zurückliefert. Diese Methode kann verwendet werden, um DateTimeOffset Objekte in ein
        ///// Format zu bringen, welches vom Server verstanden wird. 
        ///// </summary>
        ///// <param name="datetime">Das umzuwandelnde DateTimeOffset Objekt.</param>
        ///// <returns>Die Datums- und Uhrzeitangabe im UTC Format.</returns>
        //private string parseDateTimeToISO8601Format(DateTimeOffset datetime)
        //{
        //    TimeSpan currentUTCOffset = TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now);
        //    string datetimeString = datetime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff"); ;

        //    currentUTCOffset.ToString();
        //    if (currentUTCOffset.TotalHours == 2.0f)
        //    {
        //        datetimeString += "+0200";
        //    }
        //    else if (currentUTCOffset.TotalHours == 1.0f)
        //    {
        //        datetimeString += "+0100";
        //    }

        //    return datetimeString;
        //}

        /// <summary>
        /// Eine Hilfsmethode, die ein DateTimeOffset Objekt in das Format der koordinierten Weltzeit umwandelt und
        /// als String zurückliefert. Diese Methode kann verwendet werden, um DateTimeOffset Objekte in ein
        /// Format zu bringen, welches vom Server verstanden wird. 
        /// </summary>
        /// <param name="datetime">Das umzuwandelnde DateTimeOffset Objekt.</param>
        /// <returns>Die Datums- und Uhrzeitangabe im UTC Format.</returns>
        private string parseDateTimeToISO8601Format(DateTimeOffset datetime)
        {
            System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo("de-DE");

            string format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzzz";
            string datetimeString = datetime.ToString(format, cultureInfo);

            // Problem ist ':' in Zeitzone.
            datetimeString = datetimeString.Substring(0, datetimeString.Length - 3) + datetimeString.Substring(datetimeString.Length - 2, 2);

            return datetimeString;
        }
    }
}
