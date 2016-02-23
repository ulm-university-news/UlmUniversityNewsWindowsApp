using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Exceptions;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataHandlingLayer.Database
{
    public class ApplicationSettingsDatabaseManager
    {

        /// <summary>
        /// Lädt die aktuell gültigen Anwendungseinstellungen aus der Datenbank und gibt sie 
        /// in einem Objekt gekapselt zurück.
        /// </summary>
        /// <returns>Eine Instanz von AppSetting.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Laden der Anwendungseinstellungen fehlschlägt.</exception>
        public AppSettings LoadApplicationSettings()
        {
            AppSettings appSettings = null;
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"SELECT * 
                                       FROM Settings
                                       WHERE Id=?;";
                        using(var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, 0);        // Settings unter Id 0 abgespeichert.

                            if(stmt.Step() == SQLiteResult.ROW)
                            {
                                OrderOption channelSetting = (OrderOption) Enum.ToObject(typeof(OrderOption), stmt["ChannelSettings_OrderOptions_OrderId"]);
                                OrderOption conversationSetting = (OrderOption)Enum.ToObject(typeof(OrderOption), stmt["ConversationSettings_OrderOptions_OrderId"]);
                                OrderOption groupSetting = (OrderOption)Enum.ToObject(typeof(OrderOption), stmt["GroupSettings_OrderOptions_OrderId"]);
                                OrderOption ballotSetting = (OrderOption)Enum.ToObject(typeof(OrderOption), stmt["BallotSettings_OrderOptions_OrderId"]);
                                OrderOption announcementSetting = (OrderOption)Enum.ToObject(typeof(OrderOption), stmt["AnnouncementSettings_OrderOptions_OrderId"]);
                                OrderOption generalListOrderSetting = (OrderOption)Enum.ToObject(typeof(OrderOption), stmt["GeneralListOrder_OrderOptions_OrderId"]);
                                Language languageSetting = (Language)Enum.ToObject(typeof(Language), stmt["LanguageSettings_LanguageId"]);
                                NotificationSetting notificationSetting = (NotificationSetting)Enum.ToObject(typeof(NotificationSetting), stmt["NotificationSettings_NotifierId"]);

                                appSettings = new AppSettings()
                                {
                                    ChannelOderSetting = channelSetting,
                                    ConversationOrderSetting = conversationSetting,
                                    GroupOrderSetting = groupSetting,
                                    BallotOrderSetting = ballotSetting,
                                    AnnouncementOrderSetting = announcementSetting,
                                    GeneralListOrderSetting = generalListOrderSetting,
                                    LanguageSetting = languageSetting,
                                    MsgNotificationSetting = notificationSetting
                                };
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        // Hier keine Abbildung auf DatabaseException.
                        Debug.WriteLine("SQLiteException has occurred in LoadApplicationSettings. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Failed to load application settings.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in LoadApplicationSettings. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Failed to load application settings.");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
            }

            return appSettings;
        }

        /// <summary>
        /// Aktualisiere die Anwendungseinstellungen in der Datenbank.
        /// Die aktualisierten Daten werden gekapselt in einem AppSettings Objekt übergeben.
        /// </summary>
        /// <param name="updatedSettings">Das AppSettings Objekt mit den aktualisierten Anwendungsdaten.</param>
        public void UpdateApplicationSettings(AppSettings updatedSettings)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try{
                        string sql = @"UPDATE Settings 
                                       SET  ChannelSettings_OrderOptions_OrderId=?, ConversationSettings_OrderOptions_OrderId=?,
                                            GroupSettings_OrderOptions_OrderId=?, BallotSettings_OrderOptions_OrderId=?,
                                            AnnouncementSettings_OrderOptions_OrderId=?, GeneralListOrder_OrderOptions_OrderId=?,
                                            LanguageSettings_LanguageId=?, NotificationSettings_NotifierId=?
                                        WHERE Id=0;";
                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, (int)updatedSettings.ChannelOderSetting);
                            stmt.Bind(2, (int)updatedSettings.ConversationOrderSetting);
                            stmt.Bind(3, (int)updatedSettings.GroupOrderSetting);
                            stmt.Bind(4, (int)updatedSettings.BallotOrderSetting);
                            stmt.Bind(5, (int)updatedSettings.AnnouncementOrderSetting);
                            stmt.Bind(6, (int)updatedSettings.GeneralListOrderSetting);
                            stmt.Bind(7, (int)updatedSettings.LanguageSetting);
                            stmt.Bind(8, (int)updatedSettings.MsgNotificationSetting);

                            stmt.Step();

                            Debug.WriteLine("Performed update on Settings table.");
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        // Hier keine Abbildung auf DatabaseException.
                        Debug.WriteLine("SQLiteException has occurred in UpdateApplicationSettings. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Failed to update application settings.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in UpdateApplicationSettings. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Failed to update application settings.");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
            }

        }

        /// <summary>
        /// Liefert die eingestellte bevorzugte Sprache des lokalen Nutzers zurück.
        /// </summary>
        /// <returns>Die bevorzugte Sprache des lokalen Nutzers.</returns>
        public Language GetFavoredLanguage()
        {
            Language favoredLanguage = Language.ENGLISH;
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT Language 
                            FROM Settings AS s JOIN LanguageSettings AS l ON s.LanguageSettings_Id=l.Id;";
                        using (var stmt = conn.Prepare(query))
                        {
                            if(stmt.Step() == SQLiteResult.ROW)
                            {
                                favoredLanguage = (Language)Enum.ToObject(typeof(Language), stmt["Language"]);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        // Hier keine Abbildung auf DatabaseException.
                        Debug.WriteLine("SQLiteException has occurred in GetFavoredLanguage. The message is: {0}.", sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in GetFavoredLanguage. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
            }

            return favoredLanguage;
        }
    }
}
