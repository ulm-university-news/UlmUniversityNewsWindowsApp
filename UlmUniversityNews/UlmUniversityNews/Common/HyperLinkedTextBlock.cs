﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace UlmUniversityNews.Common
{
    /// <summary>
    /// An implementation of a TextBlock with support of highlighting hypertexts.
    /// The implementation is based on http://sonyarouje.com/2014/07/16/winrt-textblock-with-hyperlinks/
    /// </summary>
    public class HyperLinkedTextBlock
    {
        /// <summary>
        /// Definition einer DependencyProperty des Typ strings mit dem Namen "Text".
        /// </summary>
        public static readonly DependencyProperty ArticleContentProperty =
            DependencyProperty.RegisterAttached(
                "Text",
                typeof(string),
                typeof(HyperLinkedTextBlock),
                new PropertyMetadata(null, OnInlineListPropertyChanged));

        /// <summary>
        ///  Callback-Methode, die gerufen wird, wenn sich am registrierten Dependency Objekt "Text" etwas geändert hat.
        /// </summary>
        /// <param name="obj">Das betroffene DependencyObjekt, dem das DependencyProperty zugeordnet ist.</param>
        /// <param name="e">Die übergebenen Parameter.</param>
        private static void OnInlineListPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextBlock textBlock = obj as TextBlock;
            if (textBlock == null)
            {
                Debug.WriteLine("Could not extract TextBlock object. Cannot search content for hyperlinks.");
                return;
            }

            // Hole den neuen Wert des Properties.
            string text = e.NewValue as string;
            if (text == null)
                return;

            // Lösche die aktuellen Inline Textelemente des TextBlocks.
            textBlock.Inlines.Clear();

            // Prüfe, ob der neue Text mindestens einen Hyperlink enthält.
            string refText = text.ToLower();
            if (refText.Contains("http:") || refText.Contains("https:")
                || refText.Contains("www.") || refText.Contains(".de") 
                || refText.Contains(".com"))
            {
                // Splitte Text an Leerzeichen auf.
                string[] splittedText = text.Split(new string[] { " " }, StringSplitOptions.None);
                // Untersuche einzelne Textelemente auf Hyperlinks.
                // Füge die Inline Controls entsprechend hinzu.
                AddInlineControls(textBlock, splittedText);
            }
            else
            {
                // Füge den Text direkt als Run Inline Element dem TextBlock hinzu.
                Run runElement = GetRunControl(text);
                textBlock.Inlines.Add(runElement);
            }
        }

        /// <summary>
        /// Füge abhängig vom Inhalt der aufgeteilten Zeichenfolgen entsprechende Inline
        /// Elemente zum TextBlock hinzu. 
        /// </summary>
        /// <param name="textBlock">Der TextBlock, zu dem Inline Elemente hinzugefügt werden sollen.</param>
        /// <param name="splittedText">Der aufgeteilte Text.</param>
        private static void AddInlineControls(TextBlock textBlock, string[] splittedText)
        {
            // Iteriere über alle Teilstrings.
            foreach(string text in splittedText)
            {
                string refText = text.ToLower();
                if (refText.StartsWith("http:") || refText.StartsWith("https:")
                    || refText.StartsWith("www.") || refText.EndsWith(".de")
                    || refText.EndsWith(".com"))
                {
                    // Füge Hyperlink hinzu.
                    Hyperlink link = GetHyperLink(text);
                    if (link != null)
                    {
                        textBlock.Inlines.Add(link);
                    }
                    else
                    {
                        // Füge Run Element hinzu.
                        Run runElement = GetRunControl(text);
                        textBlock.Inlines.Add(runElement);
                    }
                }
                else
                {
                    // Füge Run Element hinzu.
                    Run runElement = GetRunControl(text);
                    textBlock.Inlines.Add(runElement);
                }
            }
        }

        /// <summary>
        /// Generiert eine Instanz der Klasse Run, die den übergebenen
        /// Text als Wert beinhaltet. Die Run Instanz kann als Inline
        /// Block in den TextBlock geladen werden.
        /// </summary>
        /// <param name="text">Der Text, der in die Run Instanz geladen werden soll.</param>
        /// <returns>Eine Instanz der Klasse Run.</returns>
        private static Run GetRunControl(string text)
        {
            Run run = new Run();
            run.Text = text + " ";
            return run;
        }

        /// <summary>
        /// Generiert eine Instanz der Klasse Hyperlink, die den übergebenen
        /// Text als Wert beinhaltet. Die Hyperlink Instanz kann als Inline
        /// Block in den TextBlock geladen werden.
        /// </summary>
        /// <param name="uri">Der Text, der als Hyperlink interpretiert werden soll.</param>
        /// <returns>Eine Instanz der Klasse Hyperlink, oder null, falls String nicht in
        ///     Hyperlink umgewandelt werden konnte.</returns>
        private static Hyperlink GetHyperLink(string uri)
        {
            if(!uri.ToLower().StartsWith("http://") && !uri.ToLower().StartsWith("https://"))
            {
                uri = "http://" + uri;
            }

            bool isValid = true;

            // Prüfe, ob String eine valide URL darstellt.
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                // Prüfe, ob hinten ein Zeichen angehängt ist, z.B. ein ',', welches
                // die Uri ungültig macht.
                if (Uri.IsWellFormedUriString(uri.Substring(0, uri.Length - 1), UriKind.Absolute))
                {
                    uri = uri.Substring(0, uri.Length - 1);
                }
                else
                {
                    isValid = false;
                }
            }

            // Erstelle Instanz von Hyperlink.
            Hyperlink hyper = null;
            if (isValid)
            {
                hyper = new Hyperlink();
                Uri hyperlinkUri = null;
                Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out hyperlinkUri);
                hyper.NavigateUri = hyperlinkUri;
                hyper.Inlines.Add(GetRunControl(uri));
            }

            return hyper;
        }

        public static string GetText(TextBlock element)
        {
            if (element != null)
            {
                return element.GetValue(ArticleContentProperty) as string;
            }
            return string.Empty;
        }

        public static void SetText(TextBlock element, string value)
        {
            if(element != null)
            {
                element.SetValue(ArticleContentProperty, value);
            }
        } 
    }
}
