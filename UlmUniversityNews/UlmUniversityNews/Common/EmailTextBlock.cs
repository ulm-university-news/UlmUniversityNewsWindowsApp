using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace UlmUniversityNews.Common
{
    public class EmailTextBlock
    {
        #region Constants
        /// <summary>
        /// Der Name der Text-Property des Email-TextBlock.
        /// </summary>
        private const string EmailTextPropertyName = "EmailText";
        #endregion Constants

        #region DependencyProperties
        /// <summary>
        /// DependencyProperty für die Texteigenschaft des Email-TextBlock.
        /// </summary>
        public static readonly DependencyProperty EmailTextProperty = 
            DependencyProperty.RegisterAttached(
            EmailTextPropertyName,
            typeof(string),
            typeof(EmailTextBlock),
            new PropertyMetadata(null, OnEmailTextPropertyChanged)
            );
        #endregion DependencyProperties

        #region EventHandler
        /// <summary>
        /// Wird aufgerufen, wenn sich das EmailText Property geändert hat.
        /// </summary>
        /// <param name="obj">Das zugehörige DependencyObject, hier normal ein TextBlock.</param>
        /// <param name="e">Die Eventparameter.</param>
        private static void OnEmailTextPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextBlock attachedTextBlock = obj as TextBlock;
            if (attachedTextBlock == null)
                return;

            string emailText = e.NewValue as string;
            attachedTextBlock.Inlines.Clear();

            if (emailText.Contains("@"))
            {
                string[] splittedString = emailText.Split(new string[] { " " }, StringSplitOptions.None);
                AddInlineControls(attachedTextBlock, splittedString);
            }
            else
            {
                // Schreibe Text in Run-Element.
                Run run = new Run();
                run.Text = emailText;
                attachedTextBlock.Inlines.Add(run);
            }
        }
        #endregion EventHandler

        #region AccessProperties
        /// <summary>
        /// Gibt den aktuellen Wert der DependencyProperty EmailText zurück.
        /// </summary>
        /// <param name="attachedTextBlock">Der TextBlock, der als Email-TextBlock dient.</param>
        /// <returns>Den Inhalt der Eigenschaft als String.</returns>
        public static string GetEmailText(TextBlock attachedTextBlock)
        {
            if (attachedTextBlock != null)
                return attachedTextBlock.GetValue(EmailTextProperty) as string;

            return string.Empty;
        }

        /// <summary>
        /// Setzt den Wert des DependencyProperty EmailText.
        /// </summary>
        /// <param name="attachedTextBlock">Der TextBlock, der als Email-TextBlock dient.</param>
        /// <param name="value">Der neue Wert.</param>
        public static void SetEmailText(TextBlock attachedTextBlock, string value)
        {
            if (attachedTextBlock != null)
            {
                attachedTextBlock.SetValue(EmailTextProperty, value);
            }
        }
        #endregion AccessProperties

        /// <summary>
        /// Fügt dem übergebenen TextBlock Element Inline Elemente hinzu, die den übergebenen Text wiederspiegeln.
        /// Hierbei werden valide Email-Adresse als Hyperlinks abgebildet.
        /// </summary>
        /// <param name="attachedTextBlock">Der TextBlock, dem die Inline Elemente hinzugefügt werden sollen.</param>
        /// <param name="splittedEmailText">Der Text, der nach einem Trennungskriterium in Einzelteile aufgesplittet wurde.
        ///     Jeder Teilstring wird als ein Inline Element abgebildet.</param>
        private static void AddInlineControls(TextBlock attachedTextBlock, string[] splittedEmailText)
        {
            // Gehe einzelne Segmente durch.
            foreach (string stringPart in splittedEmailText)
            {
                if (stringPart.Contains("@"))
                {
                    // Prüfe zunächst, ob es sich um eine valide Email-Adresse handelt.
                    bool isValid = isValidEmail(stringPart);
                    if (isValid)
                    {
                        Hyperlink hyperlinkEmail = new Hyperlink();
                        Run content = new Run();
                        content.Text = stringPart;
                        hyperlinkEmail.Inlines.Add(content);
                        hyperlinkEmail.Click += hyperlinkEmail_Click;

                        // Füge den Hyperlink hinzu.
                        attachedTextBlock.Inlines.Add(hyperlinkEmail);

                        // Füge noch Leerzeichen nach Adresse ein.
                        Run space = new Run();
                        space.Text = " ";
                        attachedTextBlock.Inlines.Add(space);
                    }
                    else
                    {
                        // Füge normales Run Element hinzu.
                        Run run = new Run();
                        run.Text = stringPart + " ";
                        attachedTextBlock.Inlines.Add(run);
                    }
                }
                else
                {
                    // Füge normales Run Element hinzu.
                    Run run = new Run();
                    run.Text = stringPart + " ";
                    attachedTextBlock.Inlines.Add(run);
                }
            }
        }

        /// <summary>
        /// Behandelt einen Klick auf den Link mit der Email-Adresse.
        /// </summary>
        /// <param name="sender">Die Eventquelle.</param>
        /// <param name="args">Eventparameter.</param>
        static async void hyperlinkEmail_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            string body = string.Empty;
            string subject = string.Empty;
            string mailAddr = string.Empty;
            // Lese inline Element mit Inhalt des Hyperlinks aus.
            if (sender != null && sender.Inlines.Count == 1)
            {
                Run addrInline = sender.Inlines[0] as Run;
                if (addrInline != null)
                {
                    mailAddr = addrInline.Text;
                }
            }

            if (mailAddr != null && mailAddr != string.Empty)
            {
                var mailto = new Uri("mailto:" + mailAddr + "?subject=" + subject + "&body=" + body);
                await Windows.System.Launcher.LaunchUriAsync(mailto);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Cannot send mail to an empty address.");
            }
        }

        /// <summary>
        /// Diese Methode prüft, ob eine Email Adresse ein gültiges Format aufweist.
        /// Die Methode wurde übernommen aus: https://msdn.microsoft.com/de-de/library/01escwtf%28v=vs.110%29.aspx
        /// </summary>
        /// <param name="strIn">Die zu prüfende Adresse als String.</param>
        /// <returns>Liefert true, wenn gültiges Email-Format, ansonsten false.</returns>
        private static bool isValidEmail(string strIn)
        {
            if (String.IsNullOrEmpty(strIn))
                return false;

            // Return true if strIn is in valid e-mail format.
            try
            {
                return Regex.IsMatch(strIn,
                      @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                      RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
