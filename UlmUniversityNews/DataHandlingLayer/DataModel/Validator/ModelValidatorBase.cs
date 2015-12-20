using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel.Validator
{
    /// <summary>
    /// Die Klasse ModelValidatorBase ist eine abstrakte Klasse, die eine Basis für
    /// die Validierung von Properties gegenüber Validierungsregeln im Model darstellt.
    /// Die Klasse enthält ein Verzeichnis, welches bei der Validierung aufgetretene Fehler
    /// zusammen mit dem entsprechenden Property speichert. Der Fehler wird hierbei nicht zwangsläufig
    /// als textuelle Beschreibung gespeichert, sondern kann auch in Form eines Ressourcenschlüssels 
    /// gespeichert werden, welcher dann auf entsprechende Ressourceneinträge abgebildet werden kann.
    /// Dadurch können Fehlernachrichten in verschiedenen Sprachen erstellt und über den Schlüssel
    /// referenziert werden.
    /// </summary>
    public abstract class ModelValidatorBase
    {
        // Verzeichnis, in dem Validierungsfehler für die einzelnen Properties gespeichert werden.
        private Dictionary<string, string> validationErrorMap;

        /// <summary>
        /// Konstruktor für die Basisklasse ModelValidatorBase.
        /// </summary>
        protected ModelValidatorBase()
        {
            validationErrorMap = new Dictionary<string, string>();
        }

        /// <summary>
        /// Prüft, ob für die gegebene Klasse Validierungsfehler aufgetreten sind.
        /// </summary>
        /// <returns>Liefert true zurück, wenn es Validierungsfehler gibt, sonst false.</returns>
        public bool HasValidationErrors(){
            if(validationErrorMap.Count > 0){
                return true;
            }
            return false;
        }

        /// <summary>
        /// Prüft, ob für das gegebene Property ein Validierungsfehler aufgetreten ist.
        /// </summary>
        /// <param name="property">Die zu prüfende Property.</param>
        /// <returns>Liefert true zurück, wenn es einen Validierungsfehler gibt, sonst false.</returns>
        public bool HasValidationError(string property)
        {
            if(validationErrorMap.ContainsKey(property))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Setzt alle aufgetretenen Validierungsfehler zurück.
        /// </summary>
        public void ClearValidationErrors()
        {
            validationErrorMap.Clear();
        }

        /// <summary>
        /// Liefert die aufgetretenen Validierungsfehler in einem Verzeichnis zurück.
        /// </summary>
        /// <returns>Verzeichnis, was die Fehler auf die entsprechenden Properties abbildet.</returns>
        public Dictionary<string, string> GetValidationErrors()
        {
            return validationErrorMap;
        }

        /// <summary>
        /// Liefert den Fehlerstring zurück, der für die angegebenen Property im Verzeichnis gehalten wird.
        /// </summary>
        /// <param name="property">Die Property, für die der Validierungsfehler abgefragt werden soll.</param>
        /// <returns>Den Fehlerstring, oder null wenn zu dieser Property kein Eintrag im Verzeichnis existiert.</returns>
        public String GetValidationErrors(string property)
        {
            if(validationErrorMap.ContainsKey(property)){
                string errorString;
                bool successful = validationErrorMap.TryGetValue(property,out errorString);
                if(successful)
                {
                    return errorString;
                }
            }
            return null;
        }

        /// <summary>
        /// Fügt einen aufgetretenen Fehler dem Verzeichnis der Validierungsfehler hinzu. 
        /// </summary>
        /// <param name="property">Die Property, für die der Validierungsfehler aufgetreten ist.</param>
        /// <param name="validationError">Eine Fehlerbeschreibung, oder ein Ressourcenschlüssel auf eine Fehlerbeschreibung.</param>
        public void SetValidationError(string property, string validationError)
        {
            if(validationErrorMap.ContainsKey(property))
            {
                validationErrorMap[property] = validationError;
            }
            else
            {
                validationErrorMap.Add(property, validationError);
            }
        }

        /// <summary>
        /// Stößt die Validierung aller Properties der gegebenen Klasse an.
        /// </summary>
        abstract public void ValidateAll();

        /// <summary>
        /// Eine Hilfsmethode, die prüft, ob ein String in einem gegebenen Längenintervall liegt.
        /// </summary>
        /// <param name="minCharacters">Die Mindestanzahl an Buchstaben.</param>
        /// <param name="maxCharacters">Die Maximalanzahl an Buchstaben.</param>
        /// <param name="input">Der zu prüfende Eingabestring.</param>
        /// <returns></returns>
        protected bool checkStringRange(int minCharacters, int maxCharacters, string input)
        {
            if(input.Length < minCharacters || input.Length > maxCharacters)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Eine Hilfsmethode, die prüft, ob ein String ein bestimmtes Format aufweißt. Die Prüfung 
        /// des Eingabestrings erfolgt hierbei gegen einen regulären Ausdruck.
        /// </summary>
        /// <param name="regularExpression">Der reguläre Ausdruck, gegen den geprüft wird.</param>
        /// <param name="input">Der zu prüfende Eingabestring.</param>
        /// <returns>Liefert true, wenn der String dem gegebenen Format entspricht, ansonsten false.</returns>
        protected bool checkStringFormat(string regularExpression, string input)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(input, regularExpression);
        }
    }
}
