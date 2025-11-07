using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface ILocalizationService
    {
        /// <summary>
        /// Gets the localized string for the given key.
        /// </summary>
        /// <param name="key">Localization key.</param>
        /// <returns>Localized string in the current culture.</returns>
        string this[string key] { get; }
    }
}
