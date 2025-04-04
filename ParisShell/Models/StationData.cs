using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Models {
    /// <summary>
    /// Represents data for a station, including its name and geographic coordinates.
    /// </summary>
    public class StationData {
        /// <summary>
        /// Gets or sets the name of the station.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the longitude of the station's location.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the latitude of the station's location.
        /// </summary>
        public double Latitude { get; set; }
    }
}
