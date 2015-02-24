//  Copyright 2014 Craig Courtney
//    
//  Helios is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  Helios is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace GadrocsWorkshop.Helios.Gauges.UH_1H.DualTachometer
{
    using GadrocsWorkshop.Helios.ComponentModel;
    using System;
    using System.Windows;

    [HeliosControl("Helios.UH1H.DualTachometer", "Dual Tachometer", "UH-1H Gauges", typeof(GaugeRenderer))]
    public class DualTachometer : BaseGauge
    {
        private GaugeNeedle _needle;
        private CalibrationPointCollectionDouble _needleCalibration;
        private HeliosValue _rpm;

        public DualTachometer()
            : base("Rotor RPM", new Size(340, 340))
        {
            Point center = new Point(170, 170);

            _needleCalibration = new CalibrationPointCollectionDouble(0d, 0d, 110d, 346.5d);

            Components.Add(new GaugeImage("{Helios}/Gauges/UH-1H/DualTachometer/dual_tachometer_faceplate.xaml", new Rect(0, 0, 340, 340)));

            _needle = new GaugeNeedle("{Helios}/Gauges/UH-1H/DualTachometer/dual_tachometer_needle.xaml", center, new Size(25, 152), new Point(12.5, 139.5), 45d);
            Components.Add(_needle);

            Components.Add(new GaugeImage("{Helios}/Gauges/UH-1H/DualTachometer/dual_tachometer_bezel.xaml", new Rect(0, 0, 340, 340)));

            _rpm = new HeliosValue(this, BindingValue.Empty, "", "Rotor RPM", "Current RPM of the rotor blades", "", BindingValueUnits.RPMPercent);
            _rpm.Execute += RPM_Execute;
            Actions.Add(_rpm);
        }

        private void RPM_Execute(object action, HeliosActionEventArgs e)
        {
            _rpm.SetValue(e.Value, e.BypassCascadingTriggers);
            _needle.Rotation = _needleCalibration.Interpolate(e.Value.DoubleValue);
        }
    }
}
